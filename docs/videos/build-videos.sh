#!/usr/bin/env bash
# build-videos.sh - regenerate all six showcase demo loops + posters.
#
# Deterministic. Run from anywhere; outputs land next to this script.
# All loops are silent, web-safe MP4 (H.264 + yuv420p + faststart),
# 960x540 @ 30fps, no audio, autoplay-friendly. Posters are first-
# frame WEBP at the same resolution.
#
# Requires: ffmpeg with lavfi (mandelbrot, life, cellauto, color,
# drawtext, geq) plus libx264 + libwebp. The ImageMagick/cwebp toolchain is
# NOT required - poster frames go through ffmpeg's libwebp encoder.
#
# Caveat: the Cloud VM has no working WebGL, so we cannot screen-record
# the real app. These loops are PROCEDURAL placeholders that evoke
# the live-shader / VJ / pipeline / fix-chain experience using ffmpeg's
# built-in lavfi sources. The rich showcase page declares this clearly.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Macroverse palette (hex without 0x):
#   void     06040f
#   surface  0d0820
#   blue     4488cc
#   violet   8833cc
#   teal     00ddaa
#   copper   ee8833
#   text     d0c8e8

if [ -f "/c/Windows/Fonts/consolab.ttf" ]; then
  FONT_MONO="C\\:/Windows/Fonts/consolab.ttf"
  FONT_HEAD="C\\:/Windows/Fonts/arialbd.ttf"
else
  FONT_MONO="/usr/share/fonts/truetype/jetbrains-mono/JetBrainsMono-Bold.ttf"
  FONT_HEAD="/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
  [ -f "$FONT_MONO" ] || FONT_MONO="/usr/share/fonts/truetype/dejavu/DejaVuSansMono-Bold.ttf"
  [ -f "$FONT_HEAD" ] || FONT_HEAD="$FONT_MONO"
fi

W=960
H=540
FPS=30

# Common encode tail: yuv420p + faststart for browsers, no audio.
# Per-video CRF lets us hold each output under ~1.5 MB. High-motion
# procedural sources (life, mandelbrot animation) compress poorly,
# so we bump CRF and cap -maxrate. Static-text loops (pipeline,
# expose, fix-chain) stay sharp at low CRF.
ENCODE_BASE=(
  -c:v libx264 -preset veryslow
  -pix_fmt yuv420p -movflags +faststart
  -tune animation
  -an
)

# poster() out_video out_webp
poster() {
  local in="$1" out="$2"
  ffmpeg -y -hide_banner -loglevel error \
    -i "$in" -vframes 1 -an \
    -c:v libwebp -quality 60 \
    "$out"
}

echo "[1/6] hero-loop.mp4 (12s mandelbrot, palette-mapped)"
# Render mandelbrot at half res then upscale - reduces detail (and
# bitrate) while staying soft and atmospheric in the browser.
ffmpeg -y -hide_banner -loglevel error \
  -f lavfi -i "mandelbrot=size=480x270:rate=${FPS}:end_pts=12:start_x=-0.743644:start_y=0.131826:start_scale=4:end_scale=0.4:bailout=20" \
  -t 12 \
  -vf "
    format=rgb24,
    geq=
      r='clip(0.05*255 + 0.95*r(X,Y), 0, 255)':
      g='clip(0.03*255 + 0.55*g(X,Y), 0, 255)':
      b='clip(0.10*255 + 0.95*b(X,Y), 0, 255)',
    curves=preset=darker:master='0/0 0.25/0.10 0.55/0.45 0.80/0.78 1/1',
    eq=brightness=-0.05:saturation=1.25:contrast=1.10,
    vignette=PI/4,
    scale=${W}:${H}:flags=lanczos
  " \
  "${ENCODE_BASE[@]}" -crf 36 -maxrate 900k -bufsize 1800k \
  hero-loop.mp4
poster hero-loop.mp4 hero-loop-poster.webp

