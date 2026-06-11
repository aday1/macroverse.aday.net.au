import { getVjSessionId, isVjViewOnlyMode } from './vjSession.js';
import { getStoredControlToken } from './vjTokens.js';

export interface VjControlState {
  crossfader: number;
  mixMode: string;
  deckAGlobalIndex: number;
  deckBGlobalIndex: number;
  pageA: number;
  pageB: number;
  paramsA?: Record<string, number | boolean>;
  paramsB?: Record<string, number | boolean>;
  flipV: boolean;
  flipH: boolean;
  rotation: number;
  autoVjEnabled: boolean;
  autoVjBpm: number;
}

type ControlHandler = (control: VjControlState) => void;
type ClockHandler = (state: {
  bpm: number;
  beat: number;
  bar: number;
  isPlaying: boolean;
  linkPeers: number;
}) => void;

let socket: WebSocket | null = null;
let connected = false;
let isLeader = false;
let remoteApply = false;
const controlHandlers: ControlHandler[] = [];
const clockHandlers: ClockHandler[] = [];
type ConfigHandler = (cfg: { audienceParticipation: boolean }) => void;
type AudienceMouseHandler = (mouseX: number, mouseY: number) => void;
const configHandlers: ConfigHandler[] = [];
const audienceMouseHandlers: AudienceMouseHandler[] = [];

function wsUrl(): string {
  const proto = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
  const host = window.location.host;
  return `${proto}//${host}/ws`;
}

function send(msg: Record<string, unknown>): void {
  if (!socket || socket.readyState !== WebSocket.OPEN) return;
  socket.send(JSON.stringify(msg));
}

export function reconnectVjSession(): void {
  if (socket) {
    socket.onclose = null;
    socket.close();
    socket = null;
  }
  connected = false;
  connectVjSession();
}

type ShaderLiveHandler = (detail: { deck: 'A' | 'B'; path: string; source: string }) => void;
const shaderLiveHandlers: ShaderLiveHandler[] = [];

export function connectVjSession(): void {
  if (isVjViewOnlyMode()) return;
  if (socket && (socket.readyState === WebSocket.OPEN || socket.readyState === WebSocket.CONNECTING)) {
    return;
  }
  const sessionId = getVjSessionId();
  const controlToken = getStoredControlToken(sessionId);
  socket = new WebSocket(wsUrl());
  socket.onopen = () => {
    if (controlToken) {
      send({ type: 'auth', role: 'client', token: controlToken, sessionId });
    } else {
      send({ type: 'auth', role: 'client', sessionId });
    }
    send({ type: 'session:join', sessionId });
    send({ type: 'vj:claim-leader' });
  };
  socket.onmessage = (ev) => {
    try {
      const msg = JSON.parse(ev.data as string) as Record<string, unknown>;
      if (msg.type === 'auth:ok') {
        connected = true;
      }
      if (msg.type === 'session:joined') {
        isLeader = Boolean(msg.leader);
        const control = msg.control as VjControlState | undefined;
        if (control) applyRemoteControl(control);
        if (typeof msg.audienceParticipation === 'boolean') {
          applyVjConfig({ audienceParticipation: msg.audienceParticipation });
        }
      }
      if (msg.type === 'vj:config' && typeof msg.audienceParticipation === 'boolean') {
        applyVjConfig({ audienceParticipation: msg.audienceParticipation });
      }
      if (msg.type === 'vj:audience-mouse') {
        const mx = Number(msg.mouseX);
        const my = Number(msg.mouseY);
        if (Number.isFinite(mx) && Number.isFinite(my)) {
          for (const h of audienceMouseHandlers) h(mx, my);
        }
      }
      if (msg.type === 'vj:control' && msg.control) {
        applyRemoteControl(msg.control as VjControlState);
      }
      if (msg.type === 'vj:shader-live') {
        const deck = msg.deck === 'B' ? 'B' : 'A';
        const path = typeof msg.path === 'string' ? msg.path : '';
        const source = typeof msg.source === 'string' ? msg.source : '';
        if (path && source) {
          for (const h of shaderLiveHandlers) h({ deck, path, source });
        }
      }
      if (msg.type === 'clock:state') {
        for (const h of clockHandlers) {
          h({
            bpm: Number(msg.bpm) || 120,
            beat: Number(msg.beat) || 1,
            bar: Number(msg.bar) || 1,
            isPlaying: Boolean(msg.isPlaying),
            linkPeers: Number(msg.linkPeers) || 0
          });
        }
      }
    } catch {
      /* ignore */
    }
  };
  socket.onclose = () => {
    connected = false;
    setTimeout(() => connectVjSession(), 3000);
  };
}

export function publishVjShaderLive(deck: 'A' | 'B', path: string, source: string): void {
  if (isVjViewOnlyMode()) return;
  send({
    type: 'vj:shader-live',
    sessionId: getVjSessionId(),
    payload: { deck, path: path.replace(/\\/g, '|'), source },
  });
}

export function onVjShaderLive(handler: ShaderLiveHandler): () => void {
  shaderLiveHandlers.push(handler);
  return () => {
    const i = shaderLiveHandlers.indexOf(handler);
    if (i >= 0) shaderLiveHandlers.splice(i, 1);
  };
}

export function publishVjControl(patch: Partial<VjControlState>): void {
  if (remoteApply || isVjViewOnlyMode()) return;
  send({ type: 'vj:control', sessionId: getVjSessionId(), payload: patch });
}

export function publishVjConfig(cfg: { audienceParticipation: boolean }): void {
  if (isVjViewOnlyMode()) return;
  send({ type: 'vj:config', sessionId: getVjSessionId(), payload: cfg });
}

function applyVjConfig(cfg: { audienceParticipation: boolean }): void {
  for (const h of configHandlers) h(cfg);
}

export function onVjConfig(handler: ConfigHandler): () => void {
  configHandlers.push(handler);
  return () => {
    const i = configHandlers.indexOf(handler);
    if (i >= 0) configHandlers.splice(i, 1);
  };
}

export function onAudienceMouse(handler: AudienceMouseHandler): () => void {
  audienceMouseHandlers.push(handler);
  return () => {
    const i = audienceMouseHandlers.indexOf(handler);
    if (i >= 0) audienceMouseHandlers.splice(i, 1);
  };
}

export function onRemoteVjControl(handler: ControlHandler): () => void {
  controlHandlers.push(handler);
  return () => {
    const i = controlHandlers.indexOf(handler);
    if (i >= 0) controlHandlers.splice(i, 1);
  };
}

export function onBridgeClock(handler: ClockHandler): () => void {
  clockHandlers.push(handler);
  return () => {
    const i = clockHandlers.indexOf(handler);
    if (i >= 0) clockHandlers.splice(i, 1);
  };
}

export function requestSessionClock(): void {
  send({ type: 'clock:request', sessionId: getVjSessionId() });
}

function applyRemoteControl(control: VjControlState): void {
  remoteApply = true;
  try {
    for (const h of controlHandlers) h(control);
  } finally {
    remoteApply = false;
  }
}

export function isVjWsConnected(): boolean {
  return connected;
}

export function isVjSessionLeader(): boolean {
  return isLeader;
}
