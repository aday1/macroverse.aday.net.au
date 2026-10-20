#!/usr/bin/env python3
"""Read-only Steam Deck buttons, sticks, trackpads and motion for the Macroverse Deck window.

Never grabs or synthesizes input. Vendored from ArtBastard's Kefka bridge
(artbastard-native scripts/kefka/deck-input.py, commit 80d5cf9); the reader, stick filters and AHRS are
unchanged. Only the window class, the allowed page origins and the port are configurable here:

  MACROVERSE_DECK_CLASS        focused Hyprland window class that may read the pad (default macroverse-deck)
  MACROVERSE_DECK_ORIGINS      comma-separated exact page origins
                               (default http://127.0.0.1:8765,http://localhost:8765)
  MACROVERSE_DECK_INPUT_PORT   loopback TCP port (default 18783)

The server binds 127.0.0.1 only. /events (SSE) and /health answer only the exact origins above, and the pad and
motion are published only while deckhypr is in GAME mode and the focused window has the exact class.
"""
import fcntl
import glob
import json
import math
import os
from pathlib import Path
import select
import socket
import struct
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

EVENT = struct.Struct("llHHi")
# A/B/X/Y, shoulders, triggers, View/Menu, stick clicks, D-pad.
BUTTON_CODES = (0x130, 0x131, 0x133, 0x134, 0x136, 0x137, 0x138, 0x139,
                0x13A, 0x13B, 0x13D, 0x13E, 0x220, 0x221, 0x222, 0x223)
DEFAULT_CLASS = "macroverse-deck"
DEFAULT_ORIGINS = "http://127.0.0.1:8765,http://localhost:8765"
DEFAULT_PORT = 18783


def load_config(env=None):
    """Window class, exact origins and port from the environment, with Macroverse defaults."""
    env = os.environ if env is None else env
    deck_class = env.get("MACROVERSE_DECK_CLASS", "").strip() or DEFAULT_CLASS
    origins = frozenset(o.strip().rstrip("/") for o in
                        (env.get("MACROVERSE_DECK_ORIGINS", "").strip() or DEFAULT_ORIGINS).split(",") if o.strip())
    if not origins or "*" in origins or "null" in origins:
        raise ValueError("MACROVERSE_DECK_ORIGINS must list exact origins")
    port = int(env.get("MACROVERSE_DECK_INPUT_PORT", "").strip() or DEFAULT_PORT)
    if not 0 < port < 65536:
        raise ValueError("MACROVERSE_DECK_INPUT_PORT out of range")
    return {"deck_class": deck_class, "origins": origins, "port": port}


CONFIG = load_config()
ORIGINS = CONFIG["origins"]
PORT = CONFIG["port"]
LOCK = threading.Lock()
STATE = {"pad": None, "motion": None}
# hid-steam "Steam Deck" absolute axes. Sticks follow the standard gamepad convention (+x right, +y down).
STICK_CODES = (0, 1, 3, 4)          # ABS_X/ABS_Y left stick, ABS_RX/ABS_RY right stick
TOUCHPAD_CODES = (16, 17, 18, 19)   # ABS_HAT0X/Y left trackpad, ABS_HAT1X/Y right trackpad; 0,0 when untouched
TRIGGER_CODES = (20, 21)            # right, left trigger
ABS_CODES = STICK_CODES + TOUCHPAD_CODES + TRIGGER_CODES
MOTION_STALE = 0.25                 # seconds: older motion samples are not published
DECK_CLASS = CONFIG["deck_class"]


