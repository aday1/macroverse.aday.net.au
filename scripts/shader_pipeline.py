#!/usr/bin/env python3
"""
Macroverse V5 Shader Processing Pipeline
-----------------------------------------
Processes all GLSL/ISF shaders from TRAINING_MACROVERSV5 (when present):
1. Inventory & deduplicate
2. Validate GLSL compilation
3. Parse/fix ISF headers
4. Fix common compilation errors
5. Expose ISF parameters with frame index support
6. Categorize and tag shaders
7. Sort into shaders/VJ-Sorted-Production
8. Generate index and report
"""

import os
import sys
import json
import re
import hashlib
import shutil
import subprocess
import time as time_module
from pathlib import Path
from collections import defaultdict, Counter

# Paths (workspace = project root, one level up from scripts/)
WORKSPACE = Path(__file__).resolve().parent.parent
TRAINING = WORKSPACE / "TRAINING_MACROVERSV5"
SORTED_DIR = WORKSPACE / "shaders" / "VJ-Sorted-Production"
REPORT_PATH = WORKSPACE / "SHADER_PROCESSING_REPORT.txt"
INDEX_PATH = WORKSPACE / "training-shader-index.json"

SHADER_EXTENSIONS = {".fs", ".glsl", ".frag", ".vert", ".vs"}

# ISF standard uniforms (not user params)
ISF_BUILTINS = {"TIME", "FRAMEINDEX", "RENDERSIZE", "PASSINDEX", "DATE",
                "TIMEDELTA", "isf_FragNormCoord"}
GLSL_BUILTINS = {"time", "resolution", "mouse", "gl_FragCoord",
                 "gl_FragColor", "surfacePosition"}

# Category keywords for auto-classification
CATEGORY_KEYWORDS = {
    "3d": ["sphere", "cube", "torus", "ray", "march", "sdf", "distance",
           "3d", "camera", "orbit", "rotate3d", "perspective", "volumetric"],
    "fractal": ["mandel", "julia", "fractal", "iterate", "iteration",
                "mandelbrot", "sierpin", "dragon", "barnsley", "ifs"],
    "particles": ["particle", "spark", "dust", "snow", "rain", "star",
                  "confetti", "fireflies", "emitter", "swarm"],
    "plasma": ["plasma", "lava", "magma", "fire", "flame", "heat",
               "glow", "burn", "molten", "inferno"],
    "psychedelic": ["psychedel", "kaleidoscope", "trippy", "acid",
                    "warp", "morph", "melt", "hypno", "vortex"],
    "abstract": ["abstract", "wave", "sine", "pattern", "geometric",
                 "flow", "organic", "liquid", "fluid", "nebula"],
    "grid": ["grid", "checker", "tile", "voronoi", "cell", "hexag",
             "lattice", "matrix", "mosaic", "tessell"],
    "tunnel": ["tunnel", "corridor", "fly", "zoom", "wormhole",
               "hyperspace", "infinite", "depth"],
    "noise": ["noise", "perlin", "simplex", "fbm", "turbulence",
              "worley", "cellular", "distort"],
    "color": ["gradient", "rainbow", "spectrum", "hue", "colorful",
              "neon", "glow", "bright", "vibrant"],
    "space": ["space", "star", "galaxy", "cosmic", "universe", "nebula",
              "planet", "solar", "constellation", "aurora"],
    "water": ["water", "ocean", "sea", "wave", "ripple", "aqua",
              "underwater", "pool", "caustic", "reflect"],
    "geometric": ["circle", "square", "triangle", "polygon", "spiral",
                  "ring", "line", "dot", "cross", "diamond"],
}

# Common GLSL fixes
COMMON_FIXES = [
    # Fix missing precision qualifier
    (r'^(void\s+main)', r'precision mediump float;\n\1'),
    # Fix gl_FragColor in GLSL ES 300
    (r'gl_FragColor\s*=', 'gl_FragColor ='),
    # Fix deprecated texture2D -> texture in newer GLSL
    # (keep texture2D for ISF compatibility)
]


def sha256_file(path):
    """Compute SHA256 hash of file content (normalized whitespace)."""
    try:
        with open(path, "r", errors="replace") as f:
            content = f.read().strip()
        return hashlib.sha256(content.encode()).hexdigest()
    except Exception:
        return None


def parse_isf_header(content):
    """Extract ISF JSON header from shader content."""
    match = re.match(r'\s*/\*\s*(\{.*?\})\s*\*/', content, re.DOTALL)
    if not match:
        return None, content
    json_str = match.group(1)
    try:
        header = json.loads(json_str)
        body = content[match.end():]
        return header, body
    except json.JSONDecodeError:
        # Try to fix common JSON issues
        fixed = json_str
        fixed = re.sub(r',\s*]', ']', fixed)
        fixed = re.sub(r',\s*}', '}', fixed)
        fixed = re.sub(r'\]\s*,\s*\{', '], {', fixed)  # fix ], { after array element
        # Fix trailing comma before closing bracket
        fixed = re.sub(r',(\s*[\]}])', r'\1', fixed)
        try:
            header = json.loads(fixed)
            body = content[match.end():]
            return header, body
        except json.JSONDecodeError:
            return None, content


