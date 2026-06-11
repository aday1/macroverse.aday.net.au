#!/usr/bin/env python3
"""
Macroverse Shader Fixer - Aggressive repair of broken shaders.
Trashes unrecoverable junk and applies targeted fixes to the rest.
"""

import os
import re
import json
import glob
import shutil
import subprocess
from pathlib import Path
from collections import Counter

SORTED_DIR = Path("/workspace/SORTED_FOR_PRODUCTION")
BROKEN_DIR = SORTED_DIR / "BROKEN"
ISF_DIR = SORTED_DIR / "ISF"

stats = Counter()


def validate_glsl(content):
    """Validate GLSL with full ISF preamble. Returns (success, errors, full_test_code)."""
    m = re.match(r'\s*/\*\s*(\{.*?\})\s*\*/', content, re.DOTALL)
    header = None
    body = content[m.end():] if m else content
    if m:
        try:
            fixed_json = re.sub(r',(\s*[\]}])', r'\1', m.group(1))
            header = json.loads(fixed_json)
        except:
            pass

    preamble = "#version 120\n"
    preamble += "uniform vec2 RENDERSIZE;\nuniform float TIME;\nuniform int FRAMEINDEX;\n"
    preamble += "uniform int PASSINDEX;\nuniform vec4 DATE;\nuniform float TIMEDELTA;\n"
    preamble += "varying vec2 isf_FragNormCoord;\nvarying vec2 surfacePosition;\n\n"

    if header and "INPUTS" in header:
        for inp in header.get("INPUTS", []):
            name = inp.get("NAME", "")
            itype = inp.get("TYPE", "float")
            glsl_type = {"float": "float", "bool": "bool", "int": "int",
                        "color": "vec4", "point2D": "vec2", "image": "sampler2D"}.get(itype, "float")
            if name:
                preamble += f"uniform {glsl_type} {name};\n"

    test_code = re.sub(r'#version\s+\d+(\s+\w+)?', '', body)
    test_code = re.sub(r'precision\s+(lowp|mediump|highp)\s+(float|int|sampler2D)\s*;', '', test_code)
    test_code = re.sub(r'\b(lowp|mediump|highp)\b\s+', '', test_code)
    # Remove #ifdef GL_ES blocks properly (multi-line)
    test_code = re.sub(r'#ifdef\s+GL_ES\b[^\n]*\n(.*?\n)?#endif[^\n]*\n?', '', test_code, flags=re.DOTALL)
    test_code = re.sub(r'#if\s+defined\s*\(\s*GL_ES\s*\)[^\n]*\n(.*?\n)?#endif[^\n]*\n?', '', test_code, flags=re.DOTALL)

    if header and "INPUTS" in header:
        for inp in header.get("INPUTS", []):
            name = inp.get("NAME", "")
            if name:
                test_code = re.sub(rf'uniform\s+\w+\s+{re.escape(name)}\s*;[^\n]*\n?', '', test_code)
    for b in ["surfacePosition", "isf_FragNormCoord"]:
        test_code = re.sub(rf'varying\s+vec2\s+{b}\s*;[^\n]*\n?', '', test_code)
    for b in ["RENDERSIZE", "TIME", "FRAMEINDEX", "PASSINDEX", "DATE", "TIMEDELTA"]:
        test_code = re.sub(rf'uniform\s+\w+\s+{b}\s*;[^\n]*\n?', '', test_code)

    full = preamble + "\n" + test_code
    with open('/tmp/shader_fix_test.frag', 'w') as f:
        f.write(full)

    try:
        result = subprocess.run(["glslangValidator", "/tmp/shader_fix_test.frag"],
                              capture_output=True, timeout=5)
        errors = (result.stdout + result.stderr).decode('utf-8', errors='replace')
        error_lines = [l for l in errors.split('\n') if 'ERROR' in l]
        if result.returncode == 0 or not error_lines:
            return True, "", full
        return False, "\n".join(error_lines), full
    except:
        return False, "Validation tool error", full