class NativeInputGate:
    """Read desktop ownership without changing the mapper or compositor."""

    def __init__(self, runtime=None, proc=None, uid=None):
        self.uid = os.getuid() if uid is None else uid
        self.runtime = Path(runtime or os.environ.get("XDG_RUNTIME_DIR", f"/run/user/{self.uid}"))
        self.proc = Path(proc or "/proc")
        self.lock = threading.Lock()
        self.allowed = False
        self.reason = "Checking Deck desktop input mode"
        self.checked_at = 0
        self.generation = 0
        self.identity = None

    def live_process(self, pid, name):
        """A PID file or old mode timestamp alone does not prove a live mapper."""
        try:
            process = self.proc / str(int(pid))
            if int(pid) <= 1 or process.stat().st_uid != self.uid:
                return None
            if (process / "comm").read_text().strip() != name:
                return None
            fields = (process / "stat").read_text().rsplit(")", 1)[1].split()
            if fields[0] in ("Z", "X", "x", "T", "t"):
                return None
            return str(pid), fields[19]  # PID + start time protects against reuse.
        except (OSError, ValueError, IndexError):
            return None

    def mapper_state(self):
        try:
            mapper = self.runtime / "deckhypr"
            identity = self.live_process((mapper / "pid").read_text().strip(), "deckhypr")
            if identity is None:
                return None, "Deck desktop mapper is not running"
            state = json.loads((mapper / "state.json").read_text())
            if state.get("mode") != "game":
                return None, "Open Macroverse Deck from the launcher for native controls"
            if state.get("pad") != "kernel" or state.get("launcher") != "closed":
                return None, "Deck desktop mapper is not ready for native controls"
            # deckhypr writes only on transitions: an old ts is valid while
            # this same live process remains in GAME mode.
            return identity, None
        except (OSError, ValueError, AttributeError):
            return None, "Deck desktop mapper state is unavailable"

    def active_window(self):
        """Query a live compositor directly, ignoring stale manager signatures."""
        candidates = []
        for path in (self.runtime / "hypr").glob("*/.socket.sock"):
            try:
                pid = (path.parent / "hyprland.lock").read_text().splitlines()[0]
                if self.live_process(pid, "Hyprland"):
                    candidates.append(path)
            except (OSError, IndexError):
                continue
        # Normally there is one live session. Decline ambiguous sessions rather
        # than accepting a game window on a different desktop.
        if len(candidates) != 1:
            return None
        with socket.socket(socket.AF_UNIX, socket.SOCK_STREAM) as connection:
            connection.settimeout(0.1)
            connection.connect(str(candidates[0]))
            connection.sendall(b"j/activewindow")
            data = bytearray()
            while len(data) < 65536:
                chunk = connection.recv(65536 - len(data))
                if not chunk:
                    break
                data.extend(chunk)
            return json.loads(data)

    def check(self):
        identity, reason = self.mapper_state()
        if identity:
            try:
                active = self.active_window()
                if not isinstance(active, dict) or active.get("class") != DECK_CLASS or not active.get("address"):
                    identity, reason = None, "Focus the Macroverse Deck window for native controls"
                else:
                    identity = (*identity, active["address"])
            except (OSError, ValueError):
                identity, reason = None, "Deck window focus is unavailable"
        with self.lock:
            if identity != self.identity:
                self.generation += 1
            self.identity = identity
            self.allowed = identity is not None
            self.reason = reason
            self.checked_at = time.monotonic()

    def run(self):
        while True:
            self.check()
            time.sleep(0.2)

    def snapshot(self, pad, motion=None):
        with self.lock:
            allowed = self.allowed and time.monotonic() - self.checked_at < 0.5
            reason = self.reason if allowed or not self.allowed else "Deck desktop input check is stale"
            generation = self.generation
        fresh = motion is not None and time.monotonic() - motion.get("at", 0) < MOTION_STALE
        return {
            "connected": pad is not None,
            "motionConnected": fresh,
            "nativeAllowed": allowed,
            "nativeReason": reason,
            "pad": {**pad, "connection": f"{pad['connection']}:{generation}"} if allowed and pad else None,
            # Motion follows the same gate: nothing about the Deck's pose leaves the bridge unless the
            # Macroverse Deck window owns the controller.
            "motion": motion if allowed and fresh else None,
        }


# --- Sticks and motion, ported from the Yggdrasil Deck bridge (yggdrasil-security ops/deck-controller/deck-input.py),
# where they were measured on this same Deck (kefka) on 2026-10-18/19.