def build_isf_header(desc, category, tags, inputs, credit="GLSL Sandbox / various"):
    """Build a standard ISF JSON header."""
    header = {
        "DESCRIPTION": desc,
        "CREDIT": credit,
        "ISFVSN": "2.0",
        "CATEGORIES": [category] if isinstance(category, str) else category,
        "TAGS": tags,
        "INPUTS": inputs
    }
    return header


def standard_isf_inputs(existing_inputs=None):
    """Build standard ISF inputs with frame index support."""
    base_inputs = [
        {
            "NAME": "useFrameIndex",
            "TYPE": "bool",
            "DEFAULT": 0,
            "LABEL": "Use frame index (timeline sync)"
        },
        {
            "NAME": "fps",
            "TYPE": "float",
            "DEFAULT": 60.0,
            "MIN": 24.0,
            "MAX": 120.0
        }
    ]

    if existing_inputs:
        # Check if useFrameIndex already exists
        has_frame = any(i.get("NAME") == "useFrameIndex" for i in existing_inputs)
        has_fps = any(i.get("NAME") == "fps" for i in existing_inputs)
        if has_frame and has_fps:
            return existing_inputs
        result = list(base_inputs)
        for inp in existing_inputs:
            name = inp.get("NAME", "")
            if name not in ("useFrameIndex", "fps"):
                result.append(inp)
        return result
    return list(base_inputs)


def find_exposable_uniforms(glsl_body):
    """Find uniform declarations that could be exposed as ISF inputs."""
    # Match: uniform float/int/bool/vec2/vec3/vec4 name;
    # Also match: uniform float name; // @expose min max
    pattern = r'uniform\s+(float|int|bool|vec2|vec3|vec4)\s+(\w+)\s*;(?:\s*//\s*@expose\s+([\d.+-]+)\s+([\d.+-]+))?'
    found = []
    for match in re.finditer(pattern, glsl_body):
        utype, name, emin, emax = match.groups()
        if name.upper() in ISF_BUILTINS or name in GLSL_BUILTINS:
            continue
        if name in ("useFrameIndex", "fps", "timeScale", "mouseX", "mouseY",
                     "zoom", "colorR", "colorG", "colorB", "brightness",
                     "saturation", "contrast", "hueShift", "invert", "speed"):
            continue
        param = {"NAME": name, "TYPE": utype}
        if emin and emax:
            param["MIN"] = float(emin)
            param["MAX"] = float(emax)
            param["DEFAULT"] = (float(emin) + float(emax)) / 2.0
        else:
            # Guess reasonable ranges
            if utype == "float":
                param["MIN"] = 0.0
                param["MAX"] = 5.0
                param["DEFAULT"] = 1.0
            elif utype == "int":
                param["MIN"] = 0
                param["MAX"] = 10
                param["DEFAULT"] = 1
            elif utype == "bool":
                param["DEFAULT"] = 0
        # Generate a human-readable label
        label = re.sub(r'([A-Z])', r' \1', name).strip()
        label = label.replace('_', ' ').title()
        param["LABEL"] = label
        found.append(param)
    return found


def find_hardcoded_constants(glsl_body):
    """Find #define constants that could be exposed as parameters."""
    pattern = r'#define\s+(\w+)\s+([\d.]+)'
    found = []
    skip_names = {"PI", "TAU", "E", "EPSILON", "MAX_STEPS", "MarchSteps",
                  "NoiseSteps", "PRECISION"}
    for match in re.finditer(pattern, glsl_body):
        name, value = match.groups()
        if name.upper() in skip_names or name.startswith("GL_"):
            continue
        try:
            val = float(value)
            if 0.001 < abs(val) < 10000:
                found.append((name, val))
        except ValueError:
            pass
    return found


