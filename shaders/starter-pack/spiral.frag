precision mediump float;

uniform float val_n6_28; // @expose 0 9.42
uniform vec2 resolution;
uniform float time;

void main() {
    vec2 uv = (gl_FragCoord.xy - 0.5 * resolution) / min(resolution.x, resolution.y);
    float r = length(uv);
    float angle = atan(uv.y, uv.x);
    float spiral = mod(angle / val_n6_28 + r * 3.0 - time * 0.5, 1.0);
    float v = smoothstep(0.0, 0.1, spiral) * smoothstep(0.3, 0.2, spiral);
    vec3 col = vec3(v, v * 0.7, 1.0 - v);
    gl_FragColor = vec4(col, 1.0);
}
