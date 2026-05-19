import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(scriptDir, "..");
const privateShaders = path.resolve(repoRoot, "..", "Macroversed-FortyTwoEdition", "shaders");
const starterPack = path.join(repoRoot, "shaders", "starter-pack");

const CURATION = [
  ["plasma/orbital-plasma-colors.fs", "VJ-Sorted-Production/ISF/plasma/plasmaorbcolors.fs"],
  ["plasma/soft-glow-plasma.fs", "VJ-Sorted-Production/ISF/plasma/PrettyGlow1.fs"],
  ["plasma/classic-plasma-five.fs", "isf/plasma5.fs"],
  ["tunnel/retro-pipes.fs", "isf/PipesRetro.fs"],
  ["tunnel/lightspeed-zoom.fs", "VJ-Sorted-Production/ISF/tunnel/Lightspeed-Zoom1.fs"],
  ["tunnel/wire-tunnel.fs", "VJ-Sorted-Production/ISF/tunnel/Tunnel.fs"],
  ["fractal/cyber-fractal.frag", "glsl/Cyber-Fractal.frag"],
  ["fractal/star-nest.fs", "VJ-Sorted-Production/ISF/fractal/StarNest.fs"],
  ["fractal/road-to-fractal.fs", "VJ-Sorted-Production/ISF/fractal/RoadToFractal.fs"],
  ["geometric/disc-orbit.fs", "isf/discs.fs"],
  ["geometric/mystify-lines.fs", "isf/MystifyLines.fs"],
  ["geometric/neon-torus.frag", "glsl/Neon-Torus.frag"],
  ["geometric/crystal-sphere.frag", "glsl/Crystal-Sphere.frag"],
  ["abstract/brush-strokes.fs", "isf/abstract-brushstrokes.fs"],
  ["abstract/sine-waves.fs", "isf/Sinewaves1.fs"],
  ["abstract/gold-waves.fs", "VJ-Sorted-Production/ISF/abstract/goldwaves.fs"],
  ["noise/cloud-fbm.fs", "VJ-Sorted-Production/ISF/noise/clouds.fs"],
  ["noise/iq-noise-lab.fs", "VJ-Sorted-Production/ISF/noise/NoisefunctionsfromIQ.fs"],
  ["noise/checker-mist.fs", "core/checkerboards/core-checker-mist.fs"],
  ["space/nebula-drift.fs", "isf/Nebula.fs"],
  ["space/starfield-warp.fs", "isf/StarfieldWarp.fs"],
  ["space/planetary-glow.fs", "VJ-Sorted-Production/ISF/space/Planetary.fs"],
  ["water/retro-ripples.fs", "isf/RetroRipples.fs"],
  ["water/seascape.fs", "VJ-Sorted-Production/ISF/water/Seascape.fs"],
  ["water/aquatic-field.fs", "VJ-Sorted-Production/ISF/water/AquaticField.fs"],
  ["particles/bubble-field.fs", "isf/Bubbles.fs"],
  ["particles/star-trip.fs", "VJ-Sorted-Production/ISF/particles/StarTrip.fs"],
  ["particles/starfield-fire.fs", "VJ-Sorted-Production/ISF/particles/StarField-Fire.fs"],
  ["psychedelic/demoscene-beast.frag", "glsl/demoscene-unicorn.frag"],
  ["psychedelic/neon-flower.fs", "VJ-Sorted-Production/ISF/psychedelic/PsychedelicFlower1.fs"],
  ["psychedelic/color-vortex.fs", "VJ-Sorted-Production/ISF/psychedelic/PsychedelicVortextXY.fs"],
  ["grid/matrix-rain.fs", "isf/MatrixRain.fs"],
  ["grid/voronoi-cells.fs", "VJ-Sorted-Production/ISF/grid/VoronoiII-XY.fs"],
  ["grid/neon-xy-grid.fs", "core/grids/core-grid-neon-xy.fs"],
  ["color/gradient-pulse.fs", "isf/Gradient1.fs"],
  ["color/color-bleed.fs", "isf/Colorbleed1.fs"],
  ["color/smpte-bars.fs", "isf/SMPTE-ColorBars.fs"],
  ["misc/feedback-blur.fs", "isf/feedback-blur.fs"],
  ["misc/chroma-warp.fs", "isf/TextureFX-ChromaWarp.fs"],
  ["misc/spinner-glow.fs", "isf/Spinner.fs"]
];

const patchIsfTitle = (content, title) => {
  if (!content.trimStart().startsWith("/*{")) return content;
  return content.replace(/"DESCRIPTION"\s*:\s*"[^"]*"/, `"DESCRIPTION": "${title}"`);
};

const lines = [
  "Macroverse starter-pack source map",
  `Generated: ${new Date().toISOString()}`,
  "Public name -> private library path (under Macroversed-FortyTwoEdition/shaders/)",
  ""
];

let copied = 0;
const missing = [];

for (const [dest, srcRel] of CURATION) {
  const src = path.join(privateShaders, srcRel);
  const destPath = path.join(starterPack, dest);
  if (!fs.existsSync(src)) {
    missing.push(srcRel);
    continue;
  }
  fs.mkdirSync(path.dirname(destPath), { recursive: true });
  let body = fs.readFileSync(src, "utf8");
  const displayName = path.basename(dest, path.extname(dest)).replace(/-/g, " ");
  body = patchIsfTitle(body, displayName);
  fs.writeFileSync(destPath, body, "utf8");
  lines.push(`${dest}\t<- ${srcRel}`);
  copied += 1;
}

const mapPath = path.join(starterPack, "SOURCE-MAP.txt");
fs.writeFileSync(mapPath, `${lines.join("\n")}\n`, "utf8");

const countFiles = (dir) => {
  let n = 0;
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, ent.name);
    if (ent.isDirectory()) n += countFiles(p);
    else if (/\.(fs|frag|glsl)$/i.test(ent.name)) n += 1;
  }
  return n;
};

const total = countFiles(starterPack);
console.log(`Copied ${copied} shaders into starter-pack (${total} shader files total).`);
if (missing.length) {
  console.error("Missing sources:");
  missing.forEach((m) => console.error(`  ${m}`));
  process.exit(1);
}
