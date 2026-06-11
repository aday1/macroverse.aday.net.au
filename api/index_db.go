package main

import (
	"database/sql"
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"sync"

	_ "modernc.org/sqlite"
)

var indexDBMu sync.Mutex
var indexDB *sql.DB

func getDBPath() string {
	d := exeDir()
	if v := os.Getenv("SHADER_INDEX_DB"); v != "" {
		return v
	}
	return filepath.Join(d, "macroverse.db")
}

const indexDBSchema = `
CREATE TABLE IF NOT EXISTS shaders (
	id INTEGER PRIMARY KEY,
	path TEXT NOT NULL,
	name TEXT,
	category TEXT,
	tags TEXT,
	sets TEXT,
	notes TEXT,
	uniforms TEXT,
	fixed_name TEXT,
	favorite INTEGER DEFAULT 0,
	color TEXT,
	file_hash TEXT,
	source_root TEXT,
	format TEXT,
	param_ranges TEXT
);
CREATE INDEX IF NOT EXISTS idx_shaders_path ON shaders(path);
`

func initIndexDB() error {
	indexDBMu.Lock()
	if indexDB != nil {
		indexDBMu.Unlock()
		return nil
	}
	dbPath := getDBPath()
	db, err := sql.Open("sqlite", dbPath)
	if err != nil {
		indexDBMu.Unlock()
		return fmt.Errorf("open sqlite %s: %w", dbPath, err)
	}
	indexDB = db
	if _, err := db.Exec(indexDBSchema); err != nil {
		indexDBMu.Unlock()
		return fmt.Errorf("create schema: %w", err)
	}
	indexDBMu.Unlock()
	migrateFromJSON(dbPath)
	return nil
}

func migrateFromJSON(dbPath string) {
	legacyPath := filepath.Join(filepath.Dir(dbPath), "shader-index.json")
	if v := os.Getenv("SHADER_INDEX"); v != "" {
		legacyPath = v
	}
	data, err := os.ReadFile(legacyPath)
	if err != nil || len(data) < 3 {
		return
	}
	data = stripBOM(data)
	var arr []ShaderEntry
	if json.Unmarshal(data, &arr) != nil {
		var wrapped struct {
			Entries []ShaderEntry `json:"entries"`
		}
		if json.Unmarshal(data, &wrapped) != nil {
			return
		}
		arr = wrapped.Entries
	}
	if len(arr) == 0 {
		return
	}
	var count int
	if err := indexDB.QueryRow("SELECT COUNT(*) FROM shaders").Scan(&count); err != nil || count > 0 {
		return
	}
	if err := writeIndexToDB(arr); err != nil {
		return
	}
}

func readIndexFromDB() ([]ShaderEntry, error) {
	if err := initIndexDB(); err != nil {
		return nil, err
	}
	rows, err := indexDB.Query(`SELECT id, path, name, category, tags, sets, notes, uniforms,
		fixed_name, favorite, color, file_hash, source_root, format, param_ranges FROM shaders ORDER BY id`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()
	var out []ShaderEntry
	for rows.Next() {
		var e ShaderEntry
		var tags, sets, uniforms, paramRanges sql.NullString
		var fixedName, notes, color, fileHash, sourceRoot, format sql.NullString
		var favorite int
		if err := rows.Scan(&e.ID, &e.Path, &e.Name, &e.Category, &tags, &sets, &notes, &uniforms,
			&fixedName, &favorite, &color, &fileHash, &sourceRoot, &format, &paramRanges); err != nil {
			return nil, err
		}
		e.Favorite = favorite != 0
		if fixedName.Valid {
			e.FixedName = fixedName.String
		}
		if notes.Valid {
			e.Notes = notes.String
		}
		if color.Valid {
			e.Color = color.String
		}
		if fileHash.Valid {
			e.FileHash = fileHash.String
		}
		if sourceRoot.Valid {
			e.SourceRoot = sourceRoot.String
		}
		if format.Valid {
			e.Format = format.String
		}
		if tags.Valid && tags.String != "" {
			json.Unmarshal([]byte(tags.String), &e.Tags)
		}
		if sets.Valid && sets.String != "" {
			json.Unmarshal([]byte(sets.String), &e.Sets)
		}
		if uniforms.Valid && uniforms.String != "" {
			json.Unmarshal([]byte(uniforms.String), &e.Uniforms)
		}
		if paramRanges.Valid && paramRanges.String != "" {
			json.Unmarshal([]byte(paramRanges.String), &e.ParamRanges)
		}
		out = append(out, e)
	}
	return out, rows.Err()
}

func writeIndexToDB(arr []ShaderEntry) error {
	if err := initIndexDB(); err != nil {
		return err
	}
	tx, err := indexDB.Begin()
	if err != nil {
		return err
	}
	defer tx.Rollback()
	if _, err := tx.Exec("DELETE FROM shaders"); err != nil {
		return err
	}
	stmt, err := tx.Prepare(`INSERT INTO shaders (id, path, name, category, tags, sets, notes, uniforms,
		fixed_name, favorite, color, file_hash, source_root, format, param_ranges)
		VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`)
	if err != nil {
		return err
	}
	defer stmt.Close()
	for _, e := range arr {
		tags, _ := json.Marshal(e.Tags)
		sets, _ := json.Marshal(e.Sets)
		uniforms, _ := json.Marshal(e.Uniforms)
		paramRanges, _ := json.Marshal(e.ParamRanges)
		fav := 0
		if e.Favorite {
			fav = 1
		}
		_, err := stmt.Exec(e.ID, e.Path, e.Name, e.Category, string(tags), string(sets), e.Notes, string(uniforms),
			e.FixedName, fav, e.Color, e.FileHash, e.SourceRoot, e.Format, string(paramRanges))
		if err != nil {
			return err
		}
	}
	return tx.Commit()
}

func clearIndexDB() error {
	if err := initIndexDB(); err != nil {
		return err
	}
	_, err := indexDB.Exec("DELETE FROM shaders")
	return err
}

func closeIndexDB() {
	indexDBMu.Lock()
	defer indexDBMu.Unlock()
	if indexDB != nil {
		indexDB.Close()
		indexDB = nil
	}
}

func exportIndexToTempJSON() (string, error) {
	arr, err := readIndexFromDB()
	if err != nil {
		return "", err
	}
	f, err := os.CreateTemp("", "macroverse-index-*.json")
	if err != nil {
		return "", err
	}
	path := f.Name()
	data, err := json.MarshalIndent(arr, "", "  ")
	if err != nil {
		f.Close()
		os.Remove(path)
		return "", err
	}
	if _, err := f.Write(data); err != nil {
		f.Close()
		os.Remove(path)
		return "", err
	}
	if err := f.Close(); err != nil {
		os.Remove(path)
		return "", err
	}
	return path, nil
}

func importIndexFromJSON(path string) error {
	data, err := os.ReadFile(path)
	if err != nil {
		return err
	}
	data = stripBOM(data)
	var arr []ShaderEntry
	if err := json.Unmarshal(data, &arr); err != nil {
		var wrapped struct {
			Entries []ShaderEntry `json:"entries"`
		}
		if err2 := json.Unmarshal(data, &wrapped); err2 != nil {
			return fmt.Errorf("parse index JSON: %w", err)
		}
		arr = wrapped.Entries
	}
	return writeIndexToDB(arr)
}
