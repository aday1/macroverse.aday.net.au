/*{
    "DESCRIPTION": "matrix rain",
    "CREDIT": "Macroverse After Dark Collection",
    "ISFVSN": "2.0",
    "CATEGORIES": ["Retro"],
    "TAGS": ["after-dark", "matrix", "digital-rain", "retro", "screensaver", "code"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0, "LABEL": "Use frame index" },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "fallSpeed", "TYPE": "float", "DEFAULT": 0.8, "MIN": 0.1, "MAX": 2.0, "LABEL": "Fall speed" },
        { "NAME": "columnDensity", "TYPE": "float", "DEFAULT": 30.0, "MIN": 10.0, "MAX": 60.0, "LABEL": "Column density" },
        { "NAME": "charSize", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.5, "MAX": 2.0, "LABEL": "Character size" },
        { "NAME": "glowStrength", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 1.0, "LABEL": "Glow strength" },
        { "NAME": "colorHue", "TYPE": "float", "DEFAULT": 0.33, "MIN": 0.0, "MAX": 1.0, "LABEL": "Color hue (0.33=green)" },
        { "NAME": "fadeLength", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.1, "MAX": 1.0, "LABEL": "Trail fade length" }
    ]
}*/

#define time (useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME)
#define resolution RENDERSIZE

float hash(float n) { return fract(sin(n) * 43758.5453); }
float hash2(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }

// HSV to RGB
vec3 hsv2rgb(vec3 c) {
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

// Pseudo-character pattern: creates block-like glyphs from a grid
float charPattern(vec2 uv, float id) {
    // 5x7 character grid
    vec2 grid = floor(uv * vec2(5.0, 7.0));
    if (grid.x < 0.0 || grid.x > 4.0 || grid.y < 0.0 || grid.y > 6.0) return 0.0;

    // Generate a pseudo-random glyph based on the id
    float seed = hash(id * 127.1 + grid.x * 17.3 + grid.y * 31.7);

    // Create patterns that look like katakana/matrix characters
    float pattern = step(0.45, seed);

    // Add some structure: more likely to have pixels in center
    float centerBias = 1.0 - length(grid / vec2(4.0, 6.0) - 0.5) * 0.8;
    pattern *= step(0.3, centerBias + seed * 0.5);

    return pattern;
}

void main() {
    vec2 uv = gl_FragCoord.xy / resolution;
    float t = time;

    // Dark background
    vec3 col = vec3(0.0, 0.02, 0.0);

    float cols = floor(columnDensity);
    float cellW = 1.0 / cols;
    float cellH = cellW * (resolution.x / resolution.y) * 1.5 / charSize;

    // Which column and row are we in?
    float colIdx = floor(uv.x / cellW);
    float localX = fract(uv.x / cellW);

    // Per-column properties
    float colSpeed = (0.5 + hash(colIdx * 7.3) * 1.0) * fallSpeed;
    float colOffset = hash(colIdx * 13.1) * 20.0;
    float colLength = 8.0 + hash(colIdx * 23.7) * 16.0;
    float colBrightness = 0.5 + hash(colIdx * 31.1) * 0.5;

    // Scrolling: which row at this position?
    float scroll = t * colSpeed + colOffset;
    float rowIdx = floor(uv.y / cellH + scroll);
    float localY = fract(uv.y / cellH + scroll);

    // Head position of the rain stream
    float headRow = floor(scroll + hash(colIdx * 41.3) * colLength);
    float distFromHead = headRow - rowIdx;

    // Only render if within stream length
    if (distFromHead >= 0.0 && distFromHead < colLength) {
        // Fade: bright at head, fading towards tail
        float fade = 1.0 - distFromHead / (colLength * fadeLength);
        fade = clamp(fade, 0.0, 1.0);
        fade = pow(fade, 1.5);

        // Character changes occasionally
        float charChangeRate = 8.0;
        float charId = hash(colIdx * 100.0 + floor(rowIdx) * 7.0 + floor(t * charChangeRate) * 0.1);

        // Draw the character
        vec2 charUV = vec2(localX, 1.0 - localY);
        // Add some padding
        charUV = (charUV - 0.1) / 0.8;

        float ch = charPattern(charUV, charId * 1000.0);

        // Color
        vec3 baseColor = hsv2rgb(vec3(colorHue, 0.7, 1.0));

        // Head character is bright white-green
        float headGlow = exp(-distFromHead * 0.5);
        vec3 charColor = mix(baseColor * fade, vec3(0.8, 1.0, 0.8), headGlow * 0.7);

        col += charColor * ch * fade * colBrightness;

        // Glow effect around the character
        if (glowStrength > 0.0) {
            float glowDist = length(vec2(localX - 0.5, localY - 0.5));
            float glow = exp(-glowDist * glowDist * 8.0) * fade * glowStrength * 0.15;
            col += baseColor * glow * colBrightness;
        }
    }

    // Multiple streams per column (staggered)
    float stream2Offset = colLength * 1.5 + hash(colIdx * 53.7) * 10.0;
    float scroll2 = t * colSpeed + colOffset + stream2Offset;
    float headRow2 = floor(scroll2 + hash(colIdx * 67.3) * colLength * 0.5);
    float rowIdx2 = floor(uv.y / cellH + scroll2);
    float distFromHead2 = headRow2 - rowIdx2;
    float colLength2 = colLength * (0.5 + hash(colIdx * 71.1) * 0.5);

    if (distFromHead2 >= 0.0 && distFromHead2 < colLength2) {
        float fade2 = 1.0 - distFromHead2 / (colLength2 * fadeLength);
        fade2 = clamp(fade2, 0.0, 1.0);
        fade2 = pow(fade2, 1.5) * 0.6;

        float charId2 = hash(colIdx * 200.0 + floor(rowIdx2) * 11.0 + floor(t * 6.0) * 0.1);
        float localY2 = fract(uv.y / cellH + scroll2);
        vec2 charUV2 = vec2(localX, 1.0 - localY2);
        charUV2 = (charUV2 - 0.1) / 0.8;
        float ch2 = charPattern(charUV2, charId2 * 1000.0);

        vec3 baseColor = hsv2rgb(vec3(colorHue, 0.7, 1.0));
        col += baseColor * ch2 * fade2 * colBrightness * 0.7;
    }

    // Subtle scanline
    col *= 0.95 + 0.05 * sin(gl_FragCoord.y * 1.5);

    gl_FragColor = vec4(clamp(col, 0.0, 1.0), 1.0);
}