class StickCalibration:
    """One bounded rest-centre correction per device connection.

    Kefka's right stick rests near (-0.084, +0.077) and evdev reports flat=0. During the first DEADLINE
    seconds after the pad opens, a WINDOW-long run of motionless samples with both sticks inside RANGE and
    nothing else touched becomes the centre (capped to +-CAP). Nothing is learned afterwards, so a direction
    held during use is never absorbed. Correction keeps the full +-1 range.
    """
    STILL = 0.012
    WINDOW = 1.5
    RANGE = 0.16
    CAP = 0.15
    DEADLINE = 30.0

    def __init__(self, now):
        self.opened = now
        self.phase = "learning"
        self.centre = [0.0] * 4
        self.window = []
        self.window_start = None
        self.reason = "waiting for still sticks"

    def _busy(self, axes, buttons, touchpads):
        if any(abs(a) > self.RANGE for a in axes):
            return "stick deflected"
        if any(b > 0.05 for b in buttons):
            return "button or trigger held"
        if any(v for pad in touchpads for v in pad):
            return "touchpad touched"
        return None

    def update(self, axes, buttons, touchpads, now):
        if self.phase == "learning":
            if now - self.opened > self.DEADLINE:
                self.phase, self.window, self.reason = "skipped", [], f"no still window within {self.DEADLINE:g}s"
            else:
                busy = self._busy(axes, buttons, touchpads)
                if busy:
                    self.window, self.window_start, self.reason = [], None, busy
                else:
                    if not self.window:
                        self.window_start = now
                    self.window.append(list(axes))
                    self.reason = "measuring"
                    spread = [max(w[i] for w in self.window) - min(w[i] for w in self.window) for i in range(4)]
                    if max(spread) > self.STILL:
                        self.window, self.window_start, self.reason = [list(axes)], now, "sticks moved"
                    elif now - self.window_start >= self.WINDOW and len(self.window) >= 10:
                        self.centre = [max(-self.CAP, min(self.CAP, sum(w[i] for w in self.window) / len(self.window)))
                                       for i in range(4)]
                        self.phase, self.window, self.reason = "done", [], "rest centre learned"
        return [self.correct(a, c) for a, c in zip(axes, self.centre)]

    @staticmethod
    def correct(value, centre):
        if centre == 0:
            return value
        span = (1 - centre) if value >= centre else (1 + centre)
        return max(-1.0, min(1.0, (value - centre) / span))

    def state(self, now):
        progress = None
        if self.phase == "learning":
            progress = 0.0 if not self.window else min(1.0, (now - self.window_start) / self.WINDOW)
        return {"phase": self.phase, "centre": [round(c, 4) for c in self.centre], "reason": self.reason,
                "progress": progress}


class StickGuard:
    """Publish 0 for a stick that sits deflected and steady while the Deck is unattended.

    Kefka's right stick drifted on its own to (0.80, 0.84) over an hour on 2026-10-19 with nobody touching
    the Deck. A stick is parked after HOLD seconds beyond DEADZONE, steady within STILL, while the gyro stays
    below QUIET_RATE and nothing else is touched. It is released on a jump, on Deck motion or on any input.
    Without motion data nothing is ever parked.
    """
    DEADZONE = 0.13
    STILL = 0.03
    HOLD = 5.0
    QUIET_RATE = 1.0
    MOVE_RATE = 2.0
    RELEASE = 0.06

    def __init__(self):
        self.window = {0: [], 1: []}
        self.since = {0: None, 1: None}
        self.parked = {0: None, 1: None}
        self.quiet_since = None

    def update(self, axes, buttons, touchpads, motion, now):
        active = any(b > 0.05 for b in buttons) or any(v for pad in touchpads for v in pad)
        rate = math.hypot(*motion["rate"]) if motion else None
        handheld = rate is None or rate >= self.QUIET_RATE
        if handheld or active:
            self.quiet_since = None
        elif self.quiet_since is None:
            self.quiet_since = now
        out = list(axes)
        for i in (0, 1):
            x, y = axes[2 * i], axes[2 * i + 1]
            if self.parked[i] is not None:
                px, py = self.parked[i]
                if active or (rate is not None and rate > self.MOVE_RATE) or math.hypot(x - px, y - py) > self.RELEASE:
                    self.parked[i], self.window[i], self.since[i] = None, [], None
                else:
                    self.parked[i] = (px * .98 + x * .02, py * .98 + y * .02)
                    out[2 * i] = out[2 * i + 1] = 0.0
                    continue
            if math.hypot(x, y) <= self.DEADZONE or self.quiet_since is None:
                self.window[i], self.since[i] = [], None
                continue
            window = self.window[i]
            window.append((x, y))
            if self.since[i] is None:
                self.since[i] = now
            if max(math.hypot(a - x, b - y) for a, b in window) > self.STILL:
                self.window[i], self.since[i] = [(x, y)], now
                continue
            if now - max(self.since[i], self.quiet_since) >= self.HOLD:
                self.parked[i], self.window[i], self.since[i] = (x, y), [], None
                out[2 * i] = out[2 * i + 1] = 0.0
        return out

    def state(self):
        parked = [("left", "right")[i] for i in (0, 1) if self.parked[i] is not None]
        return {"parked": parked,
                "reason": "stick deflected with the Deck unattended; publishing 0 until it moves" if parked else None}


