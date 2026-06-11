precision mediump float;

uniform vec2 resolution;
uniform float time;

void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    float hue = uv.x + time * 0.2;
    hue = fract(hue);
    vec3 col = 0.5 + 0.5 * cos(6.28 * (hue + vec3(0, 0.33, 0.66)));
    gl_FragColor = vec4(col, 1.0);
}