echo "[2/6] vj-crossfade.mp4 (8s, two life patterns x-faded)"
# Render life at low res with mold trails (smoother frames -> tiny
# bitrate) then upscale with bicubic.
ffmpeg -y -hide_banner -loglevel error \
  -f lavfi -i "life=size=240x135:rate=${FPS}:rule=B3/S23:random_seed=42:ratio=0.45:death_color=0x06040f:life_color=0x00ddaa:mold_color=0x10605a:mold=12" \
  -f lavfi -i "life=size=240x135:rate=${FPS}:rule=B36/S23:random_seed=99:ratio=0.30:death_color=0x06040f:life_color=0xee8833:mold_color=0x603020:mold=12" \
  -filter_complex "
    [0:v]scale=${W}:${H}:flags=bicubic,format=rgba,setpts=PTS-STARTPTS,trim=duration=8[a];
    [1:v]scale=${W}:${H}:flags=bicubic,format=rgba,setpts=PTS-STARTPTS,trim=duration=8[b];
    [a][b]blend=all_expr='if(lte(T,3),A,if(gte(T,5),B,A*(1-(T-3)/2)+B*((T-3)/2)))'[xf];
    [xf]drawtext=fontfile='${FONT_MONO}':text='DECK A':x=24:y=24:fontsize=22:fontcolor=0x00ddaa:enable='lt(t,4)',
        drawtext=fontfile='${FONT_MONO}':text='DECK B':x=w-tw-24:y=24:fontsize=22:fontcolor=0xee8833:enable='gt(t,4)',
        drawtext=fontfile='${FONT_MONO}':text='CROSSFADE':x=(w-tw)/2:y=h-th-28:fontsize=20:fontcolor=0xd0c8e8:enable='between(t,3,5)',
        format=yuv420p
  " \
  -t 8 \
  "${ENCODE_BASE[@]}" -crf 34 -maxrate 800k -bufsize 1600k \
  vj-crossfade.mp4
poster vj-crossfade.mp4 vj-crossfade-poster.webp

echo "[3/6] pipeline-flow.mp4 (8s, animated text flow)"
ffmpeg -y -hide_banner -loglevel error \
  -f lavfi -i "color=c=0x06040f:size=${W}x${H}:rate=${FPS}:duration=8" \
  -f lavfi -i "life=size=${W}x${H}:rate=${FPS}:rule=B3/S23:random_seed=7:ratio=0.08:death_color=0x06040f:life_color=0x150a30:mold_color=0x0d0820:mold=24" \
  -filter_complex "
    [0:v][1:v]blend=all_mode=screen:all_opacity=0.6,format=rgba[bg];
    [bg]
      drawtext=fontfile='${FONT_HEAD}':text='SIGNAL FLOW':x=(w-tw)/2:y=64:fontsize=34:fontcolor=0xee8833:alpha='if(lt(t,0.6),t/0.6,1)',
      drawtext=fontfile='${FONT_MONO}':text='GLSL':x=80:y=240:fontsize=44:fontcolor=0x4488cc:borderw=2:bordercolor=0x06040f:alpha='if(lt(t,0.8),0,if(lt(t,1.4),(t-0.8)/0.6,1))',
      drawtext=fontfile='${FONT_MONO}':text='->':x=240:y=246:fontsize=44:fontcolor=0xd0c8e8:alpha='if(lt(t,1.6),0,if(lt(t,2.2),(t-1.6)/0.6,1))',
      drawtext=fontfile='${FONT_MONO}':text='ISF':x=320:y=240:fontsize=44:fontcolor=0x8833cc:borderw=2:bordercolor=0x06040f:alpha='if(lt(t,2.4),0,if(lt(t,3.0),(t-2.4)/0.6,1))',
      drawtext=fontfile='${FONT_MONO}':text='->':x=440:y=246:fontsize=44:fontcolor=0xd0c8e8:alpha='if(lt(t,3.2),0,if(lt(t,3.8),(t-3.2)/0.6,1))',
      drawtext=fontfile='${FONT_MONO}':text='WIRE':x=520:y=240:fontsize=44:fontcolor=0xee8833:borderw=2:bordercolor=0x06040f:alpha='if(lt(t,4.0),0,if(lt(t,4.6),(t-4.0)/0.6,1))',
      drawtext=fontfile='${FONT_MONO}':text='->':x=680:y=246:fontsize=44:fontcolor=0xd0c8e8:alpha='if(lt(t,4.8),0,if(lt(t,5.4),(t-4.8)/0.6,1))',
      drawtext=fontfile='${FONT_MONO}':text='RESOLUME':x=720:y=240:fontsize=32:fontcolor=0x00ddaa:borderw=2:bordercolor=0x06040f:alpha='if(lt(t,5.6),0,if(lt(t,6.2),(t-5.6)/0.6,1))',
      drawtext=fontfile='${FONT_MONO}':text='one binary  /  no installer  /  drop next to your shader folder':x=(w-tw)/2:y=h-92:fontsize=18:fontcolor=0x9977cc:alpha='if(lt(t,6.4),0,if(lt(t,7.2),(t-6.4)/0.8,1))',
      format=yuv420p
  " \
  -t 8 \
  "${ENCODE_BASE[@]}" -crf 30 \
  pipeline-flow.mp4
