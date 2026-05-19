precision mediump float;

uniform vec2 resolution;
uniform float time;

float rand(vec2 p) {
    return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    float n = rand(uv * 100.0 + time);
    vec3 col = vec3(n, n * 0.9, n * 1.1);
    gl_FragColor = vec4(col, 1.0);
}
