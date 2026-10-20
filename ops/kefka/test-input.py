#!/usr/bin/env python3
"""Offline tests for the Macroverse Deck input bridge (vendored from ArtBastard's Kefka bridge tests).

Run: python3 ops/kefka/test-input.py   (no Deck, compositor or network needed)
"""
import importlib.util
import json
import os
from pathlib import Path
import socket
import sys
import tempfile
import threading
import time
import unittest
from unittest.mock import patch

sys.dont_write_bytecode = True  # keep ops/kefka free of __pycache__ (it is deployed as-is)
BRIDGE_PATH = Path(__file__).with_name("deck-input.py")
BRIDGE_ENV = ("MACROVERSE_DECK_CLASS", "MACROVERSE_DECK_ORIGINS", "MACROVERSE_DECK_INPUT_PORT")


def load_bridge(env=None):
    """Import a fresh copy of the bridge with exactly these MACROVERSE_DECK_* variables set."""
    clean = {k: v for k, v in os.environ.items() if k not in BRIDGE_ENV}
    clean.update(env or {})
    with patch.dict(os.environ, clean, clear=True):
        spec = importlib.util.spec_from_file_location("deck_input", BRIDGE_PATH)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
    return module


bridge = load_bridge()


class ReaderTests(unittest.TestCase):
    def test_hid_steam_button_layout_matches_standard_gamepad(self):
        info = {20: (0, 0, 32767, 0, 0, 0), 21: (0, 0, 32767, 0, 0, 0)}
        for code, index in [(0x130, 0), (0x131, 1), (0x133, 2), (0x134, 3),
                            (0x136, 4), (0x137, 5), (0x220, 12), (0x221, 13),
                            (0x222, 14), (0x223, 15)]:
            pad = bridge.make_pad({code: True}, {20: 0, 21: 0}, info, 42)
            self.assertEqual(pad["buttons"][index], 1)
            self.assertEqual(sum(pad["buttons"]), 1)
            self.assertEqual(pad["connection"], 42)

    def test_triggers_have_correct_side_and_are_bounded(self):
        info = {20: (0, 0, 100, 0, 0, 0), 21: (0, 0, 100, 0, 0, 0)}
        pad = bridge.make_pad({}, {20: 25, 21: 100}, info, 1)
        self.assertEqual(pad["buttons"][6:8], [1, .25])
        self.assertEqual(bridge.trigger_value(-10, info[20]), 0)
        self.assertEqual(bridge.trigger_value(110, info[20]), 1)
        self.assertEqual(bridge.trigger_value(1, (0, 0, 0, 0, 0, 0)), 0)


