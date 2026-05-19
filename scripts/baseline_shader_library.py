#!/usr/bin/env python3
"""
Baseline shader library: recategorize, reindex, rename, tag, and report.
Scans all shaders under shaders/, parses ISF headers, assigns category/tags/sets,
proposes better names, detects non-shaders, and outputs shader-index.json + report.
Do NOT add "isf" or "ISF" to tags (format is separate).
"""

import json
import re
import sys
from pathlib import Path
from collections import Counter, defaultdict

WORKSPACE = Path(__file__).resolve().parent.parent
SHADERS_ROOT = WORKSPACE / "shaders"
INDEX_PATH = WORKSPACE / "shader-index.json"
REPORT_PATH = WORKSPACE / "SHADER_BASELINE_REPORT.txt"
SHADER_EXTENSIONS = {".fs", ".glsl", ".frag", ".vert", ".vs"}

# Example sets: set name -> list of category/tag triggers (shader gets set if category or any tag matches)
EXAMPLE_SETS = {
    "Water & Ocean": ["water", "seascape", "ocean", "ripple", "pool"],
    "Tunnels": ["tunnel", "zoom", "corridor"],
    "Plasma & Fire": ["plasma", "fire", "flame", "heat", "lava"],
    "Grids & Dots": ["grid", "dotmatrix", "matrix", "voronoi", "checker"],
    "Space & Stars": ["space", "star", "cosmic", "nebula", "galaxy"],
    "Starter Pack": ["abstract", "color", "gradient", "plasma"],
    "Text & Typography": ["text", "typography", "dotmatrix", "glyph"],
}

# Category keywords (subset from shader_pipeline; add text/utility)
CATEGORY_KEYWORDS = {
    "3d": ["sphere", "cube", "torus", "ray", "march", "sdf", "distance", "3d", "camera", "orbit", "perspective"],
    "fractal": ["mandel", "julia", "fractal", "iterate", "mandelbrot", "sierpin"],
    "particles": ["particle", "spark", "dust", "snow", "star", "emitter"],
    "plasma": ["plasma", "lava", "magma", "fire", "flame", "heat", "glow", "molten"],
    "psychedelic": ["psychedel", "kaleidoscope", "trippy", "warp", "morph", "melt", "vortex"],
    "abstract": ["abstract", "wave", "sine", "pattern", "flow", "organic", "liquid", "nebula"],
    "grid": ["grid", "checker", "tile", "voronoi", "cell", "hexag", "lattice", "matrix", "mosaic"],
    "tunnel": ["tunnel", "corridor", "zoom", "wormhole", "hyperspace", "infinite", "depth"],
    "noise": ["noise", "perlin", "simplex", "fbm", "turbulence", "worley", "cellular"],
    "color": ["gradient", "rainbow", "spectrum", "hue", "neon", "bright", "vibrant"],
    "space": ["space", "star", "galaxy", "cosmic", "universe", "nebula", "planet", "solar", "aurora"],
    "water": ["water", "ocean", "sea", "wave", "ripple", "aqua", "pool", "caustic", "seascape"],
    "geometric": ["circle", "square", "triangle", "polygon", "spiral", "ring", "line", "dot", "cross", "diamond"],
    "text": ["text", "typography", "glyph", "char", "font", "dotmatrix", "16segment"],
    "utility": ["test", "debug", "smpte", "colorbar", "solid", "reference"],
}


def parse_isf_header(content):
    """Extract ISF JSON header from shader content."""
    match = re.match(r"\s*/\*\s*(\{.*?\})\s*\*/", content, re.DOTALL)
    if not match:
        return None, content
    json_str = match.group(1)
    try:
        header = json.loads(json_str)
        return header, content[match.end() :]
    except json.JSONDecodeError:
        fixed = json_str
        fixed = re.sub(r",\s*]", "]", fixed)
        fixed = re.sub(r",\s*}", "}", fixed)
        try:
            header = json.loads(fixed)
            return header, content[match.end() :]
        except json.JSONDecodeError:
            return None, content


def categorize(content, filename, existing_cats=None):
    """Return (primary_category, list_of_tags). Never include 'isf' in tags."""
    lower = content.lower()[:4000]
    name_lower = filename.lower()
    scores = Counter()
    for cat, keywords in CATEGORY_KEYWORDS.items():
        for kw in keywords:
            if kw in name_lower:
                scores[cat] += 3
            if kw in lower:
                scores[cat] += 1
    if existing_cats:
        for c in existing_cats:
            c = c.lower()
            if c in CATEGORY_KEYWORDS:
                scores[c] += 5
    if scores:
        primary = scores.most_common(1)[0][0]
        tags = [c for c, _ in scores.most_common(5) if scores[c] >= 1]
        tags = [t for t in tags if t not in ("isf", "ISF")]
        if not tags:
            tags = [primary]
        return primary, list(dict.fromkeys(tags))
    return "misc", ["misc"]


