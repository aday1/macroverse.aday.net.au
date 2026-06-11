#!/usr/bin/env python3
"""
Macroverse Shader Renamer
Analyzes GLSL code content to generate meaningful names for UNSORTED shaders.
Uses code pattern analysis, keyword detection, and Macroverse-themed fallbacks.
"""

import os
import re
import json
import glob
import random
import hashlib
from pathlib import Path
from collections import Counter

SORTED_DIR = Path("/workspace/SORTED_FOR_PRODUCTION")

# Code pattern signatures -> name components
TECHNIQUE_SIGNATURES = {
    # Raymarching / SDF
    (r'sdSphere|sdBox|sdTorus|sdCylinder|sdCapsule', 'RaymarchPrimitive'),
    (r'sdRoundBox|sdHexPrism|sdOctahedron', 'RaymarchGeo'),
    (r'rayMarch|ray_march|marchRay|march\s*\(', 'Raymarch'),
    (r'sceneSDF|mapScene|map\s*\(\s*vec3', 'SDFScene'),
    (r'calcNormal|getNormal|estimateNormal', 'LitSurface'),

    # Fractals
    (r'mandelbrot|z\.x\s*\*\s*z\.x\s*-\s*z\.y\s*\*\s*z\.y', 'Mandelbrot'),
    (r'julia|juliaSet', 'Julia'),
    (r'sierpinski|sierpin', 'Sierpinski'),
    (r'menger|mengerSponge', 'MengerSponge'),
    (r'fractal.*iter|for.*z\s*=.*z\s*\*', 'FractalZoom'),

    # Noise patterns
    (r'fbm\s*\(|FBM\s*\(', 'FBMNoise'),
    (r'simplex|snoise\s*\(', 'SimplexFlow'),
    (r'perlin|pnoise', 'PerlinField'),
    (r'worley|cellular|voronoi', 'VoronoiCell'),
    (r'hash\s*\(.*\)\s*\*.*hash|random.*noise', 'HashNoise'),

    # Visual effects
    (r'kaleidoscope|mod\s*\(\s*a.*6\.28|segments.*atan', 'Kaleidoscope'),
    (r'tunnel|1\.0\s*/\s*\(\s*r\s*\+', 'Tunnel'),
    (r'plasma|sin.*cos.*sin.*time', 'PlasmaWave'),
    (r'blur|gaussian|bloom|glow.*pow', 'GlowBloom'),
    (r'ripple|wave.*sin.*length', 'RippleWave'),
    (r'vortex|swirl|twist.*atan', 'VortexSwirl'),
    (r'particle|emitter|spark', 'ParticleField'),
    (r'fire|flame|burn|ember', 'FlameEffect'),
    (r'water|ocean|sea|caustic', 'AquaticField'),
    (r'star|constellation|galaxy', 'StarField'),
    (r'lightning|electric|bolt|arc', 'ElectricArc'),
    (r'smoke|fog|cloud|mist', 'AtmosphericHaze'),
    (r'crystal|gem|diamond|facet', 'CrystalForm'),
    (r'circuit|tech|digital|matrix|cyber', 'CircuitMatrix'),
    (r'organic|cell|bio|life|grow', 'OrganicForm'),
    (r'pulse|heartbeat|throb|beat', 'PulseRhythm'),
    (r'mirror|reflect|chrome|metallic', 'MirrorSurface'),
    (r'gradient|ramp|fade|blend', 'GradientShift'),
    (r'grid|checker|tile|lattice', 'GridPattern'),
    (r'spiral|helix|coil|fibonacci', 'SpiralMotion'),
    (r'explod|burst|shatter|fragment', 'ShatterBurst'),
    (r'morph|transform|evolve|mutate', 'MorphField'),
    (r'disco|strobe|flash|blink', 'StrobeLight'),
    (r'ink|paint|watercolor|brush', 'InkWash'),
    (r'neon|luminous|phosphor', 'NeonGlow'),
    (r'aurora|northern.*light', 'AuroraBorealis'),
    (r'sunset|sunrise|horizon|sky', 'HorizonLight'),
    (r'lava|volcanic|magma', 'MoltenFlow'),
    (r'ice|frost|freeze|cryo', 'FrostCrystal'),
    (r'warp|distort|bend|deform', 'WarpDistort'),
    (r'dots?\s*\(|point|stipple', 'DotMatrix'),
    (r'ring|circle.*sin|concentric', 'ConcentricRings'),
    (r'cube|box.*rot', 'RotatingCube'),
    (r'sphere.*rot|orb.*spin', 'OrbitalSphere'),
    (r'torus.*rot|donut', 'TorusKnot'),
    (r'text|char|font|glyph|ASCII', 'TextGlyph'),
    (r'space.*warp|hyperspace|jump', 'HyperspaceJump'),
    (r'terrain|landscape|mountain|height', 'TerrainScape'),
    (r'city|building|skyscraper|urban', 'CityScape'),
    (r'flower|petal|bloom|blossom', 'FloralBloom'),
    (r'eye|iris|pupil', 'CosmicEye'),
    (r'heart|love|valentine', 'HeartPulse'),
    (r'clock|time.*display|watch', 'ChronoDisplay'),
    (r'balls?.*bounce|sphere.*phys', 'BouncingSpheres'),
}