class NativeGateTests(unittest.TestCase):
    def setUp(self):
        self.scratch = tempfile.TemporaryDirectory(prefix="ab-gate-")
        self.addCleanup(self.scratch.cleanup)
        self.root = Path(self.scratch.name)
        self.runtime = self.root / "run"
        self.proc = self.root / "proc"
        self.mapper = self.runtime / "deckhypr"
        self.mapper.mkdir(parents=True)
        self.mapper.joinpath("pid").write_text("123")
        self.process(123, "deckhypr")
        self.state()
        self.gate = bridge.NativeInputGate(self.runtime, self.proc, os.getuid())
        self.active = {"class": bridge.DECK_CLASS, "address": "0xabc"}
        self.pad = {"connection": 1, "buttons": [0] * 16}

    def process(self, pid, name, status="S", started="100"):
        folder = self.proc / str(pid)
        folder.mkdir(parents=True, exist_ok=True)
        folder.joinpath("comm").write_text(name + "\n")
        folder.joinpath("stat").write_text(
            f"{pid} ({name}) " + " ".join([status] + ["0"] * 18 + [started]))

    def state(self, mode="game", pad="kernel", launcher="closed"):
        self.mapper.joinpath("state.json").write_text(json.dumps(
            {"mode": mode, "pad": pad, "launcher": launcher, "ts": 1}))

    def check(self):
        with patch.object(self.gate, "active_window", return_value=self.active):
            self.gate.check()
        return self.gate.snapshot(self.pad)

    def test_old_mode_timestamp_is_valid_only_with_live_mapper_and_exact_window(self):
        state = self.check()
        self.assertTrue(state["connected"])
        self.assertTrue(state["nativeAllowed"])
        self.assertEqual(state["pad"]["buttons"], self.pad["buttons"])
        self.assertEqual(state["pad"]["connection"], "1:1")

    def test_desktop_menu_missing_mapper_and_invalid_state_fail_closed(self):
        for mode in ("desktop", "menu", None):
            self.state(mode=mode)
            state = self.check()
            self.assertTrue(state["connected"], "Reader health remains independent of mode")
            self.assertFalse(state["nativeAllowed"])
            self.assertIsNone(state["pad"])
        self.state()
        self.mapper.joinpath("pid").unlink()
        self.assertFalse(self.check()["nativeAllowed"], "Stale GAME state cannot replace a live process")
        self.mapper.joinpath("pid").write_text("123")
        for broken in ("{", "null", "[]"):
            self.mapper.joinpath("state.json").write_text(broken)
            self.assertFalse(self.check()["nativeAllowed"])

    def test_dead_stopped_reused_or_unreadable_mapper_is_rejected(self):
        for status in ("Z", "X", "T", "t"):
            self.process(123, "deckhypr", status=status)
            self.assertFalse(self.check()["nativeAllowed"])
        self.process(123, "other-process")
        self.assertFalse(self.check()["nativeAllowed"])
        self.proc.joinpath("123/stat").write_text("unreadable")
        self.assertFalse(self.check()["nativeAllowed"])

    def test_focus_pad_handoff_or_stale_check_revokes_native_input(self):
        original = self.check()["pad"]["connection"]
        self.active = {"class": "google-chrome", "address": "0xdef"}
        self.assertFalse(self.check()["nativeAllowed"])
        self.active = {"class": bridge.DECK_CLASS, "address": "0xabc"}
        self.assertNotEqual(self.check()["pad"]["connection"], original,
                            "Returning after handoff must require a new neutral gate")
        for params in ({"pad": "steam"}, {"pad": "none"}, {"launcher": "open"}):
            self.state(**params)
            self.assertFalse(self.check()["nativeAllowed"])
        self.state()
        self.check()
        self.gate.checked_at = time.monotonic() - 1
        self.assertFalse(self.gate.snapshot(self.pad)["nativeAllowed"])
        self.assertIsNone(self.gate.snapshot(self.pad)["pad"])

    def test_other_apps_ordinary_browser_and_similarly_named_windows_are_denied(self):
        for app_class in ("artbastard-deck", "artbastard-console", "macroverse-deck-other", "Macroverse-deck",
                          "chrome-127.0.0.1__deck.html-Default", "chrome-127.0.0.1__8765-Default",
                          "google-chrome"):
            with self.subTest(app_class=app_class):
                self.active = {"class": app_class, "address": "0xdef"}
                state = self.check()
                self.assertFalse(state["nativeAllowed"])
                self.assertIsNone(state["pad"])

    def test_active_window_query_ignores_stale_signatures(self):
        self.process(456, "Hyprland")
        live = self.runtime / "hypr" / "live"
        stale = self.runtime / "hypr" / "stale"
        live.mkdir(parents=True)
        stale.mkdir()
        live.joinpath("hyprland.lock").write_text("456\nwayland-1\n")
        stale.joinpath("hyprland.lock").write_text("999\nwayland-0\n")
        stale.joinpath(".socket.sock").touch()
        listener = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
        self.addCleanup(listener.close)
        listener.bind(str(live / ".socket.sock"))
        listener.listen(1)
        requests = []

        def answer():
            connection, _ = listener.accept()
            with connection:
                requests.append(connection.recv(1024))
                connection.sendall(json.dumps(self.active).encode())

        worker = threading.Thread(target=answer, daemon=True)
        worker.start()
        self.gate.check()
        worker.join(timeout=1)
        self.assertEqual(requests, [b"j/activewindow"])
        self.assertTrue(self.gate.snapshot(self.pad)["nativeAllowed"])


