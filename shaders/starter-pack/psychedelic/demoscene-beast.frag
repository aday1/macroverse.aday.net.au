precision mediump float;

uniform float val_n0_7; // @expose -0.30000000000000004 1.7
uniform float rotSpeed; // @expose 0.1 2
uniform float hornLength; // @expose 0.3 1.5
uniform float hornGlow; // @expose 0.5 5
uniform float bodyScale; // @expose 0.5 2
uniform float scrollSpeed; // @expose 0.5 5
uniform float copperBars; // @expose 2 12
uniform float starDensity; // @expose 10 100
uniform float colorHue; // @expose 0 1
uniform float bgPulse; // @expose 0 2
uniform vec2 resolution;
uniform float time;

mat3 rotY(float a){float c=cos(a),s=sin(a);return mat3(c,0,s,0,1,0,-s,0,c);}
mat3 rotX(float a){float c=cos(a),s=sin(a);return mat3(1,0,0,0,c,-s,0,s,c);}
mat3 rotZ(float a){float c=cos(a),s=sin(a);return mat3(c,-s,0,s,c,0,0,0,1);}

float sdSphere(vec3 p, float r){return length(p)-r;}
float sdCapsule(vec3 p, vec3 a, vec3 b, float r){vec3 pa=p-a,ba=b-a;float h=clamp(dot(pa,ba)/dot(ba,ba),0.0,1.0);return length(pa-ba*h)-r;}
float sdCone(vec3 p, float h, float r){vec2 q=vec2(length(p.xz),p.y);vec2 tip=vec2(0,h);vec2 e=vec2(-r,2.0*h);float d=dot(q-tip,e)/dot(e,e);d=clamp(d,0.0,1.0);vec2 nearest=tip+e*d;return length(q-nearest);}
float opU(float a, float b){return min(a,b);}
float opS(float a, float b){return max(a,-b);}
float smin(float a, float b, float k){float h=clamp(0.5+0.5*(b-a)/k,0.0,1.0);return mix(b,a,h)-k*h*(1.0-h);}

float unicorn(vec3 p) {
    p *= 1.0 / bodyScale;
    float head = sdSphere(p - vec3(0.0, 0.3, 0.8), 0.35);
    float snout = sdSphere(p - vec3(0.0, 0.15, 1.15), 0.2);
    head = smin(head, snout, 0.15);
    vec3 hp = p - vec3(0.0, 0.75, 0.7);
    hp = rotX(-0.3) * rotZ(sin(time * 0.5) * 0.05) * hp;
    float horn = sdCone(hp, hornLength, 0.06);
    float body = sdCapsule(p, vec3(0.0, 0.0, -0.5), vec3(0.0, 0.1, 0.6), 0.35);
    float neck = sdCapsule(p, vec3(0.0, 0.1, 0.5), vec3(0.0, 0.3, 0.8), 0.2);
    float fl1 = sdCapsule(p, vec3(0.2, -0.1, 0.3), vec3(0.2, -0.7, 0.35), 0.08);
    float fl2 = sdCapsule(p, vec3(-0.2, -0.1, 0.3), vec3(-0.2, -0.7, 0.35), 0.08);
    float bl1 = sdCapsule(p, vec3(0.2, -0.1, -0.3), vec3(0.2, -0.7, -0.25), 0.08);
    float bl2 = sdCapsule(p, vec3(-0.2, -0.1, -0.3), vec3(-0.2, -0.7, -0.25), 0.08);
    float tail = sdCapsule(p, vec3(0.0, 0.05, -val_n0_7), vec3(0.0, 0.3 + sin(time) * 0.15, -1.1), 0.06);
    float ear1 = sdCapsule(p, vec3(0.12, 0.5, 0.75), vec3(0.15, 0.7, 0.7), 0.04);
    float ear2 = sdCapsule(p, vec3(-0.12, 0.5, 0.75), vec3(-0.15, 0.7, 0.7), 0.04);
    float d = smin(body, neck, 0.2);
    d = smin(d, head, 0.15);
    d = opU(d, horn);
    d = opU(d, fl1); d = opU(d, fl2);
    d = opU(d, bl1); d = opU(d, bl2);
    d = smin(d, tail, 0.1);
    d = opU(d, ear1); d = opU(d, ear2);
    return d * bodyScale;
}

vec3 calcNormal(vec3 p){vec2 e=vec2(0.002,0);return normalize(vec3(unicorn(p+e.xyy)-unicorn(p-e.xyy),unicorn(p+e.yxy)-unicorn(p-e.yxy),unicorn(p+e.yyx)-unicorn(p-e.yyx)));}

vec3 hue2rgb(float h){return clamp(abs(mod(h*6.0+vec3(0,4,2),6.0)-3.0)-1.0,0.0,1.0);}

