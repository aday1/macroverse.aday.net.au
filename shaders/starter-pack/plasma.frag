precision mediump float;

uniform vec2 resolution;
uniform float time;

void main() {
    vec2 uv = gl_FragCoord.xy / resolution.xy;
    float x = uv.x * 10.0;
    float y = uv.y * 10.0;
    float t = time * 0.8;

    float v1 = sin(x + t);
    float v2 = sin(y + t * 0.5);
    float v3 = sin(x + y + t);
    float v4 = sin(sqrt(x * x + y * y) + t);

    float v = (v1 + v2 + v3 + v4) * 0.25;

    vec3 col;
    col.r = sin(v * 3.14159) * 0.5 + 0.5;
    col.g = sin(v * 3.14159 + 2.094) * 0.5 + 0.5;
    col.b = sin(v * 3.14159 + 4.188) * 0.5 + 0.5;

    gl_FragColor = vec4(col, 1.0);
}
