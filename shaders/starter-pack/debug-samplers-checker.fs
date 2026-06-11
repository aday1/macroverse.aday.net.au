// Macroverse built-in: Debug two sampler2D with checker pattern
// Alternating cells: tex and tex2. Verifies both texture slots work.
// Use large cells to see clearly which sampler is which

uniform sampler2D tex;
uniform sampler2D tex2;

void main() {
  vec2 uv = gl_FragCoord.xy / RENDERSIZE.xy;
  ivec2 cell = ivec2(floor(uv.x * 8.0), floor(uv.y * 8.0));
  vec2 cellUv = vec2(fract(uv.x * 8.0), fract(uv.y * 8.0));
  vec4 c;
  if ((cell.x + cell.y) % 2 == 0) {
    c = texture2D(tex, cellUv);
  } else {
    c = texture2D(tex2, cellUv);
  }
  gl_FragColor = c;
}