def is_junk(body):
    """Check if the shader body is web scraping junk or empty."""
    junk_indicators = ['fullscreengallery', 'hide code', 'compiled successfully',
                       'parentdiff', '<html', '<div', '<script', 'DOCTYPE',
                       'gallery', 'hide codecompiled', 'window.', 'document.']
    for j in junk_indicators:
        if j in body:
            return True

    clean = re.sub(r'#define\s+\w+\b[^\n]*', '', body)
    clean = re.sub(r'precision\s+\w+\s+\w+;', '', clean)
    clean = re.sub(r'#ifdef\s+GL_ES.*?#endif', '', clean, flags=re.DOTALL)
    clean = re.sub(r'varying\s+\w+\s+\w+;', '', clean)
    clean = re.sub(r'#extension[^\n]*', '', clean)
    clean = re.sub(r'uniform\s+\w+\s+\w+;[^\n]*', '', clean)
    clean = re.sub(r'//[^\n]*', '', clean)
    clean = re.sub(r'/\*.*?\*/', '', clean, flags=re.DOTALL)
    clean = clean.strip()

    if len(clean) < 30:
        return True

    if 'void main' not in body and 'void _userMain' not in body:
        # Check if it's a fragment with no main at all
        if 'gl_Frag' not in body and 'fragColor' not in body.lower():
            return True

    return False


def fix_endif_mismatch(content):
    """Fix #ifdef GL_ES / #endif mismatch issues."""
    m = re.match(r'\s*/\*\s*(\{.*?\})\s*\*/', content, re.DOTALL)
    body = content[m.end():] if m else content

    # The issue: #ifdef GL_ES gets stripped by validation but #endif remains.
    # Fix: properly remove ALL #ifdef GL_ES ... #endif blocks
    # Handle nested and multi-line blocks
    fixed = body

    # Pattern 1: #ifdef GL_ES\nprecision...\n#endif
    fixed = re.sub(
        r'#ifdef\s+GL_ES\s*\n\s*precision\s+\w+\s+float\s*;\s*\n\s*#endif',
        'precision mediump float;', fixed)

    # Pattern 2: #ifdef GL_ES\n...\n#else\n...\n#endif
    fixed = re.sub(
        r'#ifdef\s+GL_ES\s*\n(.*?)#else\s*\n(.*?)#endif',
        lambda m: m.group(2),  # keep the #else branch (non-ES)
        fixed, flags=re.DOTALL)

    # Pattern 3: standalone #ifdef GL_ES ... #endif
    fixed = re.sub(
        r'#ifdef\s+GL_ES\b[^\n]*\n(.*?\n)?#endif[^\n]*\n?',
        '', fixed, flags=re.DOTALL)

    # Pattern 4: #if defined(GL_ES) ... #endif
    fixed = re.sub(
        r'#if\s+defined\s*\(\s*GL_ES\s*\)[^\n]*\n(.*?\n)?#endif[^\n]*\n?',
        '', fixed, flags=re.DOTALL)

    # Pattern 5: orphaned #endif at the top of the file
    lines = fixed.split('\n')
    cleaned_lines = []
    ifdef_depth = 0
    for line in lines:
        stripped = line.strip()
        if stripped.startswith('#ifdef') or stripped.startswith('#if ') or stripped.startswith('#if('):
            ifdef_depth += 1
            cleaned_lines.append(line)
        elif stripped.startswith('#endif'):
            if ifdef_depth > 0:
                ifdef_depth -= 1
                cleaned_lines.append(line)
            # else: skip orphaned #endif
        elif stripped.startswith('#else') or stripped.startswith('#elif'):
            if ifdef_depth > 0:
                cleaned_lines.append(line)
        else:
            cleaned_lines.append(line)
    fixed = '\n'.join(cleaned_lines)

    if m:
        return content[:m.end()] + fixed
    return fixed


