precision mediump float;

uniform float tunnelSpeed; // @expose 0.1 3
uniform float tunnelTwist; // @expose 0 8
uniform float tunnelRings; // @expose 2 20
uniform float tunnelRadius; // @expose 0.5 3
uniform float busScale; // @expose 0.3 2
uniform float busDistance; // @expose 1 8
uniform float busBounce; // @expose 0 1
uniform float busColorHue; // @expose 0 1
uniform float neonGlow; // @expose 0.5 5
uniform float fogDensity; // @expose 0.01 0.2
uniform vec2 resolution;
uniform float time;

mat3 rotY(float a){float c=cos(a),s=sin(a);return mat3(c,0,s,0,1,0,-s,0,c);}
mat3 rotX(float a){float c=cos(a),s=sin(a);return mat3(1,0,0,0,c,-s,0,s,c);}
vec3 hue2rgb(float h){return clamp(abs(mod(h*6.0+vec3(0,4,2),6.0)-3.0)-1.0,0.0,1.0);}

float sdBox(vec3 p, vec3 b){vec3 q=abs(p)-b;return length(max(q,0.0))+min(max(q.x,max(q.y,q.z)),0.0);}
float sdRoundBox(vec3 p, vec3 b, float r){return sdBox(p,b)-r;}
float sdSphere(vec3 p, float r){return length(p)-r;}
float sdCylinder(vec3 p, float r, float h){vec2 d=abs(vec2(length(p.xz),p.y))-vec2(r,h);return min(max(d.x,d.y),0.0)+length(max(d,0.0));}

float bus(vec3 p) {
    p *= 1.0 / busScale;
    float body = sdRoundBox(p, vec3(0.6, 0.35, 1.2), 0.08);
    float roof = sdRoundBox(p - vec3(0.0, 0.42, -0.1), vec3(0.5, 0.12, 0.9), 0.06);
    body = min(body, roof);
    float windshield = sdBox(p - vec3(0.0, 0.2, 1.22), vec3(0.42, 0.22, 0.02));
    body = max(body, -windshield);
    float w1 = sdCylinder(p - vec3(0.45, -0.4, 0.7), 0.15, 0.08);
    float w2 = sdCylinder(p - vec3(-0.45, -0.4, 0.7), 0.15, 0.08);
    float w3 = sdCylinder(p - vec3(0.45, -0.4, -0.7), 0.15, 0.08);
    float w4 = sdCylinder(p - vec3(-0.45, -0.4, -0.7), 0.15, 0.08);
    body = min(body, min(min(w1,w2), min(w3,w4)));
    float headlight1 = sdSphere(p - vec3(0.3, 0.0, 1.28), 0.08);
    float headlight2 = sdSphere(p - vec3(-0.3, 0.0, 1.28), 0.08);
    body = min(body, min(headlight1, headlight2));
    float bumper = sdRoundBox(p - vec3(0.0, -0.2, 1.25), vec3(0.55, 0.06, 0.04), 0.02);
    body = min(body, bumper);
    return body * busScale;
}

float tunnel(vec3 p) {
    float angle = atan(p.y, p.x);
    float wave = sin(angle * tunnelRings + p.z * tunnelTwist * 0.5 + time * tunnelSpeed) * 0.15;
    return -(length(p.xy) - tunnelRadius - wave);
}

float scene(vec3 p) {
    float bounce = sin(time * 3.0) * busBounce * 0.15;
    float sway = sin(time * 1.7) * 0.1;
    vec3 bp = p - vec3(sway, -0.3 + bounce, busDistance + sin(time * 0.5) * 2.0);
    bp = rotY(sin(time * 0.8) * 0.15) * bp;
    float b = bus(bp);
    float t = tunnel(p);
    return min(b, t);
}

