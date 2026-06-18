package main

import "testing"

func TestNormalizeThumbnailKey(t *testing.T) {
	cases := map[string]string{
		`shaders\VJ-Generated\ISF\ambient\cloud.fs`: "shaders|VJ-Generated|ISF|ambient|cloud.fs",
		"shaders/VJ-Generated/ISF/ambient/cloud.fs": "shaders|VJ-Generated|ISF|ambient|cloud.fs",
		"|shaders||starter-pack|noise.fs|":          "shaders|starter-pack|noise.fs",
	}
	for in, want := range cases {
		if got := normalizeThumbnailKey(in); got != want {
			t.Fatalf("normalizeThumbnailKey(%q) = %q, want %q", in, got, want)
		}
	}
}

func TestDedupeShaderEntries(t *testing.T) {
	entries := []ShaderEntry{
		{ID: 1, Path: `shaders\one.fs`, FileHash: "aaa"},
		{ID: 2, Path: "shaders/one.fs", FileHash: "bbb"},
		{ID: 3, Path: "shaders/two.fs", FileHash: "aaa"},
		{ID: 4, Path: "shaders/three.fs", FileHash: "ccc"},
	}
	got, removed := dedupeShaderEntries(entries)
	if removed != 2 {
		t.Fatalf("removed = %d, want 2", removed)
	}
	if len(got) != 2 {
		t.Fatalf("len(got) = %d, want 2", len(got))
	}
	if got[0].ID != 1 || got[1].ID != 4 {
		t.Fatalf("kept IDs = [%d %d], want [1 4]", got[0].ID, got[1].ID)
	}
}