def fix_undeclared_variables(content, errors):
    """Fix undeclared variable errors by adding declarations."""
    m = re.match(r'\s*/\*\s*(\{.*?\})\s*\*/', content, re.DOTALL)
    header = None
    body = content[m.end():] if m else content
    if m:
        try:
            fixed_json = re.sub(r',(\s*[\]}])', r'\1', m.group(1))
            header = json.loads(fixed_json)
        except:
            pass

    fixed = body
    changes = []

    # Extract all undeclared identifiers from errors
    undeclared = set(re.findall(r"'(\w+)'\s*:\s*undeclared identifier", errors))

    for name in undeclared:
        if name in ('gl_FragColor', 'gl_FragCoord'):
            continue

        # ShaderToy compatibility
        shadertoy_map = {
            'iResolution': '#define iResolution vec3(RENDERSIZE, 1.0)',
            'iTime': '#define iTime TIME',
            'iGlobalTime': '#define iGlobalTime TIME',
            'iTimeDelta': '#define iTimeDelta TIMEDELTA',
            'iFrame': '#define iFrame FRAMEINDEX',
            'iMouse': '#define iMouse vec4(0.0)',
            'iDate': '#define iDate DATE',
            'iChannel0': 'uniform sampler2D iChannel0;',
            'iChannel1': 'uniform sampler2D iChannel1;',
            'iChannel2': 'uniform sampler2D iChannel2;',
            'iChannelResolution': '#define iChannelResolution vec3[4](vec3(RENDERSIZE,1.0),vec3(RENDERSIZE,1.0),vec3(RENDERSIZE,1.0),vec3(RENDERSIZE,1.0))',
        }
        if name in shadertoy_map:
            fixed = shadertoy_map[name] + '\n' + fixed
            changes.append(f"Added ShaderToy compat: {name}")
            continue

        # Math constants
        math_constants = {
            'PI': '#define PI 3.14159265359',
            'M_PI': '#define M_PI 3.14159265359',
            'TAU': '#define TAU 6.28318530718',
            'PISQR': '#define PISQR 9.8696044011',
            'HALF_PI': '#define HALF_PI 1.57079632679',
            'E': '#define E 2.71828182846',
            'PHI': '#define PHI 1.61803398875',
        }
        if name in math_constants:
            fixed = math_constants[name] + '\n' + fixed
            changes.append(f"Added math constant: {name}")
            continue

        # Common shader variables that should be uniforms
        common_uniforms = {
            'time': '#define time TIME',
            'speed': 'uniform float speed; // @expose 0.1 3',
            'zoom': 'uniform float zoom; // @expose 0.1 4',
            'scale': 'uniform float scale; // @expose 0.1 5',
            'intensity': 'uniform float intensity; // @expose 0.1 5',
            'vibration': 'uniform float vibration; // @expose 0 2',
            'amplitude': 'uniform float amplitude; // @expose 0 3',
            'frequency': 'uniform float frequency; // @expose 0.1 10',
            'phase': 'uniform float phase; // @expose 0 6.28',
            'rotation': 'uniform float rotation; // @expose 0 6.28',
            'amount': 'uniform float amount; // @expose 0 2',
            'radius': 'uniform float radius; // @expose 0.1 3',
            'size': 'uniform float size; // @expose 0.1 5',
            'power': 'uniform float power; // @expose 0.1 5',
            'decay': 'uniform float decay; // @expose 0 2',
            'threshold': 'uniform float threshold; // @expose 0 1',
            'offset': 'uniform float offset; // @expose -2 2',
            'colorShift': 'uniform float colorShift; // @expose 0 1',
            'brightness': 'uniform float brightness; // @expose 0 2',
            'contrast': 'uniform float contrast; // @expose 0 3',
            'saturation': 'uniform float saturation; // @expose 0 3',
            'hueShift': 'uniform float hueShift; // @expose 0 1',
            'mouseX': 'uniform float mouseX; // @expose -1 1',
            'mouseY': 'uniform float mouseY; // @expose -1 1',
            'timeOffset': 'uniform float timeOffset; // @expose 0 10',
            'gTime': '#define gTime TIME',
            'fSequenceTime': '#define fSequenceTime TIME',
            'position': 'uniform vec2 position;',
            'inputImage': 'uniform sampler2D inputImage;',
        }
        if name in common_uniforms:
            fixed = common_uniforms[name] + '\n' + fixed
            changes.append(f"Added declaration: {name}")
            continue

        # For #define-style constants that are numeric
        # Check if it looks like it was meant to be a constant
        if name.isupper() or name.startswith('MAX_') or name.startswith('NUM_'):
            # Guess a value based on context
            fixed = f'#define {name} 1.0\n' + fixed
            changes.append(f"Added fallback constant: {name} = 1.0")
            continue

        # Generic float uniform for anything else
        if re.search(rf'\b{name}\b\s*[\*\+\-/=]', fixed):
            fixed = f'uniform float {name}; // @expose 0 1\n' + fixed
            changes.append(f"Added generic uniform: {name}")

    if m:
        return content[:m.end()] + fixed, changes
    return fixed, changes