# Color palette detection
COLOR_SIGNATURES = {
    (r'vec[34]\s*\(\s*1\.0\s*,\s*0\.\d+\s*,\s*0\.0', 'Fire'),
    (r'vec[34]\s*\(\s*0\.0\s*,\s*0\.\d+\s*,\s*1\.0', 'Ocean'),
    (r'vec[34]\s*\(\s*0\.\d+\s*,\s*1\.0\s*,\s*0\.\d+', 'Emerald'),
    (r'vec[34]\s*\(\s*1\.0\s*,\s*0\.0\s*,\s*1\.0', 'Magenta'),
    (r'vec[34]\s*\(\s*1\.0\s*,\s*1\.0\s*,\s*0\.0', 'Solar'),
    (r'vec[34]\s*\(\s*0\.0\s*,\s*1\.0\s*,\s*1\.0', 'Cyan'),
    (r'rainbow|hue\s*\*\s*6|hsv|hsl|hue2rgb', 'Rainbow'),
    (r'0\.5\s*\+\s*0\.5\s*\*\s*cos\s*\(.*vec3', 'Spectrum'),
}

# Motion/animation detection
MOTION_SIGNATURES = {
    (r'sin\s*\(.*time.*\)\s*\*\s*cos', 'Oscillating'),
    (r'rotate|rotY|rotX|rotZ|mat[23]\s*\(.*cos', 'Rotating'),
    (r'scroll|pan|slide|offset.*time', 'Scrolling'),
    (r'zoom|scale.*time|magnif', 'Zooming'),
    (r'pulse|throb|sin\s*\(.*time\s*\*\s*[5-9]', 'Pulsing'),
    (r'morph|lerp.*time|mix.*sin.*time', 'Morphing'),
    (r'orbit|revolv|circl.*time', 'Orbiting'),
}

# Macroverse-themed name components for fallback
MACROVERSE_PREFIXES = [
    "Astral", "Cosmic", "Nebula", "Void", "Quantum",
    "Stellar", "Nova", "Pulse", "Drift", "Flux",
    "Prism", "Echo", "Phase", "Aether", "Vertex",
    "Cipher", "Nexus", "Surge", "Rift", "Apex",
    "Veil", "Core", "Glyph", "Arc", "Ember",
    "Photon", "Ion", "Plasma", "Aurora", "Zenith",
    "Helix", "Fractal", "Synth", "Grid", "Wave",
    "Warp", "Bloom", "Shard", "Ring", "Orbit",
]

