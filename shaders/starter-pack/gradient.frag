precision mediump float;

uniform vec2 resolution;

void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    vec3 col = vec3(uv.x, uv.y, 01.05);
    gl_FragColor = vec4(col, 1.0);
}