def fix_redefinition(content, errors):
    """Fix redefinition conflicts by removing duplicate declarations."""
    m = re.match(r'\s*/\*\s*(\{.*?\})\s*\*/', content, re.DOTALL)
    body = content[m.end():] if m else content
    fixed = body
    changes = []

    # Find what's redefined
    redefined = re.findall(r"'(\w+)'\s*:\s*redefinition", errors)
    for name in redefined:
        # Remove duplicate uniform declarations (keep the first one)
        pattern = rf'(uniform\s+\w+\s+{re.escape(name)}\s*;[^\n]*\n)'
        matches = list(re.finditer(pattern, fixed))
        if len(matches) > 1:
            # Remove all but the first
            for match in reversed(matches[1:]):
                fixed = fixed[:match.start()] + fixed[match.end():]
            changes.append(f"Removed duplicate uniform: {name}")

        # Remove duplicate #define
        pattern = rf'(#define\s+{re.escape(name)}\b[^\n]*\n)'
        matches = list(re.finditer(pattern, fixed))
        if len(matches) > 1:
            for match in reversed(matches[1:]):
                fixed = fixed[:match.start()] + fixed[match.end():]
            changes.append(f"Removed duplicate #define: {name}")

    if m:
        return content[:m.end()] + fixed, changes
    return fixed, changes


def fix_scalar_swizzle(content, errors):
    """Fix scalar swizzle issues by adding proper type casts."""
    m = re.match(r'\s*/\*\s*(\{.*?\})\s*\*/', content, re.DOTALL)
    body = content[m.end():] if m else content
    fixed = body
    changes = []

    # Scalar swizzle: float_val.x or float_val.xy
    # Fix by wrapping in vec constructor
    # e.g., length(p).x -> float(length(p))
    # Common pattern: some_float.x, some_float.xy, some_float.xyz

    # Fix float.x patterns
    # This is tricky - we need to find cases where a scalar is swizzled
    # Common in ShaderToy ports: iResolution.xy where iResolution is vec3
    # But if we defined iResolution as vec3, it should work

    # The real issue is often: float_expr.x or float_expr.xy
    # Let's try a different approach - check if specific lines have the issue

    # Often it's texture2D().x or similar
    # Replace .x on known float functions with nothing
    float_funcs = ['length', 'distance', 'dot', 'abs', 'sin', 'cos', 'tan',
                   'asin', 'acos', 'atan', 'pow', 'exp', 'log', 'sqrt',
                   'floor', 'ceil', 'fract', 'mod', 'min', 'max', 'clamp',
                   'step', 'smoothstep', 'sign']
    for func in float_funcs:
        # Match func(...).<swizzle>
        pattern = rf'({func}\s*\([^)]*\))\.([xyzw]+)'
        if re.search(pattern, fixed):
            fixed = re.sub(pattern, r'\1', fixed)
            changes.append(f"Removed scalar swizzle from {func}()")

    # Fix common .x on float values (heuristic)
    # float val = expr; ... val.x -> just val
    # This is very context-dependent, so be conservative

    if changes:
        if m:
            return content[:m.end()] + fixed, changes
        return fixed, changes
    return content, changes


