/*{
    "DESCRIPTION": "retro pipes",
    "CREDIT": "Macroverse After Dark Collection",
    "ISFVSN": "2.0",
    "CATEGORIES": ["Retro"],
    "TAGS": ["after-dark", "pipes", "windows", "retro", "screensaver", "3d"],
    "INPUTS": [
        { "NAME": "useFrameIndex", "TYPE": "bool", "DEFAULT": 0, "LABEL": "Use frame index" },
        { "NAME": "fps", "TYPE": "float", "DEFAULT": 60.0, "MIN": 24.0, "MAX": 120.0 },
        { "NAME": "growSpeed", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.1, "MAX": 1.5, "LABEL": "Grow speed" },
        { "NAME": "pipeRadius", "TYPE": "float", "DEFAULT": 0.06, "MIN": 0.02, "MAX": 0.15, "LABEL": "Pipe radius" },
        { "NAME": "pipeCount", "TYPE": "float", "DEFAULT": 5.0, "MIN": 2.0, "MAX": 10.0, "LABEL": "Pipe count" },
        { "NAME": "jointStyle", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 2.0, "LABEL": "Joint (0=ball,1=cube,2=none)" },
        { "NAME": "shininess", "TYPE": "float", "DEFAULT": 0.7, "MIN": 0.0, "MAX": 1.0, "LABEL": "Shininess" },
        { "NAME": "colorful", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0, "LABEL": "Color variety" }
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

// 3D rotation around Y axis
vec3 rotY(vec3 p, float a) {
    float ca = cos(a), sa = sin(a);
    return vec3(ca * p.x + sa * p.z, p.y, -sa * p.x + ca * p.z);
}

// 3D rotation around X axis
vec3 rotX(vec3 p, float a) {
    float ca = cos(a), sa = sin(a);
    return vec3(p.x, ca * p.y - sa * p.z, sa * p.y + ca * p.z);
}

// Capsule SDF (pipe segment between two points)
float sdCapsule(vec3 p, vec3 a, vec3 b, float r) {
    vec3 pa = p - a, ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h) - r;
}

// Sphere SDF (joint)
float sdSphere(vec3 p, vec3 c, float r) {
    return length(p - c) - r;
}

// Box SDF (joint)
float sdBox(vec3 p, vec3 c, float s) {
    vec3 d = abs(p - c) - s;
    return length(max(d, 0.0)) + min(max(d.x, max(d.y, d.z)), 0.0);
}

// Direction from segment index (axis-aligned, grid-like movement)
vec3 pipeDir(float segIdx, float pipeId) {
    float h = hash(segIdx * 17.3 + pipeId * 31.7);
    int dir = int(mod(h * 6.0, 6.0));
    if (dir == 0) return vec3(1.0, 0.0, 0.0);
    if (dir == 1) return vec3(-1.0, 0.0, 0.0);
    if (dir == 2) return vec3(0.0, 1.0, 0.0);
    if (dir == 3) return vec3(0.0, -1.0, 0.0);
    if (dir == 4) return vec3(0.0, 0.0, 1.0);
    return vec3(0.0, 0.0, -1.0);
}

struct HitInfo {
    float dist;
    vec3 color;
    vec3 normal;
};

// Scene: multiple pipes
HitInfo sceneDist(vec3 p) {
    float t = time * growSpeed;
    float bestDist = 100.0;
    vec3 bestCol = vec3(0.5);
    vec3 bestNorm = vec3(0.0, 1.0, 0.0);

    float pipes = floor(pipeCount);

    for (float pi = 0.0; pi < 10.0; pi++) {
        if (pi >= pipes) break;

        vec3 pipeCol = hsv2rgb(vec3(
            hash(pi * 7.1) * colorful,
            0.5 + hash(pi * 11.3) * 0.4 * colorful,
            0.6 + hash(pi * 13.7) * 0.4
        ));

        // Starting position for this pipe
        vec3 start = vec3(
            (hash(pi * 23.1) - 0.5) * 2.0,
            (hash(pi * 29.3) - 0.5) * 2.0,
            (hash(pi * 37.7) - 0.5) * 2.0
        );

        float segLen = 0.3 + hash(pi * 43.1) * 0.3;
        float maxSegs = 6.0 + floor(t * 3.0 + hash(pi * 53.7) * 5.0);
        maxSegs = min(maxSegs, 20.0);

        vec3 prev = start;

        for (float s = 0.0; s < 20.0; s++) {
            if (s >= maxSegs) break;

            vec3 dir = pipeDir(s, pi);
            // Avoid reversing direction
            if (s > 0.0) {
                vec3 prevDir = pipeDir(s - 1.0, pi);
                if (dot(dir, prevDir) < -0.5) dir = -dir;
            }

            vec3 next = prev + dir * segLen;

            // Pipe segment
            float segD = sdCapsule(p, prev, next, pipeRadius);
            if (segD < bestDist) {
                bestDist = segD;
                bestCol = pipeCol;
                // Approximate normal
                vec3 pa = p - prev;
                vec3 ba = next - prev;
                float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
                bestNorm = normalize(pa - ba * h);
            }

            // Joint at connection point
            float joint = floor(jointStyle);
            if (joint < 0.5) {
                // Ball joint
                float jd = sdSphere(p, next, pipeRadius * 1.3);
                if (jd < bestDist) {
                    bestDist = jd;
                    bestCol = pipeCol * 1.1;
                    bestNorm = normalize(p - next);
                }
            } else if (joint < 1.5) {
                // Cube joint
                float jd = sdBox(p, next, pipeRadius * 1.1);
                if (jd < bestDist) {
                    bestDist = jd;
                    bestCol = pipeCol * 1.1;
                    vec3 d = p - next;
                    vec3 ad = abs(d);
                    bestNorm = (ad.x > ad.y && ad.x > ad.z) ? vec3(sign(d.x), 0, 0) :
                               (ad.y > ad.z) ? vec3(0, sign(d.y), 0) : vec3(0, 0, sign(d.z));
                }
            }

            prev = next;
        }
    }

    return HitInfo(bestDist, bestCol, bestNorm);
}

void main() {
    vec2 uv = gl_FragCoord.xy / resolution;
    float aspect = resolution.x / resolution.y;

    // Camera
    float camT = time * 0.2;
    vec3 ro = vec3(sin(camT) * 3.0, 1.5 + sin(camT * 0.7) * 0.5, cos(camT) * 3.0);
    vec3 target = vec3(0.0, 0.0, 0.0);
    vec3 fwd = normalize(target - ro);
    vec3 right = normalize(cross(fwd, vec3(0.0, 1.0, 0.0)));
    vec3 up = cross(right, fwd);

    vec2 screen = (uv - 0.5) * vec2(aspect, 1.0);
    vec3 rd = normalize(fwd + screen.x * right + screen.y * up);

    // Background: dark gray gradient
    vec3 col = vec3(0.05 + 0.03 * uv.y);

    // Ray march
    float t = 0.0;
    bool hit = false;
    HitInfo info;

    for (int i = 0; i < 80; i++) {
        vec3 p = ro + rd * t;
        info = sceneDist(p);

        if (info.dist < 0.002) {
            hit = true;
            break;
        }
        if (t > 20.0) break;

        t += info.dist * 0.8;
    }

    if (hit) {
        vec3 p = ro + rd * t;

        // Lighting
        vec3 lightDir = normalize(vec3(0.5, 1.0, 0.3));
        vec3 n = info.normal;

        float diff = max(dot(n, lightDir), 0.0) * 0.7 + 0.3;
        vec3 h = normalize(lightDir - rd);
        float spec = pow(max(dot(n, h), 0.0), 32.0) * shininess;

        col = info.color * diff + vec3(1.0) * spec;

        // Fog
        float fog = exp(-t * 0.15);
        col = mix(vec3(0.05), col, fog);
    }

    gl_FragColor = vec4(clamp(col, 0.0, 1.0), 1.0);
}