# --- Sticks, trackpads and motion (ported from the Yggdrasil Deck bridge tests, same Deck) ---
import math
import random

QUIET = [0.0] * 16
NOPAD = [[0, 0], [0, 0]]
KEFKA_REST = [0.0679, 0.0209, -0.0836, 0.0769]
STILL_DECK = {"at": 0, "rate": [0.1, 0.05, 0.12], "gravity": [-0.5, 0.08, -0.9]}
HAND_DECK = {"at": 0, "rate": [2.5, -1.2, 0.8], "gravity": [-0.5, 0.08, -0.9]}


def feed_cal(cal, frames, hz=50.0, t0=0.0, buttons=QUIET, pads=NOPAD):
    out, t = [], t0
    for axes in frames:
        out.append(cal.update(list(axes), buttons, pads, t))
        t += 1.0 / hz
    return out, t


def rest(n, noise=0.0006, seed=1):
    rnd = random.Random(seed)
    return [[a + rnd.gauss(0, noise) for a in KEFKA_REST] for _ in range(n)]


class StickCalibrationTests(unittest.TestCase):
    def test_rest_is_learned_once_and_removed(self):
        cal = bridge.StickCalibration(0.0)
        out, _ = feed_cal(cal, rest(150))
        self.assertEqual(cal.phase, "done")
        self.assertTrue(all(abs(v) < 0.005 for v in out[-1]))

    def test_held_input_blocks_learning_and_use_never_relearns(self):
        cal = bridge.StickCalibration(0.0)
        feed_cal(cal, rest(250), buttons=[1.0] + [0.0] * 15)
        self.assertEqual(cal.phase, "learning")
        cal = bridge.StickCalibration(0.0)
        _, t = feed_cal(cal, rest(150))
        centre = list(cal.centre)
        feed_cal(cal, [[0.0, 0.0, 0.12, 0.12]] * 1000, t0=t)
        self.assertEqual(cal.centre, centre)

    def test_full_range_and_monotonic(self):
        c = -0.0836
        self.assertEqual(bridge.StickCalibration.correct(1.0, c), 1.0)
        self.assertEqual(bridge.StickCalibration.correct(-1.0, c), -1.0)
        ys = [bridge.StickCalibration.correct(x / 100, c) for x in range(-100, 101)]
        self.assertEqual(ys, sorted(ys))


class StickGuardTests(unittest.TestCase):
    def run_guard(self, frames, motion=STILL_DECK, buttons=QUIET, pads=NOPAD, hz=50.0, t0=0.0, guard=None):
        guard = guard or bridge.StickGuard()
        out, t = [], t0
        for axes in frames:
            out.append(guard.update(list(axes), buttons, pads, motion, t))
            t += 1.0 / hz
        return guard, out, t

    def test_unattended_drift_is_parked_and_released_by_a_jump(self):
        guard, out, t = self.run_guard([[0.0, 0.0, 0.5, 0.5]] * 350)
        self.assertEqual(out[-1][2:], [0.0, 0.0])
        self.assertEqual(guard.state()["parked"], ["right"])
        _, out, _ = self.run_guard([[0.0, 0.0, 0.7, 0.5]], t0=t, guard=guard)
        self.assertEqual(guard.state()["parked"], [])
        self.assertEqual(out[-1][2:], [0.7, 0.5])

    def test_handheld_or_pressed_or_no_motion_never_parks(self):
        for kwargs in (dict(motion=HAND_DECK), dict(motion=None), dict(buttons=[0.0] * 6 + [0.7] + [0.0] * 9),
                       dict(pads=[[0, 0], [500, 20]])):
            guard, out, _ = self.run_guard([[0.0, 0.0, 0.5, 0.5]] * 1000, **kwargs)
            self.assertEqual(out[-1][2:], [0.5, 0.5], kwargs)


