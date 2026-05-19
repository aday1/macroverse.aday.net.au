import { getVjSessionId } from './vjSession.js';

const CONTROL_KEY_PREFIX = 'macroverse-vj-control-token:';
const VIEWER_KEY_PREFIX = 'macroverse-vj-viewer-token:';

export interface VjTokenPair {
  sessionId: string;
  viewerToken: string;
  controlToken: string;
}

function storageKey(prefix: string, sessionId: string): string {
  return prefix + sessionId.trim().slice(0, 64) || 'default';
}

export function getStoredControlToken(sessionId?: string): string | null {
  const sid = (sessionId ?? getVjSessionId()).trim().slice(0, 64) || 'default';
  try {
    return localStorage.getItem(storageKey(CONTROL_KEY_PREFIX, sid));
  } catch {
    return null;
  }
}

export function getStoredViewerToken(sessionId?: string): string | null {
  const sid = (sessionId ?? getVjSessionId()).trim().slice(0, 64) || 'default';
  try {
    return localStorage.getItem(storageKey(VIEWER_KEY_PREFIX, sid));
  } catch {
    return null;
  }
}

function storeTokens(pair: VjTokenPair): void {
  try {
    localStorage.setItem(storageKey(CONTROL_KEY_PREFIX, pair.sessionId), pair.controlToken);
    localStorage.setItem(storageKey(VIEWER_KEY_PREFIX, pair.sessionId), pair.viewerToken);
  } catch {
    /* ignore */
  }
}

export function storeControlToken(sessionId: string, controlToken: string): void {
  try {
    const sid = sessionId.trim().slice(0, 64) || 'default';
    localStorage.setItem(storageKey(CONTROL_KEY_PREFIX, sid), controlToken);
  } catch {
    /* ignore */
  }
}

import { parseVjTokenPayload } from './vjTokenParse.js';

export { parseVjTokenPayload } from './vjTokenParse.js';

export function getControlTokenFromUrl(): string | null {
  try {
    const t = new URLSearchParams(window.location.search).get('controlToken');
    return t && t.trim() ? t.trim() : null;
  } catch {
    return null;
  }
}

/** Apply operator token from join URL; returns session id if valid shape. */
export function applyControlTokenFromUrl(): string | null {
  const token = getControlTokenFromUrl();
  if (!token) return null;
  const payload = parseVjTokenPayload(token);
  if (!payload || payload.role !== 'operator') return null;
  if (payload.exp && payload.exp < Date.now()) return null;
  storeControlToken(payload.sessionId, token);
  return payload.sessionId;
}

/** Mint or refresh viewer + operator tokens for this gig session. */
export async function ensureVjTokens(sessionId?: string): Promise<VjTokenPair> {
  const sid = (sessionId ?? getVjSessionId()).trim().slice(0, 64) || 'default';
  const existingControl = getStoredControlToken(sid);
  const res = await fetch('/api/vj/tokens', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      sessionId: sid,
      ...(existingControl ? { controlToken: existingControl } : {}),
    }),
  });
  if (!res.ok) {
    throw new Error(`vj tokens: ${res.status}`);
  }
  const data = (await res.json()) as VjTokenPair;
  storeTokens(data);
  return data;
}

export function vjControlQuery(sessionId?: string): string {
  const token = getStoredControlToken(sessionId);
  if (token) {
    return `controlToken=${encodeURIComponent(token)}`;
  }
  return `sessionId=${encodeURIComponent((sessionId ?? getVjSessionId()).trim().slice(0, 64) || 'default')}`;
}

export function vjViewQuery(sessionId?: string): string {
  const token = getStoredViewerToken(sessionId);
  if (token) {
    return `viewToken=${encodeURIComponent(token)}`;
  }
  return `sessionId=${encodeURIComponent((sessionId ?? getVjSessionId()).trim().slice(0, 64) || 'default')}`;
}

/** viewToken from vj-output.html URL (audience QR). */
export function vjJoinQuery(sessionId?: string): string {
  const token = getStoredControlToken(sessionId);
  if (token) {
    return `controlToken=${encodeURIComponent(token)}`;
  }
  return `sessionId=${encodeURIComponent((sessionId ?? getVjSessionId()).trim().slice(0, 64) || 'default')}`;
}

export function getViewTokenFromUrl(): string | null {
  try {
    const t = new URLSearchParams(window.location.search).get('viewToken');
    return t && t.trim() ? t.trim() : null;
  } catch {
    return null;
  }
}