def categorize_shader(filename, content, existing_cats=None):
    """Determine shader category based on filename and content analysis."""
    # Normalize existing categories to lowercase
    if existing_cats:
        existing_cats = [c.lower() for c in existing_cats]
    if existing_cats and existing_cats != ["misc"] and existing_cats[0] in CATEGORY_KEYWORDS:
        return existing_cats[0], existing_cats

    lower_name = filename.lower()
    lower_content = content.lower()[:3000]  # First 3000 chars for speed

    scores = Counter()
    for cat, keywords in CATEGORY_KEYWORDS.items():
        for kw in keywords:
            if kw in lower_name:
                scores[cat] += 3  # Filename match is stronger
            if kw in lower_content:
                scores[cat] += 1

    # Specific pattern detection
    if "sdSphere" in content or "sdBox" in content or "sdTorus" in content:
        scores["3d"] += 5
    if "mandelbrot" in lower_content or "z = vec2(z.x*z.x" in content:
        scores["fractal"] += 5
    if "fbm" in content or "simplex" in lower_content:
        scores["noise"] += 3
    if "atan(uv.y, uv.x)" in content and "mod(a" in content:
        scores["psychedelic"] += 3  # kaleidoscope pattern
    if re.search(r'for.*float.*i.*<.*\d+', content):
        if "particle" in lower_content or "star" in lower_content:
            scores["particles"] += 3

    if scores:
        primary = scores.most_common(1)[0][0]
        # Get all categories with significant scores
        tags = [cat for cat, score in scores.most_common(3) if score >= 2]
        if not tags:
            tags = [primary]
        return primary, tags
    return "misc", ["misc"]


def generate_clean_name(filename, category):
    """Generate a clean, standardized name for the shader."""
    name = Path(filename).stem
    # Remove UNSORTED/UNSORTEDSHADER prefix
    name = re.sub(r'^UNSORTED(SHADER)?(\d+)?$', '', name)
    # Remove numeric-only names
    if re.match(r'^\d+$', name) or not name:
        return None  # Needs content-based naming
    # Clean up
    name = re.sub(r'[-_]+', '-', name)
    name = name.strip('-')
    return name


def validate_glsl(shader_path, content=None):
    """Validate GLSL using glslangValidator with ISF compatibility. Returns (success, errors)."""
    if content is None:
        with open(shader_path, 'r', errors='replace') as f:
            content = f.read()

    header, body = parse_isf_header(content)

    # Build ISF preamble with all builtins and declared inputs
    preamble_lines = [
        "#version 120",
        "",
        "// ISF built-in uniforms",
        "uniform vec2 RENDERSIZE;",
        "uniform float TIME;",
        "uniform int FRAMEINDEX;",
        "uniform int PASSINDEX;",
        "uniform vec4 DATE;",
        "uniform float TIMEDELTA;",
        "varying vec2 isf_FragNormCoord;",
        "varying vec2 surfacePosition;",
        "",
    ]

    # Declare all ISF INPUTS as uniforms
    if header and "INPUTS" in header:
        preamble_lines.append("// ISF input uniforms")
        for inp in header["INPUTS"]:
            name = inp.get("NAME", "")
            itype = inp.get("TYPE", "float")
            if not name:
                continue
            glsl_type = {
                "float": "float",
                "bool": "bool",
                "int": "int",
                "long": "int",
                "color": "vec4",
                "point2D": "vec2",
                "image": "sampler2D",
                "event": "bool",
            }.get(itype, "float")
            preamble_lines.append(f"uniform {glsl_type} {name};")
        preamble_lines.append("")

    test_code = body

    # Remove #version directives from body (we set it in preamble)
    test_code = re.sub(r'#version\s+\d+(\s+\w+)?', '', test_code)

    # Remove precision qualifiers (not valid in GLSL 120 desktop)
    test_code = re.sub(r'precision\s+(lowp|mediump|highp)\s+(float|int|sampler2D)\s*;', '', test_code)
    # Also remove inline precision qualifiers
    test_code = re.sub(r'\b(lowp|mediump|highp)\b\s+', '', test_code)

    # Remove GL_ES conditional blocks (we're validating as desktop GL)
    test_code = re.sub(r'#ifdef\s+GL_ES\s*\n.*?\n\s*#endif', '', test_code, flags=re.DOTALL)
    test_code = re.sub(r'#if\s+defined\s*\(\s*GL_ES\s*\).*?#endif', '', test_code, flags=re.DOTALL)

    # Remove duplicate declarations that match ISF inputs or preamble
    if header and "INPUTS" in header:
        input_names = {inp.get("NAME", "") for inp in header["INPUTS"]}
        for name in input_names:
            if name:
                test_code = re.sub(
                    rf'uniform\s+\w+\s+{re.escape(name)}\s*;[^\n]*\n?', '', test_code
                )

    # Remove varying/uniform declarations that match preamble builtins
    for builtin in ["surfacePosition", "isf_FragNormCoord"]:
        test_code = re.sub(
            rf'varying\s+vec2\s+{builtin}\s*;[^\n]*\n?', '', test_code
        )
    for builtin in ["RENDERSIZE", "TIME", "FRAMEINDEX", "PASSINDEX", "DATE", "TIMEDELTA"]:
        test_code = re.sub(
            rf'uniform\s+\w+\s+{builtin}\s*;[^\n]*\n?', '', test_code
        )

    # Check for #extension directives and move them to top
    extensions = re.findall(r'(#extension\s+\S+\s*:\s*\w+)', test_code)
    test_code_no_ext = re.sub(r'#extension\s+\S+\s*:\s*\w+', '', test_code)

    full_code = "\n".join(preamble_lines)
    for ext in extensions:
        full_code += "\n" + ext
    full_code += "\n" + test_code_no_ext

    tmp = "/tmp/shader_validate.frag"
    with open(tmp, 'w') as f:
        f.write(full_code)

    try:
        result = subprocess.run(
            ["glslangValidator", tmp],
            capture_output=True, timeout=5
        )
        stdout = result.stdout.decode('utf-8', errors='replace')
        stderr = result.stderr.decode('utf-8', errors='replace')
        if result.returncode == 0:
            return True, ""
        errors = stdout + stderr
        error_lines = [l for l in errors.split('\n')
                      if 'ERROR' in l and 'deprecated' not in l.lower()]
        if not error_lines:
            return True, ""
        return False, "\n".join(error_lines)
    except (subprocess.TimeoutExpired, FileNotFoundError):
        return False, "Validation tool error"