MACROVERSE_SUFFIXES = [
    "Field", "Weave", "Matrix", "Flow", "Storm",
    "Dance", "Spiral", "Bloom", "Pulse", "Wave",
    "Dream", "Zone", "Realm", "Gate", "Bridge",
    "Engine", "Drive", "Core", "Forge", "Lattice",
    "Pattern", "Cascade", "Prism", "Mirror", "Lens",
    "Burst", "Flash", "Glow", "Haze", "Mist",
    "Thread", "Web", "Net", "Mesh", "Grid",
    "Signal", "Echo", "Trace", "Path", "Orbit",
]


def analyze_shader_code(body):
    """Analyze GLSL code and return name components."""
    techniques = []
    colors = []
    motions = []

    lower_body = body.lower()
    code_sample = body[:5000]  # First 5000 chars for speed

    # Detect techniques
    for pattern, name in TECHNIQUE_SIGNATURES:
        if re.search(pattern, code_sample, re.IGNORECASE):
            techniques.append(name)

    # Detect color palettes
    for pattern, name in COLOR_SIGNATURES:
        if re.search(pattern, code_sample, re.IGNORECASE):
            colors.append(name)

    # Detect motion types
    for pattern, name in MOTION_SIGNATURES:
        if re.search(pattern, code_sample, re.IGNORECASE):
            motions.append(name)

    # Complexity analysis
    line_count = body.count('\n')
    func_count = len(re.findall(r'\b\w+\s+\w+\s*\([^)]*\)\s*\{', body))
    has_loops = bool(re.search(r'for\s*\(', body))

    return {
        'techniques': techniques[:3],
        'colors': colors[:2],
        'motions': motions[:2],
        'line_count': line_count,
        'func_count': func_count,
        'has_loops': has_loops,
    }


def generate_name(analysis, file_hash):
    """Generate a descriptive name from code analysis."""
    parts = []

    # Primary technique
    if analysis['techniques']:
        parts.append(analysis['techniques'][0])
    elif analysis['motions']:
        parts.append(analysis['motions'][0])

    # Color modifier
    if analysis['colors']:
        parts.append(analysis['colors'][0])

    # Secondary technique or motion
    if len(analysis['techniques']) > 1:
        parts.append(analysis['techniques'][1])
    elif analysis['motions'] and analysis['techniques']:
        parts.append(analysis['motions'][0])

    if parts:
        return '-'.join(parts)

    # Fallback: Macroverse-themed name based on hash for consistency
    hash_int = int(file_hash[:8], 16)
    prefix = MACROVERSE_PREFIXES[hash_int % len(MACROVERSE_PREFIXES)]
    suffix = MACROVERSE_SUFFIXES[(hash_int >> 8) % len(MACROVERSE_SUFFIXES)]
    variant = (hash_int >> 16) % 100
    return f"{prefix}{suffix}{variant:02d}"