def is_likely_shader(content):
    """False if file is clearly not a shader (no main, no frag output)."""
    if len(content.strip()) < 50:
        return False
    if "void main" not in content and "void main(" not in content:
        return False
    if "gl_FragColor" not in content and "FRAGCOLOR" not in content and "fragColor" not in content:
        return False
    if content.strip().lower().startswith("# this is") or "readme" in content[:200].lower():
        return False
    return True


def propose_name(relpath, header, category):
    """Propose a cleaner filename stem (no extension)."""
    stem = Path(relpath).stem
    # Remove duplicate suffixes _1, _1_1 -> treat as Alt
    base = re.sub(r"_1(_1)*$", "", stem)
    if base != stem:
        suffix = stem[len(base) :].replace("_", "-")
        stem = base + suffix if suffix else base

    # HorizonLight-FrostCrystal-2..11 -> Seascape-Vn
    m = re.match(r"HorizonLight-FrostCrystal-(\d+)$", stem, re.I)
    if m:
        return f"Seascape-V{m.group(1)}"

    # Seascape_1 -> Seascape-Alt
    if stem.endswith("_1") and "seascape" in stem.lower():
        return stem.replace("_1", "-Alt").replace("_", "-")

    # DotMatrix-*: keep as-is but clean; DotMatrix-41 -> DotMatrix-Grid (generic)
    if re.match(r"DotMatrix-\d+$", stem, re.I):
        return stem + "-Grid"
    if "DotMatrix" in stem:
        stem = re.sub(r"[-_](\d+)$", r"-\1", stem)

    # Infinate -> Infinite
    stem = stem.replace("InfinateZoomer", "InfiniteZoomer").replace("Infinate", "Infinite")

    # readme.fs, mods.fs etc
    if stem.lower() in ("readme", "mods", "default"):
        return stem

    # Generic cleanup: multiple dashes/underscores
    stem = re.sub(r"[-_]+", "-", stem).strip("-")
    return stem or "unnamed"


def assign_sets(category, tags):
    """Return list of example set names this shader belongs to."""
    out = []
    cat_lower = category.lower()
    tags_lower = [t.lower() for t in tags]
    for set_name, triggers in EXAMPLE_SETS.items():
        for t in triggers:
            if t in cat_lower or any(t in tag for tag in tags_lower):
                out.append(set_name)
                break
    return out


def collect_shaders():
    """Yield (relpath, content, format) for every shader file under SHADERS_ROOT."""
    for ext in SHADER_EXTENSIONS:
        for f in SHADERS_ROOT.rglob("*" + ext):
            if not f.is_file():
                continue
            try:
                content = f.read_text(encoding="utf-8", errors="replace")
            except Exception:
                continue
            rel = f.relative_to(WORKSPACE).as_posix()
            fmt = "isf" if ext == ".fs" else "glsl"
            yield rel, content, fmt


