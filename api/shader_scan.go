package main

import (
	"crypto/sha256"
	"encoding/hex"
	"os"
	"path/filepath"
	"strings"
)

func buildShaderEntry(path string, id int) ShaderEntry {
	ext := strings.ToLower(filepath.Ext(path))
	format := "glsl"
	if ext == ".fs" || ext == ".isf" {
		format = "isf"
	}

	data, _ := os.ReadFile(path)
	h := sha256.Sum256(data)
	fileHash := hex.EncodeToString(h[:8])

	category := shaderCategoryFromPath(path)
	tags, sets := metadataFromISF(data, path, category)

	return ShaderEntry{
		ID:       id,
		Path:     path,
		Name:     shaderNameFromPath(path),
		Category: category,
		Tags:     tags,
		Sets:     sets,
		Format:   format,
		FileHash: fileHash,
	}
}