def fix_common_glsl_errors(content, errors=""):
    """Apply common fixes to broken GLSL shaders."""
    header, body = parse_isf_header(content)
    fixed = body
    changes = []

    # Fix 1: Remove conflicting #version directives
    if re.search(r'#version\s+\d+', fixed):
        fixed = re.sub(r'#version\s+\d+(\s+\w+)?', '', fixed)
        changes.append("Removed conflicting #version directive")

    # Fix 2: Missing precision qualifier (add after #define lines)
    if "precision" not in fixed[:500] and "void main" in fixed:
        # Find good insertion point (after #define lines)
        lines = fixed.split('\n')
        insert_idx = 0
        for i, line in enumerate(lines):
            if line.strip().startswith('#define') or line.strip().startswith('#extension'):
                insert_idx = i + 1
            elif line.strip() and not line.strip().startswith('//') and not line.strip().startswith('#'):
                break
        lines.insert(insert_idx, 'precision mediump float;')
        fixed = '\n'.join(lines)
        changes.append("Added missing precision qualifier")

    # Fix 3: Undeclared variable 'surfacePosition'
    if "surfacePosition" in fixed and "varying vec2 surfacePosition" not in fixed:
        # Insert after precision/defines
        lines = fixed.split('\n')
        insert_idx = 0
        for i, line in enumerate(lines):
            stripped = line.strip()
            if stripped.startswith('precision') or stripped.startswith('#define') or stripped.startswith('#extension'):
                insert_idx = i + 1
        lines.insert(insert_idx, 'varying vec2 surfacePosition;')
        fixed = '\n'.join(lines)
        changes.append("Added missing 'varying vec2 surfacePosition' declaration")

    # Fix 4: Missing #extension before use of dFdx/dFdy
    if ("dFdx" in fixed or "dFdy" in fixed or "fwidth" in fixed):
        if "GL_OES_standard_derivatives" not in fixed:
            fixed = "#extension GL_OES_standard_derivatives : enable\n" + fixed
            changes.append("Added missing GL_OES_standard_derivatives extension")

    # Fix 5: Undeclared 'zoom' uniform (common missing uniform)
    error_lower = errors.lower()
    undeclared = re.findall(r"'(\w+)'\s*:\s*undeclared identifier", errors)
    for name in undeclared:
        if name in ("zoom", "scale", "intensity", "offset", "amplitude",
                     "frequency", "phase", "rotation", "amount"):
            if f"uniform float {name}" not in fixed:
                # Add uniform declaration
                lines = fixed.split('\n')
                insert_idx = 0
                for i, line in enumerate(lines):
                    stripped = line.strip()
                    if stripped.startswith('precision') or stripped.startswith('varying') or stripped.startswith('#'):
                        insert_idx = i + 1
                lines.insert(insert_idx, f'uniform float {name}; // @expose 0 1')
                fixed = '\n'.join(lines)
                changes.append(f"Added missing uniform '{name}'")

    # Fix 6: Replace deprecated texture2D in #version 300+
    if "#version 300" in fixed:
        fixed = fixed.replace("texture2D(", "texture(")
        fixed = fixed.replace("varying", "in")
        changes.append("Updated deprecated GLSL calls for version 300+")

    # Fix 7: Common typos
    fixed = fixed.replace("precisionmediump", "precision mediump")
    fixed = fixed.replace("vooid", "void")

    # Fix 8: ShaderToy compatibility - add defines for iResolution, iTime etc.
    shadertoy_vars = {
        "iResolution": ("vec3", "vec3(RENDERSIZE, 1.0)"),
        "iTime": ("float", "TIME"),
        "iGlobalTime": ("float", "TIME"),
        "iTimeDelta": ("float", "TIMEDELTA"),
        "iFrame": ("int", "FRAMEINDEX"),
        "iMouse": ("vec4", "vec4(0.0)"),
        "iDate": ("vec4", "DATE"),
        "iChannelTime": ("float", "TIME"),
    }
    for var, (vtype, replacement) in shadertoy_vars.items():
        if var in fixed and f"#define {var}" not in fixed and f"uniform" not in fixed[:fixed.find(var) if var in fixed else 0]:
            fixed = f"#define {var} {replacement}\n" + fixed
            changes.append(f"Added ShaderToy compat: {var}")

    # Fix 9: Reserved word 'filter' - rename to 'filterVal'
    if re.search(r'\bfilter\b', fixed) and "Reserved" in errors:
        fixed = re.sub(r'\bfilter\b', 'filterVal', fixed)
        changes.append("Renamed reserved word 'filter' to 'filterVal'")

    # Fix 10: Undeclared 'time' - add #define time TIME
    if "'time' : undeclared" in errors.lower() or "'time'" in errors:
        if "#define time" not in fixed and "uniform float time" not in fixed:
            fixed = "#define time TIME\n" + fixed
            changes.append("Added #define time TIME for compatibility")

    # Fix 11: Undeclared common uniforms from ISF context
    for name in re.findall(r"'(\w+)'\s*:\s*undeclared identifier", errors):
        if name in ("speed", "zoom", "scale", "intensity", "offset",
                    "amplitude", "frequency", "phase", "rotation", "amount",
                    "radius", "size", "power", "decay", "threshold",
                    "colorShift", "waveSpeed"):
            if f"uniform float {name}" not in fixed and f"#define {name}" not in fixed:
                lines = fixed.split('\n')
                insert_idx = 0
                for i, line in enumerate(lines):
                    stripped = line.strip()
                    if stripped.startswith(('precision', 'varying', '#define', '#extension')):
                        insert_idx = i + 1
                lines.insert(insert_idx, f'uniform float {name}; // @expose 0 5')
                fixed = '\n'.join(lines)
                changes.append(f"Added missing uniform '{name}'")

    # Reconstruct with header
    if header:
        return format_isf_file(header, fixed), changes
    return fixed, changes


