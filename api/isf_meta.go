package main

import (
	"encoding/json"
	"os"
	"path/filepath"
	"regexp"
	"strings"
)

var isfHeaderRE = regexp.MustCompile(`/\*\s*(\{[\s\S]*?\})\s*\*/`)

var presetSetNames = map[string]bool{
	"vj-ambient":        true,
	"vj-techno":         true,
	"vj-cosmic":         true,
	"vj-glitch":         true,
	"vj-geometric":      true,
	"vj-organic":        true,
	"vj-wire-ready":     true,
	"vj-dark":           true,
	"vj-colour":         true,
	"macroverse-origin": true,
	"macroverse-set":    true,
}

func parseISFHeader(data []byte) map[string]interface{} {
	m := isfHeaderRE.FindSubmatch(data)
	if len(m) < 2 {
		return nil
	}
	var header map[string]interface{}
	if json.Unmarshal(m[1], &header) != nil {
		return nil
	}
	return header
}

func isfStringSlice(v interface{}) []string {
	arr, ok := v.([]interface{})
	if !ok {
		return nil
	}
	out := make([]string, 0, len(arr))
	for _, item := range arr {
		if s, ok := item.(string); ok && s != "" {
			out = append(out, s)
		}
	}
	return out
}

func uniqueStrings(in []string) []string {
	seen := make(map[string]bool, len(in))
	out := make([]string, 0, len(in))
	for _, s := range in {
		if s == "" || seen[s] {
			continue
		}
		seen[s] = true
		out = append(out, s)
	}
	return out
}

func sameStringSet(a, b []string) bool {
	if len(a) != len(b) {
		return false
	}
	seen := make(map[string]int, len(a))
	for _, s := range a {
		seen[s]++
	}
	for _, s := range b {
		if seen[s] == 0 {
			return false
		}
		seen[s]--
	}
	return true
}

func shaderCategoryFromPath(path string) string {
	dir := filepath.Base(filepath.Dir(path))
	if dir != "" && dir != "." {
		return dir
	}
	return "uncategorized"
}

func shaderNameFromPath(path string) string {
	base := strings.TrimSuffix(filepath.Base(path), filepath.Ext(path))
	name := strings.ReplaceAll(base, "-", " ")
	return strings.ReplaceAll(name, "_", " ")
}

func metadataFromISF(data []byte, path, category string) (tags []string, sets []string) {
	slashPath := filepath.ToSlash(path)

	if category == "macroverse" || strings.Contains(slashPath, "/macroverse/") {
		sets = []string{"macroverse-origin", "macroverse-set", "vj-cosmic", "vj-wire-ready"}
	}

	header := parseISFHeader(data)
	if header == nil {
		if strings.Contains(slashPath, "/VJ-Generated/") {
			if !containsString(sets, "vj-wire-ready") {
				sets = append(sets, "vj-wire-ready")
			}
			if category != "" && category != "uncategorized" && category != "ISF" {
				tags = []string{"generated", "vj", category}
			}
		}
		return uniqueStrings(tags), uniqueStrings(sets)
	}

	rawTags := isfStringSlice(header["TAGS"])
	if len(rawTags) == 0 {
		rawTags = isfStringSlice(header["tags"])
	}

	tagSet := make([]string, 0, len(rawTags))
	setSet := make([]string, 0, len(rawTags))
	for _, t := range rawTags {
		if presetSetNames[t] {
			setSet = append(setSet, t)
		} else {
			tagSet = append(tagSet, t)
		}
	}

	if cats := isfStringSlice(header["CATEGORIES"]); len(cats) > 0 {
		for _, c := range cats {
			if !containsString(tagSet, c) {
				tagSet = append(tagSet, c)
			}
		}
	}

	ext := strings.ToLower(filepath.Ext(path))
	if ext == ".fs" || ext == ".isf" {
		if !containsString(setSet, "vj-wire-ready") {
			setSet = append(setSet, "vj-wire-ready")
		}
	}

	if category == "macroverse" || strings.Contains(slashPath, "/macroverse/") {
		for _, s := range []string{"macroverse-origin", "macroverse-set", "vj-cosmic"} {
			if !containsString(setSet, s) {
				setSet = append(setSet, s)
			}
		}
	}

	return uniqueStrings(tagSet), uniqueStrings(setSet)
}

func containsString(arr []string, target string) bool {
	for _, s := range arr {
		if s == target {
			return true
		}
	}
	return false
}

func shouldBackfillMetadata(e ShaderEntry) bool {
	slashPath := filepath.ToSlash(e.Path)
	if strings.Contains(slashPath, "/VJ-Generated/") {
		return true
	}
	if len(e.Tags) == 0 && len(e.Sets) == 0 {
		ext := strings.ToLower(filepath.Ext(e.Path))
		return ext == ".fs" || ext == ".isf"
	}
	return false
}

func backfillEntriesMetadata(entries []ShaderEntry) int {
	updated := 0
	for i := range entries {
		if !shouldBackfillMetadata(entries[i]) {
			continue
		}
		data, err := osReadFile(entries[i].Path)
		if err != nil {
			continue
		}
		category := entries[i].Category
		if category == "" {
			category = shaderCategoryFromPath(entries[i].Path)
			entries[i].Category = category
		}
		tags, sets := metadataFromISF(data, entries[i].Path, category)
		changed := false
		if len(tags) > 0 && !sameStringSet(entries[i].Tags, tags) {
			entries[i].Tags = tags
			changed = true
		}
		if len(sets) > 0 && !sameStringSet(entries[i].Sets, sets) {
			entries[i].Sets = sets
			changed = true
		}
		if changed {
			updated++
		}
	}
	return updated
}

// osReadFile is a thin wrapper so tests can stub; uses os.ReadFile in production.
var osReadFile = func(path string) ([]byte, error) {
	return os.ReadFile(path)
}