precision mediump float;

uniform float val_n0_5; // @expose -0.5 1.5
uniform vec2 resolution;
uniform float time;

void main() {
    vec2 uv = gl_FragCoord.xy / resolution - val_n0_5;
    uv.x *= resolution.x / resolution.y;
    float d = length(uv);
    float pulse = 0.5 + 0.5 * sin(time * 2.0);
    float ring = smoothstep(pulse + 0.05, pulse, d) * smoothstep(pulse - 0.05, pulse, d);
    vec3 col = vec3(ring * 0.8, ring * 0.4, 1.0);
    gl_FragColor = vec4(col, 1.0);
}
