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

function clearStoredTokens(sessionId: string): void {
  try {
    localStorage.removeItem(storageKey(CONTROL_KEY_PREFIX, sessionId));
    localStorage.removeItem(storageKey(VIEWER_KEY_PREFIX, sessionId));
  } catch {
    /* ignore */
  }
}

function usableControlToken(sessionId: string): string | null {
  const token = getStoredControlToken(sessionId);
  if (!token) return null;
  const payload = parseVjTokenPayload(token);
  if (!payload || payload.role !== 'operator') {
    clearStoredTokens(sessionId);
    return null;
  }
  if (payload.sessionId !== sessionId) {
    clearStoredTokens(sessionId);
    return null;
  }
  if (payload.exp && payload.exp < Date.now()) {
    clearStoredTokens(sessionId);
    return null;
  }
  return token;
}

function usableViewerToken(sessionId: string): string | null {
  const token = getStoredViewerToken(sessionId);
  if (!token) return null;
  const payload = parseVjTokenPayload(token);
  if (!payload || payload.role !== 'viewer') {
    return null;
  }
  if (payload.sessionId !== sessionId) {
    return null;
  }
  if (payload.exp && payload.exp < Date.now()) {
    return null;
  }
  return token;
}

const mintInflight = new Map<string, Promise<VjTokenPair>>();
const VJ_TOKEN_DEPLOY_KEY = 'macroverse-vj-token-deploy';

export function clearVjSessionTokens(sessionId?: string): void {
  const sid = (sessionId ?? getVjSessionId()).trim().slice(0, 64) || 'default';
  clearStoredTokens(sid);
}

/** Drop stored tokens when a new build is deployed (HMAC secret may have rotated). */
export function invalidateVjTokensIfDeployChanged(deploySha: string): void {
  const sha = deploySha.trim().slice(0, 64);
  if (!sha) return;
  try {
    const prev = localStorage.getItem(VJ_TOKEN_DEPLOY_KEY);
    if (prev && prev !== sha) {
      for (let i = localStorage.length - 1; i >= 0; i--) {
        const key = localStorage.key(i);
        if (!key) continue;
        if (key.startsWith(CONTROL_KEY_PREFIX) || key.startsWith(VIEWER_KEY_PREFIX)) {
          localStorage.removeItem(key);
        }
      }
    }
    localStorage.setItem(VJ_TOKEN_DEPLOY_KEY, sha);
  } catch {
    /* ignore */
  }
}

export async function syncVjTokensWithDeployMeta(): Promise<void> {
  try {
    const r = await fetch('/deploy-meta.json', { cache: 'no-store' });
    if (!r.ok) return;
    const meta = (await r.json()) as { last_git_sha_short?: string; version?: string };
    const sha = meta.last_git_sha_short || meta.version || '';
    invalidateVjTokensIfDeployChanged(sha);
  } catch {
    /* ignore */
  }
}

/** Mint or refresh viewer + operator tokens for this gig session. */
export async function ensureVjTokens(sessionId?: string): Promise<VjTokenPair> {
  const sid = (sessionId ?? getVjSessionId()).trim().slice(0, 64) || 'default';
  const pending = mintInflight.get(sid);
  if (pending) return pending;

  const existingControl = usableControlToken(sid);
  const existingViewer = usableViewerToken(sid);
  if (existingControl && existingViewer) {
    return {
      sessionId: sid,
      controlToken: existingControl,
      viewerToken: existingViewer,
    };
  }

  const work = (async (): Promise<VjTokenPair> => {
    async function requestTokens(): Promise<VjTokenPair> {
      const existingControl = usableControlToken(sid);
      const existingViewer = usableViewerToken(sid);
      const res = await fetch('/api/vj/tokens', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          sessionId: sid,
          ...(existingControl ? { controlToken: existingControl } : {}),
          ...(existingViewer ? { viewerToken: existingViewer } : {}),
        }),
      });
      if (!res.ok) {
        throw new Error(`vj tokens: ${res.status}`);
      }
      const data = (await res.json()) as VjTokenPair;
      storeTokens(data);
      return data;
    }

    try {
      return await requestTokens();
    } catch (err) {
      if (err instanceof Error && err.message.includes('403') && getStoredControlToken(sid)) {
        clearStoredTokens(sid);
        return await requestTokens();
      }
      throw err;
    }
  })();

  mintInflight.set(sid, work);
  try {
    return await work;
  } finally {
    mintInflight.delete(sid);
  }
}

export function vjControlQuery(sessionId?: string): string {
  const sid = (sessionId ?? getVjSessionId()).trim().slice(0, 64) || 'default';
  const token = usableControlToken(sid);
  if (token) {
    return `controlToken=${encodeURIComponent(token)}`;
  }
  return `sessionId=${encodeURIComponent(sid)}`;
}

export function vjViewQuery(sessionId?: string): string {
  const sid = (sessionId ?? getVjSessionId()).trim().slice(0, 64) || 'default';
  const token = usableViewerToken(sid);
  if (token) {
    return `viewToken=${encodeURIComponent(token)}`;
  }
  return `sessionId=${encodeURIComponent(sid)}`;
}

/** viewToken from vj-output.html URL (audience QR). */
export function vjJoinQuery(sessionId?: string): string {
  const sid = (sessionId ?? getVjSessionId()).trim().slice(0, 64) || 'default';
  const token = usableControlToken(sid);
  if (token) {
    return `controlToken=${encodeURIComponent(token)}`;
  }
  return `sessionId=${encodeURIComponent(sid)}`;
}

export function getViewTokenFromUrl(): string | null {
  try {
    const t = new URLSearchParams(window.location.search).get('viewToken');
    return t && t.trim() ? t.trim() : null;
  } catch {
    return null;
  }
}