poster pipeline-flow.mp4 pipeline-flow-poster.webp

echo "[4/6] gallery-grid.mp4 (8s, 4x2 tile grid of distinct procedurals)"
# Each tile: 120x68 source -> 240x135 displayed - low-res sources keep
# bitrate down. Final composite is 960x540.
TW=120; TH=68
DW=240; DH=135
ffmpeg -y -hide_banner -loglevel error \
  -f lavfi -i "life=size=${TW}x${TH}:rate=${FPS}:rule=B3/S23:random_seed=11:ratio=0.45:life_color=0x4488cc:death_color=0x06040f" \
  -f lavfi -i "life=size=${TW}x${TH}:rate=${FPS}:rule=B36/S23:random_seed=22:ratio=0.30:life_color=0x00ddaa:death_color=0x06040f" \
  -f lavfi -i "life=size=${TW}x${TH}:rate=${FPS}:rule=B2/S:random_seed=33:ratio=0.10:life_color=0xee8833:death_color=0x06040f" \
  -f lavfi -i "life=size=${TW}x${TH}:rate=${FPS}:rule=B345/S5:random_seed=44:ratio=0.40:life_color=0x8833cc:death_color=0x06040f" \
  -f lavfi -i "cellauto=size=${TW}x${TH}:rate=${FPS}:rule=30:random_seed=55:ratio=0.35" \
  -f lavfi -i "cellauto=size=${TW}x${TH}:rate=${FPS}:rule=110:random_seed=66:ratio=0.40" \
  -f lavfi -i "cellauto=size=${TW}x${TH}:rate=${FPS}:rule=150:random_seed=77:ratio=0.50" \
  -f lavfi -i "cellauto=size=${TW}x${TH}:rate=${FPS}:rule=90:random_seed=88:ratio=0.45" \
  -filter_complex "
    [0:v]scale=${DW}:${DH}:flags=bicubic,format=rgba,trim=duration=8,setpts=PTS-STARTPTS[t0];
    [1:v]scale=${DW}:${DH}:flags=bicubic,format=rgba,trim=duration=8,setpts=PTS-STARTPTS[t1];
    [2:v]scale=${DW}:${DH}:flags=bicubic,format=rgba,trim=duration=8,setpts=PTS-STARTPTS[t2];
    [3:v]scale=${DW}:${DH}:flags=bicubic,format=rgba,trim=duration=8,setpts=PTS-STARTPTS[t3];
    [4:v]scale=${DW}:${DH}:flags=bicubic,format=rgba,trim=duration=8,setpts=PTS-STARTPTS[t4];
    [5:v]scale=${DW}:${DH}:flags=bicubic,format=rgba,trim=duration=8,setpts=PTS-STARTPTS[t5];
    [6:v]scale=${DW}:${DH}:flags=bicubic,format=rgba,trim=duration=8,setpts=PTS-STARTPTS[t6];
    [7:v]scale=${DW}:${DH}:flags=bicubic,format=rgba,trim=duration=8,setpts=PTS-STARTPTS[t7];
    [t0][t1][t2][t3]hstack=inputs=4[row0];
    [t4][t5][t6][t7]hstack=inputs=4[row1];
    [row0][row1]vstack=inputs=2,
      pad=${W}:${H}:(ow-iw)/2:(oh-ih)/2:0x06040f,
      drawbox=x=0:y=0:w=${W}:h=${H}:color=0x4488cc@0.18:t=2,
      drawtext=fontfile='${FONT_MONO}':text='GALLERY  /  8 LIVE SHADERS':x=(w-tw)/2:y=h-44:fontsize=18:fontcolor=0xd0c8e8:box=1:boxcolor=0x06040f@0.7:boxborderw=8,
      format=yuv420p
  " \
  -t 8 \
  "${ENCODE_BASE[@]}" -crf 34 -maxrate 800k -bufsize 1600k \
  gallery-grid.mp4
