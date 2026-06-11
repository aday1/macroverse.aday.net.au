/*{
    "DESCRIPTION": "mystify lines",
    "CREDIT": "Macroverse After Dark Collection",
    "ISFVSN": "2.0",
    "CATEGORIES": ["Retro"],
    "TAGS": ["after-dark", "mystify", "windows", "retro", "screensaver", "lines"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0, "LABEL": "Use frame index" },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "speed", "TYPE": "float", "DEFAULT": 0.4, "MIN": 0.05, "MAX": 1.5, "LABEL": "Vertex speed" },
        { "NAME": "lineWidth", "TYPE": "float", "DEFAULT": 2.0, "MIN": 0.5, "MAX": 6.0, "LABEL": "Line thickness" },
        { "NAME": "trailCount", "TYPE": "float", "DEFAULT": 8.0, "MIN": 1.0, "MAX": 16.0, "LABEL": "Trail length" },
        { "NAME": "polyCount", "TYPE": "float", "DEFAULT": 2.0, "MIN": 1.0, "MAX": 4.0, "LABEL": "Polygon count" },
        { "NAME": "vertexCount", "TYPE": "float", "DEFAULT": 4.0, "MIN": 3.0, "MAX": 6.0, "LABEL": "Vertices per poly" },
        { "NAME": "hueSpeed", "TYPE": "float", "DEFAULT": 0.2, "MIN": 0.0, "MAX": 1.0, "LABEL": "Color cycle speed" }
    ]
}*/

#define time (useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME)
#define resolution RENDERSIZE

float hash(float n) { return fract(sin(n) * 43758.5453); }

// HSV to RGB
vec3 hsv2rgb(vec3 c) {
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

// Distance from point to line segment
float distToSegment(vec2 p, vec2 a, vec2 b) {
    vec2 pa = p - a;
    vec2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h);
}

// Bouncing vertex position using ping-pong
vec2 bouncingVertex(float id, float polyId, float t) {
    float seedX = hash(id * 17.3 + polyId * 31.7);
    float seedY = hash(id * 23.1 + polyId * 47.3);
    float spdX = 0.3 + hash(id * 7.1 + polyId * 11.3) * 0.7;
    float spdY = 0.3 + hash(id * 13.7 + polyId * 19.1) * 0.7;

    float px = abs(mod(t * speed * spdX + seedX * 10.0, 2.0) - 1.0);
    float py = abs(mod(t * speed * spdY + seedY * 10.0, 2.0) - 1.0);

    return vec2(px, py);
}

void main() {
    vec2 uv = gl_FragCoord.xy / resolution;
    float aspect = resolution.x / resolution.y;
    float t = time;

    // Black background
    vec3 col = vec3(0.01);

    float pixelSize = 1.0 / resolution.y;
    float lw = lineWidth * pixelSize;
    float polys = floor(polyCount);
    float verts = floor(vertexCount);
    float trails = floor(trailCount);

    // Draw each polygon with trails
    for (float p = 0.0; p < 4.0; p++) {
        if (p >= polys) break;

        float polyHue = fract(p * 0.35 + t * hueSpeed);

        for (float tr = 0.0; tr < 16.0; tr++) {
            if (tr >= trails) break;

            float trailT = t - tr * 0.04;
            float trailAlpha = 1.0 - tr / trails;
            trailAlpha *= trailAlpha; // quadratic falloff

            float trailHue = fract(polyHue + tr * 0.02);
            vec3 lineCol = hsv2rgb(vec3(trailHue, 0.9, 0.95 * trailAlpha));

            // Get all vertex positions for this trail frame
            // We'll check segments between consecutive vertices
            for (float v = 0.0; v < 6.0; v++) {
                if (v >= verts) break;

                float nextV = mod(v + 1.0, verts);

                vec2 a = bouncingVertex(v, p, trailT);
                vec2 b = bouncingVertex(nextV, p, trailT);

                // Scale to screen with aspect
                vec2 puv = vec2(uv.x * aspect, uv.y);
                vec2 pa = vec2(a.x * aspect, a.y);
                vec2 pb = vec2(b.x * aspect, b.y);

                float d = distToSegment(puv, pa, pb);
                float line = smoothstep(lw, lw * 0.3, d);

                col += lineCol * line * trailAlpha;
            }
        }
    }

    // Clamp to prevent blown-out areas
    col = min(col, vec3(1.2));

    gl_FragColor = vec4(col, 1.0);
}
