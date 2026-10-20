// Fake Kefka deck-input bridge: the SSE payload shape of ops/kefka/deck-input.py with scriptable state.
// Sticks use the bridge convention (+y DOWN), trackpads +y UP; pose() is yaw right / pitch up / roll
// clockwise in degrees. Point the page at it with ?deckBridge=http://127.0.0.1:PORT/events.
import { createServer } from 'node:http';

export function quat(axis, deg) {
  const h = (deg * Math.PI) / 360;
  const s = Math.sin(h);
  return [Math.cos(h), axis[0] * s, axis[1] * s, axis[2] * s];
}
export function qmul(a, b) {
  return [
    a[0] * b[0] - a[1] * b[1] - a[2] * b[2] - a[3] * b[3],
    a[0] * b[1] + a[1] * b[0] + a[2] * b[3] - a[3] * b[2],
    a[0] * b[2] - a[1] * b[3] + a[2] * b[0] + a[3] * b[1],
    a[0] * b[3] + a[1] * b[2] - a[2] * b[1] + a[3] * b[0],
  ];
}
/** Deck pose: yaw right +, pitch up +, roll clockwise + (degrees). */
export function pose(yaw, pitch, roll) {
  return qmul(qmul(quat([0, 1, 0], -yaw), quat([1, 0, 0], pitch)), quat([0, 0, 1], -roll));
}

export function startFakeBridge(port = 18799) {
  const state = {
    nativeAllowed: true,
    buttons: new Array(16).fill(0),
    axes: [0, 0, 0, 0], // bridge convention: +y DOWN for sticks
    touch: [false, false],
    touchpads: [[0, 0], [0, 0]], // +y UP for trackpads
    pose: [0, -40, 0],
    resting: false,
  };
  const clients = new Set();
  const payload = () => {
    const at = performance.now() / 1000;
    return {
      connected: true,
      motionConnected: true,
      nativeAllowed: state.nativeAllowed,
      nativeReason: state.nativeAllowed ? null : 'Focus the Macroverse Deck window for native controls',
      pad: state.nativeAllowed ? {
        buttons: state.buttons.slice(),
        connection: '1:1',
        touch: state.touch.slice(),
        touchpads: state.touchpads.map((p, i) => (state.touch[i] ? p.slice() : [0, 0])),
        axes: state.axes.slice(),
        sticks: { calibration: { phase: 'done' }, guard: { parked: [] } },
      } : null,
      motion: state.nativeAllowed ? {
        at, rate: [0, 0, 0], gravity: [0, 0.77, 0.64], q: pose(...state.pose).map((x) => Math.round(x * 1e7) / 1e7),
        bias: [0, 0, 0], resting: state.resting,
      } : null,
    };
  };
  const server = createServer((req, res) => {
    const origin = req.headers.origin || '*';
    if (req.url === '/events') {
      res.writeHead(200, {
        'Content-Type': 'text/event-stream', 'Cache-Control': 'no-store',
        'Access-Control-Allow-Origin': origin, Vary: 'Origin',
      });
      clients.add(res);
      req.on('close', () => clients.delete(res));
      return;
    }
    res.writeHead(404).end();
  });
  const timer = setInterval(() => {
    const line = 'data: ' + JSON.stringify(payload()) + '\n\n';
    for (const c of clients) c.write(line);
  }, 20);
  server.listen(port, '127.0.0.1');
  return {
    state,
    clients,
    press(i, v = 1) { state.buttons[i] = v; },
    release(i) { state.buttons[i] = 0; },
    close() { clearInterval(timer); for (const c of clients) c.end(); server.close(); },
  };
}
