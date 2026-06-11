precision mediump float;

uniform float rotSpeed; // @expose 0.1 2
uniform float sphereSize; // @expose 0.3 1.5
uniform float lightX; // @expose -3 3
uniform float lightY; // @expose -3 3
uniform float specular; // @expose 0.1 5
uniform float ambient; // @expose 0.05 0.5
uniform float colorR; // @expose 0 1
uniform float colorG; // @expose 0 1
uniform float colorB; // @expose 0 1
uniform vec2 resolution;
uniform float time;

float sdSphere(vec3 p, float r) { return length(p) - r; }

mat3 rotY(float a) { float c = cos(a), s = sin(a); return mat3(c,0,s, 0,1,0, -s,0,c); }
mat3 rotX(float a) { float c = cos(a), s = sin(a); return mat3(1,0,0, 0,c,-s, 0,s,c); }

float scene(vec3 p) {
    p = rotY(time * rotSpeed) * rotX(time * rotSpeed * 0.7) * p;
    return sdSphere(p, sphereSize);
}

vec3 calcNormal(vec3 p) {
    vec2 e = vec2(0.001, 0.0);
    return normalize(vec3(scene(p+e.xyy)-scene(p-e.xyy), scene(p+e.yxy)-scene(p-e.yxy), scene(p+e.yyx)-scene(p-e.yyx)));
}

void main() {
    vec2 uv = (gl_FragCoord.xy - 0.5 * resolution) / min(resolution.x, resolution.y);
    vec3 ro = vec3(0.0, 0.0, 3.0);
    vec3 rd = normalize(vec3(uv, -1.5));
    float t = 0.0;
    for (int i = 0; i < 64; i++) {
        float d = scene(ro + rd * t);
        if (d < 0.001 || t > 20.0) break;
        t += d;
    }
    vec3 col = vec3(0.02);
    if (t < 20.0) {
        vec3 p = ro + rd * t;
        vec3 n = calcNormal(p);
        vec3 lDir = normalize(vec3(lightX, lightY, 2.0));
        float diff = max(dot(n, lDir), 0.0);
        vec3 h = normalize(lDir - rd);
        float spec = pow(max(dot(n, h), 0.0), 32.0) * specular;
        col = vec3(colorR, colorG, colorB) * (ambient + diff * 0.8) + vec3(1.0) * spec;
    }
    gl_FragColor = vec4(col, 1.0);
}
