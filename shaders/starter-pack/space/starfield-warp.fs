/*{
    "DESCRIPTION": "starfield warp",
    "CREDIT": "Macroverse After Dark Collection",
    "ISFVSN": "2.0",
    "CATEGORIES": ["Retro"],
    "TAGS": ["after-dark", "starfield", "warp", "retro", "screensaver", "space"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0, "LABEL": "Use frame index" },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "warpSpeed", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.05, "MAX": 2.0, "LABEL": "Warp speed" },
        { "NAME": "starDensity", "TYPE": "float", "DEFAULT": 200.0, "MIN": 50.0, "MAX": 600.0, "LABEL": "Star density" },
        { "NAME": "layerCount", "TYPE": "float", "DEFAULT": 4.0, "MIN": 1.0, "MAX": 6.0, "LABEL": "Star layers" },
        { "NAME": "trailLength", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0, "LABEL": "Trail length" },
        { "NAME": "starBrightness", "TYPE": "float", "DEFAULT": 0.9, "MIN": 0.2, "MAX": 1.5, "LABEL": "Star brightness" },
        { "NAME": "colorTint", "TYPE": "float", "DEFAULT": 0.1, "MIN": 0.0, "MAX": 1.0, "LABEL": "Color tint" }
    ]
}*/

#define time (useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME)
#define resolution RENDERSIZE

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

// Single star layer
vec3 starLayer(vec2 uv, float layerDepth, float t) {
    vec3 col = vec3(0.0);
    float speed = warpSpeed * (0.3 + layerDepth * 0.7);

    // Center the UV so stars emanate from center
    vec2 centered = uv - 0.5;
    float aspect = resolution.x / resolution.y;
    centered.x *= aspect;

    // Zoom effect based on time
    float zoom = mod(t * speed, 1.0);
    float scale = mix(starDensity * 0.5, starDensity * 2.0, zoom);

    vec2 grid = floor(centered * scale);
    vec2 frac = fract(centered * scale) - 0.5;

    for (float dx = -1.0; dx <= 1.0; dx++) {
        for (float dy = -1.0; dy <= 1.0; dy++) {
            vec2 offset = vec2(dx, dy);
            vec2 cell = grid + offset;

            float h = hash(cell + layerDepth * 100.0);
            if (h > 0.7) continue; // skip some cells for randomness

            // Star position within cell
            vec2 starPos = vec2(hash(cell * 1.3 + 7.0), hash(cell * 2.7 + 13.0)) - 0.5;
            vec2 diff = frac - offset - starPos * 0.6;

            // Distance from center of screen affects star size (perspective)
            float distFromCenter = length((cell + starPos) / scale);

            // Perspective: stars near center are small dots, far from center are streaks
            float streakFactor = distFromCenter * trailLength * 3.0;
            vec2 streakDir = normalize(centered + 0.001);

            // Elongate the star in the radial direction
            float radialDist = abs(dot(diff, streakDir));
            float perpDist = length(diff - streakDir * dot(diff, streakDir));

            float starSize = 0.03 + streakFactor * 0.08;
            float d = perpDist / (0.015 + 0.005 * layerDepth);
            float streak = smoothstep(starSize, 0.0, radialDist);

            float brightness = exp(-d * d * 8.0) * streak;

            // Depth-based sizing: closer layers have brighter stars
            brightness *= (0.4 + layerDepth * 0.6) * starBrightness;

            // Twinkle
            float twinkle = 0.7 + 0.3 * sin(t * 3.0 + h * 30.0);
            brightness *= twinkle;

            // Color: mostly white with subtle tint
            vec3 starCol = vec3(1.0);
            float hue = h * 6.28;
            starCol = mix(starCol, vec3(
                0.7 + 0.3 * sin(hue),
                0.7 + 0.3 * sin(hue + 2.094),
                0.7 + 0.3 * sin(hue + 4.189)
            ), colorTint);

            col += starCol * brightness;
        }
    }

    return col;
}

void main() {
    vec2 uv = gl_FragCoord.xy / resolution;
    float t = time;

    // Deep space background
    vec3 col = vec3(0.005, 0.005, 0.015);

    // Stack multiple star layers at different depths
    float layers = floor(layerCount);
    for (float i = 0.0; i < 6.0; i++) {
        if (i >= layers) break;
        float depth = (i + 1.0) / layers;
        col += starLayer(uv, depth, t + i * 1.7);
    }

    // Subtle radial gradient (space feel)
    vec2 c = uv - 0.5;
    float aspect = resolution.x / resolution.y;
    c.x *= aspect;
    float vignette = 1.0 - length(c) * 0.4;
    col *= vignette;

    // Clamp
    col = clamp(col, 0.0, 1.0);

    gl_FragColor = vec4(col, 1.0);
}
