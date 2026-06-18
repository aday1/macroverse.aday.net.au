package main

import (
	"archive/zip"
	"bufio"
	"bytes"
	"context"
	"crypto/sha256"
	"encoding/base64"
	"encoding/hex"
	"encoding/json"
	"flag"
	"fmt"
	"io"
	"log"
	"math"
	"net"
	"net/http"
	"net/url"
	"os"
	"os/exec"
	"path/filepath"
	"regexp"
	"runtime"
	"sort"
	"strconv"
	"strings"
	"sync"
	"sync/atomic"
	"time"
)

func init() {
	initConsole()
	log.SetOutput(os.Stdout)
}

// bannedShaderBasenames — removed from index, templates, and scans (do not ship).
var bannedShaderBasenames = map[string]struct{}{
	"core-text-template.fs": {},
	"core-text-template":    {},
}

func isBannedShader(pathOrName string) bool {
	if pathOrName == "" {
		return false
	}
	base := filepath.Base(strings.ReplaceAll(pathOrName, "|", string(filepath.Separator)))
	if _, ok := bannedShaderBasenames[base]; ok {
		return true
	}
	stem := strings.TrimSuffix(base, filepath.Ext(base))
	_, ok := bannedShaderBasenames[stem]
	return ok
}

func purgeBannedShadersFromDisk() int {
	removed := 0
	seen := make(map[string]struct{})
	tryRoots := append([]string{}, getSourcePaths()...)
	if wd, err := os.Getwd(); err == nil {
		tryRoots = append(tryRoots, wd, filepath.Join(wd, "shaders"))
	}
	tryRoots = append(tryRoots, filepath.Join(exeDir(), shadersBaseDir))
	for _, root := range tryRoots {
		if root == "" {
			continue
		}
		_ = filepath.Walk(root, func(path string, info os.FileInfo, err error) error {
			if err != nil || info.IsDir() {
				if info != nil && info.IsDir() {
					base := filepath.Base(path)
					if base == ".git" || base == "node_modules" {
						return filepath.SkipDir
					}
				}
				return nil
			}
			if !isBannedShader(path) {
				return nil
			}
			if _, ok := seen[path]; ok {
				return nil
			}
			seen[path] = struct{}{}
			if rmErr := os.Remove(path); rmErr == nil {
				removed++
				logSection("BAN", "deleted banned shader file: "+path)
			} else {
				logSection("BAN", "failed to delete banned shader "+path+": "+rmErr.Error())
			}
			return nil
		})
	}
	return removed
}

func frontendDir() string {
	if exe, err := os.Executable(); err == nil {
		d := filepath.Dir(exe)
		dist := filepath.Join(d, "frontend-build")
		if _, err := os.Stat(dist); err == nil {
			return dist
		}
		viteDist := filepath.Join(d, "frontend", "dist")
		if _, err := os.Stat(viteDist); err == nil {
			return viteDist
		}
		f := filepath.Join(d, "frontend")
		if _, err := os.Stat(f); err == nil {
			return f
		}
	}
	return "frontend"
}

func exeDir() string {
	if exe, err := os.Executable(); err == nil {
		return filepath.Dir(exe)
	}
	if wd, err := os.Getwd(); err == nil {
		return wd
	}
	return "."
}

const defaultPort = "8765"

const defaultVFXRootFallback = `D:\AV-Library-Syncthing\VFX - GLSL`

const shadersBaseDir = "shaders"

func getDefaultVFXRoot() string {
	d := filepath.Join(exeDir(), shadersBaseDir)
	if info, err := os.Stat(d); err == nil && info.IsDir() {
		return d
	}
	return defaultVFXRootFallback
}

var buildVersion = "42.2"
var buildDate = ""
var releaseTag = "Version 42.2 — WebXR VR VJ + live remote extensions"

func thumbnailsPath() string {
	return filepath.Join(exeDir(), "thumbnails.json")
}

var thumbnailsMu sync.Mutex
var thumbnailsCache = make(map[string]string)

func loadThumbnailsCache() {
	thumbnailsCache = make(map[string]string)
	data, err := os.ReadFile(thumbnailsPath())
	if err != nil {
		return
	}
	if json.Unmarshal(data, &thumbnailsCache) != nil {
		thumbnailsCache = make(map[string]string)
	}
}

func saveThumbnailsCache() error {
	data, err := json.MarshalIndent(thumbnailsCache, "", "  ")
	if err != nil {
		return err
	}
	return os.WriteFile(thumbnailsPath(), data, 0644)
}

var outputProc struct {
	sync.Mutex
	cmd        *exec.Cmd
	spout      bool
	ndi        bool
	vhs        bool
	testsignal string
}

var agentProc struct {
	sync.Mutex
	running bool
}

var agentOutputBuf struct {
	mu    sync.Mutex
	lines []string
}

var agentCooldownMu sync.Mutex
var lastAgentCall time.Time

const agentCooldownSec = 15

func agentInCooldown() bool {
	agentCooldownMu.Lock()
	defer agentCooldownMu.Unlock()
	return time.Since(lastAgentCall) < agentCooldownSec*time.Second
}

func agentCooldownRemainingSec() int {
	agentCooldownMu.Lock()
	defer agentCooldownMu.Unlock()
	elapsed := time.Since(lastAgentCall)
	if elapsed >= agentCooldownSec*time.Second {
		return 0
	}
	return agentCooldownSec - int(elapsed.Seconds())
}

func agentCooldownSet() {
	agentCooldownMu.Lock()
	lastAgentCall = time.Now()
	agentCooldownMu.Unlock()
}

const agentOutputMax = 300

func agentOutputAppend(line string) {
	agentOutputBuf.mu.Lock()
	agentOutputBuf.lines = append(agentOutputBuf.lines, line)
	if len(agentOutputBuf.lines) > agentOutputMax {
		agentOutputBuf.lines = agentOutputBuf.lines[len(agentOutputBuf.lines)-agentOutputMax:]
	}
	agentOutputBuf.mu.Unlock()
}

type agentOutputWriter struct {
	rem string
}

func (w *agentOutputWriter) Write(p []byte) (n int, err error) {
	s := w.rem + string(p)
	w.rem = ""
	idx := strings.LastIndex(s, "\n")
	if idx >= 0 {
		for _, line := range strings.Split(s[:idx], "\n") {
			if t := strings.TrimSpace(line); t != "" {
				agentOutputAppend(t)
			}
		}
		w.rem = s[idx+1:]
	} else {
		w.rem = s
	}
	return len(p), nil
}

// readonlyEnv blocks writes when READONLY=true. Cloud host mode also blocks filesystem writes.
var readonlyEnv = os.Getenv("READONLY") == "true"

func isReadonlyHost() bool {
	if readonlyEnv {
		return true
	}
	return hostMode() == "cloud"
}

// noExternalLLM blocks all outbound LLM calls (Ollama, Cursor) when set.
// Set via DISABLE_EXTERNAL_LLM=true. Enforced at the provider level — cannot
// be overridden via the settings API.
var noExternalLLM = os.Getenv("DISABLE_EXTERNAL_LLM") == "true"

// hostMode returns "cloud" or "desktop". Cloud hides local shell integrations
// (Explorer, Notepad, Cursor IDE, cursor-agent) in the web UI.
// Override with MACROVERSE_HOST_MODE=cloud|desktop. Default: windows=desktop, else cloud.
func hostMode() string {
	if m := strings.ToLower(strings.TrimSpace(os.Getenv("MACROVERSE_HOST_MODE"))); m == "cloud" || m == "desktop" {
		return m
	}
	if runtime.GOOS == "windows" {
		return "desktop"
	}
	return "cloud"
}

func hostCapabilities() map[string]bool {
	desktop := hostMode() == "desktop"
	_, _, agentErr := findAgentExe()
	return map[string]bool{
		"desktopShell":  desktop,
		"cursorAgent":   desktop && agentErr == nil,
		"localAgents":   desktop,
		"wirePipeline":  desktop,
		"videoOutput":   desktop,
	}
}

// writeBlocked returns true and writes a 403 JSON error if the host is read-only.
func writeBlocked(w http.ResponseWriter) bool {
	if !isReadonlyHost() {
		return false
	}
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusForbidden)
	json.NewEncoder(w).Encode(map[string]string{
		"error": "This is a read-only demo instance. Writes are disabled.",
	})
	return true
}

// cloudSafeMutatingAPI is true for /api paths that may use POST (etc.) on cloud hosts
// without touching the server shader library — VJ session, thumbnail batch fetch, etc.
func cloudSafeMutatingAPI(path string) bool {
	switch path {
	case "/api/vj/tokens",
		"/api/vj/session-config",
		"/api/vj-output/audience-mouse",
		"/api/vj-output/state",
		"/api/thumbnails",
		"/api/thumbnail":
		return true
	default:
		return false
	}
}

var oscState struct {
	sync.Mutex
	conn    *net.UDPConn
	port    int
	running bool
	clients map[chan string]bool
}

func oscInit() {
	oscState.clients = make(map[chan string]bool)
}

func oscBroadcast(msg string) {
	oscState.Lock()
	defer oscState.Unlock()
	for ch := range oscState.clients {
		select {
		case ch <- msg:
		default:
		}
	}
}

func oscStart(port int) error {
	oscState.Lock()
	if oscState.running {
		oscState.Unlock()
		oscStop()
		oscState.Lock()
	}
	addr, err := net.ResolveUDPAddr("udp", ":"+strconv.Itoa(port))
	if err != nil {
		oscState.Unlock()
		return err
	}
	conn, err := net.ListenUDP("udp", addr)
	if err != nil {
		oscState.Unlock()
		return err
	}
	oscState.conn = conn
	oscState.port = port
	oscState.running = true
	oscState.Unlock()

	go func() {
		buf := make([]byte, 4096)
		for {
			n, _, err := conn.ReadFromUDP(buf)
			if err != nil {
				return
			}
			if n < 8 {
				continue
			}
			address, typeTag, args := parseOSCMessage(buf[:n])
			if address == "" || len(args) == 0 {
				continue
			}
			_ = typeTag
			val := args[0]
			logSection("OSC", fmt.Sprintf("recv %s = %.4f", address, val))
			msg := fmt.Sprintf(`{"address":%q,"value":%v}`, address, val)
			if address == "/shader/select" || address == "/shader/index" || address == "/shader/switch" {
				logSection("OSC", fmt.Sprintf("shader select -> index %d", int(val)))
				msg = fmt.Sprintf(`{"address":%q,"value":%v,"type":"select"}`, address, val)
			}
			oscBroadcast(msg)
		}
	}()
	log.Printf("OSC listening on UDP :%d", port)
	return nil
}

func oscStop() {
	oscState.Lock()
	defer oscState.Unlock()
	if oscState.conn != nil {
		oscState.conn.Close()
		oscState.conn = nil
	}
	oscState.running = false
}

func parseOSCMessage(data []byte) (address string, typeTag string, args []float64) {
	if len(data) < 4 || data[0] != '/' {
		return
	}
	i := 0
	for i < len(data) && data[i] != 0 {
		i++
	}
	address = string(data[:i])
	i = oscPad4(i + 1)
	if i >= len(data) {
		return
	}
	if data[i] != ',' {
		return
	}
	tagStart := i + 1
	for i < len(data) && data[i] != 0 {
		i++
	}
	typeTag = string(data[tagStart:i])
	i = oscPad4(i + 1)
	for _, t := range typeTag {
		if i+4 > len(data) {
			break
		}
		switch t {
		case 'f':
			bits := uint32(data[i])<<24 | uint32(data[i+1])<<16 | uint32(data[i+2])<<8 | uint32(data[i+3])
			v := float64(math.Float32frombits(bits))
			args = append(args, v)
			i += 4
		case 'i':
			v := int32(data[i])<<24 | int32(data[i+1])<<16 | int32(data[i+2])<<8 | int32(data[i+3])
			args = append(args, float64(v))
			i += 4
		default:
			i += 4
		}
	}
	return
}

func oscPad4(n int) int {
	return (n + 3) &^ 3
}

func lookupCursorExe() (string, error) {
	if exe, err := exec.LookPath("cursor"); err == nil {
		return exe, nil
	}
	if runtime.GOOS == "windows" {
		if exe, err := exec.LookPath("cursor.cmd"); err == nil {
			return exe, nil
		}
	}
	return "", exec.ErrNotFound
}

func findAgentExe() (string, []string, error) {
	var exe string
	var prefix []string
	if e, err := exec.LookPath("cursor-agent"); err == nil {
		exe = e
	} else if e, err := exec.LookPath("agent"); err == nil {
		exe = e
	} else if e, err := lookupCursorExe(); err == nil {
		exe = e
		prefix = []string{"--agent"}
	} else {
		local := filepath.Join(filepath.Dir(os.Args[0]), "cursor-agent.exe")
		if _, err := os.Stat(local); err == nil {
			exe = local
		} else {
			home, _ := os.UserHomeDir()
			if home != "" {
				localAppData := filepath.Join(home, "AppData", "Local", "cursor-agent")
				for _, name := range []string{"cursor-agent.exe", "cursor-agent.cmd", "cursor-agent.bat"} {
					p := filepath.Join(localAppData, name)
					if _, err := os.Stat(p); err == nil {
						exe = p
						break
					}
				}
			}
		}
	}
	if exe == "" {
		return "", nil, exec.ErrNotFound
	}
	return exe, prefix, nil
}

func buildAgentCmd(args ...string) (*exec.Cmd, error) {
	exe, prefix, err := findAgentExe()
	if err != nil {
		return nil, err
	}
	trust := []string{"--trust"}
	allArgs := append(append(prefix, trust...), args...)
	return cmdForExe(exe, allArgs...), nil
}

func buildAgentInteractiveCmd() (*exec.Cmd, error) {
	exe, prefix, err := findAgentExe()
	if err != nil {
		return nil, err
	}
	trust := []string{"--trust"}
	allArgs := append(prefix, trust...)
	exe = sanitizeCmdArg(exe)
	lower := strings.ToLower(exe)
	isBatch := strings.HasSuffix(lower, ".cmd") || strings.HasSuffix(lower, ".bat")
	if isBatch {
		dir := filepath.Dir(exe)
		base := strings.TrimSuffix(filepath.Base(exe), filepath.Ext(exe))
		exeInDir := filepath.Join(dir, base+".exe")
		if _, err := os.Stat(exeInDir); err == nil {
			return exec.Command(exeInDir, allArgs...), nil
		}
		all := make([]string, 0, 2+len(allArgs))
		all = append(all, "/c", exe)
		for _, a := range allArgs {
			all = append(all, sanitizeCmdArg(a))
		}
		return exec.Command("cmd", all...), nil
	}
	for i := range allArgs {
		allArgs[i] = sanitizeCmdArg(allArgs[i])
	}
	return exec.Command(exe, allArgs...), nil
}

func buildAgentPrintCmd() (*exec.Cmd, error) {
	exe, prefix, err := findAgentExe()
	if err != nil {
		return nil, err
	}
	exe = sanitizeCmdArg(exe)
	trust := []string{"--trust", "--print"}
	allArgs := append(prefix, trust...)
	lower := strings.ToLower(exe)
	isBatch := strings.HasSuffix(lower, ".cmd") || strings.HasSuffix(lower, ".bat")
	if isBatch {
		dir := filepath.Dir(exe)
		base := strings.TrimSuffix(filepath.Base(exe), filepath.Ext(exe))
		exeInDir := filepath.Join(dir, base+".exe")
		if _, err := os.Stat(exeInDir); err == nil {
			return exec.Command(exeInDir, allArgs...), nil
		}
		all := make([]string, 0, 2+len(allArgs))
		all = append(all, "/c", exe)
		for _, a := range allArgs {
			all = append(all, sanitizeCmdArg(a))
		}
		return exec.Command("cmd", all...), nil
	}
	for i := range allArgs {
		allArgs[i] = sanitizeCmdArg(allArgs[i])
	}
	return exec.Command(exe, allArgs...), nil
}

// localSuggestParams parses shader source with regex to find exposable params and
// interesting numeric literals. Works without cursor-agent -- instant results.
func localSuggestParams(content string) (params []string, literals []map[string]interface{}) {
	skipSet := map[string]bool{
		"time": true, "mouse": true, "resolution": true,
		"TIME": true, "RENDERSIZE": true, "FRAMEINDEX": true, "PASSINDEX": true,
		"mouseX": true, "mouseY": true, "timeScale": true,
	}
	lines := strings.Split(content, "\n")
	uniformRe := regexp.MustCompile(`^\s*uniform\s+(float|int|bool|vec[234])\s+(\w+)\s*;`)
	exposeRe := regexp.MustCompile(`//\s*@expose`)
	literalRe := regexp.MustCompile(`(?:^|[^a-zA-Z_])(\d+\.\d+f?|\d+\.f?|\.\d+f?)(?:[^a-zA-Z_]|$)`)
	intLiteralRe := regexp.MustCompile(`(?:^|[^a-zA-Z_.\d])(\d{2,})(?:[^a-zA-Z_.\d]|$)`)
	mainStarted := false
	seenParams := map[string]bool{}
	seenLiterals := map[float64]bool{}

	for i, line := range lines {
		trimmed := strings.TrimSpace(line)
		if strings.HasPrefix(trimmed, "void main") {
			mainStarted = true
		}

		m := uniformRe.FindStringSubmatch(line)
		if m != nil && !skipSet[m[2]] && !exposeRe.MatchString(line) {
			name := m[2]
			if !seenParams[name] {
				seenParams[name] = true
				params = append(params, name)
			}
		}

		if !mainStarted {
			continue
		}
		if strings.HasPrefix(trimmed, "//") || strings.HasPrefix(trimmed, "/*") || strings.HasPrefix(trimmed, "uniform ") {
			continue
		}

		for _, fm := range literalRe.FindAllStringSubmatch(line, -1) {
			val, err := strconv.ParseFloat(strings.TrimSuffix(fm[1], "f"), 64)
			if err != nil || val == 0.0 || val == 1.0 || val == -1.0 {
				continue
			}
			if !seenLiterals[val] {
				seenLiterals[val] = true
				literals = append(literals, map[string]interface{}{"value": val, "line": i + 1})
			}
		}
		for _, im := range intLiteralRe.FindAllStringSubmatch(line, -1) {
			val, err := strconv.ParseFloat(im[1], 64)
			if err != nil || val == 0 || val == 1 {
				continue
			}
			if !seenLiterals[val] {
				seenLiterals[val] = true
				literals = append(literals, map[string]interface{}{"value": val, "line": i + 1})
			}
		}
	}
	if len(literals) > 30 {
		literals = literals[:30]
	}
	return
}

// stripLeadingGarbageShader removes leading lines that look like DOM/HTML/CSS junk
// (e.g. "fullscreengalleryhide" from browser extensions) accidentally prepended to shader source.
func stripLeadingGarbageShader(src string) string {
	if src == "" {
		return src
	}
	lines := strings.Split(src, "\n")
	i := 0
	validStart := regexp.MustCompile(`(?i)^(#|precision\s|//|/\*|uniform\s|varying\s|attribute\s|void\s|const\s|layout\s|in\s|out\s|flat\s|smooth\s|float\s|vec[234]\s|mat[234]\s|int\s|bool\s|sampler2D\s|if\s|for\s|while\s|return\s|discard\s|struct\s)`)
	singleWord := regexp.MustCompile(`^[a-zA-Z_][a-zA-Z0-9_]*$`)
	for i < len(lines) {
		t := strings.TrimSpace(lines[i])
		if t == "" {
			i++
			continue
		}
		if validStart.MatchString(t) {
			break
		}
		if singleWord.MatchString(t) && len(t) > 10 {
			i++
			continue
		}
		break
	}
	if i == 0 {
		return src
	}
	return strings.Join(lines[i:], "\n")
}

// sanitizeCmdArg removes null bytes and other runes that break Windows CreateProcess.
func sanitizeCmdArg(s string) string {
	return strings.ReplaceAll(s, "\x00", "")
}

func cmdForExe(exe string, args ...string) *exec.Cmd {
	exe = sanitizeCmdArg(exe)
	if runtime.GOOS == "windows" {
		lower := strings.ToLower(exe)
		if strings.HasSuffix(lower, ".cmd") || strings.HasSuffix(lower, ".bat") {
			all := make([]string, 0, 2+len(args))
			all = append(all, "/c", exe)
			for _, a := range args {
				all = append(all, sanitizeCmdArg(a))
			}
			return exec.Command("cmd.exe", all...)
		}
	}
	for i := range args {
		args[i] = sanitizeCmdArg(args[i])
	}
	return exec.Command(exe, args...)
}

type LLMProvider struct {
	Name     string `json:"name"`
	Enabled  bool   `json:"enabled"`
	Priority int    `json:"priority"`
	Model    string `json:"model,omitempty"`
	Endpoint string `json:"endpoint,omitempty"`
}

type LLMConfig struct {
	Providers []LLMProvider `json:"providers"`
}

type AppSettings struct {
	PreviewWidth        int           `json:"previewWidth"`
	PreviewHeight       int           `json:"previewHeight"`
	TargetFps           int           `json:"targetFps"`
	EnablePipeline      bool          `json:"enablePipeline"`
	EnableOutput        bool          `json:"enableOutput"`
	EnableGit           bool          `json:"enableGit"`
	ShowThumbnails      bool          `json:"showThumbnails"`
	ListViewMode        string        `json:"listViewMode"`
	SkipSplash          bool          `json:"skipSplash"`
	PreviewAspect       string        `json:"previewAspect,omitempty"`
	AutoOptimizeQuality bool          `json:"autoOptimizeQuality"`
	CursorApiKey        string        `json:"cursorApiKey,omitempty"`
	VfxRoot             string        `json:"vfxRoot,omitempty"`
	SourcePaths         []string      `json:"sourcePaths,omitempty"`
	IndexPath           string        `json:"indexPath,omitempty"`
	OutputPath          string        `json:"outputPath,omitempty"`
	WatchFolders        bool          `json:"watchFolders"`
	WirePath            string        `json:"wirePath,omitempty"`
	DefaultView         string        `json:"defaultView,omitempty"`
	DefaultParamValue   float64       `json:"defaultParamValue,omitempty"`
	DefaultTimeScale    float64       `json:"defaultTimeScale,omitempty"`
	LLMProviders        []LLMProvider `json:"llmProviders,omitempty"`
	GitHubToken         string        `json:"githubToken,omitempty"`
	HardResetPath       string        `json:"hardResetPath,omitempty"` // subfolder within first source path; defaults to "custom"
}

// getHardResetPath returns the configured Hard Reset target path,
// defaulting to <first source path>/custom if not set.
func (s AppSettings) getHardResetPath() string {
	if s.HardResetPath != "" {
		return s.HardResetPath
	}
	return filepath.Join(getVfxRoot(), "custom")
}

type ShaderError struct {
	ID           string `json:"id"`
	Path         string `json:"path"`
	Filename     string `json:"filename"`
	Error        string `json:"error"`
	ErrorHash    string `json:"errorHash"`
	Status       string `json:"status"` // "open", "fixed", "unrecoverable"
	FixMethod    string `json:"fixMethod,omitempty"`
	TriedSummary string `json:"triedSummary,omitempty"`
	Created      string `json:"created"`
	Resolved     string `json:"resolved,omitempty"`
	Attempts     int    `json:"attempts"`
}

type unrecoverableRecord struct {
	Path         string `json:"path"`
	Filename     string `json:"filename"`
	CompileError string `json:"compileError"`
	TriedSummary string `json:"triedSummary"`
	Timestamp    string `json:"timestamp"`
}

var errorLog struct {
	sync.Mutex
	entries []ShaderError
	path    string
}

func errorLogPath() string {
	idx := getIndexPath()
	if idx != "" {
		return filepath.Join(filepath.Dir(idx), "shader-errors.json")
	}
	return "shader-errors.json"
}

func unrecoverableShadersPath() string {
	idx := getIndexPath()
	if idx != "" {
		return filepath.Join(filepath.Dir(idx), "unrecoverable-shaders.json")
	}
	return "unrecoverable-shaders.json"
}

const maxUnrecoverableRecords = 500

func appendUnrecoverableShader(path, filename, compileErr, triedSummary string) {
	compileShort := compileErr
	if len(compileShort) > 200 {
		compileShort = compileShort[:200] + "..."
	}
	rec := unrecoverableRecord{
		Path:         path,
		Filename:     filename,
		CompileError: compileShort,
		TriedSummary: triedSummary,
		Timestamp:    time.Now().Format(time.RFC3339),
	}
	fpath := unrecoverableShadersPath()
	data, err := os.ReadFile(fpath)
	var list []unrecoverableRecord
	if err == nil {
		_ = json.Unmarshal(data, &list)
	}
	if list == nil {
		list = []unrecoverableRecord{}
	}
	list = append(list, rec)
	if len(list) > maxUnrecoverableRecords {
		list = list[len(list)-maxUnrecoverableRecords:]
	}
	out, _ := json.MarshalIndent(list, "", "  ")
	os.WriteFile(fpath, out, 0644)
}

func debugFixErrorLogPath() string {
	idx := getIndexPath()
	if idx != "" {
		return filepath.Join(filepath.Dir(idx), "debug-fix-errors.log")
	}
	return "debug-fix-errors.log"
}

func appendDebugFixError(path, filename, compileErr, reason, verbose, triedSummary string) {
	logPath := debugFixErrorLogPath()
	f, err := os.OpenFile(logPath, os.O_APPEND|os.O_CREATE|os.O_WRONLY, 0644)
	if err != nil {
		logSection("DEBUG", "could not open debug-fix-errors.log: "+err.Error())
		return
	}
	defer f.Close()
	ts := time.Now().Format(time.RFC3339)
	line := fmt.Sprintf(
		"[%s] UNRECOVERABLE\n  file:    %s (%s)\n  error:   %s\n  reason:  %s\n  detail:  %s\n  tried:   %s\n---\n",
		ts, path, filename, compileErr, reason, verbose, triedSummary,
	)
	f.WriteString(line)
	logSection("DEBUG", "logged unrecoverable to "+logPath)
}

func loadErrorLog() {
	errorLog.Lock()
	defer errorLog.Unlock()
	errorLog.path = errorLogPath()
	data, err := os.ReadFile(errorLog.path)
	if err != nil {
		errorLog.entries = []ShaderError{}
		return
	}
	var entries []ShaderError
	if json.Unmarshal(data, &entries) == nil {
		errorLog.entries = entries
	}
}

func saveErrorLog() {
	data, err := json.MarshalIndent(errorLog.entries, "", "  ")
	if err != nil {
		return
	}
	os.WriteFile(errorLog.path, data, 0644)
}

func errorHashStr(path, errMsg string) string {
	errCore := regexp.MustCompile(`\d+:\d+:\s*`).ReplaceAllString(errMsg, "")
	h := sha256.Sum256([]byte(path + "|" + errCore))
	return hex.EncodeToString(h[:8])
}

func reportShaderError(path, filename, errMsg string) *ShaderError {
	errorLog.Lock()
	defer errorLog.Unlock()
	hash := errorHashStr(path, errMsg)
	for i := range errorLog.entries {
		if errorLog.entries[i].ErrorHash == hash && errorLog.entries[i].Status == "open" {
			errorLog.entries[i].Attempts++
			return &errorLog.entries[i]
		}
	}
	entry := ShaderError{
		ID:        fmt.Sprintf("ERR-%s", hash[:8]),
		Path:      path,
		Filename:  filename,
		Error:     errMsg,
		ErrorHash: hash,
		Status:    "open",
		Created:   time.Now().Format(time.RFC3339),
		Attempts:  1,
	}
	errorLog.entries = append(errorLog.entries, entry)
	saveErrorLog()
	logSection("ERRORS", "new issue "+entry.ID+": "+filename+" - "+errMsg[:min(len(errMsg), 80)])
	return &entry
}

func resolveShaderError(path, errMsg, method string) {
	errorLog.Lock()
	defer errorLog.Unlock()
	hash := errorHashStr(path, errMsg)
	for i := range errorLog.entries {
		if errorLog.entries[i].ErrorHash == hash && errorLog.entries[i].Status == "open" {
			errorLog.entries[i].Status = "fixed"
			errorLog.entries[i].FixMethod = method
			errorLog.entries[i].Resolved = time.Now().Format(time.RFC3339)
			logSection("ERRORS", "resolved "+errorLog.entries[i].ID+": "+method)
			break
		}
	}
	saveErrorLog()
}

func markErrorUnrecoverable(path, errMsg, triedSummary string) {
	errorLog.Lock()
	defer errorLog.Unlock()
	hash := errorHashStr(path, errMsg)
	for i := range errorLog.entries {
		if errorLog.entries[i].ErrorHash == hash && errorLog.entries[i].Status == "open" {
			errorLog.entries[i].Status = "unrecoverable"
			errorLog.entries[i].TriedSummary = triedSummary
			logSection("ERRORS", "marked unrecoverable "+errorLog.entries[i].ID)
			break
		}
	}
	saveErrorLog()
}

func ensureErrorEntryThenMarkUnrecoverable(path, filename, errMsg, triedSummary string) {
	hash := errorHashStr(path, errMsg)
	errorLog.Lock()
	for i := range errorLog.entries {
		if errorLog.entries[i].ErrorHash == hash {
			errorLog.entries[i].Status = "unrecoverable"
			errorLog.entries[i].TriedSummary = triedSummary
			logSection("ERRORS", "marked unrecoverable "+errorLog.entries[i].ID)
			saveErrorLog()
			errorLog.Unlock()
			return
		}
	}
	errorLog.Unlock()
	reportShaderError(path, filename, errMsg)
	markErrorUnrecoverable(path, errMsg, triedSummary)
}

// findGitRoot finds the git repository root for a given file or directory path.
// Returns empty string if not in a git repo.
func findGitRoot(pathOrDir string) string {
	dir := pathOrDir
	if info, err := os.Stat(pathOrDir); err == nil && !info.IsDir() {
		dir = filepath.Dir(pathOrDir)
	}
	cmd := exec.Command("git", "-C", dir, "rev-parse", "--show-toplevel")
	out, err := cmd.Output()
	if err != nil {
		return ""
	}
	return strings.TrimSpace(string(out))
}

func gitCommitSave(dir, filePath string) {
	if _, err := exec.LookPath("git"); err != nil {
		return
	}
	// Find proper git root instead of assuming dir is the repo root
	root := findGitRoot(filePath)
	if root == "" {
		root = dir
	}
	rel, _ := filepath.Rel(root, filePath)
	if rel == "" {
		rel = filePath
	}
	rel = filepath.ToSlash(rel)
	addCmd := exec.Command("git", "add", rel)
	addCmd.Dir = root
	if err := addCmd.Run(); err != nil {
		logSection("GIT", "add failed: "+err.Error())
		return
	}
	msg := fmt.Sprintf("[macroverse] save %s v%s", filepath.Base(filePath), time.Now().Format("20060102-150405"))
	msg = strings.ReplaceAll(msg, "\r", " ")
	msg = strings.ReplaceAll(msg, "\n", " ")
	commitCmd := exec.Command("git", "commit", "-m", msg, "--", rel)
	commitCmd.Dir = root
	out, err := commitCmd.CombinedOutput()
	if err != nil {
		outStr := strings.TrimSpace(string(out))
		if !strings.Contains(outStr, "nothing to commit") {
			logSection("GIT", "commit failed: "+err.Error()+" "+outStr)
		}
		return
	}
	logSection("GIT", "committed save for "+filepath.Base(filePath))
}

func gitCommitFix(dir, filePath, errMsg, method string) {
	if _, err := exec.LookPath("git"); err != nil {
		return
	}
	root := findGitRoot(filePath)
	if root == "" {
		root = dir
	}
	rel, _ := filepath.Rel(root, filePath)
	if rel == "" {
		rel = filepath.Base(filePath)
	}
	rel = filepath.ToSlash(rel)
	rel = strings.ReplaceAll(rel, "\x00", "")
	addCmd := exec.Command("git", "add", rel)
	addCmd.Dir = root
	if out, err := addCmd.CombinedOutput(); err != nil {
		logSection("GIT", "add failed: "+err.Error()+" "+strings.TrimSpace(string(out)))
		return
	}
	errShort := errMsg
	if len(errShort) > 120 {
		errShort = errShort[:120] + "..."
	}
	msg := fmt.Sprintf("[macroverse] fix %s via %s - Error: %s", filepath.Base(filePath), method, errShort)
	msg = strings.ReplaceAll(msg, "\r", " ")
	msg = strings.ReplaceAll(msg, "\n", " ")
	msg = strings.ReplaceAll(msg, "\"", "'")
	msg = strings.ReplaceAll(msg, "\x00", "")
	tmpMsg, tmpErr := os.CreateTemp("", "macroverse-commit-*.txt")
	var commitCmd *exec.Cmd
	if tmpErr == nil {
		tmpMsg.WriteString(msg)
		tmpMsg.Close()
		defer os.Remove(tmpMsg.Name())
		commitCmd = exec.Command("git", "commit", "-F", tmpMsg.Name(), "--", rel)
	} else {
		commitCmd = exec.Command("git", "commit", "-m", msg, "--", rel)
	}
	commitCmd.Dir = root
	out, err := commitCmd.CombinedOutput()
	if err != nil {
		outStr := strings.TrimSpace(string(out))
		if !strings.Contains(outStr, "nothing to commit") {
			logSection("GIT", "commit failed: "+err.Error()+" "+outStr)
		}
		return
	}
	logSection("GIT", "committed fix for "+filepath.Base(filePath)+" ("+method+")")
}

var pathConfig struct {
	sync.RWMutex
	indexPath   string
	sourcePaths []string
}

var folderWatcher struct {
	sync.Mutex
	running    bool
	stopCh     chan struct{}
	knownFiles map[string]time.Time
	newCount   int64
}

var shaderExts = map[string]bool{
	".glsl": true, ".frag": true, ".fs": true, ".isf": true,
}

func startFolderWatcher() {
	folderWatcher.Lock()
	if folderWatcher.running {
		folderWatcher.Unlock()
		return
	}
	folderWatcher.stopCh = make(chan struct{})
	folderWatcher.running = true
	if folderWatcher.knownFiles == nil {
		folderWatcher.knownFiles = make(map[string]time.Time)
	}
	folderWatcher.Unlock()

	logSection("WATCH", "folder watcher started")

	go func() {
		snap := scanAllFiles()
		folderWatcher.Lock()
		folderWatcher.knownFiles = snap
		folderWatcher.Unlock()
		logSection("WATCH", fmt.Sprintf("baseline: %d files tracked", len(snap)))

		ticker := time.NewTicker(30 * time.Second)
		defer ticker.Stop()
		for {
			select {
			case <-folderWatcher.stopCh:
				logSection("WATCH", "folder watcher stopped")
				return
			case <-ticker.C:
				current := scanAllFiles()
				folderWatcher.Lock()
				known := folderWatcher.knownFiles
				var newFiles []string
				for path, mod := range current {
					prev, exists := known[path]
					if !exists || mod.After(prev) {
						newFiles = append(newFiles, path)
					}
				}
				folderWatcher.knownFiles = current
				folderWatcher.newCount += int64(len(newFiles))
				folderWatcher.Unlock()

				// Detect deletions: files in knownFiles that disappeared from current
				deletedCount := 0
				for path := range known {
					if _, exists := current[path]; !exists {
						deletedCount++
					}
				}
				if len(newFiles) > 0 || deletedCount > 0 {
					logSection("WATCH", fmt.Sprintf("%d new/modified, %d deleted shader(s) detected, triggering incremental index", len(newFiles), deletedCount))
					for _, f := range newFiles {
						if len(f) > 80 {
							f = "..." + f[len(f)-77:]
						}
						logSection("WATCH", "  + "+f)
					}
					triggerIncrementalIndex()
				}
			}
		}
	}()
}

func stopFolderWatcher() {
	folderWatcher.Lock()
	defer folderWatcher.Unlock()
	if !folderWatcher.running {
		return
	}
	close(folderWatcher.stopCh)
	folderWatcher.running = false
	logSection("WATCH", "folder watcher stopped")
}

func isWatcherRunning() bool {
	folderWatcher.Lock()
	defer folderWatcher.Unlock()
	return folderWatcher.running
}

func scanAllFiles() map[string]time.Time {
	paths := getSourcePaths()
	files := make(map[string]time.Time)
	for _, root := range paths {
		filepath.Walk(root, func(path string, info os.FileInfo, err error) error {
			if err != nil {
				return nil
			}
			if info.IsDir() {
				base := filepath.Base(path)
				if base == ".git" || base == "node_modules" || base == ".svn" {
					return filepath.SkipDir
				}
				return nil
			}
			ext := strings.ToLower(filepath.Ext(path))
			if shaderExts[ext] && !isBannedShader(path) {
				files[path] = info.ModTime()
			}
			return nil
		})
	}
	return files
}

var indexRunning int32

func triggerIncrementalIndex() {
	if !atomic.CompareAndSwapInt32(&indexRunning, 0, 1) {
		logSection("WATCH", "index already running, skipping")
		return
	}
	go func() {
		defer atomic.StoreInt32(&indexRunning, 0)
		files := scanAllFiles()
		existing, _ := readIndex()
		if existing == nil {
			existing = []ShaderEntry{}
		}

		known := make(map[string]bool)
		maxID := 0
		for _, e := range existing {
			known[e.Path] = true
			if e.ID > maxID {
				maxID = e.ID
			}
		}

		added := 0
		for path := range files {
			if known[path] {
				continue
			}
			maxID++
			ext := strings.ToLower(filepath.Ext(path))
			format := "glsl"
			if ext == ".fs" || ext == ".isf" {
				format = "isf"
			}
			base := strings.TrimSuffix(filepath.Base(path), filepath.Ext(path))
			name := strings.ReplaceAll(base, "-", " ")
			name = strings.ReplaceAll(name, "_", " ")

			data, _ := os.ReadFile(path)
			h := sha256.Sum256(data)
			fileHash := hex.EncodeToString(h[:8])

			category := "uncategorized"
			dir := filepath.Base(filepath.Dir(path))
			if dir != "" && dir != "." {
				category = dir
			}

			sets := []string{}
			if category == "macroverse" || strings.Contains(filepath.ToSlash(path), "/macroverse/") {
				sets = []string{"macroverse-origin", "macroverse-set", "vj-cosmic", "vj-wire-ready"}
			}

			existing = append(existing, ShaderEntry{
				ID:       maxID,
				Path:     path,
				Name:     name,
				Category: category,
				Tags:     []string{},
				Sets:     sets,
				Format:   format,
				FileHash: fileHash,
			})
			added++
		}

		removed := 0
		var cleaned []ShaderEntry
		for _, e := range existing {
			if _, err := os.Stat(e.Path); err != nil {
				removed++
				continue
			}
			ext := strings.ToLower(filepath.Ext(e.Path))
			if !shaderExts[ext] {
				removed++
				continue
			}
			cleaned = append(cleaned, e)
		}

		writeIndex(cleaned)
		logSection("WATCH", fmt.Sprintf("native scan done: %d total, %d added, %d removed", len(cleaned), added, removed))
	}()
}

func getIndexPath() string {
	pathConfig.RLock()
	defer pathConfig.RUnlock()
	return pathConfig.indexPath
}

func doFactoryReset() error {
	d := exeDir()
	settingsPath := filepath.Join(d, "shader-preview-settings.json")
	errPath := filepath.Join(d, "shader-errors.json")

	if err := clearIndexDB(); err != nil {
		return fmt.Errorf("reset index: %w", err)
	}
	errorLog.Lock()
	errorLog.entries = []ShaderError{}
	errorLog.path = errPath
	errorLog.Unlock()
	if err := os.WriteFile(errPath, []byte("[]"), 0644); err != nil {
		// non-fatal
	}
	defaultSettings := AppSettings{
		PreviewWidth: 854, PreviewHeight: 480, TargetFps: 30,
		SourcePaths: []string{getDefaultVFXRoot()}, VfxRoot: getDefaultVFXRoot(),
		EnablePipeline: true, EnableOutput: false, EnableGit: false,
		ShowThumbnails: true, ListViewMode: "list", SkipSplash: false,
		AutoOptimizeQuality: true, WatchFolders: false,
	}
	data, _ := json.MarshalIndent(defaultSettings, "", "  ")
	if err := os.WriteFile(settingsPath, data, 0644); err != nil {
		return fmt.Errorf("reset settings: %w", err)
	}
	pathConfig.Lock()
	pathConfig.indexPath = getDBPath()
	pathConfig.sourcePaths = []string{getDefaultVFXRoot()}
	pathConfig.Unlock()
	settingsMu.Lock()
	appSettings = defaultSettings
	settingsMu.Unlock()
	// Immediately re-scan so the fresh DB reflects only files actually on disk
	go triggerIncrementalIndex()
	return nil
}

func getSourcePaths() []string {
	pathConfig.RLock()
	defer pathConfig.RUnlock()
	out := make([]string, len(pathConfig.sourcePaths))
	copy(out, pathConfig.sourcePaths)
	return out
}

func getVfxRoot() string {
	paths := getSourcePaths()
	if len(paths) > 0 {
		return paths[0]
	}
	return getDefaultVFXRoot()
}

const (
	cReset     = "\033[0m"
	cBold      = "\033[1m"
	cDim       = "\033[2m"
	cRed       = "\033[31m"
	cGreen     = "\033[32m"
	cYellow    = "\033[33m"
	cBlue      = "\033[34m"
	cMagenta   = "\033[35m"
	cCyan      = "\033[36m"
	cWhite     = "\033[37m"
	cBrRed     = "\033[91m"
	cBrGreen   = "\033[92m"
	cBrYellow  = "\033[93m"
	cBrCyan    = "\033[96m"
	cBrMagenta = "\033[95m"
	cBgRed     = "\033[41m"
	cBgGreen   = "\033[42m"
	cBgBlue    = "\033[44m"
	cBgMagenta = "\033[45m"
	cBgCyan    = "\033[46m"
)

var tagColors = map[string]string{
	"BOOT":     cBrCyan,
	"HTTP":     cDim,
	"INDEX":    cBrGreen,
	"SETTINGS": cYellow,
	"PIPELINE": cMagenta,
	"SHADER":   cGreen,
	"OSC":      cBrMagenta,
	"MIDI":     cBrYellow,
	"CURSOR":   cCyan,
	"OUTPUT":   cBlue,
	"SOURCES":  cYellow,
	"ERROR":    cBrRed,
	"WARN":     cYellow,
	"CONSOLE":  cBrYellow,
	"WATCH":    cGreen,
	"ERRORS":   cBrRed,
}

// stripPipelineLine removes ANSI escape codes and non-printable/non-ASCII runes so pipeline log lines render cleanly.
func stripPipelineLine(s string) string {
	var b strings.Builder
	b.Grow(len(s))
	inEsc := false
	inCSI := false
	for _, r := range s {
		if inEsc {
			if !inCSI && r == '[' {
				inCSI = true
				continue
			}
			if inCSI {
				if (r >= '0' && r <= '9') || r == ';' || r == '?' {
					continue
				}
				if (r >= 'A' && r <= 'Z') || (r >= 'a' && r <= 'z') {
					inEsc = false
					inCSI = false
					continue
				}
			}
			inEsc = false
			inCSI = false
			continue
		}
		if r == 0x1b {
			inEsc = true
			inCSI = false
			continue
		}
		if r >= 32 && r < 127 {
			b.WriteRune(r)
		} else if r == '\t' || r == '\n' || r == '\r' {
			b.WriteRune(r)
		} else {
			b.WriteRune(' ')
		}
	}
	return b.String()
}

func buildFixThinking(orig, fixed, compileErr string) string {
	var steps []string
	steps = append(steps, "COMPILE ERROR ANALYSIS:")
	for _, line := range strings.Split(compileErr, "\n") {
		line = strings.TrimSpace(line)
		if line == "" {
			continue
		}
		steps = append(steps, "  "+line)
		if len(steps) > 12 {
			steps = append(steps, "  ... ("+strconv.Itoa(strings.Count(compileErr, "ERROR"))+" total errors)")
			break
		}
	}
	steps = append(steps, "")
	steps = append(steps, "FIX REASONING:")
	if strings.Contains(compileErr, "no matching overloaded function found") {
		funcRe := regexp.MustCompile(`'(\w+)'\s*:\s*no matching overloaded function found`)
		for _, m := range funcRe.FindAllStringSubmatch(compileErr, 3) {
			if len(m) >= 2 {
				steps = append(steps, "  -> '"+m[1]+"()' is not a GLSL built-in. Replaced with vec constructor based on argument count.")
			}
		}
	}
	if strings.Contains(compileErr, "undeclared identifier") {
		nameRe := regexp.MustCompile(`'(\w+)'\s*:\s*undeclared identifier`)
		for _, m := range nameRe.FindAllStringSubmatch(compileErr, 5) {
			if len(m) >= 2 {
				steps = append(steps, "  -> '"+m[1]+"' is not declared. Added as uniform float with @expose for live control.")
			}
		}
	}
	if strings.Contains(compileErr, "syntax error") {
		steps = append(steps, "  -> Syntax error detected. Checked for missing semicolons and unmatched braces.")
	}
	if strings.Contains(compileErr, "dimension mismatch") || strings.Contains(compileErr, "cannot convert") {
		steps = append(steps, "  -> Type/dimension mismatch. Attempted type coercion or constructor fix.")
	}
	if strings.Contains(compileErr, "precision") {
		steps = append(steps, "  -> Missing precision qualifier. Added 'precision highp float;'")
	}
	if strings.Contains(compileErr, "Fsqrt") {
		steps = append(steps, "  -> Non-standard 'Fsqrt' found. Replaced with GLSL 'sqrt()'.")
	}
	if strings.Contains(compileErr, "texture") && strings.Contains(compileErr, "no matching") {
		steps = append(steps, "  -> 'texture()' not available in WebGL 1.0. Replaced with 'texture2D()'.")
	}
	if strings.Contains(compileErr, "gl_FragData") {
		steps = append(steps, "  -> gl_FragData[0] not available in this context. Replaced with gl_FragColor.")
	}
	if strings.Contains(compileErr, "uniform") && strings.Contains(compileErr, "global scope") {
		steps = append(steps, "  -> Uniforms were inside a function. Moved to global scope.")
	}
	origLines := strings.Count(orig, "\n")
	fixedLines := strings.Count(fixed, "\n")
	steps = append(steps, "")
	steps = append(steps, fmt.Sprintf("RESULT: %d lines -> %d lines. Minimal changes to preserve shader intent.", origLines, fixedLines))
	return strings.Join(steps, "\n")
}

func simplifyCompileError(err string) string {
	lines := strings.Split(err, "\n")
	nonEmpty := 0
	typeCount := make(map[string][]string)
	for _, line := range lines {
		line = strings.TrimSpace(line)
		if line == "" {
			continue
		}
		nonEmpty++
		lineMatch := regexp.MustCompile(`0:(\d+):`).FindStringSubmatch(line)
		ln := ""
		if len(lineMatch) > 1 {
			ln = lineMatch[1]
		}
		msg := line
		if idx := strings.Index(line, ":"); idx >= 0 && idx < 25 {
			msg = strings.TrimSpace(line[idx+1:])
		}
		key := msg
		if len(key) > 90 {
			key = key[:90]
		}
		if typeCount[key] == nil {
			typeCount[key] = []string{}
		}
		if ln != "" {
			typeCount[key] = append(typeCount[key], ln)
		}
	}
	if nonEmpty <= 15 {
		return err
	}
	var out []string
	out = append(out, "TLDR (simplified from "+strconv.Itoa(nonEmpty)+" repeated errors):")
	for msg, lns := range typeCount {
		dedup := make(map[string]bool)
		for _, l := range lns {
			dedup[l] = true
		}
		sorted := make([]string, 0, len(dedup))
		for l := range dedup {
			sorted = append(sorted, l)
		}
		sort.Slice(sorted, func(i, j int) bool {
			a, _ := strconv.Atoi(sorted[i])
			b, _ := strconv.Atoi(sorted[j])
			return a < b
		})
		lineStr := strings.Join(sorted, ",")
		if len(sorted) > 5 {
			lineStr = sorted[0] + "-" + sorted[len(sorted)-1] + " (" + strconv.Itoa(len(sorted)) + " places)"
		}
		out = append(out, "  line "+lineStr+": "+msg)
	}
	return strings.Join(out, "\n")
}

func logSection(tag, msg string) {
	col, ok := tagColors[tag]
	if !ok {
		col = cWhite
	}
	isErr := tag == "ERROR" || strings.Contains(msg, "ERROR")
	msgCol := cReset
	if isErr {
		msgCol = cBrRed
	} else if strings.HasPrefix(msg, "  |") || strings.HasPrefix(msg, "  [") {
		msgCol = cDim
	}
	ts := time.Now().Format("15:04:05")
	fmt.Fprintf(os.Stdout, "%s%s%-10s%s %s%s%s\n",
		cDim, ts+" ", cReset,
		col+cBold+"["+tag+"]"+cReset,
		msgCol, msg, cReset)
	serverLogAppend(ts + " [" + tag + "] " + msg)
}

type statusRecorder struct {
	http.ResponseWriter
	status int
}

func (r *statusRecorder) WriteHeader(code int) {
	r.status = code
	r.ResponseWriter.WriteHeader(code)
}

func (r *statusRecorder) Flush() {
	if f, ok := r.ResponseWriter.(http.Flusher); ok {
		f.Flush()
	}
}

func (r *statusRecorder) Hijack() (net.Conn, *bufio.ReadWriter, error) {
	if h, ok := r.ResponseWriter.(http.Hijacker); ok {
		return h.Hijack()
	}
	return nil, nil, fmt.Errorf("response does not implement http.Hijacker")
}

type versionInfo struct {
	version, buildDate, gitRev, gitBranch, display, releaseTag string
	gitDirty                                                   bool
}

func getVersionInfo() versionInfo {
	var v versionInfo
	v.releaseTag = releaseTag
	v.version = buildVersion
	v.buildDate = buildDate
	if v.buildDate == "" {
		if exe, err := os.Executable(); err == nil {
			if fi, err := os.Stat(exe); err == nil {
				v.buildDate = strings.ReplaceAll(fi.ModTime().Format("2006-01-02 15:04"), " ", "_")
			}
		}
		if v.buildDate == "" {
			v.buildDate = "unknown"
		}
	}
	v.buildDate = strings.ReplaceAll(v.buildDate, "_", " ")
	repoDir := exeDir()
	for _, d := range []string{repoDir, filepath.Dir(repoDir)} {
		cmd := exec.Command("git", "-C", d, "rev-parse", "--short", "HEAD")
		cmd.Dir = d
		if out, err := cmd.Output(); err == nil {
			v.gitRev = strings.TrimSpace(string(out))
			break
		}
	}
	if v.gitRev != "" {
		for _, d := range []string{repoDir, filepath.Dir(repoDir)} {
			cmd := exec.Command("git", "-C", d, "rev-parse", "--abbrev-ref", "HEAD")
			cmd.Dir = d
			if out, err := cmd.Output(); err == nil {
				v.gitBranch = strings.TrimSpace(string(out))
				break
			}
		}
		for _, d := range []string{repoDir, filepath.Dir(repoDir)} {
			cmd := exec.Command("git", "-C", d, "status", "--short")
			cmd.Dir = d
			if out, err := cmd.Output(); err == nil {
				v.gitDirty = len(strings.TrimSpace(string(out))) > 0
				break
			}
		}
	}
	v.display = v.version
	if v.gitRev != "" {
		v.display = v.gitRev
		if v.gitDirty {
			v.display += " (dirty)"
		}
		if v.gitBranch != "" && v.gitBranch != "HEAD" {
			v.display += " " + v.gitBranch
		}
	}
	return v
}

func asciiArtPath() string {
	return filepath.Join(exeDir(), "ascii.art")
}

func readASCIIArt() ([]string, error) {
	data, err := os.ReadFile(asciiArtPath())
	if err != nil {
		return nil, err
	}
	raw := strings.TrimSpace(string(data))
	if raw == "" {
		return nil, fmt.Errorf("empty ascii.art")
	}
	return strings.Split(raw, "\n"), nil
}

func logBanner(lines ...string) {
	sep := cDim + strings.Repeat("=", 52) + cReset
	art, _ := readASCIIArt()
	for _, a := range art {
		a = strings.TrimRight(a, "\r")
		if a != "" {
			fmt.Fprintf(os.Stdout, "%s%s%s\n", cBrCyan, a, cReset)
		} else {
			fmt.Fprintln(os.Stdout)
		}
	}
	fmt.Fprintln(os.Stdout, sep)
	for _, l := range lines {
		fmt.Fprintf(os.Stdout, "  %s%s%s\n", cBrCyan+cBold, l, cReset)
	}
	fmt.Fprintln(os.Stdout, sep)
}

func logHTTP(method, rawPath string, status int, dur time.Duration) {
	displayPath := rawPath
	if strings.HasPrefix(rawPath, "/api/shader?path=") {
		if u, err := url.ParseQuery(rawPath[len("/api/shader?"):]); err == nil {
			if p := u.Get("path"); p != "" {
				decoded, _ := url.QueryUnescape(p)
				if runtime.GOOS == "windows" {
					decoded = strings.ReplaceAll(decoded, "|", string(filepath.Separator))
				}
				if len(decoded) > 70 {
					displayPath = "/api/shader  ..." + decoded[len(decoded)-66:]
				} else {
					displayPath = "/api/shader  " + decoded
				}
			}
		}
	}
	statusCol := cBrGreen
	if status >= 400 && status < 500 {
		statusCol = cYellow
	} else if status >= 500 {
		statusCol = cBrRed
	}
	methodCol := cCyan
	if method == "POST" {
		methodCol = cBrMagenta
	}
	durStr := dur.Round(time.Millisecond).String()
	if dur < time.Millisecond {
		durStr = "<1ms"
	}
	ts := time.Now().Format("15:04:05")
	fmt.Fprintf(os.Stdout, "%s%s%s %s %s%-6s%s %s%s%s %s%d%s %s%s%s\n",
		cDim, ts, cReset,
		cBrYellow+"|"+cReset,
		methodCol, method, cReset,
		cGreen, displayPath, cReset,
		statusCol, status, cReset,
		cDim, durStr, cReset)
}

func readIndex() ([]ShaderEntry, error) {
	return readIndexFromDB()
}

func writeIndex(arr []ShaderEntry) error {
	return writeIndexToDB(arr)
}

var settingsMu sync.RWMutex
var appSettings = AppSettings{
	PreviewWidth:   854,
	PreviewHeight:  480,
	TargetFps:      30,
	EnablePipeline: true,
	EnableOutput:   true,
	EnableGit:      true,
}

type ParamRange struct {
	Id  string  `json:"id"`
	Min float64 `json:"min"`
	Max float64 `json:"max"`
	Def float64 `json:"def,omitempty"`
}

type ShaderEntry struct {
	ID          int          `json:"id"`
	Path        string       `json:"path"`
	Name        string       `json:"name"`
	Category    string       `json:"category"`
	Tags        []string     `json:"tags,omitempty"`
	Sets        []string     `json:"sets,omitempty"`
	Notes       string       `json:"notes,omitempty"`
	Uniforms    []string     `json:"uniforms,omitempty"`
	FixedName   string       `json:"fixedName,omitempty"`
	Favorite    bool         `json:"favorite,omitempty"`
	Color       string       `json:"color,omitempty"`
	FileHash    string       `json:"fileHash,omitempty"`
	SourceRoot  string       `json:"sourceRoot,omitempty"`
	Format      string       `json:"format,omitempty"`
	ParamRanges []ParamRange `json:"paramRanges,omitempty"`
}

func stripBOM(data []byte) []byte {
	if len(data) >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF {
		return data[3:]
	}
	return data
}

func loadSettings(path string) {
	data, err := os.ReadFile(path)
	if err != nil {
		return
	}
	data = stripBOM(data)
	var s AppSettings
	if json.Unmarshal(data, &s) != nil {
		return
	}
	if s.PreviewWidth > 0 && s.PreviewHeight > 0 {
		settingsMu.Lock()
		appSettings.PreviewWidth = s.PreviewWidth
		appSettings.PreviewHeight = s.PreviewHeight
		if s.TargetFps > 0 {
			appSettings.TargetFps = s.TargetFps
		}
		appSettings.EnablePipeline = s.EnablePipeline
		appSettings.EnableOutput = s.EnableOutput
		appSettings.EnableGit = s.EnableGit
		appSettings.ShowThumbnails = s.ShowThumbnails
		if s.ListViewMode != "" {
			appSettings.ListViewMode = s.ListViewMode
		}
		appSettings.SkipSplash = s.SkipSplash
		if s.PreviewAspect != "" {
			appSettings.PreviewAspect = s.PreviewAspect
		}
		appSettings.AutoOptimizeQuality = s.AutoOptimizeQuality
		if s.CursorApiKey != "" {
			appSettings.CursorApiKey = strings.TrimSpace(s.CursorApiKey)
		}
		if len(s.SourcePaths) > 0 {
			appSettings.SourcePaths = s.SourcePaths
		}
		if s.VfxRoot != "" {
			appSettings.VfxRoot = strings.TrimSpace(s.VfxRoot)
			if len(appSettings.SourcePaths) == 0 {
				appSettings.SourcePaths = []string{appSettings.VfxRoot}
			}
		}
		if s.IndexPath != "" {
			appSettings.IndexPath = strings.TrimSpace(s.IndexPath)
		}
		if s.OutputPath != "" {
			appSettings.OutputPath = strings.TrimSpace(s.OutputPath)
		}
		if s.WirePath != "" {
			appSettings.WirePath = strings.TrimSpace(s.WirePath)
		}
		if len(s.LLMProviders) > 0 {
			appSettings.LLMProviders = s.LLMProviders
		}
		appSettings.DefaultParamValue = s.DefaultParamValue
		appSettings.DefaultTimeScale = s.DefaultTimeScale
		settingsMu.Unlock()
	}
}

func loadCursorApiKeyFromEnv(dir string) {
	if noExternalLLM {
		return
	}
	if k := os.Getenv("CURSOR_API_KEY"); k != "" {
		settingsMu.Lock()
		appSettings.CursorApiKey = strings.TrimSpace(k)
		settingsMu.Unlock()
		return
	}
	for _, d := range []string{dir, exeDir()} {
		if d == "" {
			continue
		}
		envPath := filepath.Join(d, ".env")
		data, err := os.ReadFile(envPath)
		if err != nil {
			continue
		}
		scanner := bufio.NewScanner(strings.NewReader(string(data)))
		for scanner.Scan() {
			line := strings.TrimSpace(scanner.Text())
			if line == "" || strings.HasPrefix(line, "#") {
				continue
			}
			if idx := strings.Index(line, "="); idx > 0 {
				key := strings.TrimSpace(line[:idx])
				val := strings.TrimSpace(line[idx+1:])
				if strings.TrimSpace(strings.ToUpper(key)) == "CURSOR_API_KEY" && val != "" {
					val = strings.Trim(val, "\"'")
					if val != "" {
						settingsMu.Lock()
						appSettings.CursorApiKey = val
						settingsMu.Unlock()
					}
					return
				}
			}
		}
	}
}

func defaultLLMProviders() []LLMProvider {
	return []LLMProvider{
		{Name: "local", Enabled: true, Priority: 1},
		{Name: "ollama", Enabled: true, Priority: 2, Model: "llama3.2", Endpoint: "http://localhost:11434"},
		{Name: "cursor", Enabled: false, Priority: 3},
	}
}

func getLLMProviders() []LLMProvider {
	settingsMu.RLock()
	defer settingsMu.RUnlock()
	var providers []LLMProvider
	if len(appSettings.LLMProviders) == 0 {
		providers = defaultLLMProviders()
	} else {
		providers = appSettings.LLMProviders
	}
	if noExternalLLM {
		var localOnly []LLMProvider
		for _, p := range providers {
			if p.Name == "local" {
				localOnly = append(localOnly, p)
			}
		}
		return localOnly
	}
	return providers
}

func getLLMProvidersSorted() []LLMProvider {
	providers := getLLMProviders()
	sort.Slice(providers, func(i, j int) bool {
		return providers[i].Priority < providers[j].Priority
	})
	return providers
}

func ollamaIsAvailable(endpoint string) bool {
	if endpoint == "" {
		endpoint = "http://localhost:11434"
	}
	client := &http.Client{Timeout: 3 * time.Second}
	resp, err := client.Get(endpoint + "/api/tags")
	if err != nil {
		return false
	}
	resp.Body.Close()
	return resp.StatusCode == 200
}

func ollamaListModels(endpoint string) ([]string, error) {
	if endpoint == "" {
		endpoint = "http://localhost:11434"
	}
	client := &http.Client{Timeout: 10 * time.Second}
	resp, err := client.Get(endpoint + "/api/tags")
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()
	var result struct {
		Models []struct {
			Name string `json:"name"`
		} `json:"models"`
	}
	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		return nil, err
	}
	var names []string
	for _, m := range result.Models {
		names = append(names, m.Name)
	}
	return names, nil
}

// ollamaPickModel returns a model name that is installed. Prefers preferred if in list;
// else prefers code-related names (coder, code, qwen, llama, phi, etc.); else first in list.
func ollamaPickModel(endpoint, preferred string) string {
	list, err := ollamaListModels(endpoint)
	if err != nil || len(list) == 0 {
		return preferred
	}
	pref := strings.ToLower(preferred)
	for _, name := range list {
		if strings.ToLower(name) == pref || strings.HasPrefix(strings.ToLower(name), pref+":") {
			return name
		}
	}
	codeKeywords := []string{"coder", "code", "qwen", "llama", "phi", "mistral", "deepseek", "starcoder", "granite"}
	for _, kw := range codeKeywords {
		for _, name := range list {
			if strings.Contains(strings.ToLower(name), kw) {
				return name
			}
		}
	}
	return list[0]
}

func ollamaGenerate(endpoint, model, prompt string) (string, error) {
	if endpoint == "" {
		endpoint = "http://localhost:11434"
	}
	if model == "" {
		model = "llama3.2"
	}
	model = ollamaPickModel(endpoint, model)
	client := &http.Client{Timeout: 120 * time.Second}
	body, _ := json.Marshal(map[string]interface{}{
		"model":  model,
		"prompt": prompt,
		"stream": false,
		"options": map[string]interface{}{
			"temperature": 0.3,
			"num_predict": 4096,
		},
	})
	resp, err := client.Post(endpoint+"/api/generate", "application/json", strings.NewReader(string(body)))
	if err != nil {
		return "", fmt.Errorf("ollama: %w", err)
	}
	defer resp.Body.Close()
	var result struct {
		Response string `json:"response"`
	}
	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		return "", fmt.Errorf("ollama decode: %w", err)
	}
	return result.Response, nil
}

// llmFixShader tries to fix a shader using the LLM priority chain.
// The "local" provider is handled separately (regex fixes in cursor-fix handler).
// This function handles only the "ollama" and "cursor" providers.
func llmFixShader(content, compileErr, filename string, isISF bool, previousErrors []string) (string, string, error) {
	providers := getLLMProvidersSorted()
	for _, p := range providers {
		if !p.Enabled || p.Name == "local" {
			continue
		}
		if p.Name == "ollama" {
			if !ollamaIsAvailable(p.Endpoint) {
				logSection("LLM", "ollama not available at "+p.Endpoint+", skipping")
				continue
			}
			model := ollamaPickModel(p.Endpoint, p.Model)
			logSection("LLM", "trying ollama fix with model "+model)
			prompt := buildShaderFixPrompt(content, compileErr, filename, isISF, previousErrors)
			result, err := ollamaGenerate(p.Endpoint, model, prompt)
			if err != nil {
				logSection("LLM", "ollama fix failed: "+err.Error())
				continue
			}
			fixed := extractShaderFromResponse(result)
			if fixed != "" {
				logSection("LLM", "ollama fix succeeded")
				return fixed, "Ollama (" + p.Model + ") fix applied", nil
			}
			logSection("LLM", "ollama returned empty fix")
			continue
		}
		if p.Name == "cursor" {
			return "", "", fmt.Errorf("use-cursor-agent")
		}
	}
	return "", "", fmt.Errorf("no LLM provider could fix the shader")
}

// llmGenerateShader generates shader code from a description using the LLM chain.
func llmGenerateShader(template, description, genre string) (string, error) {
	providers := getLLMProvidersSorted()
	for _, p := range providers {
		if !p.Enabled || p.Name == "local" {
			continue
		}
		if p.Name == "ollama" {
			if !ollamaIsAvailable(p.Endpoint) {
				continue
			}
			logSection("LLM", "trying ollama generate with model "+p.Model)
			prompt := "You are a GLSL shader expert. Generate a complete, working fragment shader.\n\n" +
				"Genre: " + genre + "\n" +
				"Description: " + description + "\n\n" +
				"Starting template:\n```glsl\n" + template + "\n```\n\n" +
				"REQUIREMENTS:\n" +
				"1. Output ONLY the complete shader code, no explanation\n" +
				"2. Keep precision highp float and gl_FragColor output\n" +
				"3. Use uniform float paramName; // @expose min max for all tweakable parameters\n" +
				"4. Must compile in WebGL 1.0 (GLSL ES 1.00)\n" +
				"5. Use TIME for animation, RENDERSIZE for resolution\n" +
				"6. Be creative and visually impressive\n" +
				"7. For 3D objects use raymarching with SDF functions\n"
			result, err := ollamaGenerate(p.Endpoint, p.Model, prompt)
			if err != nil {
				logSection("LLM", "ollama generate failed: "+err.Error())
				continue
			}
			code := extractShaderFromResponse(result)
			if code != "" {
				return code, nil
			}
			continue
		}
		if p.Name == "cursor" {
			return "", fmt.Errorf("use-cursor-agent")
		}
	}
	return "", fmt.Errorf("no LLM provider available for generation")
}

// llmSuggestParams uses LLM to find exposable parameters in shader code.
func llmSuggestParams(content string) ([]string, []map[string]interface{}, string, error) {
	providers := getLLMProvidersSorted()
	for _, p := range providers {
		if !p.Enabled || p.Name == "local" {
			continue
		}
		if p.Name == "ollama" {
			if !ollamaIsAvailable(p.Endpoint) {
				continue
			}
			logSection("LLM", "trying ollama suggest-params with model "+p.Model)
			prompt := "Analyze this GLSL shader and identify numeric values that should become uniform parameters (sliders).\n\n" +
				"```glsl\n" + content + "\n```\n\n" +
				"Output a JSON array of parameter names that should be exposed as uniforms.\n" +
				"Focus on: hardcoded floats that control visual appearance (speeds, scales, colors, etc).\n" +
				"Skip: time, mouse, resolution, RENDERSIZE, TIME built-ins.\n" +
				"Format: [\"paramName1\", \"paramName2\", ...]\n" +
				"Output ONLY the JSON array, nothing else."
			result, err := ollamaGenerate(p.Endpoint, p.Model, prompt)
			if err != nil {
				continue
			}
			arrMatch := regexp.MustCompile(`\[[\s\S]*?\]`).FindString(result)
			if arrMatch != "" {
				var params []string
				if json.Unmarshal([]byte(arrMatch), &params) == nil && len(params) > 0 {
					return params, nil, "Ollama (" + p.Model + ")", nil
				}
			}
			continue
		}
		if p.Name == "cursor" {
			return nil, nil, "", fmt.Errorf("use-cursor-agent")
		}
	}
	return nil, nil, "", fmt.Errorf("no LLM provider available")
}

func buildShaderFixPrompt(content, compileErr, filename string, isISF bool, previousErrors []string) string {
	format := "GLSL"
	if isISF {
		format = "ISF"
	}
	prevStr := ""
	if len(previousErrors) > 0 {
		prevStr = "\nPrevious fix attempts produced these errors:\n" + strings.Join(previousErrors, "\n") + "\n"
	}
	return "Fix this " + format + " fragment shader that has compile errors.\n\n" +
		"Filename: " + filename + "\n" +
		"Compile error:\n" + compileErr + "\n" + prevStr +
		"\nShader source:\n```glsl\n" + content + "\n```\n\n" +
		"REQUIREMENTS:\n" +
		"1. Output ONLY the fixed shader code, no explanation\n" +
		"2. Fix all compile errors\n" +
		"3. Keep the same visual intent\n" +
		"4. Must be WebGL 1.0 compatible (GLSL ES 1.00)\n" +
		"5. Keep gl_FragColor output\n" +
		"6. Preserve all uniform declarations and @expose annotations\n"
}

func extractShaderFromResponse(response string) string {
	codeBlockRe := regexp.MustCompile("(?s)```(?:glsl|hlsl|frag|c)?\\s*\\n(.*?)```")
	m := codeBlockRe.FindStringSubmatch(response)
	if len(m) >= 2 {
		return strings.TrimSpace(m[1])
	}
	trimmed := strings.TrimSpace(response)
	if strings.Contains(trimmed, "void main") || strings.Contains(trimmed, "gl_FragColor") {
		return trimmed
	}
	return ""
}

func saveCurrentSettings() {
	settingsMu.RLock()
	data, err := json.MarshalIndent(appSettings, "", "  ")
	settingsMu.RUnlock()
	if err != nil {
		logSection("SETTINGS", "save error: "+err.Error())
		return
	}
	settingsPath := filepath.Join(exeDir(), "shader-preview-settings.json")
	if err := os.WriteFile(settingsPath, data, 0644); err != nil {
		logSection("SETTINGS", "write error: "+err.Error())
	}
}

var serverLogBuf struct {
	mu    sync.Mutex
	lines []string
}

const serverLogMax = 200

func serverLogAppend(line string) {
	serverLogBuf.mu.Lock()
	serverLogBuf.lines = append(serverLogBuf.lines, line)
	if len(serverLogBuf.lines) > serverLogMax {
		serverLogBuf.lines = serverLogBuf.lines[len(serverLogBuf.lines)-serverLogMax:]
	}
	serverLogBuf.mu.Unlock()
}

func findPS1(name string) string {
	dirs := []string{}
	if exe, err := os.Executable(); err == nil {
		dirs = append(dirs, filepath.Dir(exe))
	}
	if cwd, err := os.Getwd(); err == nil {
		dirs = append(dirs, cwd, filepath.Join(cwd, ".."))
	}
	for _, d := range dirs {
		p := filepath.Join(d, name)
		if _, err := os.Stat(p); err == nil {
			return p
		}
	}
	return ""
}

func killAllMacroverseProcesses() int {
	logSection("CONSOLE", "kill-all is only supported interactively; no platform-specific kill performed")
	return 0
}

func printConsoleHelp() {
	fmt.Fprintf(os.Stdout, "\n%s%s  Console shortcuts:%s\n", cBold, cBrCyan, cReset)
	fmt.Fprintf(os.Stdout, "%s    R%s  Restart server\n", cBrGreen, cReset)
	fmt.Fprintf(os.Stdout, "%s    B%s  Rebuild (launch-macroverse.ps1) and restart\n", cBrGreen, cReset)
	fmt.Fprintf(os.Stdout, "%s    S%s  Re-scan / re-index all source paths\n", cBrGreen, cReset)
	fmt.Fprintf(os.Stdout, "%s    F%s  Factory reset (delete settings, index, errors - start fresh)\n", cBrYellow, cReset)
	fmt.Fprintf(os.Stdout, "%s    Q%s  Quit this server\n", cYellow, cReset)
	fmt.Fprintf(os.Stdout, "%s    K%s  Kill ALL Macroverse sessions\n", cBrRed, cReset)
	fmt.Fprintf(os.Stdout, "%s    W%s  Launch web (open http://localhost in browser)\n", cBrGreen, cReset)
	fmt.Fprintf(os.Stdout, "%s    L%s  Launch additional instance (new port)\n", cBrGreen, cReset)
	fmt.Fprintf(os.Stdout, "%s    ?%s  Show this help\n", cDim, cReset)
	fmt.Fprintln(os.Stdout)
}

func consoleKeyListener(port string) {
	printConsoleHelp()
	reader := bufio.NewReader(os.Stdin)
	for {
		b, err := reader.ReadByte()
		if err != nil {
			return
		}
		switch b {
		case 'r', 'R':
			logSection("CONSOLE", "Restart requested (R)")
			exePath, err := os.Executable()
			if err != nil {
				logSection("CONSOLE", "cannot find exe: "+err.Error())
				continue
			}
			args := []string{"-restart"}
			if port != "" && port != defaultPort {
				args = append(args, "-port", port)
			}
			cmd := exec.Command(exePath, args...)
			cmd.Dir = exeDir()
			cmd.Stdout = os.Stdout
			cmd.Stderr = os.Stderr
			cmd.Start()
			os.Exit(0)

		case 'b', 'B':
			exePath, err := os.Executable()
			if err != nil {
				logSection("CONSOLE", "Rebuild: cannot find exe: "+err.Error())
				continue
			}
			apiDir := filepath.Join(exeDir(), "api")
			if _, err := os.Stat(apiDir); err != nil {
				logSection("CONSOLE", "Rebuild: api source dir not found at: "+apiDir)
				continue
			}
			logSection("CONSOLE", "Rebuild: compiling...")
			// On Windows the running exe is locked; rename it out of the way first
			ext := filepath.Ext(exePath)
			oldPath := exePath[:len(exePath)-len(ext)] + ".old" + ext
			renamed := false
			if runtime.GOOS == "windows" {
				if err := os.Rename(exePath, oldPath); err != nil {
					logSection("CONSOLE", "Rebuild: cannot stage exe: "+err.Error())
					continue
				}
				renamed = true
			}
			buildCmd := exec.Command("go", "build", "-o", exePath, ".")
			buildCmd.Dir = apiDir
			buildCmd.Stdout = os.Stdout
			buildCmd.Stderr = os.Stderr
			if err := buildCmd.Run(); err != nil {
				logSection("CONSOLE", "Rebuild: build failed: "+err.Error())
				if renamed {
					os.Rename(oldPath, exePath) // restore on failure
				}
				continue
			}
			logSection("CONSOLE", "Rebuild: OK — restarting...")
			args := []string{"-restart"}
			if port != "" && port != defaultPort {
				args = append(args, "-port", port)
			}
			restartCmd := exec.Command(exePath, args...)
			restartCmd.Dir = exeDir()
			restartCmd.Stdout = os.Stdout
			restartCmd.Stderr = os.Stderr
			restartCmd.Start()
			os.Exit(0)

		case 'f', 'F':
			logSection("CONSOLE", "Factory reset requested (F)")
			if err := doFactoryReset(); err != nil {
				logSection("CONSOLE", "Factory reset error: "+err.Error())
			} else {
				logSection("CONSOLE", "Factory reset done. Restart to use fresh config.")
			}

		case 's', 'S':
			logSection("CONSOLE", "Re-scan requested (S)")
			go func() {
				files := scanAllFiles()
				existing, _ := readIndex()
				if existing == nil {
					existing = []ShaderEntry{}
				}

				known := make(map[string]bool)
				maxID := 0
				for _, e := range existing {
					known[e.Path] = true
					if e.ID > maxID {
						maxID = e.ID
					}
				}

				added := 0
				for path := range files {
					if known[path] {
						continue
					}
					maxID++
					ext := strings.ToLower(filepath.Ext(path))
					format := "glsl"
					if ext == ".fs" || ext == ".isf" {
						format = "isf"
					}
					base := strings.TrimSuffix(filepath.Base(path), filepath.Ext(path))
					name := strings.ReplaceAll(base, "-", " ")
					name = strings.ReplaceAll(name, "_", " ")

					data, _ := os.ReadFile(path)
					h := sha256.Sum256(data)
					fileHash := hex.EncodeToString(h[:8])

					category := "uncategorized"
					dir := filepath.Base(filepath.Dir(path))
					if dir != "" && dir != "." {
						category = dir
					}

					sets := []string{}
					if category == "macroverse" || strings.Contains(filepath.ToSlash(path), "/macroverse/") {
						sets = []string{"macroverse-origin", "macroverse-set", "vj-cosmic", "vj-wire-ready"}
					}

					existing = append(existing, ShaderEntry{
						ID:       maxID,
						Path:     path,
						Name:     name,
						Category: category,
						Tags:     []string{},
						Sets:     sets,
						Format:   format,
						FileHash: fileHash,
					})
					added++
				}

				removed := 0
				var cleaned []ShaderEntry
				for _, e := range existing {
					if _, err := os.Stat(e.Path); err != nil {
						removed++
						continue
					}
					ext := strings.ToLower(filepath.Ext(e.Path))
					if !shaderExts[ext] {
						removed++
						continue
					}
					cleaned = append(cleaned, e)
				}

				writeIndex(cleaned)
				logSection("CONSOLE", fmt.Sprintf("Scan complete: %d total, %d added, %d removed", len(cleaned), added, removed))
			}()

		case 'q', 'Q':
			logSection("CONSOLE", "Quit requested (Q)")
			os.Exit(0)

		case 'k', 'K':
			logSection("CONSOLE", "Kill ALL Macroverse sessions requested (K)")
			killAllMacroverseProcesses()
			os.Exit(0)

		case 'w', 'W':
			logSection("CONSOLE", "Launch web (W)")
			p := port
			if p == "" {
				p = defaultPort
			}
			url := "http://localhost:" + p
			var cmd *exec.Cmd
			switch runtime.GOOS {
			case "windows":
				cmd = exec.Command("rundll32", "url.dll,FileProtocolHandler", url)
			case "darwin":
				cmd = exec.Command("open", url)
			default:
				cmd = exec.Command("xdg-open", url)
			}
			if err := cmd.Start(); err != nil {
				logSection("CONSOLE", "Launch web failed: "+err.Error())
			}

		case 'l', 'L':
			logSection("CONSOLE", "Launch additional instance (L)")
			exePath, err := os.Executable()
			if err != nil {
				logSection("CONSOLE", "cannot find exe: "+err.Error())
				continue
			}
			p := port
			if p == "" {
				p = defaultPort
			}
			nextPort := p
			if n, err := strconv.Atoi(p); err == nil && n > 0 {
				nextPort = strconv.Itoa(n + 1)
			}
			args := []string{"-port", nextPort}
			cmd := exec.Command(exePath, args...)
			cmd.Dir = exeDir()
			if err := cmd.Start(); err != nil {
				logSection("CONSOLE", "Launch instance failed: "+err.Error())
			} else {
				logSection("CONSOLE", "Additional instance started on port "+nextPort)
			}

		case '?', 'h', 'H':
			printConsoleHelp()
		}
	}
}

func main() {
	log.SetFlags(log.Ltime)
	portFlag := flag.String("port", "", "HTTP port (default 8765; if busy, auto-tries +1)")
	restartFlag := flag.Bool("restart", false, "internal: wait before starting (used by /api/restart)")
	versionFlag := flag.Bool("version", false, "print version JSON and exit")
	flag.Parse()

	if *versionFlag {
		v := getVersionInfo()
		enc := json.NewEncoder(os.Stdout)
		enc.SetEscapeHTML(false)
		enc.Encode(map[string]interface{}{
			"version":    v.version,
			"buildDate":  v.buildDate,
			"gitRev":     v.gitRev,
			"gitBranch":  v.gitBranch,
			"gitDirty":   v.gitDirty,
			"releaseTag": v.releaseTag,
		})
		os.Exit(0)
	}

	if *restartFlag {
		time.Sleep(2 * time.Second)
	}

	// Clean up any .old exe left behind by a previous 'B' rebuild
	if exePath, err := os.Executable(); err == nil {
		ext := filepath.Ext(exePath)
		oldPath := exePath[:len(exePath)-len(ext)] + ".old" + ext
		os.Remove(oldPath)
	}

	logSection("BOOT", "Macroverse Wired Atelier starting up")
	logSection("BOOT", "exe dir: "+exeDir())
	logSection("BOOT", "frontend dir: "+frontendDir())

	settingsPath := filepath.Join(exeDir(), "shader-preview-settings.json")
	loadSettings(settingsPath)
	logSection("BOOT", "settings loaded from "+settingsPath)

	if err := initIndexDB(); err != nil {
		logSection("BOOT", "WARNING: init index DB: "+err.Error())
	}

	settingsMu.RLock()
	srcPaths := appSettings.SourcePaths
	oldVfx := appSettings.VfxRoot
	settingsMu.RUnlock()

	if len(srcPaths) == 0 {
		if oldVfx != "" {
			srcPaths = []string{strings.TrimSpace(oldVfx)}
		} else if v := os.Getenv("VFX_GLSL_ROOT"); v != "" {
			srcPaths = []string{v}
		} else {
			srcPaths = []string{getDefaultVFXRoot()}
		}
		settingsMu.Lock()
		appSettings.SourcePaths = srcPaths
		settingsMu.Unlock()
	}

	pathConfig.Lock()
	pathConfig.indexPath = getDBPath()
	pathConfig.sourcePaths = srcPaths
	pathConfig.Unlock()

	logSection("BOOT", "master index: "+getDBPath())
	for i, sp := range srcPaths {
		logSection("BOOT", fmt.Sprintf("source path [%d]: %s", i, sp))
	}
	if n := purgeBannedShadersFromDisk(); n > 0 {
		logSection("BOOT", fmt.Sprintf("purged %d banned shader file(s) from disk", n))
	}
	if arr, err := readIndex(); err != nil {
		logSection("BOOT", "WARNING: cannot read index: "+err.Error())
	} else {
		logSection("BOOT", fmt.Sprintf("index contains %d shader(s)", len(arr)))
		// Purge stale entries (files deleted while server was offline)
		var alive []ShaderEntry
		stale := 0
		for _, e := range arr {
			if isBannedShader(e.Path) || isBannedShader(e.Name) {
				stale++
				continue
			}
			if _, err := os.Stat(e.Path); err == nil {
				alive = append(alive, e)
			} else {
				stale++
			}
		}
		if stale > 0 {
			if err2 := writeIndex(alive); err2 == nil {
				logSection("BOOT", fmt.Sprintf("purged %d stale index entry(entries) (files no longer on disk)", stale))
			}
		}
		diskFiles := scanAllFiles()
		if len(alive) == 0 {
			logSection("BOOT", "index is empty — triggering auto-scan of source paths")
			triggerIncrementalIndex()
		} else if len(diskFiles) > len(alive) {
			logSection("BOOT", fmt.Sprintf("index has %d entries but %d shader files on disk — triggering merge scan", len(alive), len(diskFiles)))
			triggerIncrementalIndex()
		}
	}

	port := *portFlag
	if port == "" {
		port = os.Getenv("PORT")
	}
	if port == "" {
		port = defaultPort
	}

	loadCursorApiKeyFromEnv(exeDir())
	settingsMu.RLock()
	hasKey := appSettings.CursorApiKey != ""
	settingsMu.RUnlock()
	if hasKey {
		logSection("BOOT", "Cursor API key loaded")
	} else {
		logSection("BOOT", "No Cursor API key (set in Settings or .env)")
	}

	http.HandleFunc("/api/settings", func(w http.ResponseWriter, r *http.Request) {
		logSection("SETTINGS", r.Method+" /api/settings")
		if r.Method == http.MethodGet {
			settingsMu.RLock()
			s := appSettings
			settingsMu.RUnlock()
			cursorApiKeyFromEnv := os.Getenv("CURSOR_API_KEY") != ""
			if s.CursorApiKey != "" {
				s.CursorApiKey = "***"
			}
			idxPath := getIndexPath()
			settingsMu.RLock()
			outPath := appSettings.OutputPath
			settingsMu.RUnlock()
			if outPath == "" && len(s.SourcePaths) > 0 {
				outPath = filepath.Join(s.SourcePaths[0], "sorted_txt")
			}
			graveyardPath := unrecoverableShadersPath()
			resp := map[string]interface{}{
				"previewWidth":        s.PreviewWidth,
				"previewHeight":       s.PreviewHeight,
				"targetFps":           s.TargetFps,
				"enablePipeline":      s.EnablePipeline,
				"enableOutput":        s.EnableOutput,
				"enableGit":           s.EnableGit,
				"showThumbnails":      s.ShowThumbnails,
				"listViewMode":        s.ListViewMode,
				"skipSplash":          s.SkipSplash,
				"previewAspect":       s.PreviewAspect,
				"autoOptimizeQuality": s.AutoOptimizeQuality,
				"cursorApiKey":        s.CursorApiKey,
				"cursorApiKeyFromEnv": cursorApiKeyFromEnv,
				"vfxRoot":             s.VfxRoot,
				"sourcePaths":         s.SourcePaths,
				"indexPath":           idxPath,
				"graveyardPath":       graveyardPath,
				"outputPath":          outPath,
				"watchFolders":        s.WatchFolders,
				"wirePath":            s.WirePath,
				"defaultParamValue":   s.DefaultParamValue,
				"defaultTimeScale":    s.DefaultTimeScale,
				"hardResetPath":       appSettings.getHardResetPath(),
			}
			if s.GitHubToken != "" {
				resp["githubToken"] = "***"
			} else {
				resp["githubToken"] = ""
			}
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(resp)
			return
		}
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		var s AppSettings
		if err := json.NewDecoder(r.Body).Decode(&s); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		if s.PreviewWidth <= 0 {
			s.PreviewWidth = 854
		}
		if s.PreviewHeight <= 0 {
			s.PreviewHeight = 480
		}
		if s.PreviewWidth > 1920 {
			s.PreviewWidth = 1920
		}
		if s.PreviewHeight > 1080 {
			s.PreviewHeight = 1080
		}
		if s.TargetFps <= 0 {
			s.TargetFps = 30
		}
		if s.TargetFps > 60 {
			s.TargetFps = 60
		}
		settingsMu.Lock()
		appSettings.PreviewWidth = s.PreviewWidth
		appSettings.PreviewHeight = s.PreviewHeight
		appSettings.TargetFps = s.TargetFps
		appSettings.EnablePipeline = s.EnablePipeline
		appSettings.EnableOutput = s.EnableOutput
		appSettings.EnableGit = s.EnableGit
		appSettings.ShowThumbnails = s.ShowThumbnails
		appSettings.ListViewMode = s.ListViewMode
		appSettings.SkipSplash = s.SkipSplash
		appSettings.PreviewAspect = s.PreviewAspect
		appSettings.AutoOptimizeQuality = s.AutoOptimizeQuality
		if s.CursorApiKey != "" && s.CursorApiKey != "***" {
			appSettings.CursorApiKey = strings.TrimSpace(s.CursorApiKey)
		}
		if s.GitHubToken != "" && s.GitHubToken != "***" {
			appSettings.GitHubToken = strings.TrimSpace(s.GitHubToken)
		}
		if len(s.SourcePaths) > 0 {
			cleaned := []string{}
			for _, p := range s.SourcePaths {
				p = strings.TrimSpace(p)
				if p != "" {
					cleaned = append(cleaned, p)
				}
			}
			appSettings.SourcePaths = cleaned
			if len(cleaned) > 0 {
				appSettings.VfxRoot = cleaned[0]
			}
			pathConfig.Lock()
			pathConfig.sourcePaths = cleaned
			pathConfig.Unlock()
			logSection("SETTINGS", fmt.Sprintf("source paths updated: %d paths", len(cleaned)))
			for i, p := range cleaned {
				logSection("SETTINGS", fmt.Sprintf("  [%d] %s", i, p))
			}
		} else if s.VfxRoot != "" {
			vfx := strings.TrimSpace(s.VfxRoot)
			appSettings.VfxRoot = vfx
			appSettings.SourcePaths = []string{vfx}
			pathConfig.Lock()
			pathConfig.sourcePaths = []string{vfx}
			pathConfig.Unlock()
			logSection("SETTINGS", "VFX root set to: "+vfx)
		}
		if s.IndexPath != "" && s.IndexPath != "***" {
			idx := strings.TrimSpace(s.IndexPath)
			appSettings.IndexPath = idx
			pathConfig.Lock()
			pathConfig.indexPath = idx
			pathConfig.Unlock()
			logSection("SETTINGS", "index path: "+idx)
		}
		if s.OutputPath != "" {
			appSettings.OutputPath = strings.TrimSpace(s.OutputPath)
		}
		if s.WirePath != "" {
			appSettings.WirePath = strings.TrimSpace(s.WirePath)
		} else {
			appSettings.WirePath = ""
		}
		appSettings.DefaultParamValue = s.DefaultParamValue
		appSettings.DefaultTimeScale = s.DefaultTimeScale
		if s.HardResetPath != "" {
			appSettings.HardResetPath = strings.TrimSpace(s.HardResetPath)
		}
		settingsMu.Unlock()
		settingsMu.RLock()
		savedKey := appSettings.CursorApiKey
		savedGitHubToken := appSettings.GitHubToken
		savedPaths := appSettings.SourcePaths
		savedVfx := appSettings.VfxRoot
		savedIndexPath := appSettings.IndexPath
		savedOutputPath := appSettings.OutputPath
		savedWirePath := appSettings.WirePath
		settingsMu.RUnlock()
		if s.WatchFolders {
			startFolderWatcher()
		} else {
			stopFolderWatcher()
		}
		appSettings.WatchFolders = s.WatchFolders

		toSave := struct {
			PreviewWidth        int      `json:"previewWidth"`
			PreviewHeight       int      `json:"previewHeight"`
			TargetFps           int      `json:"targetFps"`
			EnablePipeline      bool     `json:"enablePipeline"`
			EnableOutput        bool     `json:"enableOutput"`
			EnableGit           bool     `json:"enableGit"`
			ShowThumbnails      bool     `json:"showThumbnails"`
			ListViewMode        string   `json:"listViewMode"`
			SkipSplash          bool     `json:"skipSplash"`
			PreviewAspect       string   `json:"previewAspect,omitempty"`
			AutoOptimizeQuality bool     `json:"autoOptimizeQuality"`
			CursorApiKey        string   `json:"cursorApiKey,omitempty"`
			VfxRoot             string   `json:"vfxRoot,omitempty"`
			SourcePaths         []string `json:"sourcePaths,omitempty"`
			IndexPath           string   `json:"indexPath,omitempty"`
			OutputPath          string   `json:"outputPath,omitempty"`
			WatchFolders        bool     `json:"watchFolders"`
			WirePath            string   `json:"wirePath,omitempty"`
			DefaultParamValue   float64  `json:"defaultParamValue,omitempty"`
			DefaultTimeScale    float64  `json:"defaultTimeScale,omitempty"`
			GitHubToken         string   `json:"githubToken,omitempty"`
		}{
			PreviewWidth:        s.PreviewWidth,
			PreviewHeight:       s.PreviewHeight,
			TargetFps:           s.TargetFps,
			EnablePipeline:      s.EnablePipeline,
			EnableOutput:        s.EnableOutput,
			EnableGit:           s.EnableGit,
			ShowThumbnails:      s.ShowThumbnails,
			ListViewMode:        s.ListViewMode,
			SkipSplash:          s.SkipSplash,
			PreviewAspect:       s.PreviewAspect,
			AutoOptimizeQuality: s.AutoOptimizeQuality,
			CursorApiKey:        savedKey,
			GitHubToken:         savedGitHubToken,
			VfxRoot:             savedVfx,
			SourcePaths:         savedPaths,
			IndexPath:           savedIndexPath,
			OutputPath:          savedOutputPath,
			WatchFolders:        s.WatchFolders,
			WirePath:            savedWirePath,
			DefaultParamValue:   s.DefaultParamValue,
			DefaultTimeScale:    s.DefaultTimeScale,
		}
		if data, err := json.MarshalIndent(toSave, "", "  "); err == nil {
			os.WriteFile(settingsPath, data, 0644)
		}
		w.Header().Set("Content-Type", "application/json")
		w.Write([]byte(`{"ok":true}`))
	})

	http.HandleFunc("/api/llm/status", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodGet {
			http.Error(w, "method not allowed", 405)
			return
		}
		providers := getLLMProviders()
		var ollamaOnline bool
		var ollamaModels []string
		for _, p := range providers {
			if p.Name == "ollama" && p.Enabled {
				ollamaOnline = ollamaIsAvailable(p.Endpoint)
				if ollamaOnline {
					ollamaModels, _ = ollamaListModels(p.Endpoint)
				}
				break
			}
		}
		cursorAvail := false
		if _, _, err := findAgentExe(); err == nil {
			cursorAvail = true
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{
			"providers":    providers,
			"ollamaOnline": ollamaOnline,
			"ollamaModels": ollamaModels,
			"cursorAgent":  cursorAvail,
		})
	})

	http.HandleFunc("/api/llm/config", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			Providers []LLMProvider `json:"providers"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		settingsMu.Lock()
		appSettings.LLMProviders = req.Providers
		settingsMu.Unlock()
		saveCurrentSettings()
		logSection("LLM", fmt.Sprintf("config updated: %d providers", len(req.Providers)))
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]bool{"ok": true})
	})

	http.HandleFunc("/api/llm/models", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodGet {
			http.Error(w, "method not allowed", 405)
			return
		}
		endpoint := r.URL.Query().Get("endpoint")
		if endpoint == "" {
			for _, p := range getLLMProviders() {
				if p.Name == "ollama" {
					endpoint = p.Endpoint
					break
				}
			}
		}
		if endpoint == "" {
			endpoint = "http://localhost:11434"
		}
		models, err := ollamaListModels(endpoint)
		if err != nil {
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(map[string]interface{}{"models": []string{}, "error": err.Error()})
			return
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{"models": models})
	})

	http.HandleFunc("/api/version", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodGet {
			http.Error(w, "method not allowed", 405)
			return
		}
		v := getVersionInfo()
		splashLine := v.display
		if v.buildDate != "" {
			splashLine += "  |  built " + v.buildDate
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{
			"version":    v.version,
			"buildDate":  v.buildDate,
			"gitRev":     v.gitRev,
			"gitBranch":  v.gitBranch,
			"gitDirty":   v.gitDirty,
			"releaseTag": v.releaseTag,
			"splashLine": splashLine,
		})
	})

	http.HandleFunc("/api/sources", func(w http.ResponseWriter, r *http.Request) {
		logSection("SOURCES", r.Method+" /api/sources")
		if r.Method == http.MethodGet {
			paths := getSourcePaths()
			pathStatus := make([]map[string]interface{}, 0, len(paths))
			statTimeout := 3 * time.Second
			for _, p := range paths {
				valid := true
				reason := ""
				suggestedPath := ""
				done := make(chan struct{})
				var statErr error
				go func() {
					_, statErr = os.Stat(p)
					close(done)
				}()
				select {
				case <-done:
					if statErr != nil {
						valid = false
						reason = statErr.Error()
						lower := strings.ToLower(reason)
						if strings.Contains(lower, "cannot find") || strings.Contains(lower, "no such file") || strings.Contains(lower, "does not exist") {
							if len(p) >= 2 && p[1] == ':' {
								reason = "path not found (drive may not exist: " + string(p[0]) + ":)"
								if runtime.GOOS == "windows" && len(p) > 2 {
									rest := strings.TrimPrefix(p[2:], "\\")
									rest = strings.TrimPrefix(rest, "/")
								driveLoop:
									for _, d := range "ABCDEFGHIJKLMNOPQRSTUVWXYZ" {
										alt := string(d) + ":\\" + rest
										if strings.EqualFold(alt, p) {
											continue
										}
										altDone := make(chan struct{})
										var altErr error
										go func() {
											_, altErr = os.Stat(alt)
											close(altDone)
										}()
										select {
										case <-altDone:
											if altErr == nil {
												suggestedPath = alt
												break driveLoop
											}
										case <-time.After(statTimeout):
											// skip this drive, try next
										}
									}
								}
							}
						}
					}
				case <-time.After(statTimeout):
					valid = false
					reason = "timeout (path slow or unreachable - check drive)"
				}
				ps := map[string]interface{}{"path": p, "valid": valid, "reason": reason}
				if suggestedPath != "" {
					ps["suggestedPath"] = suggestedPath
				}
				pathStatus = append(pathStatus, ps)
			}
			settingsMu.RLock()
			outPath := appSettings.OutputPath
			settingsMu.RUnlock()
			if outPath == "" && len(paths) > 0 {
				outPath = filepath.Join(paths[0], "sorted_txt")
			}
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(map[string]interface{}{
				"paths":      paths,
				"indexPath":  getIndexPath(),
				"outputPath": outPath,
				"pathStatus": pathStatus,
			})
			return
		}
		if r.Method == http.MethodPost {
			var req struct {
				Action  string `json:"action"`
				Path    string `json:"path"`
				Index   int    `json:"index"`
				OldPath string `json:"oldPath"`
				NewPath string `json:"newPath"`
			}
			if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
				http.Error(w, err.Error(), 400)
				return
			}
			req.Path = strings.TrimSpace(req.Path)
			req.OldPath = strings.TrimSpace(req.OldPath)
			req.NewPath = strings.TrimSpace(req.NewPath)
			pathConfig.Lock()
			paths := pathConfig.sourcePaths
			switch req.Action {
			case "replace":
				if req.OldPath == "" || req.NewPath == "" {
					pathConfig.Unlock()
					http.Error(w, "oldPath and newPath required", 400)
					return
				}
				for i, px := range paths {
					if strings.EqualFold(px, req.OldPath) {
						paths[i] = req.NewPath
						logSection("SOURCES", "replaced: "+req.OldPath+" -> "+req.NewPath)
						break
					}
				}
			case "add":
				if req.Path == "" {
					pathConfig.Unlock()
					http.Error(w, "path required", 400)
					return
				}
				for _, p := range paths {
					if strings.EqualFold(p, req.Path) {
						pathConfig.Unlock()
						http.Error(w, "path already exists", 409)
						return
					}
				}
				paths = append(paths, req.Path)
				logSection("SOURCES", "added: "+req.Path)
			case "remove":
				if req.Index >= 0 && req.Index < len(paths) {
					logSection("SOURCES", "removed: "+paths[req.Index])
					paths = append(paths[:req.Index], paths[req.Index+1:]...)
				}
			default:
				pathConfig.Unlock()
				http.Error(w, "unknown action: "+req.Action, 400)
				return
			}
			pathConfig.sourcePaths = paths
			pathConfig.Unlock()

			settingsMu.Lock()
			appSettings.SourcePaths = paths
			if len(paths) > 0 {
				appSettings.VfxRoot = paths[0]
			}
			settingsMu.Unlock()

			settingsPath := filepath.Join(exeDir(), "shader-preview-settings.json")
			settingsMu.RLock()
			sData, _ := json.MarshalIndent(appSettings, "", "  ")
			settingsMu.RUnlock()
			os.WriteFile(settingsPath, sData, 0644)

			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(map[string]interface{}{
				"ok":    true,
				"paths": paths,
			})
			return
		}
		http.Error(w, "method not allowed", 405)
	})

	http.HandleFunc("/api/hash", func(w http.ResponseWriter, r *http.Request) {
		path := r.URL.Query().Get("path")
		if path == "" {
			http.Error(w, "path required", 400)
			return
		}
		data, err := os.ReadFile(path)
		if err != nil {
			http.Error(w, err.Error(), 404)
			return
		}
		h := sha256.Sum256(data)
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]string{
			"hash": hex.EncodeToString(h[:]),
			"path": path,
		})
	})

	ps1Path := ""
	if exe, err := os.Executable(); err == nil {
		if p := filepath.Join(filepath.Dir(exe), "shader-index.ps1"); func() bool { _, err := os.Stat(p); return err == nil }() {
			ps1Path = p
		}
	}
	if ps1Path == "" {
		if wd, err := os.Getwd(); err == nil {
			if p := filepath.Join(wd, "shader-index.ps1"); func() bool { _, err := os.Stat(p); return err == nil }() {
				ps1Path = p
			}
		}
	}

	http.HandleFunc("/api/paths", func(w http.ResponseWriter, r *http.Request) {
		idx, vfx := getIndexPath(), getVfxRoot()
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{
			"indexPath":    idx,
			"settingsPath": settingsPath,
			"vfxRoot":      vfx,
			"ps1Path":      ps1Path,
			"source":       filepath.Join(vfx, "Source_Untouched"),
			"sortedTxt":    filepath.Join(vfx, "sorted_txt"),
			"sortedIsf":    filepath.Join(vfx, "sorted_isf"),
			"sourcePaths":  getSourcePaths(),
		})
	})

	http.HandleFunc("/api/open-folder", func(w http.ResponseWriter, r *http.Request) {
		folder := r.URL.Query().Get("folder")
		rawPath := r.URL.Query().Get("path")
		vfx := getVfxRoot()
		var dir string
		if rawPath != "" {
			rawPath = strings.ReplaceAll(rawPath, "|", `\`)
			dir = rawPath
		} else {
			switch folder {
			case "sortedIsf", "isf":
				dir = filepath.Join(vfx, "sorted_isf")
			case "sortedTxt", "glsl":
				dir = filepath.Join(vfx, "sorted_txt")
			case "source":
				dir = filepath.Join(vfx, "Source_Untouched")
			case "vfxRoot":
				dir = vfx
			default:
				http.Error(w, "folder or path required", 400)
				return
			}
		}
		if _, err := os.Stat(dir); os.IsNotExist(err) {
			if err := os.MkdirAll(dir, 0755); err != nil {
				http.Error(w, "mkdir: "+err.Error(), 500)
				return
			}
		}
		var cmd *exec.Cmd
		switch runtime.GOOS {
		case "windows":
			cmd = exec.Command("explorer", dir)
		case "darwin":
			cmd = exec.Command("open", dir)
		default:
			cmd = exec.Command("xdg-open", dir)
		}
		if err := cmd.Start(); err != nil {
			http.Error(w, "open: "+err.Error(), 500)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]string{"ok": "true", "path": dir})
	})

	http.HandleFunc("/api/browse-folder", func(w http.ResponseWriter, r *http.Request) {
		logSection("SOURCES", "POST /api/browse-folder")
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			Prefix string `json:"prefix"`
		}
		if r.Body != nil {
			_ = json.NewDecoder(r.Body).Decode(&req)
		}
		prefix := strings.TrimSpace(req.Prefix)
		if prefix == "" {
			if runtime.GOOS == "windows" {
				prefix = ""
			} else {
				prefix = "/"
			}
		} else {
			logSection("SOURCES", "browse-folder prefix: "+prefix)
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]string{"path": prefix})
	})

	http.HandleFunc("/api/list-dirs", func(w http.ResponseWriter, r *http.Request) {
		logSection("SOURCES", "POST /api/list-dirs")
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			Path string `json:"path"`
		}
		if r.Body != nil {
			if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
				http.Error(w, "invalid JSON: "+err.Error(), 400)
				return
			}
		}
		path := strings.TrimSpace(req.Path)
		var dirs []string
		var parent string
		if path == "" {
			parent = ""
			if runtime.GOOS == "windows" {
				for letter := 'A'; letter <= 'Z'; letter++ {
					drive := string(letter) + ":\\"
					if _, err := os.Stat(drive); err == nil {
						dirs = append(dirs, drive)
					}
				}
			} else {
				entries, err := os.ReadDir("/")
				if err != nil {
					http.Error(w, "list-dirs: "+err.Error(), 500)
					return
				}
				for _, e := range entries {
					if !e.IsDir() || strings.HasPrefix(e.Name(), ".") {
						continue
					}
					dirs = append(dirs, e.Name())
					if len(dirs) >= 100 {
						break
					}
				}
			}
		} else {
			parent = filepath.Dir(path)
			if parent == path {
				parent = ""
			}
			entries, err := os.ReadDir(path)
			if err != nil {
				http.Error(w, "list-dirs: "+err.Error(), 500)
				return
			}
			for _, e := range entries {
				if !e.IsDir() || strings.HasPrefix(e.Name(), ".") {
					continue
				}
				dirs = append(dirs, e.Name())
				if len(dirs) >= 100 {
					break
				}
			}
		}
		sort.Strings(dirs)
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{"dirs": dirs, "parent": parent})
	})

	http.HandleFunc("/api/index", func(w http.ResponseWriter, r *http.Request) {
		logSection("INDEX", "GET /api/index from "+r.RemoteAddr)
		arr, err := readIndex()
		if err != nil {
			logSection("INDEX", "ERROR reading index: "+err.Error())
			http.Error(w, err.Error(), 500)
			return
		}
		paths := getSourcePaths()
		var validBases []string
		for _, p := range paths {
			if p == "" {
				continue
			}
			if _, err := os.Stat(p); err == nil {
				abs, _ := filepath.Abs(p)
				if abs != "" {
					validBases = append(validBases, abs)
				}
			}
		}
		if len(validBases) > 0 {
			filtered := make([]ShaderEntry, 0, len(arr))
			for i := range arr {
				entryPath := strings.ReplaceAll(arr[i].Path, "|", string(filepath.Separator))
				if entryPath == "" {
					continue
				}
				absEntry, err := filepath.Abs(entryPath)
				if err != nil {
					continue
				}
				under := false
				for _, base := range validBases {
					rel, err := filepath.Rel(base, absEntry)
					if err != nil {
						continue
					}
					if !strings.HasPrefix(rel, "..") && !strings.Contains(rel, ".."+string(filepath.Separator)) {
						under = true
						break
					}
				}
				if under {
					filtered = append(filtered, arr[i])
				}
			}
			arr = filtered
			logSection("INDEX", fmt.Sprintf("filtered to %d entries under existing paths (excluded missing paths)", len(arr)))
		}
		extFiltered := make([]ShaderEntry, 0, len(arr))
		for i := range arr {
			ext := strings.ToLower(filepath.Ext(arr[i].Path))
			if shaderExts[ext] {
				extFiltered = append(extFiltered, arr[i])
			}
		}
		arr = extFiltered
		bannedFiltered := make([]ShaderEntry, 0, len(arr))
		for i := range arr {
			if !isBannedShader(arr[i].Path) && !isBannedShader(arr[i].Name) {
				bannedFiltered = append(bannedFiltered, arr[i])
			}
		}
		arr = bannedFiltered
		logSection("INDEX", fmt.Sprintf("serving %d shader entries", len(arr)))
		w.Header().Set("Content-Type", "application/json")
		w.Header().Set("Cache-Control", "no-cache, no-store")
		json.NewEncoder(w).Encode(arr)
	})

	http.HandleFunc("/api/update", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		var req struct {
			ID          int          `json:"id"`
			Name        string       `json:"name"`
			Tags        []string     `json:"tags"`
			Sets        []string     `json:"sets"`
			Notes       *string      `json:"notes"`
			Category    string       `json:"category"`
			Favorite    *bool        `json:"favorite"`
			Color       *string      `json:"color"`
			ParamRanges []ParamRange `json:"paramRanges"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		arr, err := readIndex()
		if err != nil {
			http.Error(w, err.Error(), 500)
			return
		}
		found := false
		for i := range arr {
			if arr[i].ID == req.ID {
				if req.Name != "" {
					safe := regexp.MustCompile(`[^\w\-]`).ReplaceAllString(req.Name, "")
					ext := ".txt"
					if idx := strings.LastIndex(arr[i].FixedName, "."); idx >= 0 {
						ext = arr[i].FixedName[idx:]
					}
					arr[i].FixedName = safe + ext
					arr[i].Name = safe
				}
				if req.Tags != nil {
					arr[i].Tags = req.Tags
				}
				if req.Sets != nil {
					arr[i].Sets = req.Sets
				}
				if req.Notes != nil {
					arr[i].Notes = *req.Notes
				}
				validCat := map[string]bool{
					"plasma": true, "fractal": true, "grid": true, "psychedelic": true,
					"3d": true, "particles": true, "abstract": true, "misc": true,
					"noise": true, "tunnel": true, "space": true, "water": true,
					"color": true, "geometric": true, "concept": true, "generator": true,
					"test": true, "trash": true,
				}
				if req.Category != "" && validCat[strings.ToLower(req.Category)] {
					arr[i].Category = strings.ToLower(req.Category)
				}
				if req.Favorite != nil {
					arr[i].Favorite = *req.Favorite
				}
				if req.Color != nil {
					arr[i].Color = *req.Color
				}
				if req.ParamRanges != nil {
					arr[i].ParamRanges = req.ParamRanges
				}
				found = true
				break
			}
		}
		if !found {
			http.Error(w, "id not found", 404)
			return
		}
		if err := writeIndex(arr); err != nil {
			http.Error(w, err.Error(), 500)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		w.Write([]byte(`{"ok":true}`))
	})

	http.HandleFunc("/api/shader/rename", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		var req struct {
			ID      int    `json:"id"`
			NewName string `json:"newName"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		safe := regexp.MustCompile(`[^\w\-. ]`).ReplaceAllString(req.NewName, "")
		if safe == "" {
			http.Error(w, "invalid name", 400)
			return
		}
		arr, err := readIndex()
		if err != nil {
			http.Error(w, err.Error(), 500)
			return
		}
		for i := range arr {
			if arr[i].ID != req.ID {
				continue
			}
			oldPath := strings.ReplaceAll(arr[i].Path, "|", string(filepath.Separator))
			dir := filepath.Dir(oldPath)
			ext := filepath.Ext(oldPath)
			if ext == "" {
				ext = ".fs"
			}
			newPath := filepath.Join(dir, safe+ext)
			if oldPath != newPath {
				if err := os.Rename(oldPath, newPath); err != nil {
					http.Error(w, "rename file: "+err.Error(), 500)
					return
				}
			}
			arr[i].Path = newPath
			arr[i].Name = safe
			arr[i].FixedName = safe + ext
			if err := writeIndex(arr); err != nil {
				http.Error(w, err.Error(), 500)
				return
			}
			logSection("SHADER", "renamed to "+newPath)
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(map[string]interface{}{"ok": true, "path": newPath, "name": safe})
			return
		}
		http.Error(w, "id not found", 404)
	})

	http.HandleFunc("/api/shader/bulk-rename", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		var req struct {
			Renames []struct {
				ID      int    `json:"id"`
				NewName string `json:"newName"`
			} `json:"renames"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		if len(req.Renames) == 0 {
			w.Header().Set("Content-Type", "application/json")
			w.Write([]byte(`{"ok":true,"renamed":0,"errors":[]}`))
			return
		}
		arr, err := readIndex()
		if err != nil {
			http.Error(w, err.Error(), 500)
			return
		}
		safeRe := regexp.MustCompile(`[^\w\-. ]`)
		type renameResult struct {
			ID      int    `json:"id"`
			OldName string `json:"oldName"`
			NewName string `json:"newName"`
			Error   string `json:"error,omitempty"`
		}
		var results []renameResult
		renamed := 0
		var errs []string
		for _, rn := range req.Renames {
			safe := safeRe.ReplaceAllString(rn.NewName, "")
			if safe == "" {
				errs = append(errs, fmt.Sprintf("id %d: invalid name", rn.ID))
				continue
			}
			found := false
			for i := range arr {
				if arr[i].ID != rn.ID {
					continue
				}
				found = true
				oldPath := strings.ReplaceAll(arr[i].Path, "|", string(filepath.Separator))
				dir := filepath.Dir(oldPath)
				ext := filepath.Ext(oldPath)
				if ext == "" {
					ext = ".fs"
				}
				newPath := filepath.Join(dir, safe+ext)
				if oldPath != newPath {
					if err := os.Rename(oldPath, newPath); err != nil {
						errs = append(errs, fmt.Sprintf("id %d: %s", rn.ID, err.Error()))
						results = append(results, renameResult{ID: rn.ID, OldName: arr[i].Name, NewName: safe, Error: err.Error()})
						break
					}
				}
				oldName := arr[i].Name
				arr[i].Path = newPath
				arr[i].Name = safe
				arr[i].FixedName = safe + ext
				renamed++
				results = append(results, renameResult{ID: rn.ID, OldName: oldName, NewName: safe})
				break
			}
			if !found {
				errs = append(errs, fmt.Sprintf("id %d: not found", rn.ID))
			}
		}
		if err := writeIndex(arr); err != nil {
			http.Error(w, err.Error(), 500)
			return
		}
		logSection("SHADER", fmt.Sprintf("bulk-rename: %d renamed, %d errors", renamed, len(errs)))
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{
			"ok":      true,
			"renamed": renamed,
			"errors":  errs,
			"results": results,
		})
	})

	http.HandleFunc("/api/shader/tag-scan", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		arr, err := readIndex()
		if err != nil {
			http.Error(w, err.Error(), 500)
			return
		}

		uniformRe := regexp.MustCompile(`(?i)^\s*uniform\s+(float|int|bool|vec[234]|sampler2D)\s+(\w+)\s*;`)
		mouseNames := map[string]bool{"mouse": true, "mousex": true, "mousey": true, "umouse": true}
		isfMouseRe := regexp.MustCompile(`(?i)"NAME"\s*:\s*"(mouse|mouseX|mouseY|uMouse)"`)

		roliGoodName := regexp.MustCompile(`(?i)plasma|glow|pulse|wave|colou?r|gradient|kaleid|pattern|grid|neon|flow|drift|morph|swirl|spiral|ring|circle|dot|ball|orb|fade|flash|strobe|beat|rgb|hue|rainbow|spectrum|flame|fire|lava|aurora|crystal|prism|checker|bokeh|bloom|blur|mandala|voronoi|tessellat|mosaic|tile|hex|symmetr|mirror|fractal|noise|smoke|fog|cloud|ripple|warp|electric|cyber|laser|led|matrix|pixel|retro|8bit|glitch|vhs`)
		roliExcludeName := regexp.MustCompile(`(?i)\bray\s*march|sdf|text|font|letter|char|terrain|landscape|city|building|scene|room|interior|exterior|model|mesh|geometry3d`)
		roliGoodCat := map[string]bool{"plasma": true, "color": true, "geometric": true, "psychedelic": true, "abstract": true, "noise": true, "fractal": true, "particles": true, "water": true}
		roliExcludeCat := map[string]bool{"3d": true, "concept": true}

		mouseTagged := 0
		roliTagged := 0
		uniformsFilled := 0
		scanned := 0

		for i := range arr {
			e := &arr[i]
			osPath := strings.ReplaceAll(e.Path, "|", string(filepath.Separator))
			data, readErr := os.ReadFile(osPath)
			if readErr != nil {
				continue
			}
			scanned++
			src := string(data)

			// Extract all uniforms from source
			var uniforms []string
			hasMouse := false
			for _, line := range strings.Split(src, "\n") {
				m := uniformRe.FindStringSubmatch(line)
				if m != nil {
					uName := m[2]
					uniforms = append(uniforms, uName)
					if mouseNames[strings.ToLower(uName)] {
						hasMouse = true
					}
				}
			}
			// ISF JSON header mouse inputs
			if isfMouseRe.MatchString(src) {
				hasMouse = true
			}
			// Also check for mouse usage in code without formal uniform declaration
			if !hasMouse {
				if strings.Contains(src, "mouseX") || strings.Contains(src, "mouseY") || strings.Contains(src, "uMouse") {
					hasMouse = true
				}
			}

			// Update uniforms field
			if len(uniforms) > 0 && len(e.Uniforms) == 0 {
				e.Uniforms = uniforms
				uniformsFilled++
			}

			tags := make([]string, len(e.Tags))
			copy(tags, e.Tags)
			changed := false

			// Tag mouse-interactive
			if hasMouse {
				hasTag := false
				for _, t := range tags {
					if t == "mouse-interactive" {
						hasTag = true
						break
					}
				}
				if !hasTag {
					tags = append(tags, "mouse-interactive")
					changed = true
					mouseTagged++
				}
			}

			// Roliblock scoring
			nm := strings.ToLower(e.FixedName + " " + e.Name + " " + e.Category)
			cat := strings.ToLower(e.Category)
			isRoli := false
			if hasMouse {
				isRoli = true
			}
			if roliGoodCat[cat] {
				isRoli = true
			}
			if roliGoodName.MatchString(nm) {
				isRoli = true
			}
			// Exclusions
			if roliExcludeCat[cat] {
				isRoli = false
			}
			if roliExcludeName.MatchString(nm) {
				isRoli = false
			}
			if isRoli {
				hasTag := false
				for _, t := range tags {
					if t == "roliblock" {
						hasTag = true
						break
					}
				}
				if !hasTag {
					tags = append(tags, "roliblock")
					changed = true
					roliTagged++
				}
			}

			if changed {
				e.Tags = tags
			}
		}

		if err := writeIndex(arr); err != nil {
			http.Error(w, err.Error(), 500)
			return
		}
		logSection("SHADER", fmt.Sprintf("tag-scan: scanned %d, mouse-interactive=%d, roliblock=%d, uniforms-filled=%d", scanned, mouseTagged, roliTagged, uniformsFilled))
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{
			"ok":              true,
			"scanned":         scanned,
			"mouseTagged":     mouseTagged,
			"roliTagged":      roliTagged,
			"uniformsFilled":  uniformsFilled,
		})
	})

	http.HandleFunc("/api/shader/move", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		var req struct {
			ID       int    `json:"id"`
			Category string `json:"category"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		cat := strings.ToLower(strings.TrimSpace(req.Category))
		if cat == "" {
			http.Error(w, "category required", 400)
			return
		}
		arr, err := readIndex()
		if err != nil {
			http.Error(w, err.Error(), 500)
			return
		}
		for i := range arr {
			if arr[i].ID != req.ID {
				continue
			}
			oldPath := strings.ReplaceAll(arr[i].Path, "|", string(filepath.Separator))
			oldDir := filepath.Dir(oldPath)
			filename := filepath.Base(oldPath)
			srcPaths := getSourcePaths()
			root := ""
			for _, sp := range srcPaths {
				if strings.HasPrefix(oldPath, sp) {
					root = sp
					break
				}
			}
			if root == "" && len(srcPaths) > 0 {
				root = srcPaths[0]
			}
			newDir := filepath.Join(root, cat)
			if err := os.MkdirAll(newDir, 0755); err != nil {
				http.Error(w, "mkdir: "+err.Error(), 500)
				return
			}
			newPath := filepath.Join(newDir, filename)
			if newPath == oldPath {
				arr[i].Category = cat
				writeIndex(arr)
				w.Header().Set("Content-Type", "application/json")
				json.NewEncoder(w).Encode(map[string]interface{}{"ok": true, "path": newPath})
				return
			}
			if _, err := os.Stat(newPath); err == nil {
				stem := strings.TrimSuffix(filename, filepath.Ext(filename))
				newPath = filepath.Join(newDir, stem+"-moved"+filepath.Ext(filename))
			}
			if err := os.Rename(oldPath, newPath); err != nil {
				http.Error(w, "move file: "+err.Error(), 500)
				return
			}
			// Clean up empty source directory
			if remaining, _ := os.ReadDir(oldDir); len(remaining) == 0 && oldDir != root {
				os.Remove(oldDir)
			}
			arr[i].Path = newPath
			arr[i].Category = cat
			if err := writeIndex(arr); err != nil {
				http.Error(w, err.Error(), 500)
				return
			}
			logSection("SHADER", fmt.Sprintf("moved to %s (%s)", cat, newPath))
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(map[string]interface{}{"ok": true, "path": newPath, "category": cat})
			return
		}
		http.Error(w, "id not found", 404)
	})

	http.HandleFunc("/api/shader/save", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		var req struct {
			Path        string       `json:"path"`
			Content     string       `json:"content"`
			ParamRanges []ParamRange `json:"paramRanges"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		path := strings.ReplaceAll(req.Path, "|", string(filepath.Separator))
		logSection("SHADER", "saving "+path+fmt.Sprintf(" (%d bytes)", len(req.Content)))
		if err := os.MkdirAll(filepath.Dir(path), 0755); err != nil {
			logSection("SHADER", "ERROR mkdir: "+err.Error())
			http.Error(w, "mkdir: "+err.Error(), 500)
			return
		}
		if err := os.WriteFile(path, []byte(req.Content), 0644); err != nil {
			logSection("SHADER", "ERROR write: "+err.Error())
			http.Error(w, err.Error(), 500)
			return
		}
		if len(req.ParamRanges) > 0 {
			arr, err := readIndex()
			if err == nil {
				normPath := filepath.Clean(path)
				for i := range arr {
					if filepath.Clean(strings.ReplaceAll(arr[i].Path, "|", string(filepath.Separator))) == normPath {
						arr[i].ParamRanges = req.ParamRanges
						writeIndex(arr)
						break
					}
				}
			}
		}
		settingsMu.RLock()
		doGit := appSettings.EnableGit
		settingsMu.RUnlock()
		if doGit {
			gitCommitSave(filepath.Dir(path), path)
		}
		logSection("SHADER", "saved OK")
		w.Header().Set("Content-Type", "application/json")
		w.Write([]byte(`{"ok":true}`))
	})

	http.HandleFunc("/api/git-commit", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		var req struct {
			Path string `json:"path"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		path := strings.ReplaceAll(req.Path, "|", string(filepath.Separator))
		settingsMu.RLock()
		doGit := appSettings.EnableGit
		settingsMu.RUnlock()
		if !doGit {
			w.Header().Set("Content-Type", "application/json")
			w.Write([]byte(`{"ok":true,"message":"git disabled"}`))
			return
		}
		gitCommitSave(filepath.Dir(path), path)
		w.Header().Set("Content-Type", "application/json")
		w.Write([]byte(`{"ok":true}`))
	})

	http.HandleFunc("/api/shader/delete", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		var req struct {
			ID      int      `json:"id"`
			Paths   []string `json:"paths"`
			Confirm bool     `json:"confirm"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		if req.ID <= 0 {
			http.Error(w, "id required", 400)
			return
		}
		if !req.Confirm {
			http.Error(w, "confirm: true required to delete shader files from disk", 400)
			return
		}
		paths := req.Paths
		if len(paths) > 1 {
			logSection("CURSOR", "shader/delete: refusing bulk delete (max 1 path per request)")
			http.Error(w, "only one path per request allowed to prevent accidental bulk delete", 400)
			return
		}
		for _, p := range paths {
			if p == "" {
				continue
			}
			path := strings.ReplaceAll(p, "|", string(filepath.Separator))
			absPath, err := filepath.Abs(path)
			if err != nil {
				continue
			}
			logSection("SHADER", "delete file: "+absPath)
			if err := os.Remove(absPath); err != nil && !os.IsNotExist(err) {
				log.Printf("shader/delete: remove %s: %v", absPath, err)
			}
		}
		arr, err := readIndex()
		if err != nil {
			http.Error(w, "index: "+err.Error(), 500)
			return
		}
		newArr := make([]ShaderEntry, 0, len(arr))
		for _, e := range arr {
			if e.ID != req.ID {
				newArr = append(newArr, e)
			}
		}
		if err := writeIndex(newArr); err != nil {
			http.Error(w, "write index: "+err.Error(), 500)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]string{"message": "Deleted"})
	})

	http.HandleFunc("/api/output/status", func(w http.ResponseWriter, r *http.Request) {
		outputProc.Lock()
		running := outputProc.cmd != nil
		spout := outputProc.spout
		ndi := outputProc.ndi
		vhs := outputProc.vhs
		ts := outputProc.testsignal
		outputProc.Unlock()
		w.Header().Set("Content-Type", "application/json")
		enc := json.NewEncoder(w)
		enc.Encode(map[string]interface{}{"running": running, "spout": spout, "ndi": ndi, "vhs": vhs, "testsignal": ts})
	})

	http.HandleFunc("/api/output/start", func(w http.ResponseWriter, r *http.Request) {
		logSection("OUTPUT", "POST /api/output/start")
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			Path       string `json:"path"`
			Mode       string `json:"mode"`
			Width      int    `json:"width"`
			Height     int    `json:"height"`
			Overlay    bool   `json:"overlay"`
			Spout      bool   `json:"spout"`
			Ndi        bool   `json:"ndi"`
			Vhs        bool   `json:"vhs"`
			TestSignal string `json:"testsignal"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		path := strings.ReplaceAll(req.Path, "|", string(filepath.Separator))
		if path == "" && req.TestSignal == "" {
			http.Error(w, "path or testsignal required", 400)
			return
		}
		outputProc.Lock()
		if outputProc.cmd != nil {
			outputProc.Unlock()
			http.Error(w, "output already running", 409)
			return
		}
		exeDir := ""
		if exe, err := os.Executable(); err == nil {
			exeDir = filepath.Dir(exe)
		}
		wireOut := filepath.Join(exeDir, "wire-output.exe")
		if _, err := os.Stat(wireOut); err != nil {
			outputProc.Unlock()
			http.Error(w, "wire-output.exe not found (build with CGO/gcc)", 404)
			return
		}
		width, height := req.Width, req.Height
		if width < 1 {
			width = 1920
		}
		if height < 1 {
			height = 1080
		}
		res := strconv.Itoa(width) + "x" + strconv.Itoa(height)
		args := []string{"-shader", path, "-res", res}
		if req.Overlay {
			args = append(args, "-overlay")
		}
		if req.Spout {
			args = append(args, "-spout")
		}
		if req.Ndi {
			args = append(args, "-ndi")
		}
		if req.Vhs {
			args = append(args, "-vhs")
		}
		if req.TestSignal != "" {
			args = append(args, "-testsignal", req.TestSignal)
		}
		args = append(args, "-name", fmt.Sprintf("Macroverse-%d", os.Getpid()))
		cmd := exec.Command(wireOut, args...)
		cmd.Stdout = os.Stdout
		cmd.Stderr = os.Stderr
		if err := cmd.Start(); err != nil {
			outputProc.Unlock()
			http.Error(w, err.Error(), 500)
			return
		}
		outputProc.cmd = cmd
		outputProc.spout = req.Spout
		outputProc.ndi = req.Ndi
		outputProc.vhs = req.Vhs
		outputProc.testsignal = req.TestSignal
		outputProc.Unlock()
		go func() {
			cmd.Wait()
			outputProc.Lock()
			if outputProc.cmd == cmd {
				outputProc.cmd = nil
				outputProc.spout = false
				outputProc.ndi = false
				outputProc.vhs = false
				outputProc.testsignal = ""
			}
			outputProc.Unlock()
		}()
		w.Header().Set("Content-Type", "application/json")
		w.Write([]byte(`{"ok":true,"pid":` + strconv.Itoa(cmd.Process.Pid) + `}`))
	})

	http.HandleFunc("/api/output/stop", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		outputProc.Lock()
		cmd := outputProc.cmd
		outputProc.cmd = nil
		outputProc.spout = false
		outputProc.ndi = false
		outputProc.vhs = false
		outputProc.testsignal = ""
		outputProc.Unlock()
		if cmd == nil {
			w.Header().Set("Content-Type", "application/json")
			w.Write([]byte(`{"ok":true}`))
			return
		}
		cmd.Process.Kill()
		w.Header().Set("Content-Type", "application/json")
		w.Write([]byte(`{"ok":true}`))
	})

	handleOutputToggle := func(w http.ResponseWriter, r *http.Request, flagName string) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			Enable bool `json:"enable"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		logSection("OUTPUT", fmt.Sprintf("POST /api/output/%s enable=%v", flagName, req.Enable))

		if !req.Enable {
			outputProc.Lock()
			cmd := outputProc.cmd
			outputProc.cmd = nil
			outputProc.spout = false
			outputProc.ndi = false
			outputProc.vhs = false
			outputProc.testsignal = ""
			outputProc.Unlock()
			if cmd != nil {
				cmd.Process.Kill()
			}
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(map[string]interface{}{"ok": true})
			return
		}

		outputProc.Lock()
		if outputProc.cmd != nil {
			outputProc.Unlock()
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(map[string]interface{}{"ok": true, "message": "output already running"})
			return
		}
		outputProc.Unlock()

		exeDir := ""
		if exe, err := os.Executable(); err == nil {
			exeDir = filepath.Dir(exe)
		}
		wireOut := filepath.Join(exeDir, "wire-output.exe")
		if runtime.GOOS != "windows" {
			wireOut = filepath.Join(exeDir, "wire-output")
		}
		if _, err := os.Stat(wireOut); err != nil {
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(map[string]interface{}{
				"ok":    false,
				"error": "wire-output binary not found at " + wireOut + ". Build it with CGO for Spout/NDI support, or install it next to the Macroverse42 binary.",
			})
			return
		}

		pathConfig.RLock()
		idxPath := pathConfig.indexPath
		pathConfig.RUnlock()

		shaderPath := ""
		idxData, _ := os.ReadFile(idxPath)
		var entries []map[string]interface{}
		json.Unmarshal(idxData, &entries)
		if len(entries) > 0 {
			if p, ok := entries[0]["path"].(string); ok {
				shaderPath = p
			}
		}

		settingsMu.RLock()
		pw, ph := appSettings.PreviewWidth, appSettings.PreviewHeight
		settingsMu.RUnlock()
		if pw < 1 {
			pw = 1920
		}
		if ph < 1 {
			ph = 1080
		}

		args := []string{"-res", fmt.Sprintf("%dx%d", pw, ph)}
		if shaderPath != "" {
			args = append([]string{"-shader", shaderPath}, args...)
		}
		isSpout := flagName == "spout"
		isNdi := flagName == "ndi"
		if isSpout {
			args = append(args, "-spout")
		}
		if isNdi {
			args = append(args, "-ndi")
		}
		args = append(args, "-name", fmt.Sprintf("Macroverse-%d", os.Getpid()))

		cmd := exec.Command(wireOut, args...)
		cmd.Stdout = os.Stdout
		cmd.Stderr = os.Stderr
		if err := cmd.Start(); err != nil {
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(map[string]interface{}{"ok": false, "error": "start: " + err.Error()})
			return
		}
		outputProc.Lock()
		outputProc.cmd = cmd
		outputProc.spout = isSpout
		outputProc.ndi = isNdi
		outputProc.Unlock()
		go func() {
			cmd.Wait()
			outputProc.Lock()
			if outputProc.cmd == cmd {
				outputProc.cmd = nil
				outputProc.spout = false
				outputProc.ndi = false
				outputProc.vhs = false
				outputProc.testsignal = ""
			}
			outputProc.Unlock()
		}()
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{"ok": true, "pid": cmd.Process.Pid})
	}

	http.HandleFunc("/api/output/spout", func(w http.ResponseWriter, r *http.Request) {
		handleOutputToggle(w, r, "spout")
	})

	http.HandleFunc("/api/output/ndi", func(w http.ResponseWriter, r *http.Request) {
		handleOutputToggle(w, r, "ndi")
	})

	// MacroCam virtual webcam output via MJPEG stream
	var macroCam struct {
		sync.Mutex
		enabled bool
		frame   []byte
		clients map[chan []byte]bool
	}
	macroCam.clients = make(map[chan []byte]bool)

	http.HandleFunc("/api/output/macrocam", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			Enable bool `json:"enable"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		macroCam.Lock()
		macroCam.enabled = req.Enable
		macroCam.Unlock()
		logSection("OUTPUT", fmt.Sprintf("MacroCam-%d %s", os.Getpid(), map[bool]string{true: "enabled", false: "disabled"}[req.Enable]))
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{
			"ok":        true,
			"name":      fmt.Sprintf("MacroCam-%d", os.Getpid()),
			"pid":       os.Getpid(),
			"streamUrl": fmt.Sprintf("http://localhost:%s/api/output/macrocam/stream", port),
		})
	})

	http.HandleFunc("/api/output/macrocam/frame", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		macroCam.Lock()
		if !macroCam.enabled {
			macroCam.Unlock()
			w.WriteHeader(204)
			return
		}
		data, err := io.ReadAll(r.Body)
		if err != nil {
			macroCam.Unlock()
			http.Error(w, err.Error(), 400)
			return
		}
		macroCam.frame = data
		for ch := range macroCam.clients {
			select {
			case ch <- data:
			default:
			}
		}
		macroCam.Unlock()
		w.WriteHeader(204)
	})

	http.HandleFunc("/api/output/macrocam/stream", func(w http.ResponseWriter, r *http.Request) {
		logSection("OUTPUT", "MacroCam MJPEG client connected: "+r.RemoteAddr)
		w.Header().Set("Content-Type", "multipart/x-mixed-replace; boundary=frame")
		w.Header().Set("Cache-Control", "no-cache")
		w.Header().Set("Connection", "keep-alive")
		w.Header().Set("Access-Control-Allow-Origin", "*")
		flusher, _ := w.(http.Flusher)
		ch := make(chan []byte, 2)
		macroCam.Lock()
		macroCam.clients[ch] = true
		macroCam.Unlock()
		defer func() {
			macroCam.Lock()
			delete(macroCam.clients, ch)
			macroCam.Unlock()
		}()
		ctx := r.Context()
		for {
			select {
			case <-ctx.Done():
				return
			case frame := <-ch:
				fmt.Fprintf(w, "--frame\r\nContent-Type: image/jpeg\r\nContent-Length: %d\r\n\r\n", len(frame))
				w.Write(frame)
				fmt.Fprint(w, "\r\n")
				if flusher != nil {
					flusher.Flush()
				}
			}
		}
	})

	http.HandleFunc("/api/git/add", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		var req struct {
			Path string `json:"path"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		if req.Path == "" {
			http.Error(w, "path required", 400)
			return
		}
		path := strings.ReplaceAll(req.Path, "|", string(filepath.Separator))
		root := os.Getenv("VFX_GLSL_ROOT")
		if root == "" {
			root = getVfxRoot()
		}
		cmd := exec.Command("git", "add", path)
		cmd.Dir = root
		out, err := cmd.CombinedOutput()
		if err != nil {
			http.Error(w, string(out)+err.Error(), 500)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		w.Write([]byte(`{"ok":true}`))
	})

	http.HandleFunc("/api/git/status", func(w http.ResponseWriter, r *http.Request) {
		root := os.Getenv("VFX_GLSL_ROOT")
		if root == "" {
			root = getVfxRoot()
		}
		cmd := exec.Command("git", "status", "--short")
		cmd.Dir = root
		out, err := cmd.Output()
		if err != nil {
			http.Error(w, string(out)+err.Error(), 500)
			return
		}
		w.Header().Set("Content-Type", "text/plain")
		w.Write(out)
	})

	http.HandleFunc("/api/git/commit", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		var req struct {
			Message string `json:"message"`
		}
		json.NewDecoder(r.Body).Decode(&req)
		if req.Message == "" {
			req.Message = "shader update"
		}
		root := os.Getenv("VFX_GLSL_ROOT")
		if root == "" {
			root = getVfxRoot()
		}
		cmd := exec.Command("git", "add", "-A")
		cmd.Dir = root
		cmd.Run()
		cmd = exec.Command("git", "commit", "-m", req.Message)
		cmd.Dir = root
		out, err := cmd.CombinedOutput()
		if err != nil {
			http.Error(w, string(out), 500)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		w.Write([]byte(`{"ok":true}`))
	})

	http.HandleFunc("/api/git/rollback", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		path := r.URL.Query().Get("path")
		if path == "" {
			http.Error(w, "path required", 400)
			return
		}
		path = strings.ReplaceAll(path, "|", string(filepath.Separator))
		root := os.Getenv("VFX_GLSL_ROOT")
		if root == "" {
			root = getVfxRoot()
		}
		rel, relErr := filepath.Rel(root, path)
		if relErr != nil {
			rel = path
		}
		cmd := exec.Command("git", "checkout", "--", rel)
		cmd.Dir = root
		out, err := cmd.CombinedOutput()
		if err != nil {
			http.Error(w, string(out), 500)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		w.Write([]byte(`{"ok":true}`))
	})

	http.HandleFunc("/api/open-in-explorer", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			Path string `json:"path"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil || req.Path == "" {
			http.Error(w, "path required", 400)
			return
		}
		path := strings.ReplaceAll(req.Path, "|", string(filepath.Separator))
		absPath, err := filepath.Abs(path)
		if err != nil {
			http.Error(w, "invalid path", 400)
			return
		}
		if _, err := os.Stat(absPath); err != nil {
			http.Error(w, "file not found", 404)
			return
		}
		if runtime.GOOS == "windows" {
			cmd := exec.Command("explorer", "/select,"+absPath)
			if err := cmd.Start(); err != nil {
				cmd = exec.Command("explorer", filepath.Dir(absPath))
				cmd.Start()
			}
		} else {
			dir := filepath.Dir(absPath)
			var cmd *exec.Cmd
			switch runtime.GOOS {
			case "darwin":
				cmd = exec.Command("open", dir)
			default:
				cmd = exec.Command("xdg-open", dir)
			}
			if cmd != nil {
				cmd.Start()
			}
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]string{"message": "Opened in explorer"})
	})

	http.HandleFunc("/api/github/status", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodGet {
			http.Error(w, "method not allowed", 405)
			return
		}
		cmd := exec.Command("gh", "auth", "status")
		out, err := cmd.CombinedOutput()
		loggedIn := err == nil
		user := ""
		if loggedIn {
			userCmd := exec.Command("gh", "api", "user", "-q", ".login")
			if uOut, uErr := userCmd.Output(); uErr == nil {
				user = strings.TrimSpace(string(uOut))
			}
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{
			"logged_in": loggedIn,
			"user":      user,
			"message":   strings.TrimSpace(string(out)),
		})
	})

	http.HandleFunc("/api/github/run", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		var req struct {
			Args []string `json:"args"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil || len(req.Args) == 0 {
			http.Error(w, "args required (e.g. [\"api\", \"repos/owner/repo\"])", 400)
			return
		}
		allowed := map[string]bool{"api": true, "auth": true, "repo": true, "pr": true, "issue": true}
		first := req.Args[0]
		if !allowed[first] {
			http.Error(w, "allowed first args: api, auth, repo, pr, issue", 400)
			return
		}
		if first == "auth" && (len(req.Args) < 2 || req.Args[1] != "status") {
			http.Error(w, "only gh auth status is allowed", 400)
			return
		}
		cmd := exec.Command("gh", req.Args...)
		out, err := cmd.CombinedOutput()
		exitCode := 0
		if err != nil {
			if exitErr, ok := err.(*exec.ExitError); ok {
				exitCode = exitErr.ExitCode()
			} else {
				http.Error(w, string(out)+err.Error(), 500)
				return
			}
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{
			"stdout":   string(out),
			"exitCode": exitCode,
		})
	})

	http.HandleFunc("/api/github/ai/fix", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		var req struct {
			Content string `json:"content"`
			Prompt  string `json:"prompt"`
			Path    string `json:"path"`
			Token   string `json:"token"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, "body required", 400)
			return
		}
		token := strings.TrimSpace(req.Token)
		if token == "" {
			settingsMu.RLock()
			token = appSettings.GitHubToken
			settingsMu.RUnlock()
		}
		if token == "" {
			http.Error(w, "GitHub token required (set in Settings or pass in body)", 401)
			return
		}
		prompt := strings.TrimSpace(req.Prompt)
		if prompt == "" {
			prompt = "Fix the following GLSL/ISF shader so it compiles. Return only the corrected shader source, no explanation."
		}
		fullContent := prompt + "\n\n---\n\n" + req.Content
		payload := map[string]interface{}{
			"model": "gpt-4o",
			"messages": []map[string]string{
				{"role": "user", "content": fullContent},
			},
			"max_tokens": 8000,
		}
		bodyBytes, _ := json.Marshal(payload)
		hr, err := http.NewRequest("POST", "https://api.githubcopilot.com/chat/completions", strings.NewReader(string(bodyBytes)))
		if err != nil {
			http.Error(w, "request build: "+err.Error(), 500)
			return
		}
		hr.Header.Set("Content-Type", "application/json")
		hr.Header.Set("X-Github-Token", token)
		client := &http.Client{Timeout: 60 * time.Second}
		resp, err := client.Do(hr)
		if err != nil {
			http.Error(w, "Copilot request: "+err.Error(), 502)
			return
		}
		defer resp.Body.Close()
		respBody, _ := io.ReadAll(resp.Body)
		if resp.StatusCode != http.StatusOK {
			http.Error(w, "Copilot API: "+resp.Status+" "+string(respBody), resp.StatusCode)
			return
		}
		var copilotResp struct {
			Choices []struct {
				Message struct {
					Content string `json:"content"`
				} `json:"message"`
			} `json:"choices"`
		}
		if err := json.Unmarshal(respBody, &copilotResp); err != nil {
			http.Error(w, "Copilot response parse: "+err.Error(), 502)
			return
		}
		outContent := req.Content
		if len(copilotResp.Choices) > 0 && copilotResp.Choices[0].Message.Content != "" {
			outContent = strings.TrimSpace(copilotResp.Choices[0].Message.Content)
			if strings.HasPrefix(outContent, "```") {
				if idx := strings.Index(outContent, "\n"); idx > 0 {
					outContent = outContent[idx+1:]
				}
				if strings.HasSuffix(outContent, "```") {
					outContent = strings.TrimSuffix(outContent, "```")
				}
				outContent = strings.TrimSpace(outContent)
			}
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]string{"content": outContent})
	})

	http.HandleFunc("/api/open-in-notepad", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			Path string `json:"path"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil || req.Path == "" {
			http.Error(w, "path required", 400)
			return
		}
		path := strings.ReplaceAll(req.Path, "|", string(filepath.Separator))
		absPath, err := filepath.Abs(path)
		if err != nil {
			http.Error(w, "invalid path", 400)
			return
		}
		if _, err := os.Stat(absPath); err != nil {
			http.Error(w, "file not found", 404)
			return
		}
		var cmd *exec.Cmd
		switch runtime.GOOS {
		case "windows":
			cmd = exec.Command("notepad.exe", absPath)
		case "darwin":
			cmd = exec.Command("open", "-e", absPath)
		default:
			cmd = exec.Command("xdg-open", absPath)
		}
		if cmd == nil {
			http.Error(w, "not supported", 501)
			return
		}
		if err := cmd.Start(); err != nil {
			http.Error(w, "open: "+err.Error(), 500)
			return
		}
		go cmd.Wait()
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]string{"message": "Opened in Notepad"})
	})

	http.HandleFunc("/api/open-in-wire", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			Path string `json:"path"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil || req.Path == "" {
			http.Error(w, "path required", 400)
			return
		}
		path := strings.ReplaceAll(req.Path, "|", string(filepath.Separator))
		absPath, err := filepath.Abs(path)
		if err != nil {
			http.Error(w, "invalid path", 400)
			return
		}
		if _, err := os.Stat(absPath); err != nil {
			http.Error(w, "file not found", 404)
			return
		}
		var cmd *exec.Cmd
		settingsMu.RLock()
		wirePath := strings.TrimSpace(appSettings.WirePath)
		settingsMu.RUnlock()
		if wirePath != "" {
			if _, err := os.Stat(wirePath); err == nil {
				cmd = exec.Command(wirePath, absPath)
			}
		}
		if cmd == nil {
			switch runtime.GOOS {
			case "windows":
				cmd = exec.Command("cmd", "/c", "start", "", absPath)
			case "darwin":
				cmd = exec.Command("open", absPath)
			default:
				cmd = exec.Command("xdg-open", absPath)
			}
		}
		if cmd == nil {
			http.Error(w, "not supported", 501)
			return
		}
		if err := cmd.Start(); err != nil {
			http.Error(w, "open: "+err.Error(), 500)
			return
		}
		go cmd.Wait()
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]string{"message": "Opened in Wire"})
	})

	// ---------- Seed Wire from VJ Sets ----------
	http.HandleFunc("/api/seed-wire", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			Set      string `json:"set"`
			AutoSeed bool   `json:"autoSeed"`
			DryRun   bool   `json:"dryRun"`
			Mode     string `json:"mode"`
			Features struct {
				FFT    *bool `json:"fft"`
				Webcam *bool `json:"webcam"`
				Glitch *bool `json:"glitch"`
				MIDI   *bool `json:"midi"`
			} `json:"features"`
			FxLevel string `json:"fxLevel"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		scriptPath := filepath.Join(exeDir(), "scripts", "seed-wire-from-sets.js")
		if _, err := os.Stat(scriptPath); err != nil {
			http.Error(w, "seed-wire-from-sets.js not found", 500)
			return
		}
		args := []string{scriptPath, "--json-output",
			"--db", filepath.Join(exeDir(), "macroverse.db"),
			"--output", filepath.Join(exeDir(), "resolume")}
		if req.Set != "" {
			args = append(args, "--set", req.Set)
		}
		if req.AutoSeed {
			args = append(args, "--auto-seed")
		}
		if req.DryRun {
			args = append(args, "--dry-run")
		}
		if req.Mode != "" {
			args = append(args, "--mode", req.Mode)
		}
		if req.Features.FFT != nil {
			if *req.Features.FFT {
				args = append(args, "--fft")
			} else {
				args = append(args, "--no-fft")
			}
		}
		if req.Features.Webcam != nil {
			if *req.Features.Webcam {
				args = append(args, "--webcam")
			} else {
				args = append(args, "--no-webcam")
			}
		}
		if req.Features.Glitch != nil {
			if *req.Features.Glitch {
				args = append(args, "--glitch")
			} else {
				args = append(args, "--no-glitch")
			}
		}
		if req.Features.MIDI != nil {
			if *req.Features.MIDI {
				args = append(args, "--midi")
			} else {
				args = append(args, "--no-midi")
			}
		}
		if req.FxLevel != "" {
			args = append(args, "--fx-level", req.FxLevel)
		}
		cmd := exec.Command("node", args...)
		cmd.Dir = exeDir()
		var stdout, stderr bytes.Buffer
		cmd.Stdout = &stdout
		cmd.Stderr = &stderr
		err := cmd.Run()
		if err != nil {
			http.Error(w, "script error: "+err.Error()+"\n"+stderr.String()+"\n"+stdout.String(), 500)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		w.Write(stdout.Bytes())
	})

	// ---------- Seed Avenue Composition from Wire Patches ----------
	http.HandleFunc("/api/seed-avenue", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			Name   string `json:"name"`
			Set    string `json:"set"`
			DryRun bool   `json:"dryRun"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		scriptPath := filepath.Join(exeDir(), "scripts", "seed-avenue-from-sets.js")
		if _, err := os.Stat(scriptPath); err != nil {
			http.Error(w, "seed-avenue-from-sets.js not found", 500)
			return
		}
		args := []string{scriptPath,
			"--output", filepath.Join(exeDir(), "resolume")}
		if req.Name != "" {
			args = append(args, "--name", req.Name)
		}
		if req.Set != "" {
			args = append(args, "--set", req.Set)
		}
		if req.DryRun {
			args = append(args, "--dry-run")
		}
		cmd := exec.Command("node", args...)
		cmd.Dir = exeDir()
		var stdoutAve, stderrAve bytes.Buffer
		cmd.Stdout = &stdoutAve
		cmd.Stderr = &stderrAve
		err := cmd.Run()
		if err != nil {
			http.Error(w, "script error: "+err.Error()+"\n"+stderrAve.String()+"\n"+stdoutAve.String(), 500)
			return
		}
		w.Header().Set("Content-Type", "text/plain")
		w.Write(stdoutAve.Bytes())
	})

	// ---------- Wire Pipeline Hub Endpoints ----------

	// Classify shaders as source or texture-effect based on sampler2D / image inputs
	http.HandleFunc("/api/wire/classify-effects", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		entries, err := readIndex()
		if err != nil {
			http.Error(w, err.Error(), 500)
			return
		}
		sampler2DRe := regexp.MustCompile(`(?i)uniform\s+sampler2D\s+\w+`)
		isfImageRe := regexp.MustCompile(`(?i)"TYPE"\s*:\s*"image"`)
		scanned, effectsTagged, sourcesTagged := 0, 0, 0
		for i := range entries {
			e := &entries[i]
			p := strings.ReplaceAll(e.Path, "|", string(filepath.Separator))
			if !filepath.IsAbs(p) {
				p = filepath.Join(exeDir(), p)
			}
			raw, err := os.ReadFile(p)
			if err != nil {
				continue
			}
			scanned++
			src := string(raw)
			hasTex := sampler2DRe.MatchString(src) || isfImageRe.MatchString(src)
			tags := e.Tags
			// Remove old source/texture-effect tags
			var cleaned []string
			for _, t := range tags {
				if t != "source" && t != "texture-effect" {
					cleaned = append(cleaned, t)
				}
			}
			if hasTex {
				cleaned = append(cleaned, "texture-effect")
				effectsTagged++
			} else if e.Format == "isf" || strings.HasSuffix(strings.ToLower(e.Path), ".fs") {
				cleaned = append(cleaned, "source")
				sourcesTagged++
			}
			e.Tags = cleaned
		}
		if err := writeIndex(entries); err != nil {
			http.Error(w, err.Error(), 500)
			return
		}
		log.Printf("wire/classify-effects: scanned=%d effects=%d sources=%d", scanned, effectsTagged, sourcesTagged)
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{
			"ok":            true,
			"scanned":       scanned,
			"effectsTagged": effectsTagged,
			"sourcesTagged": sourcesTagged,
		})
	})

	// List all .wire files in resolume/ with metadata
	http.HandleFunc("/api/wire/library", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodGet {
			http.Error(w, "method not allowed", 405)
			return
		}
		dir := filepath.Join(exeDir(), "resolume")
		dirEntries, err := os.ReadDir(dir)
		if err != nil {
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode([]interface{}{})
			return
		}
		type wireEntry struct {
			Name          string `json:"name"`
			DisplayName   string `json:"displayName"`
			FileName      string `json:"fileName"`
			SetName       string `json:"setName"`
			Category      string `json:"category"`
			ShaderCount   int    `json:"shaderCount"`
			FileSizeBytes int64  `json:"fileSizeBytes"`
			Path          string `json:"path"`
		}
		var results []wireEntry
		setRe := regexp.MustCompile(`^(vj-[\w-]+?)-(\d+)\.wire$`)
		tmplRe := regexp.MustCompile(`^tmpl-([\w]+)-`)
		for _, de := range dirEntries {
			if de.IsDir() || !strings.HasSuffix(de.Name(), ".wire") {
				continue
			}
			info, err := de.Info()
			if err != nil {
				continue
			}
			name := strings.TrimSuffix(de.Name(), ".wire")
			setName := ""
			if m := setRe.FindStringSubmatch(de.Name()); m != nil {
				setName = m[1]
			}
			displayName := name
			category := "mixer"
			shaderCount := 0
			fpath := filepath.Join(dir, de.Name())

			// For large VJ set files, skip JSON parse - infer from filename
			if setName != "" && info.Size() > 30000 {
				displayName = strings.ReplaceAll(setName, "-", " ") + " #" + strings.TrimSuffix(strings.TrimPrefix(de.Name(), setName+"-"), ".wire")
				if strings.Contains(setName, "-fx") {
					category = "effect"
				} else {
					category = "source"
				}
				shaderCount = int(info.Size() / 2000) // rough estimate
			} else {
				// Parse small files (templates, custom patches) for real metadata
				raw, readErr := os.ReadFile(fpath)
				if readErr == nil {
					var wireData struct {
						Patch struct {
							Meta struct {
								DisplayName string `json:"displayName"`
								Category    string `json:"category"`
							} `json:"meta"`
							Nodes map[string]json.RawMessage `json:"nodes"`
						} `json:"patch"`
					}
					if json.Unmarshal(raw, &wireData) == nil {
						if wireData.Patch.Meta.DisplayName != "" {
							displayName = wireData.Patch.Meta.DisplayName
						}
						if wireData.Patch.Meta.Category != "" {
							category = wireData.Patch.Meta.Category
						}
						shaderCount = len(wireData.Patch.Nodes)
					}
				}
			}
			// Tag template type from filename
			if m := tmplRe.FindStringSubmatch(de.Name()); m != nil {
				if setName == "" {
					setName = "tmpl-" + m[1]
				}
			}
			results = append(results, wireEntry{
				Name:          name,
				DisplayName:   displayName,
				FileName:      de.Name(),
				SetName:       setName,
				Category:      category,
				ShaderCount:   shaderCount,
				FileSizeBytes: info.Size(),
				Path:          "resolume/" + de.Name(),
			})
		}
		if results == nil {
			results = []wireEntry{}
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(results)
	})

	// Return raw .wire JSON content for clipboard copy
	http.HandleFunc("/api/wire/patch", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodGet {
			http.Error(w, "method not allowed", 405)
			return
		}
		name := r.URL.Query().Get("name")
		if name == "" {
			http.Error(w, "name required", 400)
			return
		}
		// Sanitize: only allow alphanumeric, dash, underscore
		safeName := regexp.MustCompile(`[^a-zA-Z0-9_-]`).ReplaceAllString(name, "")
		fpath := filepath.Join(exeDir(), "resolume", safeName+".wire")
		raw, err := os.ReadFile(fpath)
		if err != nil {
			http.Error(w, "not found", 404)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		w.Write(raw)
	})

	// Delete a .wire file from resolume/
	http.HandleFunc("/api/wire/delete", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		var req struct {
			Path string `json:"path"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		// Sanitize: must be in resolume/ dir and end with .wire
		base := filepath.Base(req.Path)
		if !strings.HasSuffix(base, ".wire") {
			http.Error(w, "invalid path", 400)
			return
		}
		fpath := filepath.Join(exeDir(), "resolume", base)
		if err := os.Remove(fpath); err != nil {
			http.Error(w, err.Error(), 500)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]bool{"ok": true})
	})

	// Bulk compile Wire patches - updates metadata (author, vendor, email) and validates
	http.HandleFunc("/api/wire/bulk-compile", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		resolDir := filepath.Join(exeDir(), "resolume")
		entries, err := os.ReadDir(resolDir)
		if err != nil {
			http.Error(w, "read dir: "+err.Error(), 500)
			return
		}
		compiled := 0
		var compileErrors []string
		var compiledPaths []string
		for _, entry := range entries {
			if entry.IsDir() || !strings.HasSuffix(entry.Name(), ".wire") {
				continue
			}
			wirePath := filepath.Join(resolDir, entry.Name())
			raw, err := os.ReadFile(wirePath)
			if err != nil {
				compileErrors = append(compileErrors, entry.Name()+": read: "+err.Error())
				continue
			}
			var patch map[string]interface{}
			if err := json.Unmarshal(raw, &patch); err != nil {
				compileErrors = append(compileErrors, entry.Name()+": parse: "+err.Error())
				continue
			}
			// Update patch metadata
			if p, ok := patch["patch"].(map[string]interface{}); ok {
				if meta, ok := p["meta"].(map[string]interface{}); ok {
					meta["vendor"] = "aday.net.au"
					meta["url"] = "aday@aday.net.au"
					meta["mail"] = ""
					meta["licenseName"] = ""
					// Ensure the note makes this editable
					if note, ok := meta["note"].(map[string]interface{}); ok {
						if note["text"] == "" {
							note["text"] = "Macroverse Generated"
						}
					}
				}
			}
			// Validate basic structure
			if _, ok := patch["patch"]; !ok {
				compileErrors = append(compileErrors, entry.Name()+": missing patch key")
				continue
			}
			out, err := json.MarshalIndent(patch, "", "  ")
			if err != nil {
				compileErrors = append(compileErrors, entry.Name()+": marshal: "+err.Error())
				continue
			}
			if err := os.WriteFile(wirePath, out, 0644); err != nil {
				compileErrors = append(compileErrors, entry.Name()+": write: "+err.Error())
				continue
			}
			compiled++
			compiledPaths = append(compiledPaths, wirePath)
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{
			"compiled": compiled,
			"errors":   compileErrors,
			"paths":    compiledPaths,
		})
	})

	// Generate a custom Wire patch from selected shader IDs + topology
	http.HandleFunc("/api/wire/generate", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			ShaderIDs  []int  `json:"shaderIds"`
			Topology   string `json:"topology"`
			MidiPreset string `json:"midiPreset"`
			OutputName string `json:"outputName"`
			Features   struct {
				FFT    *bool `json:"fft"`
				Webcam *bool `json:"webcam"`
				MIDI   *bool `json:"midi"`
			} `json:"features"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		if len(req.ShaderIDs) == 0 {
			http.Error(w, "no shaderIds provided", 400)
			return
		}
		entries, err := readIndex()
		if err != nil {
			http.Error(w, err.Error(), 500)
			return
		}
		// Build shader paths list
		var shaderPaths []string
		idSet := map[int]bool{}
		for _, id := range req.ShaderIDs {
			idSet[id] = true
		}
		for _, e := range entries {
			if idSet[e.ID] {
				p := strings.ReplaceAll(e.Path, "|", string(filepath.Separator))
				if !filepath.IsAbs(p) {
					p = filepath.Join(exeDir(), p)
				}
				shaderPaths = append(shaderPaths, p)
			}
		}
		if len(shaderPaths) == 0 {
			http.Error(w, "no valid shaders found for given IDs", 400)
			return
		}
		// Write temp config for the seed-wire script
		outName := req.OutputName
		if outName == "" {
			outName = fmt.Sprintf("custom-%d", time.Now().UnixMilli())
		}
		configData := map[string]interface{}{
			"shaders":    shaderPaths,
			"topology":   req.Topology,
			"midiPreset": req.MidiPreset,
			"outputName": outName,
			"features": map[string]bool{
				"fft":    req.Features.FFT == nil || *req.Features.FFT,
				"webcam": req.Features.Webcam == nil || *req.Features.Webcam,
				"midi":   req.Features.MIDI == nil || *req.Features.MIDI,
			},
		}
		configJSON, _ := json.Marshal(configData)
		tmpFile, err := os.CreateTemp("", "wire-gen-*.json")
		if err != nil {
			http.Error(w, err.Error(), 500)
			return
		}
		tmpFile.Write(configJSON)
		tmpFile.Close()
		defer os.Remove(tmpFile.Name())

		scriptPath := filepath.Join(exeDir(), "scripts", "seed-wire-from-sets.js")
		args := []string{scriptPath, "--json-output", "--config", tmpFile.Name(),
			"--db", filepath.Join(exeDir(), "macroverse.db"),
			"--output", filepath.Join(exeDir(), "resolume")}
		cmd := exec.Command("node", args...)
		cmd.Dir = exeDir()
		var stdout, stderr bytes.Buffer
		cmd.Stdout = &stdout
		cmd.Stderr = &stderr
		err = cmd.Run()
		if err != nil {
			// Even if script doesn't support --config yet, return a useful message
			http.Error(w, "generate failed: "+err.Error()+"\n"+stderr.String()+"\n"+stdout.String(), 500)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		w.Write(stdout.Bytes())
	})

	// Generate Wire effect patches from texture-effect tagged shaders
	http.HandleFunc("/api/wire/generate-effects", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		entries, err := readIndex()
		if err != nil {
			http.Error(w, err.Error(), 500)
			return
		}
		// Collect texture-effect tagged shaders
		var effectPaths []string
		for _, e := range entries {
			for _, t := range e.Tags {
				if t == "texture-effect" {
					p := strings.ReplaceAll(e.Path, "|", string(filepath.Separator))
					if !filepath.IsAbs(p) {
						p = filepath.Join(exeDir(), p)
					}
					effectPaths = append(effectPaths, p)
					break
				}
			}
		}
		if len(effectPaths) == 0 {
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(map[string]interface{}{
				"ok":        true,
				"generated": 0,
				"errors":    []string{"No shaders tagged as texture-effect. Run Tag Effects first."},
			})
			return
		}
		// Write temp config for effects generation
		configData := map[string]interface{}{
			"shaders":    effectPaths,
			"topology":   "effect",
			"midiPreset": "apc40",
			"outputName": "wire-effects",
			"features":   map[string]bool{"fft": false, "webcam": true, "midi": true},
		}
		configJSON, _ := json.Marshal(configData)
		tmpFile, err := os.CreateTemp("", "wire-effects-*.json")
		if err != nil {
			http.Error(w, err.Error(), 500)
			return
		}
		tmpFile.Write(configJSON)
		tmpFile.Close()
		defer os.Remove(tmpFile.Name())

		scriptPath := filepath.Join(exeDir(), "scripts", "seed-wire-from-sets.js")
		args := []string{scriptPath, "--json-output", "--config", tmpFile.Name(),
			"--db", filepath.Join(exeDir(), "macroverse.db"),
			"--output", filepath.Join(exeDir(), "resolume")}
		cmd := exec.Command("node", args...)
		cmd.Dir = exeDir()
		var stdoutEff, stderrEff bytes.Buffer
		cmd.Stdout = &stdoutEff
		cmd.Stderr = &stderrEff
		err = cmd.Run()
		if err != nil {
			http.Error(w, "generate-effects failed: "+err.Error()+"\n"+stderrEff.String()+"\n"+stdoutEff.String(), 500)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		w.Write(stdoutEff.Bytes())
	})

	// ---------- Bulk Wire Compile ----------
	// Runs Resolume Wire to compile .wire patches into loadable format
	http.HandleFunc("/api/wire/compile", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			Author string `json:"author"`
			Vendor string `json:"vendor"`
			URL    string `json:"url"`
			Mail   string `json:"mail"`
		}
		json.NewDecoder(r.Body).Decode(&req)
		if req.Author == "" {
			req.Author = "Macroverse"
		}
		if req.Vendor == "" {
			req.Vendor = "aday.net.au"
		}
		if req.URL == "" {
			req.URL = "aday@aday.net.au"
		}

		resolDir := filepath.Join(exeDir(), "resolume")
		files, err := os.ReadDir(resolDir)
		if err != nil {
			http.Error(w, "cannot read resolume dir: "+err.Error(), 500)
			return
		}
		var wireFiles []string
		for _, f := range files {
			if !f.IsDir() && strings.HasSuffix(f.Name(), ".wire") {
				wireFiles = append(wireFiles, filepath.Join(resolDir, f.Name()))
			}
		}
		if len(wireFiles) == 0 {
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(map[string]interface{}{
				"ok":       true,
				"compiled": 0,
				"message":  "No .wire files found to compile",
			})
			return
		}

		// Update metadata in each wire file before compile
		updated := 0
		for _, wf := range wireFiles {
			raw, err := os.ReadFile(wf)
			if err != nil {
				continue
			}
			var patch map[string]interface{}
			if err := json.Unmarshal(raw, &patch); err != nil {
				continue
			}
			if p, ok := patch["patch"].(map[string]interface{}); ok {
				if meta, ok := p["meta"].(map[string]interface{}); ok {
					meta["author"] = req.Author
					meta["vendor"] = req.Vendor
					meta["url"] = req.URL
					meta["mail"] = req.Mail
					out, _ := json.MarshalIndent(patch, "", "  ")
					os.WriteFile(wf, out, 0644)
					updated++
				}
			}
		}

		// Try to find Wire executable for compilation
		settingsMu.RLock()
		wirePath := strings.TrimSpace(appSettings.WirePath)
		settingsMu.RUnlock()

		compiled := 0
		var compileErrors []string

		if wirePath != "" {
			if _, err := os.Stat(wirePath); err == nil {
				// Wire executable found - try batch compile via command line
				for _, wf := range wireFiles {
					cmd := exec.Command(wirePath, "--compile", wf)
					cmd.Dir = resolDir
					var stderr bytes.Buffer
					cmd.Stderr = &stderr
					if err := cmd.Run(); err != nil {
						compileErrors = append(compileErrors, filepath.Base(wf)+": "+err.Error())
					} else {
						compiled++
					}
				}
			} else {
				compileErrors = append(compileErrors, "Wire executable not found at: "+wirePath)
			}
		} else {
			compileErrors = append(compileErrors, "Wire path not configured in settings. Set WirePath to compile.")
		}

		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{
			"ok":         len(compileErrors) == 0,
			"total":      len(wireFiles),
			"updated":    updated,
			"compiled":   compiled,
			"errors":     compileErrors,
			"wireFiles":  wireFiles,
		})
	})

	// ---------- Open AVC in Resolume ----------
	http.HandleFunc("/api/open-in-resolume", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			Path string `json:"path"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil || req.Path == "" {
			http.Error(w, "path required", 400)
			return
		}
		absPath, err := filepath.Abs(req.Path)
		if err != nil {
			http.Error(w, "invalid path", 400)
			return
		}
		if _, err := os.Stat(absPath); err != nil {
			http.Error(w, "file not found: "+absPath, 404)
			return
		}
		// Use OS default handler which should open .avc in Resolume
		var cmd *exec.Cmd
		switch runtime.GOOS {
		case "windows":
			cmd = exec.Command("cmd", "/c", "start", "", absPath)
		case "darwin":
			cmd = exec.Command("open", absPath)
		default:
			cmd = exec.Command("xdg-open", absPath)
		}
		if err := cmd.Start(); err != nil {
			http.Error(w, "open: "+err.Error(), 500)
			return
		}
		go cmd.Wait()
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]string{"message": "Opened in Resolume: " + absPath})
	})

	// findISFPath returns the path to the converted .fs file for an index entry (by id or by source path).
	findISFPath := func(id int, sourcePath string) string {
		sortedIsf := filepath.Join(getVfxRoot(), "sorted_isf")
		sourceDir := filepath.Join(sortedIsf, "source")
		if id > 0 {
			entries, err := readIndex()
			if err != nil {
				return ""
			}
			for _, e := range entries {
				if e.ID != id {
					continue
				}
				name := e.FixedName
				if name == "" && e.Path != "" {
					name = filepath.Base(e.Path)
				}
				if name == "" {
					continue
				}
				base := strings.TrimSuffix(name, filepath.Ext(name))
				if base == name {
					base = strings.TrimSuffix(name, ".txt")
				}
				cat := "misc"
				if e.Category != "" {
					cat = e.Category
				}
				vjSorted := filepath.Join(getVfxRoot(), "VJ-Sorted-Production", "ISF")
				candidates := []string{
					filepath.Join(sourceDir, fmt.Sprintf("%04d_%s_v1.0.fs", e.ID, base)),
					filepath.Join(vjSorted, cat, base+".fs"),
					filepath.Join(vjSorted, "misc", base+".fs"),
					filepath.Join(sortedIsf, cat, base+".fs"),
					filepath.Join(sortedIsf, "misc", base+".fs"),
				}
				for _, c := range candidates {
					if _, err := os.Stat(c); err == nil {
						return c
					}
				}
				return ""
			}
		}
		if sourcePath != "" {
			base := strings.TrimSuffix(filepath.Base(sourcePath), filepath.Ext(sourcePath))
			if base == filepath.Base(sourcePath) {
				base = strings.TrimSuffix(base, ".txt")
			}
			for _, c := range []string{
				filepath.Join(sortedIsf, "misc", base+".fs"),
				filepath.Join(sourceDir, base+".fs"),
			} {
				if _, err := os.Stat(c); err == nil {
					return c
				}
			}
		}
		return ""
	}

	http.HandleFunc("/api/isf-path", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodGet {
			http.Error(w, "method not allowed", 405)
			return
		}
		id := 0
		if s := r.URL.Query().Get("id"); s != "" {
			if n, err := strconv.Atoi(s); err == nil {
				id = n
			}
		}
		path := strings.ReplaceAll(r.URL.Query().Get("path"), "|", string(filepath.Separator))
		isfPath := findISFPath(id, path)
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{"path": isfPath, "ok": isfPath != ""})
	})

	http.HandleFunc("/api/git/log", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodGet {
			http.Error(w, "method not allowed", 405)
			return
		}
		path := r.URL.Query().Get("path")
		if path == "" {
			http.Error(w, "path required", 400)
			return
		}
		path = strings.ReplaceAll(path, "|", string(filepath.Separator))
		root := findGitRoot(path)
		if root == "" {
			root = os.Getenv("VFX_GLSL_ROOT")
			if root == "" {
				root = getVfxRoot()
			}
		}
		rel, relErr := filepath.Rel(root, path)
		if relErr != nil {
			rel = path
		}
		rel = filepath.ToSlash(rel)
		cmd := exec.Command("git", "log", "--format=%H%x09%ci%x09%s", "--follow", "--", rel)
		cmd.Dir = root
		out, err := cmd.Output()
		if err != nil {
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode([]map[string]string{})
			return
		}
		type logEntry struct {
			Sha     string `json:"sha"`
			Date    string `json:"date"`
			Subject string `json:"subject"`
		}
		var entries []logEntry
		for _, line := range strings.Split(strings.TrimSpace(string(out)), "\n") {
			if line == "" {
				continue
			}
			parts := strings.SplitN(line, "\t", 3)
			if len(parts) < 3 {
				continue
			}
			entries = append(entries, logEntry{Sha: parts[0], Date: parts[1], Subject: parts[2]})
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(entries)
	})

	http.HandleFunc("/api/git/revert-version", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		var req struct {
			Path string `json:"path"`
			Sha  string `json:"sha"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil || req.Path == "" || req.Sha == "" {
			http.Error(w, "path and sha required", 400)
			return
		}
		path := strings.ReplaceAll(req.Path, "|", string(filepath.Separator))
		root := findGitRoot(path)
		if root == "" {
			root = os.Getenv("VFX_GLSL_ROOT")
			if root == "" {
				root = getVfxRoot()
			}
		}
		rel, relErr := filepath.Rel(root, path)
		if relErr != nil {
			rel = path
		}
		rel = filepath.ToSlash(rel)
		cmd := exec.Command("git", "checkout", req.Sha, "--", rel)
		cmd.Dir = root
		out, err := cmd.CombinedOutput()
		if err != nil {
			w.Header().Set("Content-Type", "application/json")
			w.WriteHeader(500)
			json.NewEncoder(w).Encode(map[string]interface{}{"ok": false, "error": string(out)})
			return
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{"ok": true, "message": "Reverted to version " + req.Sha[:8]})
	})

	http.HandleFunc("/api/git/hard-reset-shaders", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		var req struct {
			Ref string `json:"ref"`
		}
		_ = json.NewDecoder(r.Body).Decode(&req)
		settingsMu.RLock()
		resetTarget := appSettings.getHardResetPath()
		settingsMu.RUnlock()
		if _, statErr := os.Stat(resetTarget); os.IsNotExist(statErr) {
			w.Header().Set("Content-Type", "application/json")
			w.WriteHeader(400)
			json.NewEncoder(w).Encode(map[string]interface{}{"ok": false, "error": "Hard Reset target folder not found: " + resetTarget + " — configure it in Settings → Hard Reset Path"})
			return
		}
		shadersDir := resetTarget
		root := findGitRoot(shadersDir)
		if root == "" {
			w.Header().Set("Content-Type", "application/json")
			w.WriteHeader(400)
			json.NewEncoder(w).Encode(map[string]interface{}{"ok": false, "error": "Hard Reset target is not inside a git repository: " + resetTarget})
			return
		}
		relPath, relErr := filepath.Rel(root, shadersDir)
		if relErr != nil || strings.HasPrefix(relPath, "..") || relPath == ".." {
			w.Header().Set("Content-Type", "application/json")
			w.WriteHeader(400)
			json.NewEncoder(w).Encode(map[string]interface{}{"ok": false, "error": "shaders path is not under git root"})
			return
		}
		relPath = filepath.ToSlash(relPath)
		ref := strings.TrimSpace(req.Ref)
		if ref == "" {
			ref = "init"
		}
		var resolved string
		if ref == "init" {
			cmd := exec.Command("git", "rev-list", "-1", "--reverse", "main")
			cmd.Dir = root
			out, err := cmd.Output()
			if err != nil {
				cmd = exec.Command("git", "rev-list", "-1", "--reverse", "HEAD")
				cmd.Dir = root
				out, err = cmd.Output()
			}
			if err != nil || len(out) == 0 {
				w.Header().Set("Content-Type", "application/json")
				w.WriteHeader(400)
				json.NewEncoder(w).Encode(map[string]interface{}{"ok": false, "error": "could not find first commit on main/HEAD"})
				return
			}
			resolved = strings.TrimSpace(string(out))
		} else {
			cmd := exec.Command("git", "rev-parse", ref)
			cmd.Dir = root
			out, err := cmd.Output()
			if err != nil {
				w.Header().Set("Content-Type", "application/json")
				w.WriteHeader(400)
				json.NewEncoder(w).Encode(map[string]interface{}{"ok": false, "error": "invalid ref: " + ref})
				return
			}
			resolved = strings.TrimSpace(string(out))
		}
		backupDir := filepath.Dir(getIndexPath())
		backupName := "shaders-backup-" + time.Now().Format("20060102-150405") + ".zip"
		backupPath := filepath.Join(backupDir, backupName)
		zipFile, err := os.Create(backupPath)
		if err != nil {
			w.Header().Set("Content-Type", "application/json")
			w.WriteHeader(500)
			json.NewEncoder(w).Encode(map[string]interface{}{"ok": false, "error": "create backup: " + err.Error()})
			return
		}
		zw := zip.NewWriter(zipFile)
		err = filepath.Walk(shadersDir, func(path string, info os.FileInfo, walkErr error) error {
			if walkErr != nil {
				return walkErr
			}
			if info.IsDir() {
				return nil
			}
			rel, _ := filepath.Rel(shadersDir, path)
			rel = filepath.ToSlash(rel)
			entry, err := zw.Create(rel)
			if err != nil {
				return err
			}
			f, err := os.Open(path)
			if err != nil {
				return err
			}
			_, err = io.Copy(entry, f)
			f.Close()
			return err
		})
		if err != nil {
			zw.Close()
			zipFile.Close()
			os.Remove(backupPath)
			w.Header().Set("Content-Type", "application/json")
			w.WriteHeader(500)
			json.NewEncoder(w).Encode(map[string]interface{}{"ok": false, "error": "backup zip: " + err.Error()})
			return
		}
		if err := zw.Close(); err != nil {
			zipFile.Close()
			os.Remove(backupPath)
			w.Header().Set("Content-Type", "application/json")
			w.WriteHeader(500)
			json.NewEncoder(w).Encode(map[string]interface{}{"ok": false, "error": "close zip: " + err.Error()})
			return
		}
		if err := zipFile.Close(); err != nil {
			os.Remove(backupPath)
			w.Header().Set("Content-Type", "application/json")
			w.WriteHeader(500)
			json.NewEncoder(w).Encode(map[string]interface{}{"ok": false, "error": "close backup file: " + err.Error()})
			return
		}
		checkout := exec.Command("git", "checkout", resolved, "--", relPath)
		checkout.Dir = root
		out, err := checkout.CombinedOutput()
		if err != nil {
			w.Header().Set("Content-Type", "application/json")
			w.WriteHeader(500)
			json.NewEncoder(w).Encode(map[string]interface{}{"ok": false, "error": "git checkout: " + string(out), "backupPath": backupPath})
			return
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{
			"ok":         true,
			"backupPath": backupPath,
			"ref":        resolved,
			"message":    "Shaders reset to " + resolved[:8] + ". Backup saved.",
		})
	})

	http.HandleFunc("/api/git/repo-status", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodGet {
			http.Error(w, "method not allowed", 405)
			return
		}
		path := r.URL.Query().Get("path")
		if path == "" {
			http.Error(w, "path required", 400)
			return
		}
		path = strings.ReplaceAll(path, "|", string(filepath.Separator))
		path = filepath.Clean(path)
		root := findGitRoot(path)
		isRepo := root != ""
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{
			"isRepo": isRepo,
			"root":   root,
		})
	})

	http.HandleFunc("/api/git/init", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		var req struct {
			Path string `json:"path"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil || req.Path == "" {
			http.Error(w, "path required", 400)
			return
		}
		path := strings.ReplaceAll(req.Path, "|", string(filepath.Separator))
		path = filepath.Clean(path)
		sourcePaths := getSourcePaths()
		allowed := false
		for _, p := range sourcePaths {
			if filepath.Clean(p) == path {
				allowed = true
				break
			}
		}
		if !allowed {
			w.Header().Set("Content-Type", "application/json")
			w.WriteHeader(400)
			json.NewEncoder(w).Encode(map[string]interface{}{"ok": false, "error": "path is not a configured source path"})
			return
		}
		if strings.Contains(filepath.ToSlash(path), "VJ-Sorted-Production") {
			w.Header().Set("Content-Type", "application/json")
			w.WriteHeader(400)
			json.NewEncoder(w).Encode(map[string]interface{}{"ok": false, "error": "git init is for library paths only (not VJ-Sorted-Production)"})
			return
		}
		info, err := os.Stat(path)
		if err != nil || !info.IsDir() {
			w.Header().Set("Content-Type", "application/json")
			w.WriteHeader(400)
			json.NewEncoder(w).Encode(map[string]interface{}{"ok": false, "error": "path is not an existing directory"})
			return
		}
		if findGitRoot(path) != "" {
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(map[string]interface{}{"ok": true, "message": "already a git repository"})
			return
		}
		cmd := exec.Command("git", "init")
		cmd.Dir = path
		if out, err := cmd.CombinedOutput(); err != nil {
			w.Header().Set("Content-Type", "application/json")
			w.WriteHeader(500)
			json.NewEncoder(w).Encode(map[string]interface{}{"ok": false, "error": strings.TrimSpace(string(out))})
			return
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{"ok": true, "message": "Git repository initialized"})
	})

	http.HandleFunc("/api/git/info", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodGet {
			http.Error(w, "method not allowed", 405)
			return
		}
		path := r.URL.Query().Get("path")
		if path == "" {
			http.Error(w, "path required", 400)
			return
		}
		path = strings.ReplaceAll(path, "|", string(filepath.Separator))
		root := findGitRoot(path)
		if root == "" {
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(map[string]interface{}{
				"revisions": 0, "tracked": false,
			})
			return
		}
		rel, relErr := filepath.Rel(root, path)
		if relErr != nil {
			rel = path
		}
		rel = filepath.ToSlash(rel)
		// Count revisions
		countCmd := exec.Command("git", "rev-list", "--count", "HEAD", "--", rel)
		countCmd.Dir = root
		countOut, countErr := countCmd.Output()
		revisions := 0
		if countErr == nil {
			revisions, _ = strconv.Atoi(strings.TrimSpace(string(countOut)))
		}
		// Get first and last commit dates
		firstDate := ""
		lastDate := ""
		firstSubject := ""
		lastSubject := ""
		if revisions > 0 {
			// Last (most recent) commit
			lastCmd := exec.Command("git", "log", "-1", "--format=%ci%x09%s", "--", rel)
			lastCmd.Dir = root
			if lastOut, err := lastCmd.Output(); err == nil {
				parts := strings.SplitN(strings.TrimSpace(string(lastOut)), "\t", 2)
				if len(parts) >= 1 {
					lastDate = parts[0]
				}
				if len(parts) >= 2 {
					lastSubject = parts[1]
				}
			}
			// First (oldest) commit
			firstCmd := exec.Command("git", "log", "--reverse", "--format=%ci%x09%s", "--", rel)
			firstCmd.Dir = root
			if firstOut, err := firstCmd.Output(); err == nil {
				line := strings.SplitN(strings.TrimSpace(string(firstOut)), "\n", 2)[0]
				parts := strings.SplitN(line, "\t", 2)
				if len(parts) >= 1 {
					firstDate = parts[0]
				}
				if len(parts) >= 2 {
					firstSubject = parts[1]
				}
			}
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{
			"tracked":      revisions > 0,
			"revisions":    revisions,
			"firstDate":    firstDate,
			"lastDate":     lastDate,
			"firstSubject": firstSubject,
			"lastSubject":  lastSubject,
		})
	})

	// Find shader-index.ps1 next to exe or in cwd
	findShaderIndexPS1 := func() string {
		if exe, err := os.Executable(); err == nil {
			p := filepath.Join(filepath.Dir(exe), "shader-index.ps1")
			if _, err := os.Stat(p); err == nil {
				return p
			}
		}
		if wd, err := os.Getwd(); err == nil {
			p := filepath.Join(wd, "shader-index.ps1")
			if _, err := os.Stat(p); err == nil {
				return p
			}
		}
		return ""
	}

	http.HandleFunc("/api/convert-and-open-in-wire", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			ID      *int   `json:"id"`
			Path    string `json:"path"`
			Content string `json:"content"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		id := 0
		if req.ID != nil {
			id = *req.ID
		}
		if id == 0 {
			http.Error(w, "id required", 400)
			return
		}
		savePath := strings.ReplaceAll(req.Path, "|", string(filepath.Separator))
		if savePath != "" && req.Content != "" {
			if err := os.WriteFile(savePath, []byte(req.Content), 0644); err != nil {
				w.Header().Set("Content-Type", "application/json")
				json.NewEncoder(w).Encode(map[string]interface{}{"ok": false, "error": "save: " + err.Error()})
				return
			}
		}
		ps1 := findShaderIndexPS1()
		if ps1 == "" {
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(map[string]interface{}{"ok": false, "error": "shader-index.ps1 not found"})
			return
		}
		tmpPath, expErr := exportIndexToTempJSON()
		if expErr != nil {
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(map[string]interface{}{"ok": false, "error": "export index: " + expErr.Error()})
			return
		}
		defer os.Remove(tmpPath)
		args := []string{"-ExecutionPolicy", "Bypass", "-File", ps1, "convert", "-IndexPath", tmpPath, "-Id", strconv.Itoa(id)}
		cmd := exec.Command("powershell", args...)
		cmd.Dir = getVfxRoot()
		out, err := cmd.CombinedOutput()
		if err != nil {
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(map[string]interface{}{"ok": false, "error": "convert: " + err.Error(), "output": strings.TrimSpace(string(out))})
			return
		}
		isfPath := findISFPath(id, "")
		if isfPath == "" {
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(map[string]interface{}{"ok": false, "error": "converted .fs file not found after convert", "output": strings.TrimSpace(string(out))})
			return
		}
		var openCmd *exec.Cmd
		settingsMu.RLock()
		wirePath := strings.TrimSpace(appSettings.WirePath)
		settingsMu.RUnlock()
		if wirePath != "" {
			if _, err := os.Stat(wirePath); err == nil {
				openCmd = exec.Command(wirePath, isfPath)
			}
		}
		if openCmd == nil {
			switch runtime.GOOS {
			case "windows":
				openCmd = exec.Command("cmd", "/c", "start", "", isfPath)
			case "darwin":
				openCmd = exec.Command("open", isfPath)
			default:
				openCmd = exec.Command("xdg-open", isfPath)
			}
		}
		if openCmd != nil {
			openCmd.Start()
			go openCmd.Wait()
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{"ok": true, "path": isfPath})
	})

	runPipeline := func(w http.ResponseWriter, cmd string, id int) {
		ps1 := findShaderIndexPS1()
		if ps1 == "" {
			logSection("PIPELINE", "ERROR: shader-index.ps1 not found")
			http.Error(w, "shader-index.ps1 not found (next to exe or cwd)", 404)
			return
		}
		tmpPath, err := exportIndexToTempJSON()
		if err != nil {
			logSection("PIPELINE", "ERROR: export index: "+err.Error())
			http.Error(w, "export index: "+err.Error(), 500)
			return
		}
		defer os.Remove(tmpPath)
		srcPaths := getSourcePaths()
		sourceArg := strings.Join(srcPaths, ";")
		vfx := getVfxRoot()
		args := []string{"-ExecutionPolicy", "Bypass", "-File", ps1, cmd, "-IndexPath", tmpPath, "-Source", sourceArg}
		if id > 0 {
			args = append(args, "-Id", strconv.Itoa(id))
		}
		logSection("PIPELINE", fmt.Sprintf("running: %s (ps1=%s, sources=%s, index=%s, id=%d)", cmd, ps1, sourceArg, tmpPath, id))
		c := exec.Command("powershell", args...)
		c.Dir = vfx
		c.Env = append(os.Environ(), "PIPELINE_UTF8=1")
		stdout, err := c.StdoutPipe()
		if err != nil {
			logSection("PIPELINE", "ERROR stdout pipe: "+err.Error())
			http.Error(w, err.Error(), 500)
			return
		}
		stderr, err := c.StderrPipe()
		if err != nil {
			logSection("PIPELINE", "ERROR stderr pipe: "+err.Error())
			http.Error(w, err.Error(), 500)
			return
		}
		if err := c.Start(); err != nil {
			logSection("PIPELINE", "ERROR start: "+err.Error())
			http.Error(w, err.Error(), 500)
			return
		}
		logSection("PIPELINE", "process started PID "+strconv.Itoa(c.Process.Pid))
		w.Header().Set("Content-Type", "text/plain; charset=utf-8")
		w.Header().Set("X-Content-Type-Options", "nosniff")
		pathsLabel := "Paths"
		if cmd == "scan" {
			pathsLabel = "Re-indexing paths"
		}
		pathsLine := pathsLabel + ": " + strings.Join(srcPaths, " | ") + "\n"
		w.Write([]byte(pathsLine))
		flusher, ok := w.(http.Flusher)
		if !ok {
			flusher = nil
		}
		if flusher != nil {
			flusher.Flush()
		}
		var wg sync.WaitGroup
		stream := func(rd io.Reader) {
			defer wg.Done()
			sc := bufio.NewScanner(rd)
			sc.Buffer(make([]byte, 0, 64*1024), 1024*1024)
			for sc.Scan() {
				line := sc.Text()
				clean := stripPipelineLine(line)
				logSection("PIPELINE", "  | "+clean)
				w.Write(append([]byte(line), '\n'))
				if flusher != nil {
					flusher.Flush()
				}
			}
		}
		wg.Add(2)
		go stream(stdout)
		go stream(stderr)
		wg.Wait()
		err = c.Wait()
		if err != nil {
			logSection("PIPELINE", "finished with error: "+err.Error())
		} else {
			logSection("PIPELINE", "finished OK")
		}
		if (cmd == "scan" || cmd == "organize") && err == nil {
			if impErr := importIndexFromJSON(tmpPath); impErr != nil {
				logSection("PIPELINE", "WARN: import after "+cmd+": "+impErr.Error())
			}
		}
	}

	http.HandleFunc("/api/pipeline/test", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			ID *int `json:"id"`
		}
		json.NewDecoder(r.Body).Decode(&req)
		id := 0
		if req.ID != nil {
			id = *req.ID
		}
		runPipeline(w, "test", id)
	})

	// Test harness: convert GLSL->ISF, then validate output (ISFVSN, INPUTS, @expose params)
	http.HandleFunc("/api/pipeline/test-harness", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			ID *int `json:"id"`
		}
		json.NewDecoder(r.Body).Decode(&req)
		id := 0
		if req.ID != nil {
			id = *req.ID
		}
		ps1 := findShaderIndexPS1()
		if ps1 == "" {
			http.Error(w, "shader-index.ps1 not found", 404)
			return
		}
		tmpPath, expErr := exportIndexToTempJSON()
		if expErr != nil {
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(map[string]interface{}{"ok": false, "error": "export index: " + expErr.Error()})
			return
		}
		defer os.Remove(tmpPath)
		args := []string{"-ExecutionPolicy", "Bypass", "-File", ps1, "convert", "-IndexPath", tmpPath}
		if id > 0 {
			args = append(args, "-Id", strconv.Itoa(id))
		}
		c := exec.Command("powershell", args...)
		c.Dir = getVfxRoot()
		out, err := c.CombinedOutput()
		if err != nil {
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(map[string]interface{}{
				"ok": false, "error": err.Error(), "output": string(out),
			})
			return
		}
		entries, err := readIndex()
		if err != nil {
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(map[string]interface{}{"ok": false, "error": "read index: " + err.Error()})
			return
		}
		sortedIsf := filepath.Join(getVfxRoot(), "sorted_isf")
		sourceDir := filepath.Join(sortedIsf, "source")
		var firstPath string
		for _, e := range entries {
			if id > 0 && e.ID != id {
				continue
			}
			name := e.FixedName
			if name == "" && e.Path != "" {
				name = filepath.Base(e.Path)
			}
			if name == "" {
				continue
			}
			base := strings.TrimSuffix(name, filepath.Ext(name))
			if base == name {
				base = strings.TrimSuffix(name, ".txt")
			}
			firstPath = filepath.Join(sourceDir, fmt.Sprintf("%04d_%s_v1.0.fs", e.ID, base))
			if _, err := os.Stat(firstPath); err == nil {
				break
			}
			firstPath = filepath.Join(sortedIsf, "misc", base+".fs")
			if _, err := os.Stat(firstPath); err == nil {
				break
			}
			firstPath = ""
		}
		if firstPath == "" {
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(map[string]interface{}{
				"ok": false, "error": "no shader found in index", "output": string(out),
			})
			return
		}
		content, err := os.ReadFile(firstPath)
		if err != nil {
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(map[string]interface{}{
				"ok": false, "error": "output file not found: " + firstPath, "path": firstPath, "output": string(out),
			})
			return
		}
		s := string(content)
		hasISFVSN := strings.Contains(s, `"ISFVSN"`)
		hasINPUTS := strings.Contains(s, `"INPUTS"`)
		paramNames := []string{}
		if re := regexp.MustCompile(`"NAME"\s*:\s*"([^"]+)"`); re != nil {
			for _, m := range re.FindAllStringSubmatch(s, -1) {
				if len(m) > 1 {
					paramNames = append(paramNames, m[1])
				}
			}
		}
		ok := hasISFVSN && hasINPUTS && len(paramNames) >= 5 // at least built-ins
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{
			"ok": ok, "path": firstPath, "hasISFVSN": hasISFVSN, "hasINPUTS": hasINPUTS,
			"inputCount": len(paramNames), "params": paramNames, "output": strings.TrimSpace(string(out)),
		})
	})

	http.HandleFunc("/api/pipeline/convert", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			ID *int `json:"id"`
		}
		json.NewDecoder(r.Body).Decode(&req)
		id := 0
		if req.ID != nil {
			id = *req.ID
		}
		runPipeline(w, "convert", id)
	})

	http.HandleFunc("/api/pipeline/build-isf", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		logSection("BUILD-ISF", "POST /api/pipeline/build-isf")
		var req struct {
			ID      *int   `json:"id"`
			Content string `json:"content"`
			Path    string `json:"path"`
		}
		json.NewDecoder(r.Body).Decode(&req)
		id := 0
		if req.ID != nil {
			id = *req.ID
		}
		if id == 0 {
			http.Error(w, "id required", 400)
			return
		}
		w.Header().Set("Content-Type", "text/plain; charset=utf-8")
		w.Header().Set("X-Content-Type-Options", "nosniff")
		w.Header().Set("Cache-Control", "no-cache")
		flusher, _ := w.(http.Flusher)
		emit := func(tag, msg string) {
			line := "STEP:" + tag + ":" + msg + "\n"
			w.Write([]byte(line))
			if flusher != nil {
				flusher.Flush()
			}
			logSection("BUILD-ISF", "["+tag+"] "+msg)
		}

		if req.Content != "" && req.Path != "" {
			savePath := strings.ReplaceAll(req.Path, "|", string(filepath.Separator))
			if err := os.WriteFile(savePath, []byte(req.Content), 0644); err != nil {
				emit("ERROR", "save GLSL: "+err.Error())
				return
			}
			emit("SAVE", "saved GLSL to "+savePath)
		}

		// Step 1: Run PS1 convert
		emit("CONVERT", "running PS1 convert for id "+strconv.Itoa(id)+"...")
		ps1 := findShaderIndexPS1()
		if ps1 == "" {
			emit("ERROR", "shader-index.ps1 not found")
			return
		}
		tmpPath, expErr := exportIndexToTempJSON()
		if expErr != nil {
			emit("ERROR", "export index: "+expErr.Error())
			return
		}
		defer os.Remove(tmpPath)
		args := []string{"-ExecutionPolicy", "Bypass", "-File", ps1, "convert", "-IndexPath", tmpPath, "-Id", strconv.Itoa(id)}
		cmd := exec.Command("powershell", args...)
		cmd.Dir = getVfxRoot()
		convertOut, convertErr := cmd.CombinedOutput()
		convertLog := strings.TrimSpace(string(convertOut))
		if convertLog != "" {
			for _, ln := range strings.Split(convertLog, "\n") {
				emit("CONVERT", strings.TrimSpace(ln))
			}
		}
		if convertErr != nil {
			emit("ERROR", "PS1 convert failed: "+convertErr.Error())
		}

		// Step 2: Find the output ISF file
		idxEntries, err := readIndex()
		if err != nil {
			emit("ERROR", "read index: "+err.Error())
			return
		}

		sortedIsf := filepath.Join(getVfxRoot(), "sorted_isf")
		sourceDir := filepath.Join(sortedIsf, "source")
		var isfPath string
		for _, e := range idxEntries {
			if e.ID != id {
				continue
			}
			name := e.FixedName
			if name == "" && e.Path != "" {
				name = filepath.Base(e.Path)
			}
			if name == "" {
				continue
			}
			base := strings.TrimSuffix(name, filepath.Ext(name))
			if base == name {
				base = strings.TrimSuffix(name, ".txt")
			}
			cat := "misc"
			if e.Category != "" {
				cat = e.Category
			}
			candidates := []string{
				filepath.Join(sourceDir, fmt.Sprintf("%04d_%s_v1.0.fs", e.ID, base)),
				filepath.Join(sortedIsf, cat, base+".fs"),
				filepath.Join(sortedIsf, "misc", base+".fs"),
			}
			for _, c := range candidates {
				if _, err := os.Stat(c); err == nil {
					isfPath = c
					break
				}
			}
			break
		}
		if isfPath == "" {
			emit("ERROR", "converted ISF file not found after PS1 convert")
			return
		}
		emit("CONVERT", "output: "+isfPath)

		// Step 3: Validate ISF
		validateISF := func(path string) (ok bool, issues []string, content string) {
			data, err := os.ReadFile(path)
			if err != nil {
				return false, []string{"read error: " + err.Error()}, ""
			}
			content = string(data)
			if !strings.Contains(content, `"ISFVSN"`) {
				issues = append(issues, "missing ISFVSN header")
			}
			if !strings.Contains(content, `"INPUTS"`) {
				issues = append(issues, "missing INPUTS array")
			}
			headerRe := regexp.MustCompile(`/\*\s*\{[\s\S]*?\}\s*\*/`)
			headerMatch := headerRe.FindString(content)
			if headerMatch == "" {
				issues = append(issues, "no ISF JSON header block found")
			} else {
				jsonBlock := strings.TrimPrefix(headerMatch, "/*")
				jsonBlock = strings.TrimSuffix(jsonBlock, "*/")
				jsonBlock = strings.TrimSpace(jsonBlock)
				var parsed map[string]interface{}
				if jsonErr := json.Unmarshal([]byte(jsonBlock), &parsed); jsonErr != nil {
					issues = append(issues, "ISF header JSON parse error: "+jsonErr.Error())
				} else {
					if inputs, ok := parsed["INPUTS"].([]interface{}); ok {
						if len(inputs) < 5 {
							issues = append(issues, fmt.Sprintf("only %d INPUTS (expected at least 5 built-in params)", len(inputs)))
						}
					}
				}
			}
			bodyStart := headerRe.FindStringIndex(content)
			if bodyStart != nil {
				body := content[bodyStart[1]:]
				if !strings.Contains(body, "gl_FragColor") && !strings.Contains(body, "gl_FragData") {
					issues = append(issues, "shader body has no gl_FragColor / gl_FragData output")
				}
				if strings.Contains(body, "uniform float time;") || strings.Contains(body, "uniform vec2 resolution;") {
					issues = append(issues, "stale uniform declarations (time/resolution) should be removed in ISF -- ISF uses TIME/RENDERSIZE built-ins")
				}
				sampler2DRe := regexp.MustCompile(`uniform\s+sampler2D\s+(\w+)\s*;`)
				samplerMatches := sampler2DRe.FindAllStringSubmatch(body, -1)
				if len(samplerMatches) > 0 {
					for _, sm := range samplerMatches {
						issues = append(issues, "unconverted sampler2D '"+sm[1]+"' should be ISF image INPUT -- add {\"NAME\":\""+sm[1]+"\",\"TYPE\":\"image\"} to INPUTS and use IMG_NORM_PIXEL("+sm[1]+", uv) instead of texture2D")
					}
				}
			}
			ok = len(issues) == 0
			return
		}

		ok, issues, isfContent := validateISF(isfPath)
		if ok {
			emit("VALIDATE", "ISF is valid")
			emit("DONE", isfPath)
			return
		}
		for _, iss := range issues {
			emit("VALIDATE", "issue: "+iss)
		}

		// Step 4: Cursor AI review and fix
		settingsMu.RLock()
		apiKey := appSettings.CursorApiKey
		settingsMu.RUnlock()

		issueList := strings.Join(issues, "; ")
		prompt := "You are an ISF shader specialist. " +
			"Edit ONLY the file at " + isfPath + ". Do not modify or reference any other files. " +
			"This file was auto-converted from GLSL to ISF format but has validation issues: " + issueList + ". " +
			"Fix these issues. The ISF format requires: " +
			"(1) a /* { JSON header } */ block with ISFVSN, DESCRIPTION, INPUTS array; " +
			"(2) built-in inputs: useFrameIndex (bool, DEFAULT true -- FRAMEINDEX drives animation in Wire), fps (float 24-120), timeScale (float 0.1-4), mouseX (float 0-1), mouseY (float 0-1); " +
			"(3) a #define block: #define time (useFrameIndex ? float(FRAMEINDEX)/max(fps,1.0) : TIME*timeScale) and #define mouse vec2(mouseX,mouseY); " +
			"(4) the shader body must output to gl_FragColor; " +
			"(5) remove any stale uniform declarations for time/mouse/resolution (ISF provides these); " +
			"(6) look for magic numbers and interesting uniforms that could become live-tweakable parameters and add them to INPUTS; " +
			"(7) preserve the visual intent of the original shader; " +
			"(8) convert any uniform sampler2D declarations to ISF image INPUTS -- add {\"NAME\":\"<name>\",\"TYPE\":\"image\"} to INPUTS array, " +
			"remove the uniform sampler2D declaration from the body, and replace texture2D(<name>, uv) calls with IMG_NORM_PIXEL(<name>, uv) or IMG_PIXEL(<name>, uv*RENDERSIZE); " +
			"(9) if the shader uses feedback or multi-pass, use PASSES array in the ISF header and IMG_THIS_NORM_PIXEL for self-referencing."

		agentCmd, agentErr := buildAgentCmd(prompt)
		if agentErr != nil {
			emit("CURSOR", "no cursor-agent found in PATH -- skipping AI fix, manual review needed")
			emit("DONE", isfPath)
			return
		}
		emit("CURSOR", "launching Cursor agent to fix ISF issues...")
		agentCmd.Dir = filepath.Dir(isfPath)
		if apiKey != "" {
			agentCmd.Env = append(os.Environ(), "CURSOR_API_KEY="+apiKey)
		}

		agentStdout, _ := agentCmd.StdoutPipe()
		agentStderr, _ := agentCmd.StderrPipe()
		if err := agentCmd.Start(); err != nil {
			emit("CURSOR", "agent start failed: "+err.Error()+", skipping AI fix")
			emit("DONE", isfPath)
			return
		}
		emit("CURSOR", "agent PID "+strconv.Itoa(agentCmd.Process.Pid))

		agentDone := make(chan error, 1)
		var agentOutput strings.Builder
		go func() {
			sc := bufio.NewScanner(io.MultiReader(agentStdout, agentStderr))
			for sc.Scan() {
				line := sc.Text()
				agentOutput.WriteString(line + "\n")
				emit("CURSOR", line)
			}
			agentDone <- agentCmd.Wait()
		}()

		select {
		case err := <-agentDone:
			if err != nil {
				emit("CURSOR", "agent finished with error: "+err.Error())
			} else {
				emit("CURSOR", "agent finished OK")
			}
		case <-time.After(120 * time.Second):
			agentCmd.Process.Kill()
			emit("CURSOR", "agent timed out after 120s, killed")
		}

		// Step 5: Re-validate
		ok2, issues2, _ := validateISF(isfPath)
		_ = isfContent
		if ok2 {
			emit("VALIDATE", "ISF is now valid after Cursor fix")
			emit("DONE", isfPath)
			return
		}
		for _, iss := range issues2 {
			emit("VALIDATE", "remaining issue: "+iss)
		}
		emit("DONE", isfPath)
	})

	http.HandleFunc("/api/pipeline/organize", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		runPipeline(w, "organize", 0)
	})

	http.HandleFunc("/api/native-scan", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		logSection("SCAN", "POST /api/native-scan (Go-native reindex)")
		files := scanAllFiles()
		existing, _ := readIndex()
		if existing == nil {
			existing = []ShaderEntry{}
		}

		known := make(map[string]bool)
		maxID := 0
		for _, e := range existing {
			known[e.Path] = true
			if e.ID > maxID {
				maxID = e.ID
			}
		}

		added := 0
		for path := range files {
			if known[path] {
				continue
			}
			maxID++
			ext := strings.ToLower(filepath.Ext(path))
			format := "glsl"
			if ext == ".fs" || ext == ".isf" {
				format = "isf"
			}
			base := strings.TrimSuffix(filepath.Base(path), filepath.Ext(path))
			name := strings.ReplaceAll(base, "-", " ")
			name = strings.ReplaceAll(name, "_", " ")

			data, _ := os.ReadFile(path)
			h := sha256.Sum256(data)
			fileHash := hex.EncodeToString(h[:8])

			category := "uncategorized"
			dir := filepath.Base(filepath.Dir(path))
			if dir != "" && dir != "." {
				category = dir
			}

			sets := []string{}
			if category == "macroverse" || strings.Contains(filepath.ToSlash(path), "/macroverse/") {
				sets = []string{"macroverse-origin", "macroverse-set", "vj-cosmic", "vj-wire-ready"}
			}

			existing = append(existing, ShaderEntry{
				ID:       maxID,
				Path:     path,
				Name:     name,
				Category: category,
				Tags:     []string{},
				Sets:     sets,
				Format:   format,
				FileHash: fileHash,
			})
			added++
		}

		removed := 0
		var cleaned []ShaderEntry
		for _, e := range existing {
			if _, err := os.Stat(e.Path); err != nil {
				removed++
				continue
			}
			ext := strings.ToLower(filepath.Ext(e.Path))
			if !shaderExts[ext] {
				removed++
				continue
			}
			cleaned = append(cleaned, e)
		}

		writeIndex(cleaned)
		logSection("SCAN", fmt.Sprintf("native scan done: %d total, %d added, %d removed", len(cleaned), added, removed))

		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{
			"ok":      true,
			"total":   len(cleaned),
			"added":   added,
			"removed": removed,
		})
	})

	http.HandleFunc("/api/pipeline/scan", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		runPipeline(w, "scan", 0)
	})

	http.HandleFunc("/api/errors/report", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			Path     string `json:"path"`
			Filename string `json:"filename"`
			Error    string `json:"error"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil || req.Error == "" {
			http.Error(w, "bad request", 400)
			return
		}
		path := strings.ReplaceAll(req.Path, "|", string(filepath.Separator))
		entry := reportShaderError(path, req.Filename, req.Error)
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(entry)
	})

	http.HandleFunc("/api/errors", func(w http.ResponseWriter, r *http.Request) {
		errorLog.Lock()
		entries := make([]ShaderError, len(errorLog.entries))
		copy(entries, errorLog.entries)
		errorLog.Unlock()
		filter := r.URL.Query().Get("status")
		if filter != "" {
			var filtered []ShaderError
			for _, e := range entries {
				if e.Status == filter {
					filtered = append(filtered, e)
				}
			}
			entries = filtered
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(entries)
	})

	http.HandleFunc("/api/watch/status", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{
			"running":  isWatcherRunning(),
			"newCount": folderWatcher.newCount,
		})
	})

	http.HandleFunc("/api/watch/start", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		startFolderWatcher()
		settingsMu.Lock()
		appSettings.WatchFolders = true
		settingsMu.Unlock()
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{"running": true})
	})

	http.HandleFunc("/api/watch/stop", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		stopFolderWatcher()
		settingsMu.Lock()
		appSettings.WatchFolders = false
		settingsMu.Unlock()
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{"running": false})
	})

	http.HandleFunc("/api/index/backup", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		data, err := os.ReadFile(getIndexPath())
		if err != nil {
			http.Error(w, "backup read: "+err.Error(), 500)
			return
		}
		dir := filepath.Dir(getIndexPath())
		base := filepath.Base(getIndexPath())
		ext := filepath.Ext(base)
		name := strings.TrimSuffix(base, ext)
		backupName := name + "-backup-" + time.Now().Format("20060102-150405") + ext
		backupPath := filepath.Join(dir, backupName)
		if err := os.WriteFile(backupPath, data, 0644); err != nil {
			http.Error(w, "backup write: "+err.Error(), 500)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]string{"message": "Backup saved: " + backupName, "path": backupPath})
	})

	http.HandleFunc("/api/index/clear", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		dbPath := getDBPath()
		data, err := os.ReadFile(dbPath)
		if err == nil && len(data) > 0 {
			dir := filepath.Dir(dbPath)
			base := filepath.Base(dbPath)
			ext := filepath.Ext(base)
			name := strings.TrimSuffix(base, ext)
			backupName := name + "-backup-" + time.Now().Format("20060102-150405") + ext
			backupPath := filepath.Join(dir, backupName)
			if err := os.WriteFile(backupPath, data, 0644); err == nil {
				log.Printf("index backup: %s", backupPath)
			}
		}
		if err := clearIndexDB(); err != nil {
			http.Error(w, "clear: "+err.Error(), 500)
			return
		}
		runPipeline(w, "scan", 0)
	})

	vibeTemplates := map[string]string{
		"particles":      "precision mediump float;\n\nuniform float speed; // @expose 0.1 3\nuniform float count; // @expose 5 50\nuniform float size; // @expose 0.01 0.1\nuniform float glow; // @expose 0.5 5\nuniform float colorHue; // @expose 0 1\nuniform vec2 resolution;\nuniform float time;\n\nfloat hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }\n\nvoid main() {\n    vec2 uv = gl_FragCoord.xy / resolution;\n    vec3 col = vec3(0.0);\n    for (float i = 0.0; i < 50.0; i++) {\n        if (i >= count) break;\n        vec2 pos = vec2(hash(vec2(i, 0.0)), hash(vec2(0.0, i)));\n        pos = fract(pos + time * speed * vec2(hash(vec2(i, 1.0)) - 0.5, hash(vec2(1.0, i)) - 0.5));\n        float d = length(uv - pos);\n        float brightness = size / (d + 0.001) * glow * 0.01;\n        float h = fract(colorHue + i / count);\n        vec3 rgb = abs(mod(h * 6.0 + vec3(0.0, 4.0, 2.0), 6.0) - 3.0) - 1.0;\n        col += clamp(rgb, 0.0, 1.0) * brightness;\n    }\n    gl_FragColor = vec4(col, 1.0);\n}\n",
		"fractal":        "precision mediump float;\n\nuniform float zoom; // @expose 0.5 10\nuniform float iterations; // @expose 5 50\nuniform float colorCycle; // @expose 0 5\nuniform float offsetX; // @expose -2 2\nuniform float offsetY; // @expose -2 2\nuniform vec2 resolution;\nuniform float time;\n\nvoid main() {\n    vec2 uv = (gl_FragCoord.xy - 0.5 * resolution) / min(resolution.x, resolution.y);\n    vec2 c = uv / zoom + vec2(offsetX, offsetY);\n    vec2 z = vec2(0.0);\n    float n = 0.0;\n    for (int i = 0; i < 50; i++) {\n        if (float(i) >= iterations) break;\n        z = vec2(z.x * z.x - z.y * z.y, 2.0 * z.x * z.y) + c;\n        if (dot(z, z) > 4.0) break;\n        n++;\n    }\n    float t = n / iterations;\n    vec3 col = 0.5 + 0.5 * cos(colorCycle + t * 6.28 + vec3(0.0, 1.0, 2.0) + time * 0.3);\n    if (n >= iterations) col = vec3(0.0);\n    gl_FragColor = vec4(col, 1.0);\n}\n",
		"3d-sphere":      "precision mediump float;\n\nuniform float rotSpeed; // @expose 0.1 2\nuniform float sphereSize; // @expose 0.3 1.5\nuniform float lightX; // @expose -3 3\nuniform float lightY; // @expose -3 3\nuniform float specular; // @expose 0.1 5\nuniform float ambient; // @expose 0.05 0.5\nuniform float colorR; // @expose 0 1\nuniform float colorG; // @expose 0 1\nuniform float colorB; // @expose 0 1\nuniform vec2 resolution;\nuniform float time;\n\nfloat sdSphere(vec3 p, float r) { return length(p) - r; }\n\nmat3 rotY(float a) { float c = cos(a), s = sin(a); return mat3(c,0,s, 0,1,0, -s,0,c); }\nmat3 rotX(float a) { float c = cos(a), s = sin(a); return mat3(1,0,0, 0,c,-s, 0,s,c); }\n\nfloat scene(vec3 p) {\n    p = rotY(time * rotSpeed) * rotX(time * rotSpeed * 0.7) * p;\n    return sdSphere(p, sphereSize);\n}\n\nvec3 calcNormal(vec3 p) {\n    vec2 e = vec2(0.001, 0.0);\n    return normalize(vec3(scene(p+e.xyy)-scene(p-e.xyy), scene(p+e.yxy)-scene(p-e.yxy), scene(p+e.yyx)-scene(p-e.yyx)));\n}\n\nvoid main() {\n    vec2 uv = (gl_FragCoord.xy - 0.5 * resolution) / min(resolution.x, resolution.y);\n    vec3 ro = vec3(0.0, 0.0, 3.0);\n    vec3 rd = normalize(vec3(uv, -1.5));\n    float t = 0.0;\n    for (int i = 0; i < 64; i++) {\n        float d = scene(ro + rd * t);\n        if (d < 0.001 || t > 20.0) break;\n        t += d;\n    }\n    vec3 col = vec3(0.02);\n    if (t < 20.0) {\n        vec3 p = ro + rd * t;\n        vec3 n = calcNormal(p);\n        vec3 lDir = normalize(vec3(lightX, lightY, 2.0));\n        float diff = max(dot(n, lDir), 0.0);\n        vec3 h = normalize(lDir - rd);\n        float spec = pow(max(dot(n, h), 0.0), 32.0) * specular;\n        col = vec3(colorR, colorG, colorB) * (ambient + diff * 0.8) + vec3(1.0) * spec;\n    }\n    gl_FragColor = vec4(col, 1.0);\n}\n",
		"3d-cube":        "precision mediump float;\n\nuniform float rotSpeed; // @expose 0.1 2\nuniform float cubeSize; // @expose 0.3 1.2\nuniform float roundness; // @expose 0.0 0.3\nuniform float lightAngle; // @expose 0 6.28\nuniform float colorHue; // @expose 0 1\nuniform vec2 resolution;\nuniform float time;\n\nfloat sdBox(vec3 p, vec3 b) { vec3 q = abs(p) - b; return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0); }\nfloat sdRoundBox(vec3 p, vec3 b, float r) { return sdBox(p, b) - r; }\n\nmat3 rotY(float a) { float c=cos(a),s=sin(a); return mat3(c,0,s,0,1,0,-s,0,c); }\nmat3 rotX(float a) { float c=cos(a),s=sin(a); return mat3(1,0,0,0,c,-s,0,s,c); }\n\nfloat scene(vec3 p) {\n    p = rotY(time * rotSpeed) * rotX(time * rotSpeed * 0.6) * p;\n    return sdRoundBox(p, vec3(cubeSize), roundness);\n}\n\nvec3 calcNormal(vec3 p) {\n    vec2 e = vec2(0.001, 0.0);\n    return normalize(vec3(scene(p+e.xyy)-scene(p-e.xyy), scene(p+e.yxy)-scene(p-e.yxy), scene(p+e.yyx)-scene(p-e.yyx)));\n}\n\nvec3 hue2rgb(float h) { return clamp(abs(mod(h*6.0+vec3(0,4,2),6.0)-3.0)-1.0, 0.0, 1.0); }\n\nvoid main() {\n    vec2 uv = (gl_FragCoord.xy - 0.5 * resolution) / min(resolution.x, resolution.y);\n    vec3 ro = vec3(0,0,3.5), rd = normalize(vec3(uv,-1.5));\n    float t = 0.0;\n    for (int i = 0; i < 64; i++) {\n        float d = scene(ro + rd * t);\n        if (d < 0.001 || t > 20.0) break;\n        t += d;\n    }\n    vec3 col = vec3(0.02);\n    if (t < 20.0) {\n        vec3 p = ro + rd * t;\n        vec3 n = calcNormal(p);\n        vec3 lDir = normalize(vec3(cos(lightAngle), 1.0, sin(lightAngle)));\n        float diff = max(dot(n, lDir), 0.0);\n        float spec = pow(max(dot(reflect(-lDir, n), -rd), 0.0), 16.0);\n        col = hue2rgb(colorHue) * (0.1 + diff * 0.8) + vec3(1.0) * spec * 0.5;\n    }\n    gl_FragColor = vec4(col, 1.0);\n}\n",
		"3d-torus":       "precision mediump float;\n\nuniform float rotSpeed; // @expose 0.1 2\nuniform float torusR; // @expose 0.3 1.5\nuniform float tubeR; // @expose 0.1 0.6\nuniform float twist; // @expose 0 5\nuniform float colorHue; // @expose 0 1\nuniform vec2 resolution;\nuniform float time;\n\nfloat sdTorus(vec3 p, vec2 t) { vec2 q = vec2(length(p.xz)-t.x, p.y); return length(q)-t.y; }\nmat3 rotY(float a){float c=cos(a),s=sin(a);return mat3(c,0,s,0,1,0,-s,0,c);}\nmat3 rotX(float a){float c=cos(a),s=sin(a);return mat3(1,0,0,0,c,-s,0,s,c);}\n\nfloat scene(vec3 p) {\n    p = rotY(time*rotSpeed)*rotX(time*rotSpeed*0.5)*p;\n    float a = atan(p.z, p.x) * twist;\n    p.xz = mat2(cos(a),-sin(a),sin(a),cos(a)) * p.xz;\n    return sdTorus(p, vec2(torusR, tubeR));\n}\n\nvec3 calcNormal(vec3 p){vec2 e=vec2(0.001,0);return normalize(vec3(scene(p+e.xyy)-scene(p-e.xyy),scene(p+e.yxy)-scene(p-e.yxy),scene(p+e.yyx)-scene(p-e.yyx)));}\nvec3 hue2rgb(float h){return clamp(abs(mod(h*6.0+vec3(0,4,2),6.0)-3.0)-1.0,0.0,1.0);}\n\nvoid main() {\n    vec2 uv = (gl_FragCoord.xy-0.5*resolution)/min(resolution.x,resolution.y);\n    vec3 ro=vec3(0,0,4), rd=normalize(vec3(uv,-1.5));\n    float t=0.0;\n    for(int i=0;i<96;i++){float d=scene(ro+rd*t);if(d<0.001||t>20.0)break;t+=d;}\n    vec3 col=vec3(0.02);\n    if(t<20.0){vec3 p=ro+rd*t;vec3 n=calcNormal(p);vec3 l=normalize(vec3(1,1,2));float diff=max(dot(n,l),0.0);float spec=pow(max(dot(reflect(-l,n),-rd),0.0),32.0);col=hue2rgb(colorHue)*(0.1+diff*0.8)+vec3(1)*spec*0.4;}\n    gl_FragColor=vec4(col,1.0);\n}\n",
		"tunnel":         "precision mediump float;\n\nuniform float speed; // @expose 0.1 3\nuniform float twist; // @expose 0 10\nuniform float rings; // @expose 2 20\nuniform float colorCycle; // @expose 0 3\nuniform float brightness; // @expose 0.5 3\nuniform vec2 resolution;\nuniform float time;\n\nvoid main() {\n    vec2 uv = (gl_FragCoord.xy - 0.5 * resolution) / min(resolution.x, resolution.y);\n    float r = length(uv);\n    float a = atan(uv.y, uv.x) + twist * r;\n    float t = time * speed;\n    float pattern = sin(rings * a + t) * sin(1.0 / (r + 0.1) * 4.0 - t * 2.0);\n    vec3 col = 0.5 + 0.5 * cos(colorCycle + pattern * 3.14 + vec3(0.0, 2.0, 4.0));\n    col *= brightness / (r * 2.0 + 0.5);\n    gl_FragColor = vec4(col, 1.0);\n}\n",
		"audio-reactive": "precision mediump float;\n\nuniform float speed; // @expose 0.1 3\nuniform float intensity; // @expose 0.5 5\nuniform float colorShift; // @expose 0 3\nuniform float bassReact; // @expose 0 2\nuniform float trebleReact; // @expose 0 2\nuniform vec2 resolution;\nuniform float time;\n\nvoid main() {\n    vec2 uv = (gl_FragCoord.xy - 0.5 * resolution) / min(resolution.x, resolution.y);\n    float t = time * speed;\n    float wave = sin(uv.x * 8.0 + t) * cos(uv.y * 6.0 - t * 0.7);\n    wave += sin(length(uv) * 12.0 - t * 2.0) * bassReact * 0.5;\n    wave += sin(uv.x * 20.0 + uv.y * 15.0 + t * 3.0) * trebleReact * 0.3;\n    wave *= intensity * 0.3;\n    vec3 col = 0.5 + 0.5 * cos(colorShift + wave * 4.0 + vec3(0, 2, 4));\n    col *= 0.8 + 0.2 * wave;\n    gl_FragColor = vec4(col, 1.0);\n}\n",
		"gradient":       "precision mediump float;\n\nuniform float angle; // @expose 0 6.28\nuniform float softness; // @expose 0.1 5\nuniform float colorA_R; // @expose 0 1\nuniform float colorA_G; // @expose 0 1\nuniform float colorA_B; // @expose 0 1\nuniform float colorB_R; // @expose 0 1\nuniform float colorB_G; // @expose 0 1\nuniform float colorB_B; // @expose 0 1\nuniform vec2 resolution;\nuniform float time;\n\nvoid main() {\n    vec2 uv = gl_FragCoord.xy / resolution;\n    float t = dot(uv - 0.5, vec2(cos(angle), sin(angle))) * softness + 0.5;\n    t = clamp(t, 0.0, 1.0);\n    vec3 a = vec3(colorA_R, colorA_G, colorA_B);\n    vec3 b = vec3(colorB_R, colorB_G, colorB_B);\n    gl_FragColor = vec4(mix(a, b, t), 1.0);\n}\n",
		"kaleidoscope":   "precision mediump float;\n\nuniform float segments; // @expose 3 16\nuniform float zoom; // @expose 0.5 5\nuniform float speed; // @expose 0.1 2\nuniform float colorIntensity; // @expose 0.5 3\nuniform vec2 resolution;\nuniform float time;\n\nvoid main() {\n    vec2 uv = (gl_FragCoord.xy - 0.5 * resolution) / min(resolution.x, resolution.y);\n    float a = atan(uv.y, uv.x);\n    float r = length(uv) * zoom;\n    a = mod(a, 6.2832 / segments);\n    a = abs(a - 3.1416 / segments);\n    uv = vec2(cos(a), sin(a)) * r;\n    float t = time * speed;\n    float v = sin(uv.x * 8.0 + t) * cos(uv.y * 6.0 - t) + sin(r * 4.0 - t * 1.5);\n    vec3 col = 0.5 + 0.5 * cos(v * colorIntensity + vec3(0, 2, 4) + t * 0.5);\n    gl_FragColor = vec4(col, 1.0);\n}\n",
	}

	http.HandleFunc("/api/vibe-create", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		logSection("VIBE", "POST /api/vibe-create")
		var req struct {
			Name         string `json:"name"`
			Genre        string `json:"genre"`
			Description  string `json:"description"`
			CursorApiKey string `json:"cursorApiKey"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		if req.Name == "" {
			http.Error(w, "name required", 400)
			return
		}

		safeName := regexp.MustCompile(`[^a-zA-Z0-9_-]`).ReplaceAllString(strings.TrimSpace(req.Name), "-")
		if safeName == "" {
			safeName = "vibe-shader"
		}

		settingsMu.RLock()
		srcPaths := appSettings.SourcePaths
		settingsMu.RUnlock()
		baseDir := "shaders/glsl"
		if len(srcPaths) > 0 {
			candidate := filepath.Join(srcPaths[0], "glsl")
			if info, err := os.Stat(candidate); err == nil && info.IsDir() {
				baseDir = candidate
			} else {
				baseDir = srcPaths[0]
			}
		}

		filePath := filepath.Join(baseDir, safeName+".frag")
		for i := 2; ; i++ {
			if _, err := os.Stat(filePath); err != nil {
				break
			}
			filePath = filepath.Join(baseDir, fmt.Sprintf("%s-%d.frag", safeName, i))
		}

		template := vibeTemplates[req.Genre]
		if template == "" {
			template = vibeTemplates["particles"]
		}

		os.MkdirAll(filepath.Dir(filePath), 0755)
		if err := os.WriteFile(filePath, []byte(template), 0644); err != nil {
			http.Error(w, "write: "+err.Error(), 500)
			return
		}

		arr, _ := readIndex()
		if arr == nil {
			arr = []ShaderEntry{}
		}
		maxID := 0
		for _, e := range arr {
			if e.ID > maxID {
				maxID = e.ID
			}
		}
		arr = append(arr, ShaderEntry{
			ID:       maxID + 1,
			Path:     filePath,
			Name:     req.Name,
			Category: req.Genre,
			Tags:     []string{req.Genre, "vibe"},
			Format:   "glsl",
		})
		writeIndex(arr)

		logSection("VIBE", fmt.Sprintf("created %s (genre=%s, id=%d)", filePath, req.Genre, maxID+1))

		if req.Description != "" {
			// Try LLM chain first (Ollama), then fall back to cursor agent
			llmCode, llmErr := llmGenerateShader(template, req.Description, req.Genre)
			if llmErr == nil && llmCode != "" {
				// LLM generated enhanced shader - write it
				if err := os.WriteFile(filePath, []byte(llmCode), 0644); err != nil {
					logSection("VIBE", "write LLM result failed: "+err.Error())
				} else {
					logSection("VIBE", "LLM generated shader written to "+filePath)
				}
			} else {
				// Fall back to cursor agent
				logSection("VIBE", "LLM unavailable or failed, trying cursor agent")
				apiKey := req.CursorApiKey
				if apiKey == "" || apiKey == "***" {
					settingsMu.RLock()
					apiKey = appSettings.CursorApiKey
					settingsMu.RUnlock()
				}
				prompt := "You are a GLSL/ISF shader specialist creating a new shader for Macroverse Wired Atelier (VJ tool for Resolume Wire). " +
					"Edit ONLY the file at " + filePath + ". The file already has a starter template. " +
					"The user describes their vision: " + req.Description + ". " +
					"Transform the template to match this vision. Use creative GLSL techniques. " +
					"REQUIREMENTS: (1) Keep gl_FragColor output. (2) Use uniform float paramName; // @expose <min> <max> for ALL tweakable parameters. " +
					"(3) For 3D objects use raymarching with SDF functions. (4) For textures use uniform sampler2D. " +
					"(5) WebGL 1.0 compatible (GLSL ES 1.00, precision mediump float). (6) Be creative and visually impressive."
				cmd, cmdErr := buildAgentCmd(prompt)
				if cmdErr == nil {
					cmd.Dir = filepath.Dir(filePath)
					if apiKey != "" {
						cmd.Env = append(os.Environ(), "CURSOR_API_KEY="+apiKey)
					}
					cmd.Stdout = os.Stdout
					cmd.Stderr = os.Stderr
					if cmd.Start() == nil {
						go cmd.Wait()
						logSection("VIBE", "agent launched for "+filePath)
					}
				}
			}
		}

		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{
			"ok":   true,
			"id":   maxID + 1,
			"path": filePath,
			"name": req.Name,
		})
	})

	http.HandleFunc("/api/cursor-assist-visual", func(w http.ResponseWriter, r *http.Request) {
		logSection("CURSOR", "POST /api/cursor-assist-visual")
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		var req struct {
			Path         string `json:"path"`
			Prompt       string `json:"prompt"`
			Content      string `json:"content"`
			Screenshot   string `json:"screenshot"` // base64 PNG
			CursorApiKey string `json:"cursorApiKey"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		if req.Path == "" || req.Prompt == "" {
			http.Error(w, "path and prompt required", 400)
			return
		}
		path := strings.ReplaceAll(req.Path, "|", string(filepath.Separator))
		dir := filepath.Dir(path)
		screenshotPath := ""
		if req.Screenshot != "" {
			dec := func() ([]byte, error) {
				if idx := strings.Index(req.Screenshot, ","); idx >= 0 {
					req.Screenshot = req.Screenshot[idx+1:]
				}
				return base64.StdEncoding.DecodeString(req.Screenshot)
			}
			if data, err := dec(); err == nil && len(data) > 0 {
				screenshotPath = filepath.Join(dir, "_macroverse_preview.png")
				if err := os.WriteFile(screenshotPath, data, 0644); err != nil {
					log.Printf("cursor-assist-visual: write screenshot: %v", err)
					screenshotPath = ""
				}
			}
		}
		if req.Content != "" {
			if err := os.WriteFile(path, []byte(req.Content), 0644); err != nil {
				http.Error(w, "write: "+err.Error(), 500)
				return
			}
		}
		visualCtx := ""
		if screenshotPath != "" {
			abs, _ := filepath.Abs(screenshotPath)
			visualCtx = " A screenshot of the current shader output is saved at " + abs + ". Open and look at it to understand the visual result. "
		}
		prompt := "You are a GLSL/ISF shader specialist in Macroverse Wired Atelier (a VJ tool for Resolume Wire). " +
			"CRITICAL: Edit ONLY the file at " + path + ". Do not modify or reference any other files." + visualCtx +
			"The user wants (visually): " + req.Prompt + ". " +
			"Modify the shader to achieve this. Make minimal targeted changes. " +
			"Keep gl_FragColor output. Preserve existing uniforms and @expose annotations. " +
			"If adding new tweakable parameters, declare them as: uniform float paramName; // @expose <min> <max> " +
			"If the user wants texture/image/video inputs, add: uniform sampler2D <name>; and sample with texture2D(<name>, uv). " +
			"If the user wants ISF format, use ISF header with INPUTS array, RENDERSIZE instead of resolution, TIME instead of time, " +
			"and IMG_NORM_PIXEL(<name>, uv) for texture sampling. " +
			"The shader must be compatible with WebGL 1.0 (GLSL ES 1.00). Use precision mediump float. " +
			"For Wire compatibility: expose all interesting parameters with @expose, keep uniforms for time/resolution/mouse."

		// Try Ollama LLM chain first for visual assist (inpainting/outpainting)
		if req.Content != "" {
			ollamaPrompt := "You are a GLSL shader expert. Modify this shader based on the user's visual request.\n\n" +
				"User request: " + req.Prompt + "\n\n" +
				"Current shader:\n```glsl\n" + req.Content + "\n```\n\n" +
				"Output ONLY the complete modified shader code. Keep all existing uniforms and @expose annotations. " +
				"Must be WebGL 1.0 compatible. Keep gl_FragColor output. Be creative and visually impressive."
			providers := getLLMProvidersSorted()
			for _, p := range providers {
				if !p.Enabled || p.Name != "ollama" {
					continue
				}
				if ollamaIsAvailable(p.Endpoint) {
					logSection("LLM", "trying ollama for visual assist: "+req.Prompt)
					result, err := ollamaGenerate(p.Endpoint, p.Model, ollamaPrompt)
					if err == nil {
						code := extractShaderFromResponse(result)
						if code != "" {
							if err := os.WriteFile(path, []byte(code), 0644); err == nil {
								logSection("LLM", "ollama visual assist written to "+filepath.Base(path))
								if screenshotPath != "" {
									os.Remove(screenshotPath)
								}
								w.Header().Set("Content-Type", "application/json")
								json.NewEncoder(w).Encode(map[string]string{"message": "Ollama modified shader (visual). Reload to see changes."})
								return
							}
						}
					}
					logSection("LLM", "ollama visual assist failed, falling back to cursor agent")
				}
				break
			}
		}

		cmd, cmdErr := buildAgentCmd(prompt)
		if cmdErr != nil {
			http.Error(w, "cursor-agent or agent not found in PATH. Enable Ollama in Settings for local LLM support.", 503)
			return
		}
		cmd.Dir = dir
		apiKey := req.CursorApiKey
		if apiKey == "" || apiKey == "***" {
			settingsMu.RLock()
			apiKey = appSettings.CursorApiKey
			settingsMu.RUnlock()
		}
		if apiKey != "" {
			cmd.Env = append(os.Environ(), "CURSOR_API_KEY="+apiKey)
		}
		agentOutputBuf.mu.Lock()
		agentOutputBuf.lines = nil
		agentOutputBuf.mu.Unlock()
		tee := agentOutputWriter{}
		cmd.Stdout = io.MultiWriter(os.Stdout, &tee)
		cmd.Stderr = io.MultiWriter(os.Stderr, &tee)
		if err := cmd.Start(); err != nil {
			http.Error(w, "cursor-agent: "+err.Error(), 500)
			return
		}
		agentProc.Lock()
		agentProc.running = true
		agentProc.Unlock()
		agentOutputAppend("[visual-assist] Agent started for: " + req.Prompt)
		agentOutputAppend("[visual-assist] Agent editing " + filepath.Base(path) + " -- shader will reload when done")
		go func() {
			cmd.Wait()
			agentProc.Lock()
			agentProc.running = false
			agentProc.Unlock()
			agentOutputAppend("[visual-assist] Agent finished editing " + filepath.Base(path))
			if screenshotPath != "" {
				os.Remove(screenshotPath)
			}
		}()
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]string{"message": "Cursor agent launched (with screenshot). Reload shader after edit."})
	})

	http.HandleFunc("/api/cursor-assist", func(w http.ResponseWriter, r *http.Request) {
		logSection("CURSOR", "POST /api/cursor-assist")
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		var req struct {
			Path         string `json:"path"`
			Prompt       string `json:"prompt"`
			Content      string `json:"content"`
			CursorApiKey string `json:"cursorApiKey"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		if req.Path == "" || req.Prompt == "" {
			http.Error(w, "path and prompt required", 400)
			return
		}
		path := strings.ReplaceAll(req.Path, "|", string(filepath.Separator))
		if req.Content != "" {
			if err := os.WriteFile(path, []byte(req.Content), 0644); err != nil {
				http.Error(w, "write: "+err.Error(), 500)
				return
			}
		}
		prompt := "You are a GLSL/ISF shader specialist in Macroverse Wired Atelier (a VJ tool for Resolume Wire). " +
			"Edit ONLY the file at " + path + ". Do not modify or reference any other files. " +
			"The user wants: " + req.Prompt + ". " +
			"Make minimal targeted changes. Keep gl_FragColor output. Preserve existing uniforms and @expose annotations. " +
			"If adding new tweakable parameters, declare them as: uniform float paramName; // @expose <min> <max> " +
			"If adding texture/image/video inputs, use: uniform sampler2D <name>; and sample with texture2D(<name>, uv). " +
			"For ISF format compatibility: use INPUTS array in header, RENDERSIZE/TIME built-ins, IMG_NORM_PIXEL for textures. " +
			"The shader must be WebGL 1.0 compatible (GLSL ES 1.00, precision mediump float)."
		logSection("CURSOR", "cursor-assist editing: "+filepath.Base(path))

		// Try Ollama first for vibe coding (inpainting/outpainting)
		if req.Content != "" {
			ollamaPrompt := "You are a GLSL shader expert. Modify this shader based on the user request.\n\n" +
				"User request: " + req.Prompt + "\n\n" +
				"Current shader:\n```glsl\n" + req.Content + "\n```\n\n" +
				"Output ONLY the complete modified shader code. Keep all existing uniforms and @expose annotations. " +
				"Must be WebGL 1.0 compatible. Keep gl_FragColor output."
			providers := getLLMProvidersSorted()
			for _, p := range providers {
				if !p.Enabled || p.Name != "ollama" {
					continue
				}
				if ollamaIsAvailable(p.Endpoint) {
					logSection("LLM", "trying ollama for vibe coding: "+req.Prompt)
					result, err := ollamaGenerate(p.Endpoint, p.Model, ollamaPrompt)
					if err == nil {
						code := extractShaderFromResponse(result)
						if code != "" {
							if err := os.WriteFile(path, []byte(code), 0644); err == nil {
								logSection("LLM", "ollama vibe code written to "+filepath.Base(path))
								w.Header().Set("Content-Type", "application/json")
								json.NewEncoder(w).Encode(map[string]string{"message": "Ollama modified shader. Reload to see changes."})
								return
							}
						}
					}
					logSection("LLM", "ollama vibe coding failed, falling back to cursor agent")
				}
				break
			}
		}

		cmd, cmdErr := buildAgentCmd(prompt)
		if cmdErr != nil {
			http.Error(w, "cursor-agent or agent not found in PATH", 503)
			return
		}
		cmd.Dir = filepath.Dir(path)
		apiKey := req.CursorApiKey
		if apiKey == "" || apiKey == "***" {
			settingsMu.RLock()
			apiKey = appSettings.CursorApiKey
			settingsMu.RUnlock()
		}
		if apiKey != "" {
			cmd.Env = append(os.Environ(), "CURSOR_API_KEY="+apiKey)
		}
		agentOutputBuf.mu.Lock()
		agentOutputBuf.lines = nil
		agentOutputBuf.mu.Unlock()
		tee := agentOutputWriter{}
		cmd.Stdout = io.MultiWriter(os.Stdout, &tee)
		cmd.Stderr = io.MultiWriter(os.Stderr, &tee)
		if err := cmd.Start(); err != nil {
			http.Error(w, "cursor-agent: "+err.Error(), 500)
			return
		}
		agentProc.Lock()
		agentProc.running = true
		agentProc.Unlock()
		agentOutputAppend("[cursor-assist] Agent editing " + filepath.Base(path) + ": " + req.Prompt)
		go func() {
			cmd.Wait()
			agentProc.Lock()
			agentProc.running = false
			agentProc.Unlock()
			agentOutputAppend("[cursor-assist] Agent finished editing " + filepath.Base(path))
		}()
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]string{"message": "Cursor agent launched for vibe coding"})
	})

	http.HandleFunc("/api/agent-status", func(w http.ResponseWriter, r *http.Request) {
		agentProc.Lock()
		online := agentProc.running
		agentProc.Unlock()
		remaining := agentCooldownRemainingSec()
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{"online": online, "cooldownRemainingSec": remaining})
	})

	http.HandleFunc("/api/agent-output", func(w http.ResponseWriter, r *http.Request) {
		agentOutputBuf.mu.Lock()
		lines := make([]string, len(agentOutputBuf.lines))
		copy(lines, agentOutputBuf.lines)
		agentOutputBuf.mu.Unlock()
		output := strings.Join(lines, "\n")
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{"lines": lines, "output": output})
	})

	http.HandleFunc("/api/thumbnails", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodGet && r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		thumbnailsMu.Lock()
		if len(thumbnailsCache) == 0 {
			loadThumbnailsCache()
		}
		out := make(map[string]string)
		var pathsToFetch []string
		if r.Method == http.MethodPost {
			var req struct {
				Paths []string `json:"paths"`
			}
			if json.NewDecoder(r.Body).Decode(&req) == nil && len(req.Paths) > 0 {
				pathsToFetch = req.Paths
			}
		} else if r.Method == http.MethodGet {
			if pathsParam := r.URL.Query().Get("paths"); pathsParam != "" {
				for _, k := range strings.Split(pathsParam, "|") {
					k = strings.TrimSpace(strings.ReplaceAll(k, "\\", "|"))
					if k != "" {
						pathsToFetch = append(pathsToFetch, k)
					}
				}
			}
		}
		if len(pathsToFetch) > 0 {
			for _, k := range pathsToFetch {
				k = strings.TrimSpace(strings.ReplaceAll(k, "\\", "|"))
				if k == "" {
					continue
				}
				if v, ok := thumbnailsCache[k]; ok && v != "" {
					out[k] = v
				}
			}
		} else if r.Method == http.MethodGet {
			for k, v := range thumbnailsCache {
				if v != "" {
					out[k] = v
				}
			}
		}
		thumbnailsMu.Unlock()
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(out)
	})

	http.HandleFunc("/api/thumbnail", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		cloudOnly := isReadonlyHost()
		if !cloudOnly && writeBlocked(w) {
			return
		}
		var req struct {
			Path    string `json:"path"`
			DataURL string `json:"dataUrl"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		pathNorm := strings.TrimSpace(req.Path)
		if pathNorm == "" || req.DataURL == "" {
			http.Error(w, "path and dataUrl required", 400)
			return
		}
		key := strings.ReplaceAll(pathNorm, "\\", "|")
		thumbnailsMu.Lock()
		if len(thumbnailsCache) == 0 {
			loadThumbnailsCache()
		}
		thumbnailsCache[key] = req.DataURL
		var err error
		if !cloudOnly {
			err = saveThumbnailsCache()
		}
		thumbnailsMu.Unlock()
		if err != nil {
			http.Error(w, "write: "+err.Error(), 500)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]bool{"ok": true})
	})

	http.HandleFunc("/api/cursor-fix", func(w http.ResponseWriter, r *http.Request) {
		logSection("CURSOR", "POST /api/cursor-fix")
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			Path           string   `json:"path"`
			Error          string   `json:"error"`
			Content        string   `json:"content"`
			CursorApiKey   string   `json:"cursorApiKey"`
			Filename       string   `json:"filename"`
			IsISF          bool     `json:"isISF"`
			Confirm        bool     `json:"confirm"`
			AttemptNumber  int      `json:"attemptNumber"`
			PreviousErrors []string `json:"previousErrors"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			w.Header().Set("Content-Type", "application/json")
			w.WriteHeader(400)
			json.NewEncoder(w).Encode(map[string]string{"error": err.Error()})
			return
		}
		if req.Path == "" || req.Error == "" {
			w.Header().Set("Content-Type", "application/json")
			w.WriteHeader(400)
			json.NewEncoder(w).Encode(map[string]string{"error": "path and error required"})
			return
		}
		path := strings.ReplaceAll(req.Path, "|", string(filepath.Separator))
		path = filepath.FromSlash(path)
		if path != "" && !filepath.IsAbs(path) {
			if vfx := getVfxRoot(); vfx != "" {
				base := filepath.Base(vfx)
				sep := string(filepath.Separator)
				match := base != "" && (strings.EqualFold(path, base) ||
					(len(path) > len(base)+len(sep) && strings.EqualFold(path[:len(base)], base) && path[len(base):len(base)+1] == sep))
				if match {
					trimmed := path
					if len(path) > len(base) {
						trimmed = path[len(base):]
						trimmed = strings.TrimPrefix(trimmed, sep)
					} else {
						trimmed = ""
					}
					path = filepath.Join(vfx, trimmed)
				} else {
					path = filepath.Join(vfx, path)
				}
			}
		}

		src := stripLeadingGarbageShader(req.Content)
		compileErr := req.Error
		fixed := false
		validFragment := func(s string) bool {
			s = strings.TrimSpace(s)
			if s == "" {
				return false
			}
			return strings.Contains(s, "void main(") || strings.Contains(s, "main()")
		}
		if strings.Contains(compileErr, "Missing main()") && (src == "" || !validFragment(src)) {
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(map[string]interface{}{
				"needsAgent":    false,
				"message":       "This shader is empty or has no main(). Trash it or paste valid code.",
				"unrecoverable": true,
			})
			return
		}
		if src != "" {
			orig := src
			// Fix token corruptions from prior fix attempts (e.g. Uuniform, uuniform, Ununiform)
			if strings.Contains(compileErr, "syntax error") {
				tokenRe := regexp.MustCompile(`'(\w+)'\s*:\s*syntax error`)
				for _, m := range tokenRe.FindAllStringSubmatch(compileErr, 3) {
					if len(m) < 2 {
						continue
					}
					bad := m[1]
					replacement := ""
					switch bad {
					case "Uuniform", "uuniform", "Ununiform", "unifrom", "uniorm":
						replacement = "uniform"
					case "flaot":
						replacement = "float"
					case "vce2", "vce3", "vce4":
						replacement = "vec" + bad[len(bad)-1:]
					}
					if replacement != "" && strings.Contains(src, bad) {
						src = strings.ReplaceAll(src, bad, replacement)
						fixed = true
						logSection("CURSOR", "deep-fix: corrected token '"+bad+"' -> '"+replacement+"' (syntax error corruption)")
						break
					}
				}
			}
			// --- Fix: Illegal character at fieldname start (';' or ',') ---
			// Occurs when e.g. "x.;" or "x.," is parsed as "x." then "." (field) then ";" or "," (illegal). Fix: ".;" -> ";", ".," -> ","
			if strings.Contains(compileErr, "Illegal character at fieldname start") {
				if strings.Contains(compileErr, "';'") && strings.Contains(src, ".;") {
					src = strings.ReplaceAll(src, ".;", ";")
					fixed = true
					logSection("CURSOR", "deep-fix: fixed stray .; (dot-semicolon)")
				}
				if !fixed && strings.Contains(compileErr, "','") && strings.Contains(src, ".,") {
					src = strings.ReplaceAll(src, ".,", ",")
					fixed = true
					logSection("CURSOR", "deep-fix: fixed stray ., (dot-comma)")
				}
			}
			// --- Fix: Illegal character at fieldname start ('-') ---
			// Occurs when e.g. "1.-x" is parsed as float "1." then "." (field) then "-" (illegal field start).
			// Fix: ".-" -> " - " so "1.-x" becomes "1. - x" (or "1.0 - x").
			if !fixed && strings.Contains(compileErr, "Illegal character at fieldname start") && strings.Contains(compileErr, "'-'") {
				lines := strings.Split(src, "\n")
				changed := false
				for i, oldLine := range lines {
					// Fix "number.-identifier" -> "number - identifier" (e.g. 3.-x, 1.-speed)
					newLine := regexp.MustCompile(`(\d+)\.-(\s*)(\w)`).ReplaceAllString(oldLine, "${1}.0 - ${2}${3}")
					// Fix "identifier.-identifier" -> "identifier - identifier" (typo: .- meant -)
					if newLine == oldLine {
						newLine = regexp.MustCompile(`\.-(\s*)(\w)`).ReplaceAllString(oldLine, " - ${1}${2}")
					}
					if newLine != oldLine {
						lines[i] = newLine
						changed = true
					}
				}
				if changed {
					src = strings.Join(lines, "\n")
					fixed = true
					logSection("CURSOR", "deep-fix: fixed illegal .- (dot-minus parsed as field access)")
				}
			}
			if strings.Contains(compileErr, "global variable initializers should be constant") || strings.Contains(compileErr, "constant expressions") {
				prev := src
				globalInitRe := regexp.MustCompile(`(?m)^(\s*)(float|vec2|vec3|vec4)\s+(\w+)\s*=\s*[^;]+;\s*$`)
				src = globalInitRe.ReplaceAllString(src, "${1}uniform ${2} ${3};\n")
				if src != prev {
					fixed = true
				}
			}
			if !fixed && (strings.Contains(compileErr, "'const' : no qualifiers") || strings.Contains(compileErr, "no qualifiers allowed")) {
				re := regexp.MustCompile(`(?m)^(\s*)const\s+(float|vec[234])\s+(\w+)\s*=`)
				src = re.ReplaceAllString(src, "${1}${2} ${3} =")
				if src != orig {
					fixed = true
				}
			}
			if strings.Contains(compileErr, "undeclared identifier") {
				// --- Fix common RENDERSIZE typos first (so we don't add a uniform for the typo) ---
				for _, typo := range []string{"RENDE_SIZE", "RENDER_SIZE"} {
					if strings.Contains(compileErr, "'"+typo+"'") && strings.Contains(src, typo) {
						prev := src
						src = strings.ReplaceAll(src, typo, "RENDERSIZE")
						if src != prev {
							fixed = true
							logSection("CURSOR", "deep-fix: corrected typo "+typo+" -> RENDERSIZE")
						}
						break
					}
				}
				nameRe := regexp.MustCompile(`'(\w+)'\s*:\s*undeclared identifier`)
				allMatches := nameRe.FindAllStringSubmatch(compileErr, -1)
				seen := make(map[string]bool)
				for _, m := range allMatches {
					if len(m) < 2 || seen[m[1]] {
						continue
					}
					seen[m[1]] = true
					name := m[1]
					switch name {
					case "RENDE_SIZE", "RENDER_SIZE":
						prev := src
						src = strings.ReplaceAll(src, name, "RENDERSIZE")
						if src != prev {
							fixed = true
							logSection("CURSOR", "deep-fix: corrected typo "+name+" -> RENDERSIZE")
						}
					case "time":
						if !strings.Contains(src, "uniform float time") && !strings.Contains(src, "#define time") {
							src = "uniform float time;\n" + src
							fixed = true
						}
					case "mouse":
						if !strings.Contains(src, "uniform vec2 mouse") && !strings.Contains(src, "#define mouse") {
							src = "uniform vec2 mouse;\n" + src
							fixed = true
						}
					case "resolution":
						if !strings.Contains(src, "uniform vec2 resolution") && !strings.Contains(src, "#define resolution") {
							src = "uniform vec2 resolution;\n" + src
							fixed = true
						}
					case "iGlobalTime", "iTime":
						if !strings.Contains(src, "#define "+name) && !strings.Contains(src, "uniform float "+name) {
							src = "#ifndef " + name + "\n#define " + name + " TIME\n#endif\n" + src
							fixed = true
						}
					case "iResolution":
						if !strings.Contains(src, "#define iResolution") && !strings.Contains(src, "uniform vec2 iResolution") {
							src = "#ifndef iResolution\n#define iResolution RENDERSIZE\n#endif\n" + src
							fixed = true
						}
					case "iMouse":
						if !strings.Contains(src, "#define iMouse") && !strings.Contains(src, "uniform vec4 iMouse") {
							src = "#ifndef iMouse\n#define iMouse vec4(uMouse,0.,0.)\n#endif\n" + src
							fixed = true
						}
					case "iTimeDelta", "iFrame":
						if !strings.Contains(src, "#define "+name) && !strings.Contains(src, "uniform float "+name) {
							val := "0.016"
							if name == "iFrame" {
								val = "0.0"
							}
							src = "#ifndef " + name + "\n#define " + name + " " + val + "\n#endif\n" + src
							fixed = true
						}
					default:
						commonFloatUniforms := map[string]bool{
							"zoom": true, "speed": true, "scale": true, "intensity": true, "amount": true,
							"strength": true, "density": true, "iterations": true, "detail": true,
							"brightness": true, "contrast": true, "saturation": true, "frequency": true,
							"amplitude": true, "phase": true, "offset": true, "radius": true,
							"colorR": true, "colorG": true, "colorB": true, "hueShift": true,
						}
						commonBoolUniforms := map[string]bool{
							"invert": true,
						}
						if commonBoolUniforms[name] {
							uniformLine := "uniform bool " + name + ";\n"
							if !strings.Contains(src, "uniform bool "+name) && !strings.Contains(src, "#define "+name) {
								src = uniformLine + src
								fixed = true
							}
						} else if commonFloatUniforms[name] {
							uniformLine := "uniform float " + name + ";\n"
							if !strings.Contains(src, "uniform float "+name) && !strings.Contains(src, "#define "+name) {
								src = uniformLine + src
								fixed = true
							}
						}
					}
				}
			}
			if strings.Contains(compileErr, "redefinition") && strings.Contains(compileErr, "RENDERSIZE") {
				prev := src
				redefRe := regexp.MustCompile(`(?m)#ifndef\s+RENDERSIZE\s*\n#define\s+RENDERSIZE\s+[^\n]+\n#endif\s*\n?`)
				src = redefRe.ReplaceAllString(src, "")
				if src != prev {
					fixed = true
				}
			}
			if strings.Contains(compileErr, "precision") && !strings.Contains(src, "precision ") {
				src = "precision highp float;\n" + src
				fixed = true
			}
			if strings.Contains(compileErr, "texture") && strings.Contains(compileErr, "no matching") {
				src = strings.ReplaceAll(src, "texture(", "texture2D(")
				if src != orig {
					fixed = true
				}
			}
			if strings.Contains(compileErr, "Fsqrt") || (strings.Contains(compileErr, "no matching overloaded") && strings.Contains(compileErr, "qrt")) {
				src = strings.ReplaceAll(src, "Fsqrt(", "sqrt(")
				if src != orig {
					fixed = true
				}
			}
			if strings.Contains(compileErr, "wrong operand types") && strings.Contains(compileErr, "int") && strings.Contains(compileErr, "float") {
				forRe := regexp.MustCompile(`for\s*\(\s*(int\s+\w+\s*=\s*\d+)\s*;\s*(\w+)\s*<\s*(\w+)\s*;`)
				src = forRe.ReplaceAllString(src, "for ($1; $2 < int($3);")
				if src != orig {
					fixed = true
				}
			}
			if strings.Contains(compileErr, "wrong operand types") && (strings.Contains(compileErr, "'const int'") || strings.Contains(compileErr, "'mediump int'") || strings.Contains(compileErr, "'highp int'")) {
				prev := src
				ops := []string{`\*`, `/`, `\+`, `-`}
				for _, op := range ops {
					floatOpInt := regexp.MustCompile(`(\d+\.\d*|\.\d+)\s*` + op + `\s*(\d+)([^.\d\w])`)
					src = floatOpInt.ReplaceAllString(src, "${1} "+strings.ReplaceAll(op, `\`, "")+" ${2}.0${3}")
					intOpFloat := regexp.MustCompile(`([^.\d\w])(\d+)\s*` + op + `\s*(\d+\.\d*|\.\d+)`)
					src = intOpFloat.ReplaceAllString(src, "${1}${2}.0 "+strings.ReplaceAll(op, `\`, "")+" ${3}")
				}
				if src != prev {
					fixed = true
				}
			}
			if strings.Contains(compileErr, "l-value required") || strings.Contains(compileErr, "l-value") {
				prev := src
				constAssignRe := regexp.MustCompile(`(?m)^(\s*)const\s+(float|int|vec[234]|mat[234])\s+(\w+)\s*=`)
				src = constAssignRe.ReplaceAllString(src, "${1}${2} ${3} =")
				if src != prev {
					fixed = true
				}
			}
			if (strings.Contains(compileErr, "'uniform' : only allowed at global scope") || strings.Contains(compileErr, "invalid qualifier combination")) &&
				(strings.Contains(compileErr, "for") || strings.Contains(compileErr, "Invalid init declaration")) {
				prev := src
				forLoopUniformRe := regexp.MustCompile(`for\s*\(\s*uniform\s+(float|int)\s+(\w+)\s*=`)
				src = forLoopUniformRe.ReplaceAllString(src, "for (${1} ${2} =")
				if src != prev {
					fixed = true
				}
			}
			if (strings.Contains(compileErr, "'uniform' : only allowed at global scope") || strings.Contains(compileErr, "Local variables can only use the const")) &&
				!fixed {
				prev := src
				uniformInFuncRe := regexp.MustCompile(`(?m)^(\s*)uniform\s+(float|vec[234]|bool|int)\s+(\w+)\s*;\s*$`)
				mainIdx := strings.Index(prev, "void main(")
				if mainIdx < 0 {
					mainIdx = strings.Index(prev, "main()")
				}
				if mainIdx > 0 {
					var toAdd []string
					added := make(map[string]bool)
					var toRemove [][2]int
					for _, m := range uniformInFuncRe.FindAllStringSubmatchIndex(prev, -1) {
						if m[0] > mainIdx && len(m) >= 8 {
							name := prev[m[6]:m[7]]
							typ := prev[m[4]:m[5]]
							key := typ + " " + name
							if !strings.Contains(prev[:mainIdx], "uniform "+typ+" "+name+" ") &&
								!strings.Contains(prev[:mainIdx], "uniform "+typ+" "+name+";") &&
								!added[key] {
								added[key] = true
								toAdd = append(toAdd, "uniform "+typ+" "+name+";\n")
							}
							toRemove = append(toRemove, [2]int{m[0], m[1]})
						}
					}
					if len(toRemove) > 0 {
						for i := len(toRemove) - 1; i >= 0; i-- {
							r := toRemove[i]
							prev = prev[:r[0]] + prev[r[1]:]
						}
						insert := 0
						if idx := strings.Index(prev, "*/"); idx >= 0 && idx < 800 {
							insert = idx + 2
						} else if ext := regexp.MustCompile(`(?m)^(\s*#(?:version|extension)[^\n]*\n)*`).FindString(prev); ext != "" {
							insert = len(ext)
						}
						for _, line := range toAdd {
							prev = prev[:insert] + "\n" + line + prev[insert:]
							insert += len(line) + 1
						}
						src = prev
						fixed = true
					}
				}
			}
			if strings.Contains(compileErr, "surfacePosition") && strings.Contains(compileErr, "does not match any VERTEX varying") {
				prev := src
				varyingRe := regexp.MustCompile(`(?m)^\s*varying\s+(?:vec[234]|float)\s+surfacePosition\s*;\s*\n?`)
				src = varyingRe.ReplaceAllString(src, "")
				if src != prev && !strings.Contains(src, "#define surfacePosition") {
					def := "#define surfacePosition v_uv\n"
					insert := 0
					if idx := strings.Index(src, "*/"); idx >= 0 && idx < 600 {
						insert = idx + 2
					} else if ext := regexp.MustCompile(`(?m)^(\s*#(?:version|extension)[^\n]*\n)*`).FindString(src); ext != "" {
						insert = len(ext)
					}
					src = src[:insert] + "\n" + def + src[insert:]
					fixed = true
				}
			}

			// --- DEEP FIX: unknown function calls like test(...) -> vec3(...) ---
			if strings.Contains(compileErr, "no matching overloaded function found") {
				funcNameRe := regexp.MustCompile(`'(\w+)'\s*:\s*no matching overloaded function found`)
				for _, m := range funcNameRe.FindAllStringSubmatch(compileErr, -1) {
					if len(m) < 2 {
						continue
					}
					badFunc := m[1]
					glslBuiltins := map[string]bool{
						"sin": true, "cos": true, "tan": true, "asin": true, "acos": true, "atan": true,
						"pow": true, "exp": true, "log": true, "exp2": true, "log2": true, "sqrt": true,
						"abs": true, "sign": true, "floor": true, "ceil": true, "fract": true, "mod": true,
						"min": true, "max": true, "clamp": true, "mix": true, "step": true, "smoothstep": true,
						"length": true, "distance": true, "dot": true, "cross": true, "normalize": true,
						"reflect": true, "refract": true, "texture2D": true, "texture": true,
						"vec2": true, "vec3": true, "vec4": true, "mat2": true, "mat3": true, "mat4": true,
						"float": true, "int": true, "bool": true, "ivec2": true, "ivec3": true, "ivec4": true,
						"radians": true, "degrees": true, "inversesqrt": true, "fwidth": true, "dFdx": true, "dFdy": true,
					}
					if glslBuiltins[badFunc] {
						continue
					}
					callRe := regexp.MustCompile(`\b` + regexp.QuoteMeta(badFunc) + `\s*\(`)
					locs := callRe.FindAllStringIndex(src, -1)
					for _, loc := range locs {
						after := src[loc[1]:]
						depth := 1
						end := 0
						for i, ch := range after {
							if ch == '(' {
								depth++
							} else if ch == ')' {
								depth--
								if depth == 0 {
									end = i
									break
								}
							}
						}
						if end == 0 {
							continue
						}
						args := after[:end]
						commaCount := strings.Count(args, ",")
						var replacement string
						switch commaCount {
						case 0:
							replacement = "float"
						case 1:
							replacement = "vec2"
						case 2:
							replacement = "vec3"
						case 3:
							replacement = "vec4"
						default:
							replacement = "vec4"
						}
						src = src[:loc[0]] + replacement + src[loc[0]+len(badFunc):]
						fixed = true
						logSection("CURSOR", fmt.Sprintf("deep-fix: replaced unknown function '%s' with '%s'", badFunc, replacement))
						break
					}
				}
			}

			// --- DEEP FIX: field selection requires structure or vector (.rgb .rgba .xy etc on non-vector) ---
			if !fixed && strings.Contains(compileErr, "field selection requires structure or vector") {
				fieldRe := regexp.MustCompile(`0:(\d+):\s*'(\w+)'\s*:\s*field selection`)
				for _, m := range fieldRe.FindAllStringSubmatch(compileErr, 3) {
					if len(m) < 3 {
						continue
					}
					lineNum, _ := strconv.Atoi(m[1])
					field := m[2]
					if lineNum < 1 {
						continue
					}
					lines := strings.Split(src, "\n")
					lineIdx := lineNum - 1
					if lineIdx >= len(lines) {
						continue
					}
					line := lines[lineIdx]
					switch field {
					case "rgb":
						swizzleRe := regexp.MustCompile(`(\w+)\.rgb\b`)
						if swizzleRe.MatchString(line) {
							line = swizzleRe.ReplaceAllString(line, "vec3($1, $1, $1)")
							lines[lineIdx] = line
							src = strings.Join(lines, "\n")
							fixed = true
							logSection("CURSOR", fmt.Sprintf("deep-fix: replaced var.rgb with vec3(var,var,var) at line %d (field selection)", lineNum))
						}
					case "rgba":
						swizzleRe := regexp.MustCompile(`(\w+)\.rgba\b`)
						if swizzleRe.MatchString(line) {
							line = swizzleRe.ReplaceAllString(line, "vec4($1, $1, $1, 1.0)")
							lines[lineIdx] = line
							src = strings.Join(lines, "\n")
							fixed = true
							logSection("CURSOR", fmt.Sprintf("deep-fix: replaced var.rgba with vec4(var,var,var,1) at line %d (field selection)", lineNum))
						}
					case "xy", "xz", "yz":
						escField := regexp.QuoteMeta(field)
						swizzleRe := regexp.MustCompile(`(\w+)\.` + escField + `\b`)
						if swizzleRe.MatchString(line) {
							line = swizzleRe.ReplaceAllString(line, "vec2($1, $1)")
							lines[lineIdx] = line
							src = strings.Join(lines, "\n")
							fixed = true
							logSection("CURSOR", fmt.Sprintf("deep-fix: replaced var.%s with vec2(var,var) at line %d (field selection)", field, lineNum))
						}
					case "xyz":
						swizzleRe := regexp.MustCompile(`(\w+)\.xyz\b`)
						if swizzleRe.MatchString(line) {
							line = swizzleRe.ReplaceAllString(line, "vec3($1, $1, $1)")
							lines[lineIdx] = line
							src = strings.Join(lines, "\n")
							fixed = true
							logSection("CURSOR", fmt.Sprintf("deep-fix: replaced var.xyz with vec3(var,var,var) at line %d (field selection)", lineNum))
						}
					case "x", "y", "z", "w":
						escField := regexp.QuoteMeta(field)
						swizzleRe := regexp.MustCompile(`(\w+)\.` + escField + `\b`)
						if swizzleRe.MatchString(line) {
							line = swizzleRe.ReplaceAllString(line, "$1")
							lines[lineIdx] = line
							src = strings.Join(lines, "\n")
							fixed = true
							logSection("CURSOR", fmt.Sprintf("deep-fix: replaced var.%s with var at line %d (scalar field on non-vector)", field, lineNum))
						}
					}
					if fixed {
						break
					}
				}
			}

			// --- DEEP FIX: missing semicolons (syntax error at '}') ---
			if strings.Contains(compileErr, "syntax error") {
				lineRe := regexp.MustCompile(`0:(\d+):.*syntax error`)
				for _, m := range lineRe.FindAllStringSubmatch(compileErr, 5) {
					if len(m) < 2 {
						continue
					}
					lineNum, _ := strconv.Atoi(m[1])
					if lineNum < 1 {
						continue
					}
					lines := strings.Split(src, "\n")
					checkLine := lineNum - 2
					if checkLine < 0 {
						checkLine = 0
					}
					if checkLine >= len(lines) {
						continue
					}
					trimmed := strings.TrimSpace(lines[checkLine])
					if trimmed != "" && !strings.HasSuffix(trimmed, ";") && !strings.HasSuffix(trimmed, "{") &&
						!strings.HasSuffix(trimmed, "}") && !strings.HasSuffix(trimmed, "*/") &&
						!strings.HasPrefix(trimmed, "//") && !strings.HasPrefix(trimmed, "#") &&
						!strings.HasPrefix(trimmed, "/*") && !strings.HasSuffix(trimmed, ",") {
						lines[checkLine] = lines[checkLine] + ";"
						src = strings.Join(lines, "\n")
						fixed = true
						logSection("CURSOR", fmt.Sprintf("deep-fix: added missing semicolon at line %d", checkLine+1))
					}
				}
			}

			// --- DEEP FIX: dimension mismatch / cannot convert ---
			if (strings.Contains(compileErr, "dimension mismatch") || strings.Contains(compileErr, "cannot convert from")) &&
				strings.Contains(compileErr, "no matching overloaded function found") {
				// already handled above
			}

			// --- DEEP FIX: remaining undeclared identifiers - aggressive default injection ---
			if !fixed && strings.Contains(compileErr, "undeclared identifier") {
				nameRe := regexp.MustCompile(`'(\w+)'\s*:\s*undeclared identifier`)
				allMatches := nameRe.FindAllStringSubmatch(compileErr, -1)
				seenDeep := make(map[string]bool)
				for _, m := range allMatches {
					if len(m) < 2 || seenDeep[m[1]] {
						continue
					}
					seenDeep[m[1]] = true
					name := m[1]
					switch {
					case name == "time" || name == "mouse" || name == "resolution":
						continue
					case name == "PI" || name == "pi":
						if !strings.Contains(src, "#define "+name) && !strings.Contains(src, "float "+name) {
							src = "#define " + name + " 3.14159265359\n" + src
							fixed = true
						}
					case name == "TAU" || name == "tau":
						if !strings.Contains(src, "#define "+name) && !strings.Contains(src, "float "+name) {
							src = "#define " + name + " 6.28318530718\n" + src
							fixed = true
						}
					case name == "HALF_PI":
						if !strings.Contains(src, "#define "+name) && !strings.Contains(src, "float "+name) {
							src = "#define " + name + " 1.57079632679\n" + src
							fixed = true
						}
					default:
						re := regexp.MustCompile(`\b` + regexp.QuoteMeta(name) + `\b`)
						usages := re.FindAllStringIndex(src, -1)
						if len(usages) == 0 {
							continue
						}
						contextGuessed := false
						for _, u := range usages {
							before := ""
							if u[0] > 0 {
								start := u[0] - 40
								if start < 0 {
									start = 0
								}
								before = src[start:u[0]]
							}
							after := ""
							end := u[1] + 40
							if end > len(src) {
								end = len(src)
							}
							after = src[u[1]:end]

							if strings.Contains(after, "*") || strings.Contains(after, "+") || strings.Contains(after, "-") ||
								strings.Contains(after, "/") || strings.Contains(before, "*") || strings.Contains(before, "+") {
								if !strings.Contains(src, "uniform float "+name) && !strings.Contains(src, "float "+name) &&
									!strings.Contains(src, "#define "+name) {
									src = "uniform float " + name + "; // @expose 0 1\n" + src
									fixed = true
									contextGuessed = true
									logSection("CURSOR", fmt.Sprintf("deep-fix: added 'uniform float %s' for undeclared identifier used in arithmetic", name))
								}
								break
							}
							if strings.Contains(before, "vec3(") || strings.Contains(before, "vec4(") ||
								strings.Contains(before, "vec2(") || strings.Contains(after, ")") {
								if !strings.Contains(src, "uniform float "+name) && !strings.Contains(src, "float "+name) &&
									!strings.Contains(src, "#define "+name) {
									src = "uniform float " + name + "; // @expose 0 1\n" + src
									fixed = true
									contextGuessed = true
									logSection("CURSOR", fmt.Sprintf("deep-fix: added 'uniform float %s' for undeclared identifier in vector context", name))
								}
								break
							}
						}
						if !contextGuessed && !fixed {
							if !strings.Contains(src, "float "+name) && !strings.Contains(src, "#define "+name) &&
								!strings.Contains(src, "uniform float "+name) {
								src = "uniform float " + name + "; // @expose 0 1\n" + src
								fixed = true
								logSection("CURSOR", fmt.Sprintf("deep-fix: added 'uniform float %s' as fallback for undeclared identifier", name))
							}
						}
					}
				}
			}

			// --- DEEP FIX: missing closing brace ---
			if !fixed && strings.Contains(compileErr, "syntax error") {
				openCount := strings.Count(src, "{")
				closeCount := strings.Count(src, "}")
				if openCount > closeCount {
					for openCount > closeCount {
						src = src + "\n}"
						closeCount++
					}
					fixed = true
					logSection("CURSOR", "deep-fix: added missing closing brace(s)")
				}
			}

			// --- DEEP FIX: gl_FragData[0] -> gl_FragColor ---
			if strings.Contains(compileErr, "gl_FragData") && strings.Contains(compileErr, "undeclared") {
				prev := src
				src = strings.ReplaceAll(src, "gl_FragData[0]", "gl_FragColor")
				if src != prev {
					fixed = true
					logSection("CURSOR", "deep-fix: replaced gl_FragData[0] with gl_FragColor")
				}
			}

			// --- DEEP FIX: #version 300 es / 330 not supported in WebGL 1 ---
			if strings.Contains(compileErr, "#version") || strings.Contains(compileErr, "version") {
				prev := src
				versionRe := regexp.MustCompile(`(?m)^\s*#version\s+\d+.*$`)
				src = versionRe.ReplaceAllString(src, "")
				if src != prev {
					fixed = true
					logSection("CURSOR", "deep-fix: removed unsupported #version directive")
				}
			}

			// --- DEEP FIX: out vec4 fragColor -> gl_FragColor ---
			if strings.Contains(compileErr, "'out' : storage qualifier") || (strings.Contains(compileErr, "out") && strings.Contains(compileErr, "not supported")) {
				outRe := regexp.MustCompile(`(?m)^\s*(?:layout\s*\([^)]*\)\s*)?out\s+vec4\s+(\w+)\s*;`)
				outMatch := outRe.FindStringSubmatch(src)
				if outMatch != nil {
					outName := outMatch[1]
					src = outRe.ReplaceAllString(src, "")
					src = strings.ReplaceAll(src, outName, "gl_FragColor")
					fixed = true
					logSection("CURSOR", fmt.Sprintf("deep-fix: replaced 'out vec4 %s' with gl_FragColor", outName))
				}
			}

			if fixed {
				if !validFragment(src) {
					fixed = false
					src = orig
					logSection("CURSOR", "auto-fix rejected: result would be empty or missing main()")
				}
			}
			if fixed {
				thinking := buildFixThinking(orig, src, compileErr)
				logSection("CURSOR", "applied local auto-fix for: "+compileErr[:min(len(compileErr), 80)])
				resolveShaderError(path, compileErr, "autofix")
				if err := os.WriteFile(path, []byte(src), 0644); err == nil {
					gitCommitFix(filepath.Dir(path), path, compileErr, "autofix")
				}
				w.Header().Set("Content-Type", "application/json")
				json.NewEncoder(w).Encode(map[string]interface{}{
					"message":  "Auto-fixed locally (no Cursor agent needed)",
					"fixed":    true,
					"content":  src,
					"autoFix":  true,
					"thinking": thinking,
					"fixStage": "local",
				})
				return
			}
		}

		// Try LLM providers (Ollama etc) before escalating to cursor agent
		llmTried := false
		llmResult := ""
		if !fixed && src != "" {
			llmFixed, llmThinking, llmErr := llmFixShader(src, compileErr, req.Filename, req.IsISF, req.PreviousErrors)
			llmTried = true
			if llmErr == nil && llmFixed != "" {
				src = llmFixed
				fixed = true
				logSection("LLM", "fix applied via LLM chain")
				resolveShaderError(path, compileErr, "llm-fix")
				if err := os.WriteFile(path, []byte(src), 0644); err == nil {
					gitCommitFix(filepath.Dir(path), path, compileErr, "llm-fix")
				}
				w.Header().Set("Content-Type", "application/json")
				json.NewEncoder(w).Encode(map[string]interface{}{
					"message":  "Fixed via LLM (" + llmThinking + ")",
					"fixed":    true,
					"content":  src,
					"autoFix":  true,
					"thinking": llmThinking,
					"fixStage": "ollama",
				})
				return
			} else if llmErr != nil && llmErr.Error() == "use-cursor-agent" {
				llmResult = "use-cursor-agent"
				logSection("LLM", "falling through to cursor agent")
			} else if llmErr != nil {
				llmResult = llmErr.Error()
				logSection("LLM", "LLM chain could not fix: "+llmErr.Error())
			} else {
				llmResult = "returned empty"
			}
		}

		if !req.Confirm {
			logSection("CURSOR", "no local fix found, awaiting user confirmation to use Cursor agent")
			reason := "Auto-fix could not resolve this."
			verbose := ""
			if (strings.Contains(compileErr, "iGlobalTime") || strings.Contains(compileErr, "iResolution") || strings.Contains(compileErr, "iMouse") || strings.Contains(compileErr, "iTime")) && strings.Contains(compileErr, "undeclared") {
				reason = "Shadertoy uniform names need mapping."
				verbose = "These are Shadertoy-specific names. Use Create New Shader -> Paste from Shadertoy for auto-conversion, or add #define iGlobalTime TIME etc."
			} else if strings.Contains(compileErr, "field selection requires structure or vector") {
				reason = "Accessing .x/.y/.z on a non-vector type."
				verbose = "The variable before .x/.y/.z is not a vec2/vec3/vec4. Check its declaration."
			} else if strings.Contains(compileErr, "undeclared identifier") {
				nameRe := regexp.MustCompile(`'(\w+)'\s*:\s*undeclared identifier`)
				allNames := nameRe.FindAllStringSubmatch(compileErr, 5)
				nameList := ""
				for _, m := range allNames {
					if len(m) >= 2 {
						if nameList != "" {
							nameList += ", "
						}
						nameList += m[1]
					}
				}
				reason = "Undeclared: " + nameList
				verbose = "These names are used but never declared. Could be typos, missing uniforms, or missing functions. Cursor AI will analyze context and fix."
			} else if strings.Contains(compileErr, "'uniform' : only allowed at global scope") || strings.Contains(compileErr, "invalid qualifier combination") {
				reason = "Uniforms inside functions."
				verbose = "All uniform declarations must be at the top of the file, before void main(). Also: for-loops need 'int i' not 'uniform float i'."
			} else if strings.Contains(compileErr, "l-value required") && strings.Contains(compileErr, "uniform") {
				reason = "Writing to a uniform (read-only)."
				verbose = "Uniforms are read-only in GLSL. Copy to a local variable first: float x = myUniform; then modify x."
			} else if strings.Contains(compileErr, "Loop index cannot be compared with non-constant") {
				reason = "Loop bounds must be compile-time constant."
				verbose = "Use const int or #define for loop limits. Uniforms can't be loop bounds in WebGL 1.0."
			} else if strings.Contains(compileErr, "Divide by zero") || strings.Contains(compileErr, "divide by zero") {
				reason = "IRREPARABLE: Divide by zero."
				verbose = "A division by zero was detected. Guard with max(divisor, 0.001) or ensure the denominator is never zero."
			} else if strings.Contains(compileErr, "array index out of range") || strings.Contains(compileErr, "array index out of bounds") {
				reason = "IRREPARABLE: Array index out of range."
				verbose = "Array access with an index >= array length. Clamp the index or use a valid constant."
			} else if strings.Contains(compileErr, "no matching overloaded function found") {
				funcRe := regexp.MustCompile(`'(\w+)'\s*:\s*no matching overloaded function found`)
				funcNames := funcRe.FindAllStringSubmatch(compileErr, 5)
				fnList := ""
				for _, m := range funcNames {
					if len(m) >= 2 {
						if fnList != "" {
							fnList += ", "
						}
						fnList += m[1] + "()"
					}
				}
				reason = "Unknown function: " + fnList
				verbose = "These function calls don't match any GLSL built-in or user-defined function. Likely typos or wrong argument types."
			} else if strings.Contains(compileErr, "Illegal character at fieldname start") && strings.Contains(compileErr, "'-'") {
				reason = "Float or expression followed by minus without space."
				verbose = "E.g. '3.-x' is parsed as float '3.' then dot (field access) then '-'. Use '3.0 - x' or add a space: '3. - x'. The parser thinks you are accessing a field starting with '-' which is invalid."
			} else if strings.Contains(compileErr, "Missing main()") {
				reason = "Missing main()."
				verbose = "File is empty or has no void main(). Add a minimal fragment shader or paste valid code."
			} else if strings.Contains(compileErr, "syntax error") {
				reason = "Syntax error (missing semicolon, brace, or keyword)."
				verbose = "The GLSL parser hit unexpected syntax. Common causes: missing ';' at end of statement, mismatched { }, stray characters, or ambiguous expressions like '1.-x' (use '1.0 - x')."
			}
			irreparable := strings.HasPrefix(reason, "IRREPARABLE:")
			apiKey := req.CursorApiKey
			if apiKey == "" || apiKey == "***" {
				settingsMu.RLock()
				apiKey = appSettings.CursorApiKey
				settingsMu.RUnlock()
			}
			canLaunchAgent := apiKey != "" && apiKey != "***"
			if _, cmdErr := buildAgentCmd(""); cmdErr != nil {
				canLaunchAgent = false
			}
			if irreparable && canLaunchAgent {
				logSection("CURSOR", "IRREPARABLE: best-effort agent launch (no second click needed)")
				req.Confirm = true
			} else {
				triedSummary := "Local: tried, no fix applied. "
				if llmTried {
					if llmResult != "" {
						triedSummary += "LLM: tried, " + llmResult + ". "
					} else {
						triedSummary += "LLM: tried, no fix. "
					}
				} else {
					if src == "" {
						triedSummary += "LLM: skipped (empty source). "
					} else {
						triedSummary += "LLM: not tried. "
					}
				}
				if canLaunchAgent {
					triedSummary += "Cursor: available, not confirmed (click again to launch)."
				} else {
					triedSummary += "Cursor: not available (no API key or agent)."
				}
				ensureErrorEntryThenMarkUnrecoverable(path, req.Filename, compileErr, triedSummary)
				appendUnrecoverableShader(path, req.Filename, compileErr, triedSummary)
				appendDebugFixError(path, req.Filename, compileErr, reason, verbose, triedSummary)
				simplifiedErr := simplifyCompileError(compileErr)
				fullReason := reason
				if verbose != "" {
					fullReason = reason + " " + verbose
				}
				msg := fullReason + " Use Cursor agent? (uses API tokens)"
				if irreparable {
					short := strings.TrimPrefix(reason, "IRREPARABLE: ")
					msg = "IRREPARABLE - " + short
					if verbose != "" {
						msg += " " + verbose
					}
					msg += " Use Cursor agent? (uses API tokens)"
				}
				reasonShort := strings.TrimPrefix(reason, "IRREPARABLE: ")
				w.Header().Set("Content-Type", "application/json")
				json.NewEncoder(w).Encode(map[string]interface{}{
					"needsAgent":    true,
					"unrecoverable": true,
					"triedSummary":  triedSummary,
					"irreparable":   irreparable,
					"message":       msg,
					"reason":        fullReason,
					"reasonShort":   reasonShort,
					"verbose":       verbose,
					"compileError":  simplifiedErr,
					"fixStage":      "needs-cursor",
				})
				return
			}
		}

		if agentInCooldown() {
			remaining := agentCooldownRemainingSec()
			w.Header().Set("Content-Type", "application/json")
			w.WriteHeader(429)
			json.NewEncoder(w).Encode(map[string]interface{}{
				"error":                "Agent cooldown: wait " + strconv.Itoa(remaining) + "s before next call.",
				"rateLimit":            "true",
				"cooldownRemainingSec": remaining,
			})
			return
		}

		if req.Content != "" {
			if err := os.WriteFile(path, []byte(req.Content), 0644); err != nil {
				w.Header().Set("Content-Type", "application/json")
				w.WriteHeader(500)
				json.NewEncoder(w).Encode(map[string]string{"error": "write: " + err.Error()})
				return
			}
		}
		logSection("CURSOR", "user confirmed agent use for: "+compileErr[:min(len(compileErr), 80)])
		shaderType := "GLSL fragment shader (WebGL 1.0 / GLSL ES 1.0)"
		typeHints := "Common WebGL 1.0 issues: missing 'precision highp float;', " +
			"'texture' must be 'texture2D', no 'const' qualifier on global variable initializers that reference uniforms, " +
			"for-loop bounds must be int not float, 'gl_FragData[0]' should be 'gl_FragColor', " +
			"'#extension GL_OES_standard_derivatives : enable' needed for dFdx/dFdy/fwidth, " +
			"undeclared uniforms (time, mouse, resolution) need explicit declarations. " +
			"For 'field selection requires structure or vector': .rgb/.rgba/.xy etc require a vec3/vec4 on the left; if you have a float, use vec3(x,x,x) instead of x.rgb."
		if req.IsISF {
			shaderType = "ISF (Interactive Shader Format) shader"
			typeHints = "ISF shaders have a /* { JSON } */ header block with ISFVSN, INPUTS, DESCRIPTION. " +
				"Built-in uniforms: TIME, RENDERSIZE, FRAMEINDEX, PASSINDEX. " +
				"IMPORTANT: For Wire compatibility, use FRAMEINDEX for animation timing (not TIME alone). " +
				"Include useFrameIndex bool INPUT (DEFAULT true) and #define time (useFrameIndex ? float(FRAMEINDEX)/max(fps,1.0) : TIME*timeScale). " +
				"Custom inputs go in INPUTS array. Use IMG_NORM_PIXEL for image inputs. " +
				"Do NOT redeclare TIME/RENDERSIZE as uniforms (ISF provides them). " +
				"Output to gl_FragColor."
		}
		fileName := req.Filename
		if fileName == "" {
			fileName = filepath.Base(path)
		}
		simplifiedErr := simplifyCompileError(req.Error)
		attemptContext := ""
		if req.AttemptNumber > 1 || len(req.PreviousErrors) > 0 {
			attemptContext = " This is fix attempt " + strconv.Itoa(req.AttemptNumber) + "."
			if len(req.PreviousErrors) > 0 {
				attemptContext += " Previous attempts failed with: " + strings.Join(req.PreviousErrors[:min(5, len(req.PreviousErrors))], "; ") + ". "
			}
		}
		prompt := "You are an expert " + shaderType + " debugger in Macroverse Wired Atelier. " +
			"TRY YOUR HARDEST. Do not give up. Make as many fix attempts as needed until it compiles. " +
			"The shader '" + fileName + "' at " + path + " failed to compile. " +
			attemptContext +
			"Edit ONLY this file. Do not modify or reference any other files. " +
			"The compiler reported: " + simplifiedErr + ". " +
			"Fix the compile error with minimal changes. Preserve the visual intent of the shader. " +
			"If you see 'uniform only allowed at global scope': move ALL uniform declarations to the top of the file (before any functions). " +
			"For for-loops use 'float i' or 'int i' never 'uniform float i'. Never assign to uniforms - use local variables. " +
			typeHints
		cmd, cmdErr := buildAgentCmd(prompt)
		if cmdErr != nil {
			logSection("CURSOR", "no cursor-agent in PATH, cannot fix: "+compileErr)
			w.Header().Set("Content-Type", "application/json")
			w.WriteHeader(503)
			json.NewEncoder(w).Encode(map[string]string{
				"error":   "No Cursor agent found (cursor-agent / agent / cursor not in PATH). Auto-fix could not resolve this error either.",
				"message": "Install cursor-agent or add your Cursor API key in Settings. The compile error: " + compileErr,
			})
			return
		}
		cmd.Dir = filepath.Dir(path)
		apiKey := req.CursorApiKey
		if apiKey == "" || apiKey == "***" {
			settingsMu.RLock()
			apiKey = appSettings.CursorApiKey
			settingsMu.RUnlock()
		}
		if apiKey != "" {
			cmd.Env = append(os.Environ(), "CURSOR_API_KEY="+apiKey)
		}
		agentOutputBuf.mu.Lock()
		agentOutputBuf.lines = nil
		agentOutputBuf.mu.Unlock()
		tee := agentOutputWriter{}
		cmd.Stdout = io.MultiWriter(os.Stdout, &tee)
		cmd.Stderr = io.MultiWriter(os.Stderr, &tee)
		if err := cmd.Start(); err != nil {
			logSection("CURSOR", "agent start failed: "+err.Error())
			w.Header().Set("Content-Type", "application/json")
			w.WriteHeader(500)
			json.NewEncoder(w).Encode(map[string]string{"error": "cursor-agent failed to start: " + err.Error()})
			return
		}
		agentCooldownSet()
		agentProc.Lock()
		agentProc.running = true
		agentProc.Unlock()
		fixPath := path
		fixErr := compileErr
		go func() {
			cmd.Wait()
			agentProc.Lock()
			agentProc.running = false
			agentProc.Unlock()
			resolveShaderError(fixPath, fixErr, "cursor-agent")
			gitCommitFix(filepath.Dir(fixPath), fixPath, fixErr, "cursor-agent")
		}()
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]string{"message": "Cursor CLI agent launched"})
	})

	http.HandleFunc("/api/explain", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			Path         string `json:"path"`
			Content      string `json:"content"`
			SelectedText string `json:"selectedText"`
			CursorApiKey string `json:"cursorApiKey"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		if req.Path == "" || strings.TrimSpace(req.Content) == "" {
			http.Error(w, "path and content required", 400)
			return
		}
		path := strings.ReplaceAll(req.Path, "|", string(filepath.Separator))
		basename := filepath.Base(path)
		sysPrompt := `You are a GLSL explorer and VJ tutor embedded in Macroverse Wired Atelier.
Your audience: intermediate creative coders who VJ with Resolume Wire/Arena.

STYLE RULES:
- TLDR first: one sentence visual summary at the top
- Then break down the key techniques (raymarching, SDF, noise, domain warping, etc.)
- Point out the "knobs" -- which uniforms/values to tweak for live performance
- If there are magic numbers, explain what they control visually
- Use concrete visual language ("swirling plasma", "pulsing grid", "fractal zoom")
- Keep it tight -- no fluff, no disclaimers, no "as a language model"
- If code uses gl_FragCoord, explain the coordinate space briefly
- Mention Wire/ISF compatibility tips if relevant
- DO NOT edit the file -- ONLY explain
- Format as plain text, not markdown`

		prompt := sysPrompt + "\n\n"
		if req.SelectedText != "" {
			prompt += "The user selected ONLY this code block from " + basename + ". Explain ONLY this selection:\n\n" + req.SelectedText
		} else {
			prompt += "Explain this shader (" + basename + "):\n\n" + req.Content
		}
		if strings.TrimSpace(prompt) == "" {
			http.Error(w, "Empty shader content -- nothing to explain", 400)
			return
		}
		runCmd, cmdErr := buildAgentPrintCmd()
		if cmdErr != nil {
			w.Header().Set("Content-Type", "application/json")
			w.WriteHeader(503)
			json.NewEncoder(w).Encode(map[string]string{"error": "cursor-agent not found. Enable Ollama in Settings > LLM Provider Chain for free local LLM refactoring."})
			return
		}
		ctx, cancel := context.WithTimeout(context.Background(), 90*time.Second)
		defer cancel()
		runCmd = exec.CommandContext(ctx, runCmd.Path, runCmd.Args[1:]...)
		runCmd.Dir = filepath.Dir(path)
		apiKey := req.CursorApiKey
		if apiKey == "" || apiKey == "***" {
			settingsMu.RLock()
			apiKey = appSettings.CursorApiKey
			settingsMu.RUnlock()
		}
		runCmd.Env = os.Environ()
		if apiKey != "" {
			runCmd.Env = append(runCmd.Env, "CURSOR_API_KEY="+apiKey)
		}
		stdinPipe, err := runCmd.StdinPipe()
		if err != nil {
			http.Error(w, "stdin pipe: "+err.Error(), 500)
			return
		}
		var outBuf strings.Builder
		runCmd.Stdout = &outBuf
		runCmd.Stderr = &outBuf
		if err := runCmd.Start(); err != nil {
			http.Error(w, "agent start: "+err.Error(), 500)
			return
		}
		io.Copy(stdinPipe, strings.NewReader(prompt))
		stdinPipe.Close()
		runErr := runCmd.Wait()
		out := []byte(outBuf.String())
		w.Header().Set("Content-Type", "application/json")
		text := strings.TrimSpace(string(out))
		if runErr != nil {
			json.NewEncoder(w).Encode(map[string]string{
				"text":  text,
				"error": runErr.Error(),
			})
			return
		}
		if text == "" {
			text = "No output from agent. Check Cursor IDE - the explain may have opened there. Some agents run in GUI mode."
		}
		json.NewEncoder(w).Encode(map[string]string{"text": text})
	})

	http.HandleFunc("/api/cursor-suggest-params", func(w http.ResponseWriter, r *http.Request) {
		logSection("CURSOR", "POST /api/cursor-suggest-params")
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			Path         string `json:"path"`
			Content      string `json:"content"`
			CursorApiKey string `json:"cursorApiKey"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		if req.Path == "" || strings.TrimSpace(req.Content) == "" {
			http.Error(w, "path and content required", 400)
			return
		}
		path := strings.ReplaceAll(req.Path, "|", string(filepath.Separator))
		agentOutputBuf.mu.Lock()
		agentOutputBuf.lines = nil
		agentOutputBuf.mu.Unlock()
		prompt := `You are analyzing a GLSL shader for a VJ tool (Resolume Wire / Macroverse Wired Atelier). We expose parameters as live sliders in two ways:

1) NAMED IDENTIFIERS: variable names, const names, #define names that already exist. Suggest the exact identifier as spelled in the code.
2) NUMERIC LITERALS: inline numbers (e.g. 20.0, 1.03, 0.5, 123123.25423) that control grid size, scroll offset, hash seeds, deformation amount, mask radius, scale, frequency, etc. The app can turn any such literal into a uniform and slider.

CRITICAL:
- For names: suggest ONLY exact identifier names that appear in the shader. Skip: time, mouse, resolution, TIME, RENDERSIZE, mouseX, mouseY, timeScale.
- For literals: suggest 10-25 numeric literals (floats or integers) that would make good tunable parameters. Be thorough - include grid sizes (e.g. 20.0 in floor(p.y*20.0)/20.0), offsets (1.03, 0.5), magic numbers in hash/noise (123123.25423, 31231.23123), deformation amounts (0.5, 2.0), mask parameters (0.4, .69, .6, .98, .88), scale factors, speeds, frequencies, and similar. Give each with value and the 1-based line number where it first appears.

Return a single JSON object with two keys:
- "params": array of identifier strings, e.g. ["spd","scale","zoom"] (or [] if none). Include ALL exposable names - shaders may have many parameters.
- "literals": array of {"value": <number>, "line": <1-based line>}, e.g. [{"value":20.0,"line":12},{"value":1.03,"line":15}]

If there are no suitable named identifiers, "params" may be []. There should usually be many "literals" for rich control. No explanation, no markdown, just the JSON object.

Shader to analyze:

` + req.Content
		if strings.TrimSpace(prompt) == "" {
			http.Error(w, "Empty shader content -- nothing to analyze", 400)
			return
		}
		runCmd, cmdErr := buildAgentPrintCmd()
		if cmdErr != nil {
			http.Error(w, "cursor-agent or agent not found in PATH. Use Expose (local analysis) instead.", 503)
			return
		}
		apiKey := req.CursorApiKey
		if apiKey == "" || apiKey == "***" {
			settingsMu.RLock()
			apiKey = appSettings.CursorApiKey
			settingsMu.RUnlock()
		}
		logSection("CURSOR", "suggest-params analyzing: "+filepath.Base(path))
		ctx, cancel := context.WithTimeout(context.Background(), 60*time.Second)
		defer cancel()
		runCmd = exec.CommandContext(ctx, runCmd.Path, runCmd.Args[1:]...)
		runCmd.Dir = filepath.Dir(path)
		runCmd.Env = os.Environ()
		if apiKey != "" {
			runCmd.Env = append(runCmd.Env, "CURSOR_API_KEY="+apiKey)
		}
		stdinPipe, err := runCmd.StdinPipe()
		if err != nil {
			http.Error(w, "stdin pipe: "+err.Error(), 500)
			return
		}
		var outBuf strings.Builder
		teeWriter := agentOutputWriter{}
		runCmd.Stdout = io.MultiWriter(os.Stdout, &outBuf, &teeWriter)
		runCmd.Stderr = io.MultiWriter(os.Stderr, &outBuf, &teeWriter)
		if err := runCmd.Start(); err != nil {
			http.Error(w, "agent start: "+err.Error(), 500)
			return
		}
		io.Copy(stdinPipe, strings.NewReader(prompt))
		stdinPipe.Close()
		runErr := runCmd.Wait()
		out := []byte(outBuf.String())
		text := strings.TrimSpace(string(out))
		w.Header().Set("Content-Type", "application/json")
		if runErr != nil {
			w.WriteHeader(500)
			msg := "agent failed: " + runErr.Error()
			if len(text) > 0 {
				if len(text) > 400 {
					msg = msg + ". Output: " + text[:400] + "..."
				} else {
					msg = msg + ". Output: " + text
				}
			}
			json.NewEncoder(w).Encode(map[string]interface{}{"error": msg, "params": []string{}, "raw": text})
			return
		}
		var params []string
		var literals []map[string]interface{}
		objMatch := regexp.MustCompile(`\{[\s\S]*\}`).FindString(text)
		if objMatch != "" {
			var obj map[string]interface{}
			if json.Unmarshal([]byte(objMatch), &obj) == nil {
				if p, ok := obj["params"].([]interface{}); ok {
					for _, v := range p {
						if s, ok := v.(string); ok {
							params = append(params, s)
						}
					}
				}
				if l, ok := obj["literals"].([]interface{}); ok {
					for _, item := range l {
						if m, ok := item.(map[string]interface{}); ok {
							literals = append(literals, m)
						}
					}
				}
				json.NewEncoder(w).Encode(map[string]interface{}{"params": params, "literals": literals, "raw": text})
				return
			}
		}
		arrMatch := regexp.MustCompile(`\[[\s\S]*?\]`).FindString(text)
		if arrMatch != "" {
			if json.Unmarshal([]byte(arrMatch), &params) == nil {
				json.NewEncoder(w).Encode(map[string]interface{}{"params": params, "literals": []map[string]interface{}{}, "raw": text})
				return
			}
		}
		w.WriteHeader(500)
		json.NewEncoder(w).Encode(map[string]interface{}{"error": "could not parse params from agent response (see terminal for raw output)", "params": []string{}, "literals": []map[string]interface{}{}, "raw": text})
	})

	http.HandleFunc("/api/cursor-suggest-params-stream", func(w http.ResponseWriter, r *http.Request) {
		logSection("CURSOR", "POST /api/cursor-suggest-params-stream")
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			Path         string `json:"path"`
			Content      string `json:"content"`
			CursorApiKey string `json:"cursorApiKey"`
			UseAgent     bool   `json:"useAgent"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		if req.Path == "" || strings.TrimSpace(req.Content) == "" {
			http.Error(w, "path and content required", 400)
			return
		}
		path := strings.ReplaceAll(req.Path, "|", string(filepath.Separator))
		agentOutputBuf.mu.Lock()
		agentOutputBuf.lines = nil
		agentOutputBuf.mu.Unlock()

		w.Header().Set("Content-Type", "text/event-stream")
		w.Header().Set("Cache-Control", "no-cache")
		w.Header().Set("Connection", "keep-alive")
		w.Header().Set("X-Accel-Buffering", "no")
		flusher, _ := w.(http.Flusher)

		localParams, localLiterals := localSuggestParams(req.Content)
		logSection("CURSOR", fmt.Sprintf("local analysis: %d params, %d literals", len(localParams), len(localLiterals)))
		agentOutputAppend(fmt.Sprintf("[local] Found %d params, %d literals from code analysis", len(localParams), len(localLiterals)))
		fmt.Fprintf(w, "data: [local] Found %d params, %d literals from code analysis\n\n", len(localParams), len(localLiterals))
		if flusher != nil {
			flusher.Flush()
		}

		skipAgent := !req.UseAgent || agentInCooldown()
		if skipAgent || len(localParams)+len(localLiterals) > 0 {
			reason := "[local] Local analysis complete"
			if agentInCooldown() {
				reason = "[local] Agent on cooldown, using local results only"
			} else if !req.UseAgent {
				reason = "[local] Local-only mode (click 'Use AI' for deeper analysis)"
			}
			agentOutputAppend(reason)
			fmt.Fprintf(w, "data: %s\n\n", reason)
			if flusher != nil {
				flusher.Flush()
			}
		}

		// Try LLM chain (Ollama) for param suggestion if useAgent requested and no local results
		var aiParams []string
		var aiLiterals []map[string]interface{}
		if req.UseAgent && len(localParams)+len(localLiterals) == 0 {
			llmParams, _, llmSource, llmErr := llmSuggestParams(req.Content)
			if llmErr == nil && len(llmParams) > 0 {
				aiParams = llmParams
				logSection("LLM", fmt.Sprintf("suggest-params via %s: %d params", llmSource, len(llmParams)))
				agentOutputAppend(fmt.Sprintf("[%s] Found %d params", llmSource, len(llmParams)))
				fmt.Fprintf(w, "data: [%s] Found %d params\n\n", llmSource, len(llmParams))
				if flusher != nil {
					flusher.Flush()
				}
			}
		}

		runCmd, cmdErr := buildAgentPrintCmd()
		agentAvailable := cmdErr == nil && !agentInCooldown() && req.UseAgent && len(aiParams) == 0
		if agentAvailable {
			prompt := `You are analyzing a GLSL shader for a VJ tool (Resolume Wire / Macroverse Wired Atelier). We expose parameters as live sliders in two ways:

1) NAMED IDENTIFIERS: variable names, const names, #define names that already exist. Suggest the exact identifier as spelled in the code.
2) NUMERIC LITERALS: inline numbers (e.g. 20.0, 1.03, 0.5, 123123.25423) that control grid size, scroll offset, hash seeds, deformation amount, mask radius, scale, frequency, etc. The app can turn any such literal into a uniform and slider.

CRITICAL:
- For names: suggest ONLY exact identifier names that appear in the shader. Skip: time, mouse, resolution, TIME, RENDERSIZE, mouseX, mouseY, timeScale.
- For literals: suggest 10-25 numeric literals (floats or integers) that would make good tunable parameters. Be thorough - include grid sizes (e.g. 20.0 in floor(p.y*20.0)/20.0), offsets (1.03, 0.5), magic numbers in hash/noise (123123.25423, 31231.23123), deformation amounts (0.5, 2.0), mask parameters (0.4, .69, .6, .98, .88), scale factors, speeds, frequencies, and similar. Give each with value and the 1-based line number where it first appears.

Return a single JSON object with two keys:
- "params": array of identifier strings, e.g. ["spd","scale","zoom"] (or [] if none). Include ALL exposable names - shaders may have many parameters.
- "literals": array of {"value": <number>, "line": <1-based line>}, e.g. [{"value":20.0,"line":12},{"value":1.03,"line":15}]

If there are no suitable named identifiers, "params" may be []. There should usually be many "literals" for rich control. No explanation, no markdown, just the JSON object.

Shader to analyze:

` + req.Content
			apiKey := req.CursorApiKey
			if apiKey == "" || apiKey == "***" {
				settingsMu.RLock()
				apiKey = appSettings.CursorApiKey
				settingsMu.RUnlock()
			}
			ctx, cancel := context.WithTimeout(context.Background(), 60*time.Second)
			defer cancel()
			runCmd = exec.CommandContext(ctx, runCmd.Path, runCmd.Args[1:]...)
			runCmd.Dir = filepath.Dir(path)
			runCmd.Env = os.Environ()
			if apiKey != "" {
				runCmd.Env = append(runCmd.Env, "CURSOR_API_KEY="+apiKey)
			}
			stdinPipe, stdinErr := runCmd.StdinPipe()
			if stdinErr == nil {
				stdoutPipe, _ := runCmd.StdoutPipe()
				runCmd.Stderr = runCmd.Stdout
				if startErr := runCmd.Start(); startErr == nil {
					agentCooldownSet()
					io.Copy(stdinPipe, strings.NewReader(prompt))
					stdinPipe.Close()
					agentOutputAppend("[agent] AI agent analyzing shader...")
					fmt.Fprintf(w, "data: [agent] AI agent analyzing shader...\n\n")
					if flusher != nil {
						flusher.Flush()
					}
					var outBuf strings.Builder
					sc := bufio.NewScanner(stdoutPipe)
					sc.Buffer(make([]byte, 0, 64*1024), 1024*1024)
					for sc.Scan() {
						line := sc.Text()
						outBuf.WriteString(line)
						outBuf.WriteByte('\n')
						agentOutputAppend(line)
						escaped := strings.ReplaceAll(line, "\\", "\\\\")
						escaped = strings.ReplaceAll(escaped, "\n", "\\n")
						fmt.Fprintf(w, "data: %s\n\n", escaped)
						if flusher != nil {
							flusher.Flush()
						}
					}
					runCmd.Wait()
					text := strings.TrimSpace(outBuf.String())
					objMatch := regexp.MustCompile(`\{[\s\S]*\}`).FindString(text)
					if objMatch != "" {
						var obj map[string]interface{}
						if json.Unmarshal([]byte(objMatch), &obj) == nil {
							if p, ok := obj["params"].([]interface{}); ok {
								for _, v := range p {
									if s, ok := v.(string); ok {
										aiParams = append(aiParams, s)
									}
								}
							}
							if l, ok := obj["literals"].([]interface{}); ok {
								for _, item := range l {
									if m, ok := item.(map[string]interface{}); ok {
										aiLiterals = append(aiLiterals, m)
									}
								}
							}
						}
					}
					if len(aiParams) == 0 && len(aiLiterals) == 0 {
						arrMatch := regexp.MustCompile(`\[[\s\S]*?\]`).FindString(text)
						if arrMatch != "" {
							json.Unmarshal([]byte(arrMatch), &aiParams)
						}
					}
				} else {
					agentOutputAppend("[agent] Could not start agent: " + startErr.Error())
					fmt.Fprintf(w, "data: [agent] Could not start agent, using local results\n\n")
					if flusher != nil {
						flusher.Flush()
					}
				}
			}
		} else if cmdErr != nil {
			agentOutputAppend("[local] cursor-agent not found -- using local analysis only")
			fmt.Fprintf(w, "data: [local] cursor-agent not found -- using local analysis only\n\n")
			if flusher != nil {
				flusher.Flush()
			}
		}

		mergedParamSet := map[string]bool{}
		var mergedParams []string
		for _, p := range localParams {
			if !mergedParamSet[p] {
				mergedParamSet[p] = true
				mergedParams = append(mergedParams, p)
			}
		}
		for _, p := range aiParams {
			if !mergedParamSet[p] {
				mergedParamSet[p] = true
				mergedParams = append(mergedParams, p)
			}
		}
		mergedLitSet := map[float64]bool{}
		var mergedLiterals []map[string]interface{}
		for _, l := range localLiterals {
			v, _ := l["value"].(float64)
			if !mergedLitSet[v] {
				mergedLitSet[v] = true
				mergedLiterals = append(mergedLiterals, l)
			}
		}
		for _, l := range aiLiterals {
			v, _ := l["value"].(float64)
			if !mergedLitSet[v] {
				mergedLitSet[v] = true
				mergedLiterals = append(mergedLiterals, l)
			}
		}

		result := map[string]interface{}{"done": true, "params": mergedParams, "literals": mergedLiterals}
		resultJSON, _ := json.Marshal(result)
		fmt.Fprintf(w, "data: %s\n\n", resultJSON)
		if flusher != nil {
			flusher.Flush()
		}
	})

	http.HandleFunc("/api/cursor-refactor-params", func(w http.ResponseWriter, r *http.Request) {
		logSection("CURSOR", "POST /api/cursor-refactor-params")
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			Path         string `json:"path"`
			Content      string `json:"content"`
			CursorApiKey string `json:"cursorApiKey"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		if req.Path == "" || req.Content == "" {
			http.Error(w, "path and content required", 400)
			return
		}
		path := strings.ReplaceAll(req.Path, "|", string(filepath.Separator))

		// Try Ollama LLM chain first for refactoring params
		ollamaRefactorPrompt := "You are refactoring a GLSL fragment shader. Extract magic numbers and key values " +
			"(speed, scale, color, frequency, grid size, etc.) into uniform float declarations with // @expose min max annotations.\n\n" +
			"Do not change the shader's behavior or output. Preserve the exact visual look.\n\n" +
			"Shader:\n```glsl\n" + req.Content + "\n```\n\n" +
			"Output ONLY the complete refactored shader code with uniforms and @expose annotations added."
		providers := getLLMProvidersSorted()
		for _, p := range providers {
			if !p.Enabled || p.Name != "ollama" {
				continue
			}
			if ollamaIsAvailable(p.Endpoint) {
				logSection("LLM", "trying ollama for param refactoring")
				result, err := ollamaGenerate(p.Endpoint, p.Model, ollamaRefactorPrompt)
				if err == nil {
					code := extractShaderFromResponse(result)
					if code != "" && strings.Contains(code, "@expose") {
						logSection("LLM", "ollama refactor succeeded")
						w.Header().Set("Content-Type", "application/json")
						json.NewEncoder(w).Encode(map[string]interface{}{"content": code})
						return
					}
				}
				logSection("LLM", "ollama refactor failed, falling back to cursor agent")
			}
			break
		}

		// Cursor agent fallback - check cooldown
		if agentInCooldown() {
			remaining := agentCooldownRemainingSec()
			w.Header().Set("Content-Type", "application/json")
			w.WriteHeader(429)
			json.NewEncoder(w).Encode(map[string]interface{}{
				"error":                "Agent cooldown: wait " + strconv.Itoa(remaining) + "s. Enable Ollama in Settings for free local LLM.",
				"cooldownRemainingSec": remaining,
			})
			return
		}

		prompt := `You are refactoring a GLSL fragment shader for Macroverse (AI-powered shader editor with Resolume Wire). Goal: make it safe to expose parameters as live sliders without changing the visual result. KEEP THE SHADER INTACT: do not remove or rewrite logic; only extract values into named constants and add // @expose so sliders can be added.

- Extract magic numbers and key values (speed, scale, color components, frequencies, grid size, etc.) into named constants or uniforms at the top.
- Add // @expose on the line of any constant/define/uniform that should become a slider.
- Do not change the shader's behavior or output. Preserve the exact look. No deletions of existing code beyond inlining replaced by the new constant name.
- Return a single JSON object with one key "content" whose value is the full refactored shader source. Escape newlines as \n inside the string. No other text, no markdown, no code fence.

Shader to refactor:

` + req.Content
		runCmd, cmdErr := buildAgentPrintCmd()
		if cmdErr != nil {
			http.Error(w, "cursor-agent or agent not found in PATH", 503)
			return
		}
		apiKey := req.CursorApiKey
		if apiKey == "" || apiKey == "***" {
			settingsMu.RLock()
			apiKey = appSettings.CursorApiKey
			settingsMu.RUnlock()
		}
		ctx, cancel := context.WithTimeout(context.Background(), 90*time.Second)
		defer cancel()
		runCmd = exec.CommandContext(ctx, runCmd.Path, runCmd.Args[1:]...)
		runCmd.Dir = filepath.Dir(path)
		runCmd.Env = os.Environ()
		if apiKey != "" {
			runCmd.Env = append(runCmd.Env, "CURSOR_API_KEY="+apiKey)
		}
		stdinPipe, err := runCmd.StdinPipe()
		if err != nil {
			http.Error(w, "stdin pipe: "+err.Error(), 500)
			return
		}
		stdoutPipe, _ := runCmd.StdoutPipe()
		runCmd.Stderr = runCmd.Stdout
		if err := runCmd.Start(); err != nil {
			http.Error(w, "agent start: "+err.Error(), 500)
			return
		}
		agentCooldownSet()
		io.Copy(stdinPipe, strings.NewReader(prompt))
		stdinPipe.Close()
		var outBuf strings.Builder
		io.Copy(&outBuf, stdoutPipe)
		runErr := runCmd.Wait()
		text := strings.TrimSpace(outBuf.String())
		var content string
		objMatch := regexp.MustCompile(`\{[\s\S]*\}`).FindString(text)
		if objMatch != "" {
			var obj map[string]interface{}
			if json.Unmarshal([]byte(objMatch), &obj) == nil {
				if c, ok := obj["content"].(string); ok && c != "" {
					content = strings.ReplaceAll(c, "\\n", "\n")
				}
			}
		}
		w.Header().Set("Content-Type", "application/json")
		if content == "" {
			errMsg := "could not parse refactored content from agent response"
			if runErr != nil {
				errMsg = runErr.Error()
			}
			json.NewEncoder(w).Encode(map[string]interface{}{"error": errMsg, "raw": text})
			return
		}
		json.NewEncoder(w).Encode(map[string]interface{}{"content": content})
	})

	http.HandleFunc("/api/cursor-add-video-input", func(w http.ResponseWriter, r *http.Request) {
		logSection("CURSOR", "POST /api/cursor-add-video-input")
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			Path         string `json:"path"`
			Content      string `json:"content"`
			CursorApiKey string `json:"cursorApiKey"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		if req.Path == "" || req.Content == "" {
			http.Error(w, "path and content required", 400)
			return
		}
		path := strings.ReplaceAll(req.Path, "|", string(filepath.Separator))
		if err := os.WriteFile(path, []byte(req.Content), 0644); err != nil {
			http.Error(w, "write: "+err.Error(), 500)
			return
		}
		prompt := "CRITICAL: Edit ONLY the file at " + path + ". Do not modify, open, or reference any other files. Add an optional ISF image input for webcam/video. (1) If ISF: add an INPUT with TYPE image, NAME video, LABEL 'Video / Camera' to the JSON block. (2) Modify the shader body to sample it with IMG_NORM_PIXEL(video) and blend (multiply or overlay) with the existing effect. Preserve the visual when no video is connected. Use valid ISF/GLSL. Make minimal targeted changes."
		cmd, cmdErr := buildAgentCmd(prompt)
		if cmdErr != nil {
			http.Error(w, "cursor-agent or agent not found in PATH", 503)
			return
		}
		cmd.Dir = filepath.Dir(path)
		apiKey := req.CursorApiKey
		if apiKey == "" || apiKey == "***" {
			settingsMu.RLock()
			apiKey = appSettings.CursorApiKey
			settingsMu.RUnlock()
		}
		if apiKey != "" {
			cmd.Env = append(os.Environ(), "CURSOR_API_KEY="+apiKey)
		}
		if err := cmd.Start(); err != nil {
			http.Error(w, "cursor-agent: "+err.Error(), 500)
			return
		}
		agentProc.Lock()
		agentProc.running = true
		agentProc.Unlock()
		go func() {
			cmd.Wait()
			agentProc.Lock()
			agentProc.running = false
			agentProc.Unlock()
		}()
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]string{"message": "Cursor agent adding video input - reload shader after edit"})
	})

	http.HandleFunc("/api/open-in-cursor", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			Path string `json:"path"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		if req.Path == "" {
			http.Error(w, "path required", 400)
			return
		}
		path := strings.ReplaceAll(req.Path, "|", string(filepath.Separator))
		absPath, err := filepath.Abs(path)
		if err != nil {
			http.Error(w, "invalid path: "+err.Error(), 400)
			return
		}
		if _, err := os.Stat(absPath); err != nil {
			http.Error(w, "file not found: "+absPath, 404)
			return
		}
		exe, err := lookupCursorExe()
		if err != nil {
			http.Error(w, "cursor not found in PATH (install CLI from Cursor Command Palette)", 503)
			return
		}
		cmd := exec.Command(exe, absPath)
		if err := cmd.Start(); err != nil {
			http.Error(w, "cursor: "+err.Error(), 500)
			return
		}
		go cmd.Wait()
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]string{"message": "Opened in Cursor"})
	})

	http.HandleFunc("/api/open-agent", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			Path string `json:"path"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), 400)
			return
		}
		if req.Path == "" {
			http.Error(w, "path required", 400)
			return
		}
		if agentInCooldown() {
			remaining := agentCooldownRemainingSec()
			w.Header().Set("Content-Type", "application/json")
			w.WriteHeader(429)
			json.NewEncoder(w).Encode(map[string]interface{}{
				"error":                "Agent cooldown: wait " + strconv.Itoa(remaining) + "s before next call.",
				"rateLimit":            "true",
				"cooldownRemainingSec": remaining,
			})
			return
		}
		path := strings.ReplaceAll(req.Path, "|", string(filepath.Separator))
		absPath, err := filepath.Abs(path)
		if err != nil {
			http.Error(w, "invalid path: "+err.Error(), 400)
			return
		}
		if _, err := os.Stat(absPath); err != nil {
			http.Error(w, "file not found: "+absPath, 404)
			return
		}
		dir := filepath.Dir(absPath)
		exe, prefix, cmdErr := findAgentExe()
		if cmdErr != nil || exe == "" {
			http.Error(w, "cursor-agent or agent not found in PATH", 503)
			return
		}
		trust := []string{"--trust"}
		allArgs := append(prefix, trust...)
		if runtime.GOOS == "windows" {
			inner := "cd /d \"" + dir + "\" && \"" + exe + "\" " + strings.Join(allArgs, " ")
			launchCmd := exec.Command("cmd", "/c", "start", "Cursor Agent", "cmd", "/k", inner)
			if err := launchCmd.Start(); err != nil {
				cmd, _ := buildAgentInteractiveCmd()
				if cmd != nil {
					cmd.Dir = dir
					cmd.Stdout = os.Stdout
					cmd.Stderr = os.Stderr
					if startErr := cmd.Start(); startErr != nil {
						http.Error(w, "agent start: "+startErr.Error(), 500)
						return
					}
					go cmd.Wait()
				}
			}
		} else {
			cmd, _ := buildAgentInteractiveCmd()
			if cmd != nil {
				cmd.Dir = dir
				cmd.Stdout = os.Stdout
				cmd.Stderr = os.Stderr
				if err := cmd.Start(); err != nil {
					http.Error(w, "agent start: "+err.Error(), 500)
					return
				}
				go cmd.Wait()
			}
		}
		agentCooldownSet()
		logSection("CURSOR", "open-agent started in "+dir)
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]string{"message": "Agent launched in shader directory. Check for a new terminal window (Windows) or Cursor."})
	})

	http.HandleFunc("/api/factory-reset", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		if err := doFactoryReset(); err != nil {
			http.Error(w, err.Error(), 500)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{"ok": true, "message": "Factory reset done. Reload page."})
	})

	http.HandleFunc("/api/config", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodGet {
			http.Error(w, "method not allowed", 405)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		w.Header().Set("Cache-Control", "no-cache")
		json.NewEncoder(w).Encode(map[string]interface{}{
			"readonly":          isReadonlyHost(),
			"localBrowserStore": isReadonlyHost(),
			"demo":              os.Getenv("DEMO_BANNER") == "true",
			"hostMode":          hostMode(),
			"capabilities":      hostCapabilities(),
		})
	})

	http.HandleFunc("/api/server", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodGet {
			http.Error(w, "method not allowed", 405)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{"pid": os.Getpid()})
	})

	http.HandleFunc("/api/kill", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		pid := os.Getpid()
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{"message": "Shutting down", "pid": pid})
		if f, ok := w.(http.Flusher); ok {
			f.Flush()
		}
		go func() {
			time.Sleep(200 * time.Millisecond)
			os.Exit(0)
		}()
	})

	http.HandleFunc("/api/restart", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		exePath, err := os.Executable()
		if err != nil {
			http.Error(w, "cannot get exe path: "+err.Error(), 500)
			return
		}
		args := []string{"-restart"}
		if port != "" && port != defaultPort {
			args = append(args, "-port", port)
		}
		cmd := exec.Command(exePath, args...)
		cmd.Dir = exeDir()
		cmd.Stdout = os.Stdout
		cmd.Stderr = os.Stderr
		if err := cmd.Start(); err != nil {
			http.Error(w, "restart spawn: "+err.Error(), 500)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{"message": "Restarting...", "pid": os.Getpid()})
		if f, ok := w.(http.Flusher); ok {
			f.Flush()
		}
		go func() {
			time.Sleep(200 * time.Millisecond)
			os.Exit(0)
		}()
	})

	http.HandleFunc("/api/kill-all", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		killed := killAllMacroverseProcesses()
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{"message": fmt.Sprintf("Killed %d Macroverse process(es)", killed), "killed": killed})
		go func() {
			time.Sleep(200 * time.Millisecond)
			os.Exit(0)
		}()
	})

	http.HandleFunc("/api/rebuild", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		logSection("BOOT", "Rebuild requested via API")
		w.Header().Set("Content-Type", "application/json")
		ps1 := findPS1("launch-macroverse.ps1")
		if ps1 == "" {
			json.NewEncoder(w).Encode(map[string]string{"message": "launch-macroverse.ps1 not found"})
			return
		}
		json.NewEncoder(w).Encode(map[string]string{"message": "Rebuilding and restarting via " + ps1})
		go func() {
			time.Sleep(200 * time.Millisecond)
			cmd := exec.Command("powershell", "-ExecutionPolicy", "Bypass", "-File", ps1)
			cmd.Dir = filepath.Dir(ps1)
			cmd.Stdout = os.Stdout
			cmd.Stderr = os.Stderr
			cmd.Start()
			os.Exit(0)
		}()
	})

	http.HandleFunc("/api/update/check", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodGet {
			http.Error(w, "method not allowed", 405)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		dir := exeDir()

		checkCmd := exec.Command("git", "rev-parse", "--is-inside-work-tree")
		checkCmd.Dir = dir
		if err := checkCmd.Run(); err != nil {
			json.NewEncoder(w).Encode(map[string]interface{}{
				"hasUpdates": false,
				"error":      "not a git repository",
			})
			return
		}

		fetchCmd := exec.Command("git", "fetch", "origin")
		fetchCmd.Dir = dir
		fetchOut, fetchErr := fetchCmd.CombinedOutput()
		if fetchErr != nil {
			json.NewEncoder(w).Encode(map[string]interface{}{
				"hasUpdates": false,
				"error":      "git fetch failed: " + strings.TrimSpace(string(fetchOut)),
			})
			return
		}

		localCmd := exec.Command("git", "rev-parse", "--short", "HEAD")
		localCmd.Dir = dir
		localOut, _ := localCmd.Output()
		localHead := strings.TrimSpace(string(localOut))

		remoteCmd := exec.Command("git", "rev-parse", "--short", "origin/HEAD")
		remoteCmd.Dir = dir
		remoteOut, _ := remoteCmd.Output()
		remoteHead := strings.TrimSpace(string(remoteOut))

		logCmd := exec.Command("git", "log", "HEAD..origin/HEAD", "--oneline", "--no-merges")
		logCmd.Dir = dir
		logOut, _ := logCmd.Output()
		commits := []string{}
		for _, line := range strings.Split(strings.TrimSpace(string(logOut)), "\n") {
			if line != "" {
				commits = append(commits, line)
			}
		}

		json.NewEncoder(w).Encode(map[string]interface{}{
			"hasUpdates": len(commits) > 0,
			"commits":    commits,
			"localHead":  localHead,
			"remoteHead": remoteHead,
		})
	})

	http.HandleFunc("/api/update/apply", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		if writeBlocked(w) {
			return
		}
		logSection("BOOT", "Update requested via API")
		w.Header().Set("Content-Type", "application/json")
		dir := exeDir()

		checkCmd := exec.Command("git", "rev-parse", "--is-inside-work-tree")
		checkCmd.Dir = dir
		if err := checkCmd.Run(); err != nil {
			json.NewEncoder(w).Encode(map[string]interface{}{
				"success": false,
				"error":   "not a git repository",
			})
			return
		}

		var buildScript string
		if runtime.GOOS == "windows" {
			buildScript = filepath.Join(dir, "build.bat")
		} else {
			buildScript = filepath.Join(dir, "build.sh")
		}
		if _, err := os.Stat(buildScript); err != nil {
			json.NewEncoder(w).Encode(map[string]interface{}{
				"success": false,
				"error":   "build script not found at " + buildScript,
			})
			return
		}

		exePath, err := os.Executable()
		if err != nil {
			json.NewEncoder(w).Encode(map[string]interface{}{
				"success": false,
				"error":   "cannot get exe path: " + err.Error(),
			})
			return
		}

		portArg := ""
		if port != "" && port != defaultPort {
			portArg = " -port " + port
		}

		var scriptPath string
		var scriptContent string
		if runtime.GOOS == "windows" {
			scriptPath = filepath.Join(os.TempDir(), "macroverse-update.bat")
			scriptContent = fmt.Sprintf("@echo off\r\ncd /d \"%s\"\r\ngit pull origin\r\nif errorlevel 1 (\r\n    echo git pull failed\r\n    pause\r\n    exit /b 1\r\n)\r\ncall build.bat\r\nif errorlevel 1 (\r\n    echo Build failed\r\n    pause\r\n    exit /b 1\r\n)\r\nstart \"\" \"%s\"%s\r\nexit /b 0\r\n", dir, exePath, portArg)
		} else {
			scriptPath = filepath.Join(os.TempDir(), "macroverse-update.sh")
			scriptContent = fmt.Sprintf("#!/bin/bash\ncd \"%s\"\ngit pull origin || exit 1\n./build.sh || exit 1\n\"%s\"%s &\n", dir, exePath, portArg)
		}

		if err := os.WriteFile(scriptPath, []byte(scriptContent), 0755); err != nil {
			json.NewEncoder(w).Encode(map[string]interface{}{
				"success": false,
				"error":   "cannot write update script: " + err.Error(),
			})
			return
		}

		json.NewEncoder(w).Encode(map[string]interface{}{
			"success": true,
			"message": "Pulling and rebuilding. App will restart automatically...",
		})
		if f, ok := w.(http.Flusher); ok {
			f.Flush()
		}

		go func() {
			time.Sleep(300 * time.Millisecond)
			var cmd *exec.Cmd
			if runtime.GOOS == "windows" {
				cmd = exec.Command("cmd", "/c", "start", "Macroverse Update", "/min", scriptPath)
			} else {
				cmd = exec.Command("bash", scriptPath)
			}
			cmd.Dir = dir
			cmd.Stdout = os.Stdout
			cmd.Stderr = os.Stderr
			if err := cmd.Start(); err != nil {
				logSection("UPDATE", "Failed to start update script: "+err.Error())
			}
			os.Exit(0)
		}()
	})

	http.HandleFunc("/api/server-log", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		serverLogBuf.mu.Lock()
		lines := make([]string, len(serverLogBuf.lines))
		copy(lines, serverLogBuf.lines)
		serverLogBuf.mu.Unlock()
		json.NewEncoder(w).Encode(map[string]interface{}{"lines": lines})
	})

	http.HandleFunc("/api/templates/text", func(w http.ResponseWriter, r *http.Request) {
		tryDirs := []string{
			filepath.Join(exeDir(), shadersBaseDir, "core", "text"),
		}
		if wd, err := os.Getwd(); err == nil {
			tryDirs = append(tryDirs,
				filepath.Join(wd, "shaders", "core", "text"),
				filepath.Join(wd, "core", "text"),
			)
		}
		for _, p := range getSourcePaths() {
			tryDirs = append(tryDirs, filepath.Join(p, "core", "text"))
		}
		var textDir string
		for _, d := range tryDirs {
			if info, err := os.Stat(d); err == nil && info.IsDir() {
				textDir = d
				break
			}
		}
		if textDir == "" {
			http.Error(w, "templates dir not found", 404)
			return
		}
		name := r.URL.Query().Get("name")
		if name == "" {
			entries, err := os.ReadDir(textDir)
			if err != nil {
				http.Error(w, "templates dir not found", 404)
				return
			}
			var list []struct {
				Name  string `json:"name"`
				Label string `json:"label"`
			}
			for _, e := range entries {
				if e.IsDir() || !strings.HasSuffix(strings.ToLower(e.Name()), ".fs") {
					continue
				}
				if isBannedShader(e.Name()) {
					continue
				}
				label := strings.TrimSuffix(e.Name(), ".fs")
				label = strings.TrimPrefix(label, "core-text-")
				label = strings.ReplaceAll(label, "-", " ")
				list = append(list, struct {
					Name  string `json:"name"`
					Label string `json:"label"`
				}{Name: e.Name(), Label: label})
			}
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(list)
			return
		}
		if strings.Contains(name, "..") || filepath.Separator != '/' && strings.Contains(name, string(filepath.Separator)) {
			http.Error(w, "invalid name", 400)
			return
		}
		if isBannedShader(name) {
			http.Error(w, "banned shader", 403)
			return
		}
		fpath := filepath.Join(textDir, name)
		data, err := os.ReadFile(fpath)
		if err != nil {
			http.Error(w, "file not found", 404)
			return
		}
		w.Header().Set("Content-Type", "text/plain")
		w.Write(data)
	})

	http.HandleFunc("/api/shader", func(w http.ResponseWriter, r *http.Request) {
		path := r.URL.Query().Get("path")
		if path == "" {
			http.Error(w, "path required", 400)
			return
		}
		path = strings.ReplaceAll(path, "|", string(filepath.Separator))
		if isBannedShader(path) {
			http.Error(w, "banned shader", 403)
			return
		}
		data, err := os.ReadFile(path)
		if err != nil && runtime.GOOS == "windows" && len(path) >= 2 && path[1] == ':' {
			rest := strings.TrimPrefix(path[2:], "\\")
			rest = strings.TrimPrefix(rest, "/")
			lower := strings.ToLower(err.Error())
			if strings.Contains(lower, "cannot find") || strings.Contains(lower, "no such file") || strings.Contains(lower, "does not exist") {
				for _, d := range "ABCDEFGHIJKLMNOPQRSTUVWXYZ" {
					alt := string(d) + ":\\" + rest
					if !strings.EqualFold(alt, path) {
						if data, err = os.ReadFile(alt); err == nil {
							path = alt
							break
						}
					}
				}
			}
		}
		if err != nil {
			msg := "file not found: " + path
			http.Error(w, msg, 404)
			return
		}
		w.Header().Set("Content-Type", "text/plain")
		w.Write(data)
	})

	oscInit()
	vjOutputInit()
	vjSessionMetaInit()
	wsHubInit()

	http.HandleFunc("/ws", wsHandleConnection)
	http.HandleFunc("/api/bridge/token", handleBridgeToken)
	http.HandleFunc("/api/bridge/status", handleBridgeStatus)

	http.HandleFunc("/api/osc/start", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		var req struct {
			Port int `json:"port"`
		}
		json.NewDecoder(r.Body).Decode(&req)
		if req.Port < 1024 || req.Port > 65535 {
			req.Port = 9000
		}
		if err := oscStart(req.Port); err != nil {
			http.Error(w, "osc: "+err.Error(), 500)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		w.Write([]byte(`{"ok":true,"port":` + strconv.Itoa(req.Port) + `}`))
	})

	http.HandleFunc("/api/osc/stop", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		oscStop()
		w.Header().Set("Content-Type", "application/json")
		w.Write([]byte(`{"ok":true}`))
	})

	http.HandleFunc("/api/osc/status", func(w http.ResponseWriter, r *http.Request) {
		oscState.Lock()
		running := oscState.running
		port := oscState.port
		oscState.Unlock()
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{"running": running, "port": port})
	})

	http.HandleFunc("/api/osc/stream", func(w http.ResponseWriter, r *http.Request) {
		flusher, ok := w.(http.Flusher)
		if !ok {
			http.Error(w, "streaming not supported", 500)
			return
		}
		w.Header().Set("Content-Type", "text/event-stream")
		w.Header().Set("Cache-Control", "no-cache")
		w.Header().Set("Connection", "keep-alive")
		w.Header().Set("X-Accel-Buffering", "no")

		ch := make(chan string, 64)
		oscState.Lock()
		oscState.clients[ch] = true
		oscState.Unlock()
		defer func() {
			oscState.Lock()
			delete(oscState.clients, ch)
			oscState.Unlock()
		}()

		ctx := r.Context()
		for {
			select {
			case <-ctx.Done():
				return
			case msg := <-ch:
				fmt.Fprintf(w, "data: %s\n\n", msg)
				flusher.Flush()
			}
		}
	})

	http.HandleFunc("/api/vj-sessions", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodGet {
			http.Error(w, "method not allowed", 405)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{
			"sessions":          listAllSessions(),
			"defaultSessionId": defaultVJSessionID,
		})
	})

	http.HandleFunc("/api/vj/tokens", handleVjTokens)
	http.HandleFunc("/api/vj/session-config", func(w http.ResponseWriter, r *http.Request) {
		switch r.Method {
		case http.MethodGet:
			handleVjSessionConfigGet(w, r)
		case http.MethodPost:
			handleVjSessionConfigPost(w, r)
		default:
			http.Error(w, "method not allowed", 405)
		}
	})
	http.HandleFunc("/api/vj-output/audience-mouse", handleVjAudienceMouse)

	http.HandleFunc("/api/vj-output/state", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", 405)
			return
		}
		sessionID, status := vjSessionIDFromStatePost(r)
		if status != 0 {
			http.Error(w, "control token required", status)
			return
		}
		body, err := io.ReadAll(r.Body)
		r.Body.Close()
		if err != nil || len(body) == 0 {
			http.Error(w, "body required", 400)
			return
		}
		vjOutputBroadcast(sessionID, string(body))
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{"ok": true, "sessionId": sessionID})
	})

	http.HandleFunc("/api/vj-output/stream", func(w http.ResponseWriter, r *http.Request) {
		flusher, ok := w.(http.Flusher)
		if !ok {
			http.Error(w, "streaming not supported", 500)
			return
		}
		sessionID, status := vjSessionIDFromStreamRequest(r)
		if status != 0 {
			http.Error(w, "view token required", status)
			return
		}
		w.Header().Set("Content-Type", "text/event-stream")
		w.Header().Set("Cache-Control", "no-cache")
		w.Header().Set("Connection", "keep-alive")
		w.Header().Set("X-Accel-Buffering", "no")

		ch, unsub := vjOutputStreamSubscribe(sessionID)
		defer unsub()

		ctx := r.Context()
		for {
			select {
			case <-ctx.Done():
				return
			case msg := <-ch:
				fmt.Fprintf(w, "data: %s\n\n", msg)
				flusher.Flush()
			}
		}
	})

	fsDir := http.Dir(frontendDir())
	fsHandler := http.FileServer(fsDir)
	http.Handle("/", http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if strings.HasSuffix(r.URL.Path, ".js") || strings.HasSuffix(r.URL.Path, ".html") || strings.HasSuffix(r.URL.Path, ".css") || r.URL.Path == "/" {
			w.Header().Set("Cache-Control", "no-cache, must-revalidate")
		}
		fsHandler.ServeHTTP(w, r)
	}))

	logSection("BOOT", "registered all HTTP handlers")

	portNum, _ := strconv.Atoi(port)
	if portNum <= 0 {
		portNum = 8765
	}
	var ln net.Listener
	for attempt := 0; attempt < 20; attempt++ {
		p := strconv.Itoa(portNum + attempt)
		var err error
		ln, err = net.Listen("tcp", "0.0.0.0:"+p)
		if err == nil {
			port = p
			break
		}
		if *portFlag != "" {
			log.Fatalf("port %s in use: %v", port, err)
		}
	}
	if ln == nil {
		log.Fatal("no available port in range")
	}
	srcPaths2 := getSourcePaths()
	pathLines := []string{}
	for i, p := range srcPaths2 {
		pathLines = append(pathLines, fmt.Sprintf("  source[%d]: %s", i, p))
	}
	v := getVersionInfo()
	gitLine := ""
	if v.gitRev != "" {
		gitLine = "  " + cDim + "git: " + cReset + v.gitRev
		if v.gitBranch != "" && v.gitBranch != "HEAD" {
			gitLine += " " + cDim + "(" + cReset + v.gitBranch + cDim + ")" + cReset
		}
		if v.gitDirty {
			gitLine += " " + cYellow + "[dirty]" + cReset
		}
	}
	bannerLines := []string{
		"Macroverse 42 - The Wired Atelier",
		"http://localhost:" + port,
		"",
		"  " + cYellow + "release: " + cReset + v.releaseTag,
		"  " + cDim + "build: " + cReset + v.version + "  |  " + v.buildDate,
	}
	if gitLine != "" {
		bannerLines = append(bannerLines, gitLine)
	}
	bannerLines = append(bannerLines, "", "master index: "+getIndexPath())
	bannerLines = append(bannerLines, pathLines...)
	if arr, err := readIndex(); err == nil {
		bannerLines = append(bannerLines, fmt.Sprintf("  %d shaders in database", len(arr)))
	}
	logBanner(bannerLines...)

	loadErrorLog()
	errorLog.Lock()
	openCount := 0
	for _, e := range errorLog.entries {
		if e.Status == "open" {
			openCount++
		}
	}
	errorLog.Unlock()
	if openCount > 0 {
		logSection("ERRORS", fmt.Sprintf("%d open shader issue(s) from previous session", openCount))
	}

	settingsMu.RLock()
	watchEnabled := appSettings.WatchFolders
	settingsMu.RUnlock()
	if watchEnabled {
		startFolderWatcher()
	}

	thumbnailsMu.Lock()
	loadThumbnailsCache()
	thumbnailsMu.Unlock()

	var pollCount int64
	var lastPollLog time.Time
	var pollMu sync.Mutex
	pollPaths := map[string]bool{
		"/api/agent-status":          true,
		"/api/output/status":         true,
		"/api/server":                true,
		"/api/watch/status":          true,
		"/api/output/macrocam/frame": true,
		"/api/thumbnails":            true,
		"/api/vj-output/stream":         true,
		"/api/vj-output/state":          true,
		"/api/vj-output/audience-mouse": true,
		"/api/vj/session-config":        true,
	}
	staticSuffix := []string{".js", ".css", ".html", ".ico", ".map", ".woff", ".woff2", ".png", ".jpg", ".svg"}

	handler := http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		start := time.Now()
		rec := &statusRecorder{ResponseWriter: w, status: 200}

		// Hard enforcement: in read-only mode (READONLY=true, used for public demo hosts)
		// block every mutating HTTP method on any /api/ path before the handler runs.
		// This is the single authoritative gate — individual handler writeBlocked()
		// calls are belt-and-suspenders on top of this.
		if isReadonlyHost() && strings.HasPrefix(r.URL.Path, "/api/") &&
			(r.Method == http.MethodPost || r.Method == http.MethodPut ||
				r.Method == http.MethodPatch || r.Method == http.MethodDelete) &&
			!cloudSafeMutatingAPI(r.URL.Path) {
			w.Header().Set("Content-Type", "application/json")
			w.Header().Set("Cache-Control", "no-store")
			w.WriteHeader(http.StatusForbidden)
			json.NewEncoder(w).Encode(map[string]string{
				"error": "This is a read-only demo instance. All writes are disabled.",
			})
			logHTTP(r.Method, r.URL.Path, 403, time.Since(start))
			return
		}

		http.DefaultServeMux.ServeHTTP(rec, r)
		dur := time.Since(start)
		path := r.URL.Path
		if r.URL.RawQuery != "" {
			path += "?" + r.URL.RawQuery
		}
		if path == "/" {
			return
		}
		for _, ext := range staticSuffix {
			if strings.HasSuffix(path, ext) {
				return
			}
		}
		if pollPaths[r.URL.Path] {
			pollMu.Lock()
			pollCount++
			if time.Since(lastPollLog) > 5*time.Minute {
				fmt.Fprintf(os.Stdout, "%s%s  %s[idle]%s %d polls suppressed%s\n",
					cDim, time.Now().Format("15:04:05"), cCyan, cReset, pollCount, cReset)
				pollCount = 0
				lastPollLog = time.Now()
			}
			pollMu.Unlock()
			return
		}
		logHTTP(r.Method, path, rec.status, dur)
	})
	go consoleKeyListener(port)

	log.Fatal(http.Serve(ln, handler))
}