class AhrsTests(unittest.TestCase):
    HZ = 250.0

    def sim(self, ahrs, truth, rates, seconds, bias=(0.0, 0.0, 0.0), t0=0.0):
        A, t = bridge.Ahrs, t0
        for _ in range(int(seconds * self.HZ)):
            t += 1 / self.HZ
            wr = [math.radians(r) for r in rates]
            ang = math.sqrt(sum(x * x for x in wr)) / self.HZ
            if ang > 0:
                h = math.sin(ang / 2) / (ang * self.HZ)
                truth[:] = A._norm(A._mul(truth, [math.cos(ang / 2), wr[0] * h, wr[1] * h, wr[2] * h]))
            up = ahrs.rotate([0.0, 1.0, 0.0], [truth[0], -truth[1], -truth[2], -truth[3]])
            ahrs.update([rates[i] + bias[i] for i in range(3)], up, t)
        return t

    def forward(self, ahrs):
        return ahrs.rotate([0.0, 0.0, -1.0])   # the Deck's back points -Z

    def test_right_turn_and_looking_up(self):
        a, truth = bridge.Ahrs(), [1.0, 0.0, 0.0, 0.0]
        t = self.sim(a, truth, (0, 0, 0), 0.1)
        t = self.sim(a, truth, (0, -90, 0), 1.0, t0=t)
        self.assertAlmostEqual(self.forward(a)[0], 1.0, delta=0.01)
        a, truth = bridge.Ahrs(), [1.0, 0.0, 0.0, 0.0]
        t = self.sim(a, truth, (0, 0, 0), 0.1)
        self.sim(a, truth, (90, 0, 0), 0.5, t0=t)
        self.assertAlmostEqual(self.forward(a)[1], math.sin(math.radians(45)), delta=0.01)

    def test_resting_bias_is_learned_and_does_not_drift(self):
        a, truth, b = bridge.Ahrs(), [1.0, 0.0, 0.0, 0.0], (0.17, 0.1, -0.05)
        t = self.sim(a, truth, (0, 0, 0), 8.0, bias=b)
        f0 = self.forward(a)
        self.sim(a, truth, (0, 0, 0), 60.0, bias=b, t0=t)
        f1 = self.forward(a)
        drift = math.degrees(math.acos(max(-1, min(1, sum(x * y for x, y in zip(f0, f1))))))
        self.assertLess(drift, 0.5)


class PadAndMotionPayloadTests(unittest.TestCase):
    INFO = {**{c: (0, -32767, 32767, 0, 0, 0) for c in (0, 1, 3, 4, 16, 17, 18, 19)},
            20: (0, 0, 32767, 0, 0, 0), 21: (0, 0, 32767, 0, 0, 0)}

    def test_sticks_and_touchpads_are_normalized(self):
        values = {c: 0 for c in self.INFO}
        values.update({0: 32767, 4: -16384, 18: 1000, 19: -2000})
        pad = bridge.make_pad({}, values, self.INFO, 3)
        self.assertEqual(pad["axes"][0], 1.0)
        self.assertAlmostEqual(pad["axes"][3], -0.5, places=3)
        self.assertEqual(pad["touch"], [False, True])
        self.assertEqual(len(pad["touchpads"]), 2)
        self.assertEqual(len(pad["buttons"]), 16)

    def test_old_trigger_only_payload_is_unchanged(self):
        info = {20: (0, 0, 32767, 0, 0, 0), 21: (0, 0, 32767, 0, 0, 0)}
        pad = bridge.make_pad({}, {20: 0, 21: 0}, info, 1)
        self.assertEqual(set(pad), {"buttons", "connection"})

    def test_motion_only_leaves_the_bridge_when_native_controls_are_allowed(self):
        gate = bridge.NativeInputGate(runtime="/nonexistent", proc="/nonexistent")
        motion = {"at": time.monotonic(), "rate": [0, 0, 0], "gravity": [0, 1, 0], "q": [1, 0, 0, 0]}
        pad = {"buttons": [0.0] * 16, "connection": 1}
        gate.allowed, gate.checked_at = False, time.monotonic()
        snap = gate.snapshot(pad, motion)
        self.assertIsNone(snap["motion"])
        self.assertIsNone(snap["pad"])
        self.assertTrue(snap["motionConnected"])
        gate.allowed, gate.checked_at = True, time.monotonic()
        snap = gate.snapshot(pad, motion)
        self.assertEqual(snap["motion"]["q"], [1, 0, 0, 0])
        stale = {**motion, "at": time.monotonic() - 1}
        self.assertIsNone(gate.snapshot(pad, stale)["motion"])


