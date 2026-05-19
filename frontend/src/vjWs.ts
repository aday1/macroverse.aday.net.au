import { getVjSessionId } from './vjSession.js';

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

export function connectVjSession(): void {
  if (socket && (socket.readyState === WebSocket.OPEN || socket.readyState === WebSocket.CONNECTING)) {
    return;
  }
  const sessionId = getVjSessionId();
  socket = new WebSocket(wsUrl());
  socket.onopen = () => {
    send({ type: 'auth', role: 'client', sessionId });
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
      }
      if (msg.type === 'vj:control' && msg.control) {
        applyRemoteControl(msg.control as VjControlState);
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

export function publishVjControl(patch: Partial<VjControlState>): void {
  if (remoteApply) return;
  send({ type: 'vj:control', sessionId: getVjSessionId(), payload: patch });
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
