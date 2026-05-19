import WebSocket from 'ws';
import { BridgeConfig } from './config.js';
import { AbletonLinkSession } from './link.js';

const VERSION = '1.0.0';

export class MacroverseBridgeClient {
  private config: BridgeConfig;
  private ws: WebSocket | null = null;
  private link: AbletonLinkSession;
  private running = false;
  private reconnectAttempt = 0;
  private heartbeatTimer: ReturnType<typeof setInterval> | null = null;
  private linkTimer: ReturnType<typeof setInterval> | null = null;

  constructor(config: BridgeConfig) {
    this.config = config;
    this.link = new AbletonLinkSession();
  }

  async start(): Promise<void> {
    if (!this.config.token) {
      throw new Error('Bridge token required. Mint via POST /api/bridge/token or set BRIDGE_TOKEN.');
    }
    this.running = true;
    if (this.config.linkEnabled) {
      await this.link.init();
    }
    console.log('[bridge] HDMI preview URL:', this.config.hdmiUrl);
    this.connect();
  }

  stop(): void {
    this.running = false;
    if (this.heartbeatTimer) clearInterval(this.heartbeatTimer);
    if (this.linkTimer) clearInterval(this.linkTimer);
    this.link.shutdown();
    this.ws?.close();
    this.ws = null;
  }

  private wsBaseUrl(): string {
    const u = new URL(this.config.cloudUrl);
    u.protocol = u.protocol === 'https:' ? 'wss:' : 'ws:';
    u.pathname = '/ws';
    u.search = '';
    u.hash = '';
    return u.toString();
  }

  private connect(): void {
    const url = this.wsBaseUrl();
    console.log(`[bridge] Connecting to ${url} as ${this.config.bridgeId}`);
    this.ws = new WebSocket(url);

    this.ws.on('open', () => {
      this.reconnectAttempt = 0;
      this.send({
        type: 'auth',
        role: 'bridge',
        token: this.config.token,
        sessionId: this.config.sessionId
      });
      this.send({
        type: 'bridge:hello',
        payload: { bridgeId: this.config.bridgeId, version: VERSION, caps: ['ableton-link', 'osc'] }
      });
      this.startHeartbeat();
      if (this.config.linkEnabled && this.link.isAvailable()) {
        this.linkTimer = setInterval(() => this.emitClockState(), 50);
      }
    });

    this.ws.on('message', (data) => {
      try {
        const msg = JSON.parse(data.toString()) as { type?: string };
        if (msg.type === 'bridge:clock:request') {
          this.emitClockState();
        }
      } catch {
        /* ignore */
      }
    });

    this.ws.on('close', () => this.scheduleReconnect());
    this.ws.on('error', (err) => {
      console.error('[bridge] WS error:', err.message);
      this.scheduleReconnect();
    });
  }

  private send(obj: Record<string, unknown>): void {
    if (this.ws?.readyState === WebSocket.OPEN) {
      this.ws.send(JSON.stringify(obj));
    }
  }

  private startHeartbeat(): void {
    this.heartbeatTimer = setInterval(() => {
      const snap = this.link.snapshot();
      this.send({
        type: 'bridge:heartbeat',
        payload: { bridgeId: this.config.bridgeId, linkPeers: snap.linkPeers }
      });
    }, 2000);
  }

  private emitClockState(): void {
    const snap = this.link.snapshot();
    this.send({ type: 'bridge:clock:state', payload: snap });
  }

  private scheduleReconnect(): void {
    if (!this.running) return;
    if (this.heartbeatTimer) clearInterval(this.heartbeatTimer);
    if (this.linkTimer) clearInterval(this.linkTimer);
    this.heartbeatTimer = null;
    this.linkTimer = null;
    const delay = Math.min(30000, 1000 * Math.pow(2, this.reconnectAttempt++));
    console.log(`[bridge] Reconnect in ${delay}ms`);
    setTimeout(() => {
      if (this.running) this.connect();
    }, delay);
  }
}