def fix_syntax_errors(content, errors):
    """Attempt to fix various syntax errors."""
    m = re.match(r'\s*/\*\s*(\{.*?\})\s*\*/', content, re.DOTALL)
    body = content[m.end():] if m else content
    fixed = body
    changes = []

    # Fix 1: Missing semicolons (unexpected IDENTIFIER after statement)
    # Heuristic: if a line ends with ) and next line starts with an identifier
    lines = fixed.split('\n')
    fixed_lines = []
    for i, line in enumerate(lines):
        stripped = line.rstrip()
        if stripped and not stripped.endswith((';', '{', '}', ',', '(', '/', '*', '\\', '#', ':')):
            if stripped.endswith(')'):
                # Check if next line looks like a new statement
                if i + 1 < len(lines):
                    next_stripped = lines[i + 1].strip()
                    if next_stripped and not next_stripped.startswith((')', '}', '{', '/', '*', '#', '+', '-', '|', '&', '?', ':')):
                        if not next_stripped.startswith(('else', 'while')):
                            stripped += ';'
                            changes.append(f"Added missing semicolon at line {i+1}")
        fixed_lines.append(stripped)
    fixed = '\n'.join(fixed_lines)

    # Fix 2: Replace reserved word 'filter' with 'filterVal'
    if re.search(r'\bfilter\b', fixed):
        fixed = re.sub(r'\bfilter\b', 'filterVal', fixed)
        changes.append("Renamed reserved word 'filter' -> 'filterVal'")

    # Fix 3: Replace reserved word 'input' with 'inputVal'
    if re.search(r'\binput\b', fixed) and 'inputImage' not in fixed:
        fixed = re.sub(r'\binput\b', 'inputVal', fixed)
        changes.append("Renamed reserved word 'input' -> 'inputVal'")

    # Fix 4: Fix vec2/vec3/vec4 in wrong context (missing operator)
    # e.g., "float x vec2(1.0)" should be "float x = vec2(1.0)"
    fixed = re.sub(r'(float\s+\w+)\s+(vec[234])', r'\1 = \2', fixed)

    # Fix 5: Mismatched braces - try to balance
    open_braces = fixed.count('{')
    close_braces = fixed.count('}')
    if close_braces > open_braces:
        # Remove extra closing braces from the end
        diff = close_braces - open_braces
        for _ in range(diff):
            last_brace = fixed.rfind('}')
            if last_brace > 0:
                fixed = fixed[:last_brace] + fixed[last_brace+1:]
        changes.append(f"Removed {diff} extra closing brace(s)")
    elif open_braces > close_braces:
        diff = open_braces - close_braces
        fixed += '\n' + '}\n' * diff
        changes.append(f"Added {diff} missing closing brace(s)")

    # Fix 6: l-value required (trying to assign to const/expression)
    # Common: vec3(x) = something -> just remove the assignment
    # This is too context-dependent for auto-fix

    # Fix 7: Escape characters in strings (GLSL doesn't have strings)
    fixed = fixed.replace('\\n', ' ')
    fixed = fixed.replace('\\t', ' ')

    # Fix 8: Function overload return type mismatch
    # Can't easily fix this automatically

    if m:
        return content[:m.end()] + fixed, changes
    return fixed, changes


def fix_type_errors(content, errors):
    """Fix type conversion errors."""
    m = re.match(r'\s*/\*\s*(\{.*?\})\s*\*/', content, re.DOTALL)
    body = content[m.end():] if m else content
    fixed = body
    changes = []

    # Fix: vec2 = int -> vec2 = vec2(float(int))
    # Fix: float = vec2 -> float = vec2.x
    # These are very context-specific

    # Common pattern: vec2 var = 0; should be vec2 var = vec2(0.0);
    fixed = re.sub(r'(vec2\s+\w+\s*=\s*)(\d+)\s*;', r'\1vec2(\2.0);', fixed)
    fixed = re.sub(r'(vec3\s+\w+\s*=\s*)(\d+)\s*;', r'\1vec3(\2.0);', fixed)
    fixed = re.sub(r'(vec4\s+\w+\s*=\s*)(\d+)\s*;', r'\1vec4(\2.0);', fixed)

    if fixed != body:
        changes.append("Fixed int-to-vec type conversions")

    if m:
        return content[:m.end()] + fixed, changes
    return fixed, changes


def move_to_isf(src_path):
    """Move a fixed shader from BROKEN/ to ISF/."""
    rel = Path(src_path).relative_to(BROKEN_DIR)
    category = rel.parts[0]
    filename = rel.name
    dest_dir = ISF_DIR / category
    dest_dir.mkdir(parents=True, exist_ok=True)
    dest = dest_dir / filename
    if dest.exists():
        # Add suffix
        stem = Path(filename).stem
        dest = dest_dir / f"{stem}-fixed.fs"
    shutil.move(src_path, dest)
    return str(dest)