float hash(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}

float scrollerChar(vec2 uv, float t) {
    float x = fract(uv.x * 0.15 + t * scrollSpeed * 0.02);
    float y = uv.y;
    if (y < 0.0 || y > 1.0) return 0.0;
    float cx = floor(x * 16.0);
    float bit = floor(y * 8.0);
    float seed = hash(vec2(cx + floor(t * scrollSpeed * 0.3), bit));
    return step(0.45, seed) * step(0.02, fract(x * 16.0)) * step(fract(x * 16.0), 0.9);
}

void main() {
    vec2 uv = (gl_FragCoord.xy - 0.5 * resolution) / min(resolution.x, resolution.y);
    vec2 screenUV = gl_FragCoord.xy / resolution;

    float t = time;
    vec3 col = vec3(0.0);

    float plasma = sin(screenUV.x * 8.0 + t) * sin(screenUV.y * 6.0 - t * 0.7) * 0.5 + 0.5;
    plasma += sin(length(screenUV - 0.5) * 10.0 - t * 2.0) * 0.3;
    vec3 bgCol = hue2rgb(colorHue + plasma * 0.3 + t * 0.05) * (0.08 + bgPulse * 0.04 * plasma);

    float star = hash(floor(screenUV * starDensity));
    star = step(0.97, star) * (0.5 + 0.5 * sin(t * 3.0 + star * 100.0));
    bgCol += vec3(star * 0.8);

    float copper = sin(screenUV.y * copperBars * 3.14159 + t * 2.0) * 0.5 + 0.5;
    copper *= copper;
    bgCol += hue2rgb(colorHue + 0.1) * copper * 0.06;

    col = bgCol;

    vec3 ro = vec3(0.0, 0.2, 4.0);
    vec3 rd = normalize(vec3(uv, -1.8));
    rd = rotY(t * rotSpeed) * rotX(sin(t * 0.3) * 0.2) * rd;
    ro = rotY(t * rotSpeed) * rotX(sin(t * 0.3) * 0.2) * ro;

    float tRay = 0.0;
    for (int i = 0; i < 80; i++) {
        vec3 p = ro + rd * tRay;
        float d = unicorn(p);
        if (d < 0.002 || tRay > 15.0) break;
        tRay += d * 0.8;
    }

    if (tRay < 15.0) {
        vec3 p = ro + rd * tRay;
        vec3 n = calcNormal(p);
        vec3 lDir = normalize(vec3(1.0, 1.5, 2.0));
        float diff = max(dot(n, lDir), 0.0);
        float spec = pow(max(dot(reflect(-lDir, n), -rd), 0.0), 32.0);
        float rim = pow(1.0 - max(dot(n, -rd), 0.0), 3.0);

        vec3 baseCol = hue2rgb(colorHue + 0.5) * 0.6 + vec3(0.4);
        float pn = p.y / bodyScale;
        vec3 hp = p - vec3(0.0, 0.75 * bodyScale, 0.7 * bodyScale);
        if (length(hp) < hornLength * bodyScale * 1.2) {
            float stripe = sin(hp.y * 30.0 / bodyScale) * 0.5 + 0.5;
            baseCol = mix(hue2rgb(colorHue), hue2rgb(colorHue + 0.3), stripe);
            spec += hornGlow * 0.3 * stripe;
        }

        float maneZone = smoothstep(0.3, 0.6, pn) * smoothstep(-0.2, 0.4, p.z / bodyScale);
        if (maneZone > 0.1) {
            float rainbow = sin(pn * 10.0 + t * 2.0) * 0.5 + 0.5;
            baseCol = mix(baseCol, hue2rgb(rainbow + colorHue), maneZone * 0.7);
        }

        col = baseCol * (0.15 + diff * 0.7) + vec3(1.0) * spec * 0.5;
        col += hue2rgb(colorHue + 0.2) * rim * 0.3;
        col += hue2rgb(colorHue) * hornGlow * 0.05 / (1.0 + length(hp) * 3.0);
    }

    float scrollY = screenUV.y;
    float scrollBand = smoothstep(0.08, 0.12, scrollY) * smoothstep(0.22, 0.18, scrollY);
    if (scrollBand > 0.01) {
        float charY = (scrollY - 0.1) / 0.1;
        float sc = scrollerChar(vec2(screenUV.x, charY), t);
        vec3 textCol = hue2rgb(colorHue + screenUV.x * 0.5 + t * 0.2);
        col = mix(col, textCol * 1.5, sc * scrollBand * 0.9);
        col += textCol * scrollBand * 0.05;
    }

    col = pow(col, vec3(0.9));
    col *= 1.0 - 0.3 * length(uv);

    gl_FragColor = vec4(col, 1.0);
}