class MacroverseConfigTests(unittest.TestCase):
    def test_macroverse_defaults(self):
        self.assertEqual(bridge.DECK_CLASS, "macroverse-deck")
        self.assertEqual(bridge.PORT, 18783)
        self.assertEqual(bridge.ORIGINS, frozenset({"http://127.0.0.1:8765", "http://localhost:8765"}))

    def test_environment_overrides_class_origins_and_port(self):
        other = load_bridge({"MACROVERSE_DECK_CLASS": "macroverse-deck-test",
                             "MACROVERSE_DECK_ORIGINS": " http://127.0.0.1:9000 ,http://localhost:9000/,",
                             "MACROVERSE_DECK_INPUT_PORT": "18799"})
        self.assertEqual(other.DECK_CLASS, "macroverse-deck-test")
        self.assertEqual(other.PORT, 18799)
        self.assertEqual(other.ORIGINS, frozenset({"http://127.0.0.1:9000", "http://localhost:9000"}))

    def test_empty_values_fall_back_and_wildcards_are_refused(self):
        self.assertEqual(bridge.load_config({"MACROVERSE_DECK_CLASS": " ", "MACROVERSE_DECK_ORIGINS": "",
                                             "MACROVERSE_DECK_INPUT_PORT": ""}),
                         bridge.load_config({}))
        for bad in ({"MACROVERSE_DECK_ORIGINS": "*"}, {"MACROVERSE_DECK_ORIGINS": "null"},
                    {"MACROVERSE_DECK_ORIGINS": " , "}, {"MACROVERSE_DECK_INPUT_PORT": "0"},
                    {"MACROVERSE_DECK_INPUT_PORT": "70000"}, {"MACROVERSE_DECK_INPUT_PORT": "abc"}):
            with self.subTest(bad=bad):
                with self.assertRaises(ValueError):
                    bridge.load_config(bad)

    def test_gate_uses_the_configured_class(self):
        other = load_bridge({"MACROVERSE_DECK_CLASS": "mv-test"})
        scratch = tempfile.TemporaryDirectory(prefix="mv-gate-")
        self.addCleanup(scratch.cleanup)
        root = Path(scratch.name)
        (root / "run/deckhypr").mkdir(parents=True)
        (root / "run/deckhypr/pid").write_text("123")
        (root / "run/deckhypr/state.json").write_text(json.dumps({"mode": "game", "pad": "kernel", "launcher": "closed"}))
        (root / "proc/123").mkdir(parents=True)
        (root / "proc/123/comm").write_text("deckhypr\n")
        (root / "proc/123/stat").write_text("123 (deckhypr) " + " ".join(["S"] + ["0"] * 18 + ["100"]))
        gate = other.NativeInputGate(root / "run", root / "proc", os.getuid())
        pad = {"connection": 1, "buttons": [0] * 16}
        for app_class, allowed in (("mv-test", True), ("macroverse-deck", False)):
            with patch.object(gate, "active_window", return_value={"class": app_class, "address": "0x1"}):
                gate.check()
            self.assertEqual(gate.snapshot(pad)["nativeAllowed"], allowed, app_class)
            if not allowed:
                self.assertIn("Macroverse Deck", gate.snapshot(pad)["nativeReason"])