class Ahrs:
    """Deck orientation from the IMU: gyro integrated as a quaternion, bias learned only at rest, tilt pulled
    toward the measured up vector (Mahony) only while |accel| is near 1 g. hid-steam frame: +X right, +Y up,
    +Z toward the user; gyro deg/s right-handed; accelerometer reports UP in g. World +Y is up; heading is
    arbitrary, so consumers should use changes in orientation."""
    STILL_RATE = 1.2
    STILL_ACCEL = 0.03
    STILL_HOLD = 0.6
    BIAS_TAU = 1.5
    KP = 0.8
    ACCEL_GATE = 0.12
    MAX_DT = 0.05

    def __init__(self):
        self.q = None
        self.bias = [0.0, 0.0, 0.0]
        self.rest_since = None
        self.resting = False
        self.t = None
        self.g_ref = None

    @staticmethod
    def _mul(a, b):
        return [a[0] * b[0] - a[1] * b[1] - a[2] * b[2] - a[3] * b[3],
                a[0] * b[1] + a[1] * b[0] + a[2] * b[3] - a[3] * b[2],
                a[0] * b[2] - a[1] * b[3] + a[2] * b[0] + a[3] * b[1],
                a[0] * b[3] + a[1] * b[2] - a[2] * b[1] + a[3] * b[0]]

    @staticmethod
    def _norm(q):
        n = math.sqrt(sum(x * x for x in q)) or 1.0
        return [x / n for x in q]

    def rotate(self, v, q=None):
        """Body vector to world (q v q*)."""
        q = q or self.q
        w, x, y, z = q
        return self._mul(self._mul(q, [0.0, v[0], v[1], v[2]]), [w, -x, -y, -z])[1:]

    def _init(self, up):
        n = math.sqrt(sum(a * a for a in up)) or 1.0
        u = [a / n for a in up]
        axis = [u[2], 0.0, -u[0]]
        s = math.hypot(axis[0], axis[2])
        c = u[1]
        if s < 1e-9:
            self.q = [1.0, 0.0, 0.0, 0.0] if c > 0 else [0.0, 1.0, 0.0, 0.0]
            return
        angle = math.atan2(s, c)
        h = math.sin(angle / 2)
        self.q = self._norm([math.cos(angle / 2), axis[0] / s * h, 0.0, axis[2] / s * h])

    def update(self, rate, up, now):
        amag = math.sqrt(sum(a * a for a in up))
        if self.q is None:
            if amag > 0.5:
                self._init(up)
            self.t = now
            return self.state()
        dt = min(self.MAX_DT, max(0.0, now - self.t))
        self.t = now
        w = [rate[i] - self.bias[i] for i in range(3)]
        if self.g_ref is None:
            self.g_ref = list(up)
        steady = abs(amag - 1.0) < self.ACCEL_GATE and math.dist(up, self.g_ref) < self.STILL_ACCEL
        self.g_ref = [self.g_ref[i] + (up[i] - self.g_ref[i]) * min(1.0, dt / 0.2) for i in range(3)]
        if steady and max(abs(x) for x in w) < self.STILL_RATE:
            if self.rest_since is None:
                self.rest_since = now
        else:
            self.rest_since = None
        self.resting = self.rest_since is not None and now - self.rest_since >= self.STILL_HOLD
        if self.resting and dt > 0:
            k = dt / self.BIAS_TAU
            for i in range(3):
                self.bias[i] += (rate[i] - self.bias[i]) * k
            w = [rate[i] - self.bias[i] for i in range(3)]
        wr = [math.radians(x) for x in w]
        if abs(amag - 1.0) < self.ACCEL_GATE and amag > 0:
            ub = [a / amag for a in up]
            v = self.rotate([0.0, 1.0, 0.0], [self.q[0], -self.q[1], -self.q[2], -self.q[3]])
            e = [ub[1] * v[2] - ub[2] * v[1], ub[2] * v[0] - ub[0] * v[2], ub[0] * v[1] - ub[1] * v[0]]
            wr = [wr[i] + self.KP * e[i] for i in range(3)]
        angle = math.sqrt(sum(x * x for x in wr)) * dt
        if angle > 0:
            h = math.sin(angle / 2) / (angle / dt)
            self.q = self._norm(self._mul(self.q, [math.cos(angle / 2), wr[0] * h, wr[1] * h, wr[2] * h]))
        return self.state()

    def state(self):
        return {"q": [round(x, 7) for x in self.q] if self.q else None,
                "bias": [round(b, 4) for b in self.bias], "resting": self.resting}


