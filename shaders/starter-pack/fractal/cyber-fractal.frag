precision mediump float;

uniform float zoom; // @expose 0.5 10
uniform float iterations; // @expose 5 50
uniform float colorCycle; // @expose 0 5
uniform float offsetX; // @expose -2 2
uniform float offsetY; // @expose -2 2
uniform vec2 resolution;
uniform float time;

void main() {
    vec2 uv = (gl_FragCoord.xy - 0.5 * resolution) / min(resolution.x, resolution.y);
    vec2 c = uv / zoom + vec2(offsetX, offsetY);
    vec2 z = vec2(0.0);
    float n = 0.0;
    for (int i = 0; i < 50; i++) {
        if (float(i) >= iterations) break;
        z = vec2(z.x * z.x - z.y * z.y, 2.0 * z.x * z.y) + c;
        if (dot(z, z) > 4.0) break;
        n++;
    }
    float t = n / iterations;
    vec3 col = 0.5 + 0.5 * cos(colorCycle + t * 6.28 + vec3(0.0, 1.0, 2.0) + time * 0.3);
    if (n >= iterations) col = vec3(0.0);
    gl_FragColor = vec4(col, 1.0);
}
