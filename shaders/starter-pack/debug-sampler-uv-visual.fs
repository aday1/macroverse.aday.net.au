// Macroverse built-in: Debug sampler2D with UV overlay
// Shows texture + colored UV gradient overlay (red=u, green=v)
// Helps verify texture coordinates and flipping

uniform sampler2D tex;
uniform float overlay; // @expose 0 1  (blend: 0=texture only, 1=UV gradient)

void main() {
  vec2 uv = gl_FragCoord.xy / RENDERSIZE.xy;
  vec4 texColor = texture2D(tex, uv);
  vec4 uvOverlay = vec4(uv.x, uv.y, 0.5, 0.5);
  gl_FragColor = mix(texColor, uvOverlay, overlay);
}