float sceneBus(vec3 p) {
    float bounce = sin(time * 3.0) * busBounce * 0.15;
    float sway = sin(time * 1.7) * 0.1;
    vec3 bp = p - vec3(sway, -0.3 + bounce, busDistance + sin(time * 0.5) * 2.0);
    bp = rotY(sin(time * 0.8) * 0.15) * bp;
    return bus(bp);
}

vec3 calcNormal(vec3 p){vec2 e=vec2(0.002,0);return normalize(vec3(scene(p+e.xyy)-scene(p-e.xyy),scene(p+e.yxy)-scene(p-e.yxy),scene(p+e.yyx)-scene(p-e.yyx)));}

void main() {
    vec2 uv = (gl_FragCoord.xy - 0.5 * resolution) / min(resolution.x, resolution.y);
    vec2 screenUV = gl_FragCoord.xy / resolution;

    vec3 ro = vec3(0.0, 0.0, time * tunnelSpeed * 2.0);
    vec3 rd = normalize(vec3(uv, 1.5));

    float camAngle = sin(time * 0.3) * 0.15;
    rd = rotX(camAngle) * rd;

    float tRay = 0.0;
    bool hit = false;
    for (int i = 0; i < 96; i++) {
        vec3 p = ro + rd * tRay;
        float d = scene(p);
        if (abs(d) < 0.002) { hit = true; break; }
        if (tRay > 30.0) break;
        tRay += d * 0.7;
    }

    vec3 col = vec3(0.0);

    if (hit) {
        vec3 p = ro + rd * tRay;
        vec3 n = calcNormal(p);
        float isBus = step(sceneBus(p), 0.01);
        vec3 lDir = normalize(vec3(0.5, 1.0, -1.0));
        float diff = max(dot(n, lDir), 0.0);
        float spec = pow(max(dot(reflect(-lDir, n), -rd), 0.0), 32.0);

        if (isBus > 0.5) {
            vec3 busCol = hue2rgb(busColorHue) * 0.8 + vec3(0.2);
            float stripe = smoothstep(0.0, 0.05, abs(fract(p.y * 5.0) - 0.5) - 0.3);
            busCol = mix(busCol, vec3(1.0, 0.9, 0.3), (1.0 - stripe) * 0.3);
            vec3 headlightPos1 = ro + vec3(0.3 + sin(time * 1.7) * 0.1, -0.3 + sin(time * 3.0) * busBounce * 0.15, busDistance + sin(time * 0.5) * 2.0 + 1.28 * busScale);
            vec3 headlightPos2 = headlightPos1 - vec3(0.6, 0.0, 0.0);
            float hl = neonGlow * 0.3 / (1.0 + length(p - headlightPos1) * 3.0);
            hl += neonGlow * 0.3 / (1.0 + length(p - headlightPos2) * 3.0);
            col = busCol * (0.2 + diff * 0.6) + vec3(1.0) * spec * 0.4 + vec3(1.0, 0.95, 0.8) * hl;
        } else {
            float angle = atan(n.y, n.x);
            float zCoord = p.z;
            float rings = sin(angle * tunnelRings + zCoord * 0.8 - time * tunnelSpeed * 3.0);
            float grid = abs(sin(zCoord * 2.0)) * abs(sin(angle * 8.0));
            vec3 tunnelCol = hue2rgb(zCoord * 0.03 + time * 0.1) * 0.3;
            tunnelCol += hue2rgb(angle * 0.3 + time * 0.2) * rings * 0.2;
            float neon = smoothstep(0.95, 1.0, grid) * neonGlow;
            tunnelCol += hue2rgb(zCoord * 0.05 + 0.5) * neon * 0.5;
            col = tunnelCol * (0.3 + diff * 0.5);
        }

        float fog = 1.0 - exp(-tRay * fogDensity);
        vec3 fogCol = hue2rgb(time * 0.05) * 0.1;
        col = mix(col, fogCol, fog);
    } else {
        col = hue2rgb(time * 0.05) * 0.02;
    }

    col *= 1.0 - 0.25 * length(uv);
    gl_FragColor = vec4(col, 1.0);
}