def main():
    print("=" * 60)
    print("MACROVERSE SHADER RENAMER")
    print("=" * 60)

    # Find all shaders with generic names
    all_fs = glob.glob(str(SORTED_DIR / '**/*.fs'), recursive=True)
    generic_pattern = re.compile(
        r'^(UNSORTEDSHADER\d+|UNSORTED\d+|ShaderCollections\d+|'
        r'EXPORTED_\w+|EXPORT_\w+)'
    )

    to_rename = []
    for f_path in all_fs:
        name = Path(f_path).stem
        if generic_pattern.match(name):
            to_rename.append(f_path)

    print(f"\nShaders needing names: {len(to_rename)}")

    # Process each shader
    renames = []
    used_names = set()
    name_counter = Counter()

    for i, f_path in enumerate(to_rename):
        if (i + 1) % 200 == 0:
            print(f"  Analyzing {i+1}/{len(to_rename)}...")

        try:
            with open(f_path, 'r', errors='replace') as f:
                content = f.read()
        except:
            continue

        # Parse ISF header
        m = re.match(r'\s*/\*\s*(\{.*?\})\s*\*/', content, re.DOTALL)
        body = content[m.end():] if m else content
        header = None
        if m:
            try:
                fixed_json = re.sub(r',(\s*[\]}])', r'\1', m.group(1))
                header = json.loads(fixed_json)
            except:
                pass

        # Analyze code
        analysis = analyze_shader_code(body)

        # Generate hash for consistent fallback naming
        file_hash = hashlib.md5(body.encode(errors='replace')).hexdigest()

        # Generate name
        new_name = generate_name(analysis, file_hash)

        # Ensure uniqueness
        base_name = new_name
        if base_name in used_names:
            counter = 2
            while f"{base_name}-{counter}" in used_names:
                counter += 1
            new_name = f"{base_name}-{counter}"
        used_names.add(new_name)

        old_name = Path(f_path).stem
        category = Path(f_path).parent.name

        renames.append({
            'old_path': f_path,
            'old_name': old_name,
            'new_name': new_name,
            'category': category,
            'techniques': analysis['techniques'],
            'colors': analysis['colors'],
            'motions': analysis['motions'],
        })

    # Summary
    code_named = sum(1 for r in renames if not any(
        p in r['new_name'] for p in MACROVERSE_PREFIXES
    ))
    fallback_named = len(renames) - code_named

    print(f"\nNaming results:")
    print(f"  Named by code analysis: {code_named}")
    print(f"  Macroverse-themed fallback: {fallback_named}")

    # Apply renames
    print(f"\nApplying renames...")
    applied = 0
    for r in renames:
        old_path = Path(r['old_path'])
        new_path = old_path.parent / f"{r['new_name']}.fs"

        # Skip if target already exists
        if new_path.exists() and str(new_path) != str(old_path):
            new_path = old_path.parent / f"{r['new_name']}-v2.fs"

        try:
            # Update ISF header description
            with open(old_path, 'r', errors='replace') as f:
                content = f.read()

            m = re.match(r'\s*/\*\s*(\{.*?\})\s*\*/', content, re.DOTALL)
            if m:
                try:
                    fixed_json = re.sub(r',(\s*[\]}])', r'\1', m.group(1))
                    header = json.loads(fixed_json)
                    header['DESCRIPTION'] = r['new_name']
                    body = content[m.end():]
                    new_content = f"/*{json.dumps(header, indent=4)}*/\n{body}"
                    with open(old_path, 'w') as f:
                        f.write(new_content)
                except:
                    pass

            # Rename file
            if str(new_path) != str(old_path):
                os.rename(old_path, new_path)
                applied += 1
        except Exception as e:
            pass

    print(f"  Renamed: {applied}")

    # Update the index
    print(f"\nUpdating training-shader-index.json...")
    index_path = Path("/workspace/training-shader-index.json")
    if index_path.exists():
        with open(index_path) as f:
            index = json.load(f)

        # Build rename map
        rename_map = {}
        for r in renames:
            old_base = Path(r['old_path']).stem
            rename_map[old_base] = r['new_name']

        updated = 0
        for entry in index:
            old_name = entry.get('name', '')
            if old_name in rename_map:
                entry['name'] = rename_map[old_name]
                # Update path too
                old_p = Path(entry.get('path', ''))
                new_p = old_p.parent / f"{rename_map[old_name]}.fs"
                entry['path'] = str(new_p)
                updated += 1

        with open(index_path, 'w') as f:
            json.dump(index, f, indent=2)
        print(f"  Index entries updated: {updated}")

    # Show some examples
    print(f"\n=== SAMPLE RENAMES ===")
    for r in renames[:30]:
        tech = ', '.join(r['techniques'][:2]) if r['techniques'] else 'fallback'
        print(f"  {r['old_name']:35s} -> {r['new_name']:35s}  [{tech}]")

    print(f"\n=== NAMING METHOD DISTRIBUTION ===")
    method_counts = Counter()
    for r in renames:
        if r['techniques']:
            method_counts['Code analysis (technique detected)'] += 1
        elif r['motions']:
            method_counts['Code analysis (motion detected)'] += 1
        elif r['colors']:
            method_counts['Code analysis (color detected)'] += 1
        else:
            method_counts['Macroverse-themed fallback'] += 1

    for method, count in method_counts.most_common():
        print(f"  {count:4d}  {method}")


if __name__ == "__main__":
    main()