class HttpOriginTests(unittest.TestCase):
    """The real handler on an ephemeral loopback port: exact origins only, no pad while the gate is closed."""

    def setUp(self):
        from http.server import ThreadingHTTPServer
        import http.client
        self.http = http.client
        self.server = ThreadingHTTPServer(("127.0.0.1", 0), bridge.Handler)
        self.port = self.server.server_address[1]
        threading.Thread(target=self.server.serve_forever, daemon=True).start()
        self.addCleanup(self.server.server_close)
        self.addCleanup(self.server.shutdown)
        bridge.GATE.allowed, bridge.GATE.checked_at = False, time.monotonic()
        bridge.GATE.reason = "Focus the Macroverse Deck window for native controls"

    def get(self, path, origin):
        connection = self.http.HTTPConnection("127.0.0.1", self.port, timeout=2)
        self.addCleanup(connection.close)
        headers = {"Origin": origin} if origin else {}
        connection.request("GET", path, headers=headers)
        return connection.getresponse()

    def test_health_for_allowed_origins_reports_gate_without_pad_or_motion(self):
        now = time.monotonic()
        with bridge.LOCK:
            bridge.STATE["pad"] = {"buttons": [1.0] + [0.0] * 15, "connection": 1}
            bridge.STATE["motion"] = {"at": now + 10, "rate": [0, 0, 0], "gravity": [0, 1, 0], "q": [1, 0, 0, 0]}
        self.addCleanup(bridge.STATE.update, {"pad": None, "motion": None})
        for origin in ("http://127.0.0.1:8765", "http://localhost:8765"):
            response = self.get("/health", origin)
            self.assertEqual(response.status, 200)
            self.assertEqual(response.getheader("Access-Control-Allow-Origin"), origin)
            body = json.loads(response.read())
            self.assertEqual(body["connected"], True)
            self.assertEqual(body["motionConnected"], True)
            self.assertEqual(body["nativeAllowed"], False)
            self.assertEqual(body["deckClass"], "macroverse-deck")
            self.assertNotIn("pad", body)
            self.assertNotIn("motion", body)

    def test_other_origins_and_paths_are_forbidden(self):
        for origin in (None, "http://127.0.0.1:3030", "http://kefka.lan:8765", "https://127.0.0.1:8765",
                       "http://127.0.0.1:8765.evil", "null"):
            with self.subTest(origin=origin):
                self.assertEqual(self.get("/health", origin).status, 403)
        self.assertEqual(self.get("/", "http://127.0.0.1:8765").status, 403)
        self.assertEqual(self.get("/events/x", "http://127.0.0.1:8765").status, 403)

    def test_event_stream_withholds_pad_and_motion_while_gate_is_closed(self):
        with bridge.LOCK:
            bridge.STATE["pad"] = {"buttons": [1.0] + [0.0] * 15, "connection": 1}
        self.addCleanup(bridge.STATE.update, {"pad": None, "motion": None})
        response = self.get("/events", "http://127.0.0.1:8765")
        self.assertEqual(response.status, 200)
        self.assertEqual(response.getheader("Content-Type"), "text/event-stream")
        line = response.fp.readline().decode()
        self.assertTrue(line.startswith("data: "), line)
        event = json.loads(line[6:])
        self.assertFalse(event["nativeAllowed"])
        self.assertIsNone(event["pad"])
        self.assertIsNone(event["motion"])


if __name__ == "__main__":
    unittest.main()