def format_isf_file(header, body):
    """Format a complete ISF file with header and body."""
    json_str = json.dumps(header, indent=4)
    return f"/*{json_str}*/\n\n{body}"


def time_used_as_variable(body):
    """Check if 'time' is used as a local variable or function parameter (not just TIME uniform)."""
    # Match: float time, (float time), int time etc. but not #define time
    # Also match: time = ... (assignment without uniform decl)
    if re.search(r'(?:float|int|vec\d)\s+time\b', body):
        return True
    # Check function params like void foo(float time)
    if re.search(r'\(\s*(?:float|int)\s+time\s*[,)]', body):
        return True
    return False


def ensure_frame_index_support(header, body):
    """Ensure shader supports frame index toggle."""
    inputs = header.get("INPUTS", [])
    has_frame = any(i.get("NAME") == "useFrameIndex" for i in inputs)

    if not has_frame:
        inputs = standard_isf_inputs(inputs)
        header["INPUTS"] = inputs

    # Check if 'time' is used as a variable name in the shader
    uses_time_var = time_used_as_variable(body)

    if uses_time_var:
        # 'time' is used as a local var/param - can't use #define time
        # Keep the existing #define if present, otherwise don't add one
        # The ISF runtime provides TIME directly
        existing_define = re.search(r'#define\s+time\b', body)
        if existing_define:
            # Remove it to avoid conflict, shaders using time as param handle it themselves
            body = re.sub(r'#define\s+time\b[^\n]*\n?', '', body)
    else:
        # Safe to use #define time
        time_define = '#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME)'
        if "timeScale" in body or any(i.get("NAME") == "timeScale" for i in inputs):
            time_define += ' * timeScale)'
        elif "speed" in body or any(i.get("NAME") == "speed" for i in inputs):
            time_define += ' * speed)'
        else:
            time_define += ')'

        if re.search(r'#define\s+time\b', body):
            body = re.sub(r'#define\s+time\b[^\n]*', time_define, body)
        else:
            body = time_define + "\n" + body

    return header, body