def run_baseline(apply_renames=False, delete_non_shaders=False, dry_run=True):
    """Scan shaders, build index and report. Optionally apply renames and delete non-shaders."""
    entries = []
    renames = []
    non_shaders = []
    seen_paths = set()
    id_gen = 1

    for relpath, content, fmt in collect_shaders():
        path_obj = Path(relpath)
        stem = path_obj.stem
        header, _ = parse_isf_header(content) if fmt == "isf" else (None, content)

        if not is_likely_shader(content):
            non_shaders.append((relpath, "no void main or gl_FragColor / too short / readme-like"))
            if not delete_non_shaders:
                continue
            # Will delete later; do not add to index
            continue

        existing_cats = None
        if header and "CATEGORIES" in header:
            existing_cats = header["CATEGORIES"]
        category, tags = categorize(content, stem, existing_cats)
        tags = [t for t in tags if t.lower() not in ("isf", "is f")]

        display_name = stem
        if header and header.get("DESCRIPTION"):
            display_name = header["DESCRIPTION"].strip()
        proposed_stem = propose_name(relpath, header, category)
        if proposed_stem != stem:
            new_path = path_obj.parent / (proposed_stem + path_obj.suffix)
            new_rel = new_path.as_posix()
            renames.append((relpath, new_rel, proposed_stem, display_name))

        sets = assign_sets(category, tags)
        name_for_index = proposed_stem

        # Dedupe by path (first occurrence wins)
        if relpath in seen_paths:
            continue
        seen_paths.add(relpath)

        entries.append({
            "id": id_gen,
            "path": relpath,
            "name": name_for_index,
            "category": category,
            "tags": tags,
            "sets": sets,
            "format": fmt,
        })
        id_gen += 1

    # Build report
    report_lines = [
        "SHADER BASELINE REPORT",
        "=" * 60,
        "",
        "Summary",
        "-" * 40,
        f"Total shaders indexed: {len(entries)}",
        f"Renames proposed: {len(renames)}",
        f"Non-shader files detected: {len(non_shaders)}",
        "",
    ]

    cat_counts = Counter(e["category"] for e in entries)
    report_lines.append("Categories used:")
    for cat, count in cat_counts.most_common():
        report_lines.append(f"  {cat}: {count}")
    report_lines.append("")

    all_tags = set()
    for e in entries:
        all_tags.update(e["tags"])
    report_lines.append("Tags used (sample, no isf):")
    for t in sorted(all_tags)[:80]:
        report_lines.append(f"  {t}")
    if len(all_tags) > 80:
        report_lines.append(f"  ... and {len(all_tags) - 80} more")
    report_lines.append("")

    report_lines.append("Example sets:")
    for set_name in EXAMPLE_SETS:
        count = sum(1 for e in entries if set_name in e.get("sets", []))
        report_lines.append(f"  {set_name}: {count} shaders")
    report_lines.append("")

    report_lines.append("Renames (old -> new path, new name)")
    report_lines.append("-" * 40)
    for old_rel, new_rel, new_stem, _ in renames[:200]:
        report_lines.append(f"  {old_rel}")
        report_lines.append(f"    -> {new_rel}  (name: {new_stem})")
    if len(renames) > 200:
        report_lines.append(f"  ... and {len(renames) - 200} more renames")
    report_lines.append("")

    report_lines.append("Non-shader files (do not compile / not shaders)")
    report_lines.append("-" * 40)
    for path, reason in non_shaders:
        report_lines.append(f"  {path}: {reason}")
    report_lines.append("")
    report_lines.append("=" * 60)

    report_text = "\n".join(report_lines)
    REPORT_PATH.parent.mkdir(parents=True, exist_ok=True)
    REPORT_PATH.write_text(report_text, encoding="utf-8")

    # Write index (paths stay current unless apply_renames)
    index_data = []
    for e in entries:
        index_data.append({
            "id": e["id"],
            "path": e["path"],
            "name": e["name"],
            "category": e["category"],
            "tags": e["tags"],
            "sets": e.get("sets", []),
            "format": e["format"],
        })
    INDEX_PATH.write_text(json.dumps(index_data, indent=2), encoding="utf-8")

    if apply_renames and renames:
        for old_rel, new_rel, new_stem, old_desc in renames:
            old_path = WORKSPACE / old_rel
            new_path = WORKSPACE / new_rel
            if not old_path.exists():
                continue
            if old_path.resolve() == new_path.resolve():
                continue
            if new_path.exists() and new_path != old_path:
                report_lines.append(f"Skip rename (target exists): {old_rel} -> {new_rel}")
                continue
            new_path.parent.mkdir(parents=True, exist_ok=True)
            content = old_path.read_text(encoding="utf-8", errors="replace")
            header, body = parse_isf_header(content)
            if header is not None:
                header["DESCRIPTION"] = new_stem
                json_str = json.dumps(header, indent=4)
                content = f"/*{json_str}*/\n\n{body}"
            try:
                new_path.write_text(content, encoding="utf-8")
                old_path.unlink()
            except Exception as ex:
                report_lines.append(f"Rename failed: {old_rel} -> {new_rel}: {ex}")
        # Update index paths for renamed files
        rename_map = {old: new for old, new, _, _ in renames}
        for e in index_data:
            if e["path"] in rename_map:
                e["path"] = rename_map[e["path"]]
        INDEX_PATH.write_text(json.dumps(index_data, indent=2), encoding="utf-8")

    if delete_non_shaders and non_shaders:
        report_lines.append("Deleted files (removed from disk)")
        report_lines.append("-" * 40)
        for path, reason in non_shaders:
            fp = WORKSPACE / path
            if fp.exists():
                try:
                    fp.unlink()
                    report_lines.append(f"  DELETED: {path} ({reason})")
                except Exception as ex:
                    report_lines.append(f"  FAILED: {path}: {ex}")
            else:
                report_lines.append(f"  (already gone): {path}")
        report_lines.append("")
        report_lines.append("=" * 60)
        REPORT_PATH.write_text("\n".join(report_lines), encoding="utf-8")

    return len(entries), len(renames), len(non_shaders)


def main():
    apply = "--apply-renames" in sys.argv
    delete_non = "--delete-non-shaders" in sys.argv
    dry = "--dry-run" not in sys.argv or (apply or delete_non)
    total, renames, non = run_baseline(apply_renames=apply, delete_non_shaders=delete_non, dry_run=dry)
    print(f"Indexed: {total}, renames: {renames}, non-shaders: {non}")
    print(f"Index: {INDEX_PATH}")
    print(f"Report: {REPORT_PATH}")


if __name__ == "__main__":
    main()
