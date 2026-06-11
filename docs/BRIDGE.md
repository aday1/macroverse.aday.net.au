# Macroverse LAN bridge

Connect a Raspberry Pi (or any LAN host) to the cloud Macroverse app so one gig session shares VJ preview on HDMI, deck control across devices, and Ableton Link on the venue subnet.

## Quick start

1. In Macroverse: **Settings** → **VJ Show Session ID** → e.g. `friday-main` → Apply.
2. Open the same session on phones/tablets (same ID in Settings or `?sessionId=friday-main` in the URL). On the **VJ** tab, the join QR is beside **VJ Preview**; use **Display on screen** or **Open display window** for the audience.
3. Pi HDMI (Chromium kiosk):

   `https://macroverse.aday.net.au/vj-output.html?remote=1&viewToken=…` (minted in Settings; audience QR is view-only)

4. Mint a bridge token (from any machine that can reach the server):

   `POST /api/bridge/token`  
   Body: `{"bridgeId":"pi-vj","sessionId":"friday-main"}`

5. Pi config `~/.macroverse/bridge.json`:

```json
{
  "cloudUrl": "https://macroverse.aday.net.au",
  "token": "<token from step 4>",
  "bridgeId": "pi-vj",
  "sessionId": "friday-main",
  "linkEnabled": true
}
```

6. On the Pi:

```
cd macroverse-bridge-agent
npm install
npm run build
npm start
```

The agent logs the HDMI URL. Pair with **ArtBastard** on the same Pi using a different `sessionId` if you also run DMX (see `artbastard` repo `DOCS/BRIDGE.md`).

## What syncs per session

| Feature | Transport | Notes |
| --- | --- | --- |
| VJ preview (HDMI) | SSE `/api/vj-output/stream?viewToken=` | Signed viewer token from QR (no raw sessionId) |
| Deck crossfader, mix, clips | WebSocket `/ws` + operator token | Control token minted in Settings; join links without token are view-only |
| Ableton Link | Bridge → cloud → browsers | `clock:state` events |
| OSC LAN relay | Planned | Bridge agent stub only today |

## WebSocket protocol (summary)

Clients connect to `/ws` on the same host as the API.

After connect, send:

```json
{"type":"auth","role":"client","sessionId":"friday-main"}
{"type":"session:join","sessionId":"friday-main"}
```

Deck changes:

```json
{"type":"vj:control","payload":{"crossfader":0.5}}
```

Bridge auth:

```json
{"type":"auth","role":"bridge","token":"<jwt>","sessionId":"friday-main"}
```

## systemd (Pi)

`/etc/systemd/system/macroverse-bridge.service`:

```
[Unit]
Description=Macroverse LAN bridge
After=network-online.target

[Service]
Type=simple
User=pi
WorkingDirectory=/home/pi/macroverse.aday.net.au/macroverse-bridge-agent
ExecStart=/usr/bin/node /home/pi/macroverse.aday.net.au/macroverse-bridge-agent/dist/index.js
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
```

HDMI kiosk (separate unit or autostart): open the `hdmiUrl` from bridge config in Chromium `--kiosk`.

## Production secrets

Set `BRIDGE_TOKEN_SECRET` or `MACROVERSE_BRIDGE_SECRET` on the server (same value used to sign tokens). Default dev secret is not safe for production.

## API

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/api/vj-sessions` | List active VJ sessions |
| GET | `/api/bridge/status?sessionId=` | Bridge connected for session |
| POST | `/api/bridge/token` | Mint bridge JWT |

## Troubleshooting

| Symptom | Check |
| --- | --- |
| Decks not syncing | Browser devtools → WS to `/ws`; same session ID on all devices |
| HDMI black | A leader must run VJ deck and post frames; open preview URL with `remote=1` |
| Link BPM stuck | Bridge running with `linkEnabled`; `npm rebuild abletonlink` on Pi if native module missing |
| WS fails behind nginx | `nginx/snippets/proxy-common.conf` must pass `Upgrade` (included in compose repo) |