def add_color_controls(header, body):
    """Add standard color control parameters (brightness, saturation, etc.)."""
    inputs = header.get("INPUTS", [])
    existing_names = {i.get("NAME") for i in inputs}

    color_params = [
        {"NAME": "brightness", "TYPE": "float", "DEFAULT": 0.0,
         "MIN": -1.0, "MAX": 1.0, "LABEL": "Brightness"},
        {"NAME": "saturation", "TYPE": "float", "DEFAULT": 1.0,
         "MIN": 0.0, "MAX": 3.0, "LABEL": "Saturation"},
        {"NAME": "contrast", "TYPE": "float", "DEFAULT": 1.0,
         "MIN": 0.0, "MAX": 3.0, "LABEL": "Contrast"},
        {"NAME": "hueShift", "TYPE": "float", "DEFAULT": 0.0,
         "MIN": 0.0, "MAX": 1.0, "LABEL": "Hue Shift"},
        {"NAME": "invert", "TYPE": "bool", "DEFAULT": 0,
         "LABEL": "Invert Colors"},
    ]

    added = []
    for param in color_params:
        if param["NAME"] not in existing_names:
            inputs.append(param)
            added.append(param["NAME"])

    if added:
        header["INPUTS"] = inputs

        # Add color manipulation code before final gl_FragColor
        color_code = """
// Macroverse color controls
vec3 _mv_col = gl_FragColor.rgb;
_mv_col = mix(vec3(dot(_mv_col, vec3(0.299, 0.587, 0.114))), _mv_col, saturation);
_mv_col = (_mv_col - 0.5) * contrast + 0.5;
_mv_col += brightness;
float _mv_h = hueShift * 6.2832;
float _mv_cs = cos(_mv_h), _mv_sn = sin(_mv_h);
_mv_col = _mv_col * mat3(
    0.299+0.701*_mv_cs+0.168*_mv_sn, 0.587-0.587*_mv_cs+0.330*_mv_sn, 0.114-0.114*_mv_cs-0.497*_mv_sn,
    0.299-0.299*_mv_cs-0.328*_mv_sn, 0.587+0.413*_mv_cs+0.035*_mv_sn, 0.114-0.114*_mv_cs+0.292*_mv_sn,
    0.299-0.300*_mv_cs+1.250*_mv_sn, 0.587-0.588*_mv_cs-1.050*_mv_sn, 0.114+0.886*_mv_cs-0.203*_mv_sn
);
if (invert) _mv_col = 1.0 - _mv_col;
gl_FragColor = vec4(clamp(_mv_col, 0.0, 1.0), gl_FragColor.a);
"""
        # Find the last gl_FragColor assignment and add color code after the main function's last }
        # Insert before the very last closing brace
        last_brace = body.rfind('}')
        if last_brace > 0:
            # Find the gl_FragColor assignment before this brace
            frag_assigns = list(re.finditer(r'gl_FragColor\s*=', body[:last_brace]))
            if frag_assigns:
                body = body[:last_brace] + color_code + "\n" + body[last_brace:]

    return header, body


def process_shader(filepath, stats):
    """Process a single shader file. Returns (processed_content, metadata) or None."""
    try:
        with open(filepath, 'r', errors='replace') as f:
            content = f.read()
    except Exception as e:
        stats["read_errors"] += 1
        return None

    if len(content.strip()) < 20:
        stats["empty_files"] += 1
        return None

    filename = Path(filepath).name
    header, body = parse_isf_header(content)

    is_isf = header is not None

    if not is_isf:
        # Raw GLSL - wrap in ISF format
        header = {
            "DESCRIPTION": Path(filepath).stem,
            "CREDIT": "GLSL Sandbox / various",
            "ISFVSN": "2.0",
            "CATEGORIES": ["misc"],
            "INPUTS": []
        }
        body = content
        stats["converted_to_isf"] += 1

    # Ensure frame index support
    header, body = ensure_frame_index_support(header, body)

    # Find and expose additional parameters
    extra_params = find_exposable_uniforms(body)
    existing_names = {i.get("NAME") for i in header.get("INPUTS", [])}
    new_params = [p for p in extra_params if p["NAME"] not in existing_names]
    if new_params:
        header["INPUTS"] = header.get("INPUTS", []) + new_params
        stats["params_exposed"] += len(new_params)

    # Categorize (normalize to lowercase)
    existing_cats = [c.lower() for c in header.get("CATEGORIES", ["misc"])]
    primary_cat, all_cats = categorize_shader(filename, body, existing_cats)
    header["CATEGORIES"] = [primary_cat]
    header["TAGS"] = list(set(all_cats))

    # Generate clean name
    desc = header.get("DESCRIPTION", Path(filepath).stem)
    if desc.startswith("UNSORTED") or desc.startswith("ShaderCollections"):
        clean = generate_clean_name(filename, primary_cat)
        if clean:
            header["DESCRIPTION"] = clean

    # Build output content
    output = format_isf_file(header, body)

    metadata = {
        "original_path": str(filepath),
        "name": header.get("DESCRIPTION", filename),
        "category": primary_cat,
        "tags": all_cats,
        "is_isf": is_isf,
        "param_count": len(header.get("INPUTS", [])),
        "exposed_params": len(new_params),
    }

    return output, metadata