poster gallery-grid.mp4 gallery-grid-poster.webp

echo "[5/6] expose-params.mp4 (8s, GLSL literals -> ISF sliders)"
ffmpeg -y -hide_banner -loglevel error \
  -f lavfi -i "color=c=0x06040f:size=${W}x${H}:rate=${FPS}:duration=8" \
  -filter_complex "
    [0:v]format=rgba,
      drawbox=x=40:y=40:w=${W}-80:h=180:color=0x0d0820@0.95:t=max,
      drawbox=x=40:y=40:w=${W}-80:h=180:color=0x4488cc:t=2,
      drawtext=fontfile='${FONT_MONO}':text='// raw GLSL':x=64:y=60:fontsize=16:fontcolor=0x9977cc,
      drawtext=fontfile='${FONT_MONO}':text='float speed = 2.5;':x=64:y=92:fontsize=22:fontcolor=0x00ddaa,
      drawtext=fontfile='${FONT_MONO}':text='vec3 color = vec3(0.4, 0.8, 1.0);':x=64:y=128:fontsize=22:fontcolor=0x00ddaa,
      drawtext=fontfile='${FONT_MONO}':text='float scale = 1.0;':x=64:y=164:fontsize=22:fontcolor=0x00ddaa,
      drawtext=fontfile='${FONT_HEAD}':text='[ EXPOSE ]':x=(w-tw)/2:y=250:fontsize=28:fontcolor=0xee8833:box=1:boxcolor=0x06040f@0.6:boxborderw=12:enable='gte(t,2.0)',
      drawtext=fontfile='${FONT_MONO}':text='v':x=(w-tw)/2:y=292:fontsize=28:fontcolor=0xee8833:enable='gte(t,2.5)',
      drawbox=x=40:y=320:w=${W}-80:h=180:color=0x0d0820@0.95:t=max:enable='gte(t,3.0)',
      drawbox=x=40:y=320:w=${W}-80:h=180:color=0xee8833:t=2:enable='gte(t,3.0)',
      drawtext=fontfile='${FONT_MONO}':text='// ISF INPUTS - ready for Wire':x=64:y=340:fontsize=16:fontcolor=0x9977cc:enable='gte(t,3.2)',
      drawtext=fontfile='${FONT_MONO}':text='speed   [######------]   2.50':x=64:y=372:fontsize=22:fontcolor=0xd0c8e8:enable='gte(t,3.6)',
      drawtext=fontfile='${FONT_MONO}':text='colorR  [####--------]   0.40':x=64:y=408:fontsize=22:fontcolor=0xd0c8e8:enable='gte(t,4.4)',
      drawtext=fontfile='${FONT_MONO}':text='colorG  [########----]   0.80':x=64:y=444:fontsize=22:fontcolor=0xd0c8e8:enable='gte(t,5.0)',
      drawtext=fontfile='${FONT_MONO}':text='scale   [####--------]   1.00':x=64:y=480:fontsize=22:fontcolor=0xd0c8e8:enable='gte(t,5.6)',
      format=yuv420p
  " \
  -t 8 \
  "${ENCODE_BASE[@]}" -crf 30 \
  expose-params.mp4