def main():
    print("=" * 60)
    print("MACROVERSE SHADER FIXER")
    print("=" * 60)

    broken_files = glob.glob(str(BROKEN_DIR / '**/*.fs'), recursive=True)
    print(f"\nBroken shaders to process: {len(broken_files)}")

    trashed = 0
    fixed = 0
    still_broken = 0
    fix_attempts = 0

    for f_path in broken_files:
        with open(f_path, 'r', errors='replace') as f:
            content = f.read()

        m = re.match(r'\s*/\*\s*(\{.*?\})\s*\*/', content, re.DOTALL)
        body = content[m.end():] if m else content

        # Step 1: Trash junk
        if is_junk(body):
            os.remove(f_path)
            trashed += 1
            stats['trashed'] += 1
            continue

        # Step 2: Try progressive fixes
        current = content
        all_changes = []
        max_attempts = 5

        for attempt in range(max_attempts):
            success, errors, _ = validate_glsl(current)
            if success:
                break

            fix_attempts += 1
            prev = current

            # Try each fix type based on errors
            if '#endif' in errors and 'mismatched' in errors:
                current = fix_endif_mismatch(current)
                all_changes.append("Fixed #endif mismatch")
            elif 'undeclared identifier' in errors:
                current, changes = fix_undeclared_variables(current, errors)
                all_changes.extend(changes)
            elif 'redefinition' in errors:
                current, changes = fix_redefinition(current, errors)
                all_changes.extend(changes)
            elif 'scalar swizzle' in errors or 'not supported' in errors:
                current, changes = fix_scalar_swizzle(current, errors)
                all_changes.extend(changes)
            elif 'cannot convert' in errors or 'l-value required' in errors:
                current, changes = fix_type_errors(current, errors)
                all_changes.extend(changes)
            elif 'syntax error' in errors or 'Reserved word' in errors:
                current, changes = fix_syntax_errors(current, errors)
                all_changes.extend(changes)
            else:
                # Try all fixes
                current = fix_endif_mismatch(current)
                current, c1 = fix_undeclared_variables(current, errors)
                current, c2 = fix_syntax_errors(current, errors)
                all_changes.extend(c1 + c2)

            if current == prev:
                break  # No changes made, stop

        # Check final result
        success, errors, _ = validate_glsl(current)
        if success:
            # Write fixed content and move to ISF/
            with open(f_path, 'w') as f:
                f.write(current)
            new_path = move_to_isf(f_path)
            fixed += 1
            stats['fixed'] += 1
        else:
            # Still broken - keep in BROKEN/ with whatever fixes we managed
            if current != content:
                with open(f_path, 'w') as f:
                    f.write(current)
                stats['partially_fixed'] += 1
            still_broken += 1
            stats['still_broken'] += 1

    # Clean up empty directories in BROKEN/
    for dirpath, dirnames, filenames in os.walk(str(BROKEN_DIR), topdown=False):
        if not filenames and not dirnames:
            try:
                os.rmdir(dirpath)
            except:
                pass

    print(f"\n=== RESULTS ===")
    print(f"Trashed (junk/empty):    {trashed}")
    print(f"Fixed and moved to ISF/: {fixed}")
    print(f"Partially improved:      {stats['partially_fixed']}")
    print(f"Still broken:            {still_broken}")
    print(f"Total fix attempts:      {fix_attempts}")

    # Count what's left in BROKEN/
    remaining = glob.glob(str(BROKEN_DIR / '**/*.fs'), recursive=True)
    print(f"\nRemaining in BROKEN/:    {remaining_count}")

    # Count what's in ISF/ now
    isf_count = len(glob.glob(str(ISF_DIR / '**/*.fs'), recursive=True))
    print(f"Total in ISF/:           {isf_count}")


# Fix the variable reference
remaining_count = 0

if __name__ == "__main__":
    broken_before = len(glob.glob(str(BROKEN_DIR / '**/*.fs'), recursive=True))
    main()
    remaining_count = len(glob.glob(str(BROKEN_DIR / '**/*.fs'), recursive=True))
    print(f"\nRemaining in BROKEN/:    {remaining_count}")
    isf_count = len(glob.glob(str(ISF_DIR / '**/*.fs'), recursive=True))
    print(f"Total in ISF/:           {isf_count}")