def validate_shader_batch(shader_entries):
    """Validate a batch of shaders using glslangValidator."""
    results = {}
    for path, content in shader_entries:
        success, errors = validate_glsl(path, content)
        results[path] = (success, errors)
    return results


def main():
    print("=" * 60)
    print("MACROVERSE V5 SHADER PROCESSING PIPELINE")
    print("=" * 60)
    start_time = time_module.time()

    if not TRAINING.exists():
        print(f"\nError: TRAINING_MACROVERSV5 not found at {TRAINING}")
        print("Pipeline needs that folder as input. Nothing was changed.")
        sys.exit(1)

    # Phase 1: Inventory
    print("\n[PHASE 1] Building inventory...")
    all_shaders = []
    for ext in SHADER_EXTENSIONS:
        for f in TRAINING.rglob(f"*{ext}"):
            all_shaders.append(f)
    print(f"  Found {len(all_shaders)} shader files")

    # Phase 2: Deduplicate
    print("\n[PHASE 2] Deduplicating...")
    hash_map = defaultdict(list)
    for shader in all_shaders:
        h = sha256_file(shader)
        if h:
            hash_map[h].append(shader)

    unique_shaders = []
    duplicates = 0
    for h, paths in hash_map.items():
        # Keep the one from Production-Use if available, else first found
        best = paths[0]
        for p in paths:
            if "Production-Use" in str(p):
                best = p
                break
            elif "Wire-Ready" in str(p):
                best = p
        unique_shaders.append(best)
        if len(paths) > 1:
            duplicates += len(paths) - 1

    print(f"  Unique shaders: {len(unique_shaders)}")
    print(f"  Duplicates removed: {duplicates}")

    # Phase 3: Process all shaders
    print("\n[PHASE 3] Processing shaders...")
    stats = Counter()
    processed = []
    failed = []

    for i, shader_path in enumerate(unique_shaders):
        if (i + 1) % 500 == 0:
            print(f"  Processing {i+1}/{len(unique_shaders)}...")

        result = process_shader(shader_path, stats)
        if result is None:
            failed.append(str(shader_path))
            stats["process_failed"] += 1
            continue

        content, metadata = result
        processed.append((shader_path, content, metadata))
        stats["processed"] += 1

    print(f"  Processed: {stats['processed']}")
    print(f"  Failed: {stats['process_failed']}")
    print(f"  Converted to ISF: {stats['converted_to_isf']}")
    print(f"  New params exposed: {stats['params_exposed']}")

    # Phase 4: Validate compilation
    print("\n[PHASE 4] Validating GLSL compilation...")
    compile_pass = 0
    compile_fail = 0
    compile_errors = []

    for i, (path, content, meta) in enumerate(processed):
        if (i + 1) % 500 == 0:
            print(f"  Validating {i+1}/{len(processed)}...")

        success, errors = validate_glsl(path, content)
        if success:
            compile_pass += 1
            meta["compiles"] = True
        else:
            # Try to fix
            fixed_content, changes = fix_common_glsl_errors(content, errors)
            if changes:
                success2, errors2 = validate_glsl(path, fixed_content)
                if success2:
                    compile_pass += 1
                    meta["compiles"] = True
                    meta["auto_fixed"] = True
                    meta["fix_changes"] = changes
                    # Update the content
                    processed[i] = (path, fixed_content, meta)
                    stats["auto_fixed"] += 1
                else:
                    compile_fail += 1
                    meta["compiles"] = False
                    meta["errors"] = errors2[:500]
                    compile_errors.append((str(path), errors2[:300]))
            else:
                compile_fail += 1
                meta["compiles"] = False
                meta["errors"] = errors[:500]
                compile_errors.append((str(path), errors[:300]))

    print(f"  Compile pass: {compile_pass}")
    print(f"  Compile fail: {compile_fail}")
    print(f"  Auto-fixed: {stats['auto_fixed']}")

    # Phase 5: Sort into production folders
    print("\n[PHASE 5] Creating SORTED_FOR_PRODUCTION...")
    if SORTED_DIR.exists():
        shutil.rmtree(SORTED_DIR)

    # Create category folders
    glsl_dir = SORTED_DIR / "GLSL"
    isf_dir = SORTED_DIR / "ISF"
    broken_dir = SORTED_DIR / "BROKEN"

    index_entries = []
    name_counter = Counter()

    for path, content, meta in processed:
        category = meta.get("category", "misc")

        if not meta.get("compiles", False):
            # Put broken shaders in BROKEN folder
            cat_dir = broken_dir / category
            cat_dir.mkdir(parents=True, exist_ok=True)
        else:
            # All .fs files are ISF format
            cat_dir = isf_dir / category
            cat_dir.mkdir(parents=True, exist_ok=True)

        # Generate unique filename
        name = meta.get("name", Path(path).stem)
        name = re.sub(r'[^\w\-.]', '_', name)
        if name_counter[name] > 0:
            name = f"{name}_{name_counter[name]}"
        name_counter[name] += 1

        out_path = cat_dir / f"{name}.fs"
        try:
            with open(out_path, 'w') as f:
                f.write(content)
        except Exception as e:
            stats["write_errors"] += 1
            continue

        # Build index entry
        entry = {
            "path": str(out_path.relative_to(WORKSPACE)),
            "name": meta.get("name", name),
            "category": category,
            "tags": meta.get("tags", []),
            "format": "isf",
            "compiles": meta.get("compiles", False),
            "param_count": meta.get("param_count", 0),
            "original": str(Path(path).relative_to(WORKSPACE)),
        }
        if meta.get("auto_fixed"):
            entry["auto_fixed"] = True
        index_entries.append(entry)

    # Also copy the GLSL concept sources
    concept_dir = TRAINING / "Macroverse-Unsorted" / "Concept-Source"
    if concept_dir.exists():
        glsl_concept = glsl_dir / "concept"
        glsl_concept.mkdir(parents=True, exist_ok=True)
        for f in concept_dir.glob("*.glsl"):
            shutil.copy2(f, glsl_concept / f.name)
            index_entries.append({
                "path": str((glsl_concept / f.name).relative_to(WORKSPACE)),
                "name": f.stem,
                "category": "concept",
                "tags": ["concept", "raw-glsl"],
                "format": "glsl",
                "compiles": None,
                "param_count": 0,
            })

    # Write index
    with open(INDEX_PATH, 'w') as f:
        json.dump(index_entries, f, indent=2)
    print(f"  Written {len(index_entries)} entries to index")

    # Phase 6: Generate report
    print("\n[PHASE 6] Generating report...")

    cat_counts = Counter()
    param_total = 0
    compiling_count = 0
    with_params = 0

    for entry in index_entries:
        cat_counts[entry.get("category", "misc")] += 1
        pc = entry.get("param_count", 0)
        param_total += pc
        if pc > 0:
            with_params += 1
        if entry.get("compiles"):
            compiling_count += 1

    elapsed = time_module.time() - start_time

    report_lines = [
        "=" * 60,
        "MACROVERSE V5 SHADER PROCESSING REPORT",
        "=" * 60,
        "",
        f"Processing time: {elapsed:.1f} seconds",
        "",
        "--- INVENTORY ---",
        f"Total shader files found:     {len(all_shaders)}",
        f"Duplicate files removed:      {duplicates}",
        f"Unique shaders processed:     {len(unique_shaders)}",
        f"Empty/unreadable files:       {stats['empty_files'] + stats['read_errors']}",
        "",
        "--- COMPILATION ---",
        f"Shaders that compile:         {compile_pass}",
        f"Shaders that fail to compile: {compile_fail}",
        f"Auto-fixed shaders:           {stats['auto_fixed']}",
        f"Compile success rate:         {compile_pass/(compile_pass+compile_fail)*100:.1f}%",
        "",
        "--- PARAMETERS ---",
        f"Shaders with ISF parameters:  {with_params}",
        f"Total parameters exposed:     {param_total}",
        f"New parameters discovered:    {stats['params_exposed']}",
        f"Converted to ISF format:      {stats['converted_to_isf']}",
        "",
        "--- CATEGORIES ---",
    ]

    for cat, count in cat_counts.most_common():
        report_lines.append(f"  {cat:20s} {count:5d} shaders")

    report_lines.extend([
        "",
        "--- OUTPUT ---",
        f"Production-ready (compile):   {compiling_count}",
        f"Broken (needs manual fix):    {compile_fail}",
        f"Total in SORTED_FOR_PRODUCTION: {len(index_entries)}",
        "",
        f"Index file:  {INDEX_PATH}",
        f"Output dir:  {SORTED_DIR}",
        "",
    ])

    if compile_errors:
        report_lines.append("--- SAMPLE COMPILATION ERRORS (first 20) ---")
        for path, err in compile_errors[:20]:
            short_path = Path(path).name
            report_lines.append(f"  {short_path}: {err[:100]}")
        report_lines.append("")

    report_lines.append("=" * 60)
    report_lines.append("END OF REPORT")
    report_lines.append("=" * 60)

    report_text = "\n".join(report_lines)
    with open(REPORT_PATH, 'w') as f:
        f.write(report_text)

    print(report_text)
    print(f"\nReport saved to: {REPORT_PATH}")


if __name__ == "__main__":
    main()