poster expose-params.mp4 expose-params-poster.webp

echo "[6/6] fix-chain.mp4 (8s, regex -> Ollama -> AI)"
ffmpeg -y -hide_banner -loglevel error \
  -f lavfi -i "color=c=0x06040f:size=${W}x${H}:rate=${FPS}:duration=8" \
  -filter_complex "
    [0:v]format=rgba,
      drawtext=fontfile='${FONT_HEAD}':text='SHADER FIX CHAIN':x=(w-tw)/2:y=80:fontsize=30:fontcolor=0xd0c8e8,
      drawtext=fontfile='${FONT_MONO}':text='broken shader -> stop at first success':x=(w-tw)/2:y=132:fontsize=16:fontcolor=0x9977cc,

      drawbox=x=80:y=220:w=240:h=140:color=0x0d0820:t=max,
      drawbox=x=80:y=220:w=240:h=140:color=0x4488cc:t=2,
      drawtext=fontfile='${FONT_MONO}':text='LOCAL':x=80+(240-tw)/2:y=248:fontsize=22:fontcolor=0x4488cc,
      drawtext=fontfile='${FONT_MONO}':text='REGEX':x=80+(240-tw)/2:y=278:fontsize=22:fontcolor=0x4488cc,
      drawtext=fontfile='${FONT_MONO}':text='free / instant':x=80+(240-tw)/2:y=316:fontsize=14:fontcolor=0x9977cc,
      drawbox=x=80:y=220:w=240:h=140:color=0x00ddaa@0.30:t=max:enable='gte(t,1.0)',
      drawtext=fontfile='${FONT_HEAD}':text='OK':x=80+240-58:y=222:fontsize=22:fontcolor=0x00ddaa:enable='gte(t,1.2)',

      drawbox=x=360:y=220:w=240:h=140:color=0x0d0820:t=max,
      drawbox=x=360:y=220:w=240:h=140:color=0x8833cc:t=2,
      drawtext=fontfile='${FONT_MONO}':text='OLLAMA':x=360+(240-tw)/2:y=262:fontsize=22:fontcolor=0x8833cc,
      drawtext=fontfile='${FONT_MONO}':text='free / local':x=360+(240-tw)/2:y=302:fontsize=14:fontcolor=0x9977cc,
      drawbox=x=360:y=220:w=240:h=140:color=0x4488cc@0.10:t=max:enable='between(t,2.5,4.5)',

      drawbox=x=640:y=220:w=240:h=140:color=0x0d0820:t=max,
      drawbox=x=640:y=220:w=240:h=140:color=0xee8833:t=2,
      drawtext=fontfile='${FONT_MONO}':text='AI / CURSOR':x=640+(240-tw)/2:y=262:fontsize=20:fontcolor=0xee8833,
      drawtext=fontfile='${FONT_MONO}':text='cloud tokens':x=640+(240-tw)/2:y=302:fontsize=14:fontcolor=0x9977cc,
      drawbox=x=640:y=220:w=240:h=140:color=0x4488cc@0.10:t=max:enable='gte(t,5.0)',

      drawtext=fontfile='${FONT_MONO}':text='configurable in Settings -> LLM Provider Chain':x=(w-tw)/2:y=h-72:fontsize=14:fontcolor=0x9977cc:enable='gte(t,6.5)',
      format=yuv420p
  " \
  -t 8 \
  "${ENCODE_BASE[@]}" -crf 30 \
  fix-chain.mp4
poster fix-chain.mp4 fix-chain-poster.webp

echo "---"
echo "Done. Sizes:"
ls -la *.mp4 *.webp