class StickFilter:
    """Calibration then the unattended-drift guard, per device connection."""

    def __init__(self, now):
        self.calibration = StickCalibration(now)
        self.guard = StickGuard()

    def __call__(self, raw, buttons, touchpads, now):
        with LOCK:
            motion = STATE["motion"]
        motion = motion if motion and now - motion["at"] < MOTION_STALE else None
        return self.guard.update(self.calibration.update(raw, buttons, touchpads, now), buttons, touchpads, motion, now)

    def state(self, now):
        return {"calibration": self.calibration.state(now), "guard": self.guard.state()}


GATE = NativeInputGate()


def device_name(fd):
    buffer = bytearray(256)
    fcntl.ioctl(fd, 0x81004506, buffer, True)
    return bytes(buffer).split(b"\0")[0].decode(errors="replace")


def abs_info(fd, code):
    buffer = bytearray(24)
    fcntl.ioctl(fd, 0x80184540 + code, buffer, True)
    return struct.unpack("6i", buffer)


def key_state(fd):
    buffer = bytearray(128)
    fcntl.ioctl(fd, 0x80804518, buffer, True)
    return {code: bool(buffer[code // 8] & (1 << (code % 8))) for code in BUTTON_CODES}


def trigger_value(value, info):
    low, high = info[1], info[2]
    return max(0.0, min(1.0, (value - low) / (high - low))) if high > low else 0.0


def axis_value(value, info):
    span = max(abs(info[1]), abs(info[2]))
    return max(-1.0, min(1.0, value / span)) if span else 0.0


def make_pad(keys, values, info, connection, sticks=None, now=None):
    buttons = [float(keys.get(code, False)) for code in BUTTON_CODES]
    buttons[6] = trigger_value(values[21], info[21])  # left trigger
    buttons[7] = trigger_value(values[20], info[20])  # right trigger
    pad = {"buttons": buttons, "connection": connection}
    if all(code in values and code in info for code in TOUCHPAD_CODES):
        touch_raw = [[values[16], values[17]], [values[18], values[19]]]
        pad["touch"] = [bool(p[0] or p[1]) for p in touch_raw]
        pad["touchpads"] = [[round(axis_value(values[c], info[c]), 4) for c in pair] for pair in ((16, 17), (18, 19))]
    else:
        touch_raw = [[0, 0], [0, 0]]
    if all(code in values and code in info for code in STICK_CODES):
        raw = [axis_value(values[c], info[c]) for c in STICK_CODES]
        axes = sticks(raw, buttons, touch_raw, now) if sticks else raw
        pad["axes"] = [round(a, 4) for a in axes]
        if sticks:
            pad["sticks"] = sticks.state(now)
    return pad


def reader():
    connection = 0
    while True:
        fd = None
        try:
            for path in sorted(glob.glob("/dev/input/event*")):
                candidate = None
                try:
                    candidate = os.open(path, os.O_RDONLY | os.O_NONBLOCK)
                    if device_name(candidate) == "Steam Deck":
                        fd = candidate
                        break
                except OSError:
                    pass
                if candidate is not None:
                    os.close(candidate)
            if fd is None:
                time.sleep(2)
                continue
            connection += 1
            info = {code: abs_info(fd, code) for code in ABS_CODES}
            values = {code: data[0] for code, data in info.items()}
            keys = key_state(fd)
            sticks = StickFilter(time.monotonic())
            dropped = False

            def publish():
                now = time.monotonic()
                pad = make_pad(keys, values, info, connection, sticks, now)
                with LOCK:
                    STATE["pad"] = pad

            publish()
            while True:
                if not select.select([fd], [], [], 1)[0]:
                    continue
                data = os.read(fd, EVENT.size * 256)
                if not data:
                    break
                for _, _, kind, code, value in EVENT.iter_unpack(data):
                    if kind == 0 and code == 3:  # SYN_DROPPED: resync at next report.
                        dropped = True
                    elif kind == 0 and code == 0:
                        if dropped:
                            info = {code: abs_info(fd, code) for code in ABS_CODES}
                            values = {code: data[0] for code, data in info.items()}
                            keys = key_state(fd)
                            # Reset the browser's neutral gate after missing input.
                            connection += 1
                            dropped = False
                        publish()
                    elif not dropped and kind == 3 and code in values:
                        values[code] = value
                    elif not dropped and kind == 1 and code in BUTTON_CODES:
                        keys[code] = bool(value)
        except (OSError, ValueError, struct.error):
            pass
        finally:
            if fd is not None:
                os.close(fd)
            with LOCK:
                STATE["pad"] = None
        time.sleep(1)


def motion_reader():
    """Steam Deck Motion Sensors (a separate evdev device): gyro deg/s and accelerometer UP in g."""
    while True:
        fd = None
        try:
            for path in sorted(glob.glob("/dev/input/event*")):
                candidate = None
                try:
                    candidate = os.open(path, os.O_RDONLY | os.O_NONBLOCK)
                    if device_name(candidate) == "Steam Deck Motion Sensors":
                        fd = candidate
                        break
                except OSError:
                    pass
                if candidate is not None:
                    os.close(candidate)
            if fd is None:
                time.sleep(2)
                continue
            info = {code: abs_info(fd, code) for code in range(6)}
            values = {code: data[0] for code, data in info.items()}
            ahrs = Ahrs()
            while True:
                if not select.select([fd], [], [], 1)[0]:
                    continue
                data = os.read(fd, EVENT.size * 256)
                if not data:
                    break
                for _, _, kind, code, value in EVENT.iter_unpack(data):
                    if kind == 3 and code in values:
                        values[code] = value
                    elif kind == 0 and code == 0:
                        now = time.monotonic()
                        # hid-steam gyro: +-2000 deg/s over +-32768 = 16.384 units per deg/s (the driver's
                        # advertised 16 over-rotates by 2.4 %). Accelerometer: resolution units per g.
                        rate = [values[c] / 16.384 for c in (3, 4, 5)]
                        up = [values[c] / (info[c][5] or 16384) for c in (0, 1, 2)]
                        sample = {"at": now, "rate": [round(r, 3) for r in rate], "gravity": [round(g, 4) for g in up],
                                  **ahrs.update(rate, up, now)}
                        with LOCK:
                            STATE["motion"] = sample
        except (OSError, ValueError, struct.error):
            pass
        finally:
            if fd is not None:
                os.close(fd)
            with LOCK:
                STATE["motion"] = None
        time.sleep(1)


class Handler(BaseHTTPRequestHandler):
    def log_message(self, *_args):
        pass

    def do_GET(self):
        origin = self.headers.get("Origin")
        if origin not in ORIGINS or self.path not in ("/events", "/health"):
            self.send_error(403)
            return
        self.send_response(200)
        self.send_header("Content-Type", "text/event-stream" if self.path == "/events" else "application/json")
        self.send_header("Cache-Control", "no-store")
        self.send_header("Access-Control-Allow-Origin", origin)
        self.send_header("Vary", "Origin")
        self.end_headers()
        if self.path == "/health":
            with LOCK:
                payload = GATE.snapshot(STATE["pad"], STATE["motion"])
            del payload["pad"]
            del payload["motion"]
            payload["bridge"] = "macroverse-deck-input"
            payload["deckClass"] = DECK_CLASS
            self.wfile.write(json.dumps(payload).encode())
            return
        try:
            while True:
                with LOCK:
                    payload = json.dumps(GATE.snapshot(STATE["pad"], STATE["motion"]), separators=(",", ":"))
                self.wfile.write(("data: " + payload + "\n\n").encode())
                self.wfile.flush()
                time.sleep(0.02)
        except (BrokenPipeError, ConnectionResetError, TimeoutError):
            pass


if __name__ == "__main__":
    threading.Thread(target=reader, daemon=True).start()
    threading.Thread(target=motion_reader, daemon=True).start()
    threading.Thread(target=GATE.run, daemon=True).start()
    ThreadingHTTPServer(("127.0.0.1", PORT), Handler).serve_forever()
