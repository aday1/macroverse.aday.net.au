import { parseVjTokenPayload } from './vjTokenParse.js';

const VJ_SESSION_STORAGE_KEY = 'macroverse-vj-session-id';
const VJ_VIEW_ONLY_KEY = 'macroverse-vj-view-only';
const CONTROL_KEY_PREFIX = 'macroverse-vj-control-token:';

function hasControlTokenForSession(sessionId: string): boolean {
  try {
    const sid = sessionId.trim().slice(0, 64) || 'default';
    return !!localStorage.getItem(`${CONTROL_KEY_PREFIX}${sid}`);
  } catch {
    return false;
  }
}

export function getVjSessionId(): string {
  try {
    if (typeof window !== 'undefined') {
      const params = new URLSearchParams(window.location.search);
      const controlToken = params.get('controlToken');
      if (controlToken?.trim()) {
        const payload = parseVjTokenPayload(controlToken.trim());
        if (payload?.sessionId) return payload.sessionId;
      }
      const fromUrl = params.get('sessionId');
      if (fromUrl && fromUrl.trim()) {
        return fromUrl.trim().slice(0, 64);
      }
    }
    const stored = localStorage.getItem(VJ_SESSION_STORAGE_KEY);
    if (stored && stored.trim()) {
      return stored.trim().slice(0, 64);
    }
  } catch {
    /* ignore */
  }
  return 'default';
}

export function setVjSessionId(sessionId: string): void {
  const sid = sessionId.trim().slice(0, 64) || 'default';
  try {
    localStorage.setItem(VJ_SESSION_STORAGE_KEY, sid);
    localStorage.removeItem(VJ_VIEW_ONLY_KEY);
  } catch {
    /* ignore */
  }
  if (typeof window !== 'undefined') {
    window.dispatchEvent(
      new CustomEvent('macroverse-vj-session-changed', { detail: { sessionId: sid } })
    );
  }
}

export function isVjViewOnlyMode(): boolean {
  try {
    if (localStorage.getItem(VJ_VIEW_ONLY_KEY) === '1') return true;
    if (typeof window === 'undefined') return false;
    const params = new URLSearchParams(window.location.search);
    if (params.get('viewToken')) return true;
    const path = window.location.pathname || '';
    if (path.includes('vj-output.html')) return true;
    if (path.includes('vj-vr.html')) return true;
    if (params.get('controlToken')?.trim()) return false;
    const sessionId = params.get('sessionId');
    if (!sessionId?.trim()) return false;
    return !hasControlTokenForSession(sessionId);
  } catch {
    return false;
  }
}

export function setVjViewOnlyMode(enabled: boolean): void {
  try {
    if (enabled) localStorage.setItem(VJ_VIEW_ONLY_KEY, '1');
    else localStorage.removeItem(VJ_VIEW_ONLY_KEY);
  } catch {
    /* ignore */
  }
}

export function persistVjSessionFromUrl(): string | null {
  try {
    const params = new URLSearchParams(window.location.search);
    if (params.get('viewToken')) {
      setVjViewOnlyMode(true);
      return null;
    }
    const controlToken = params.get('controlToken');
    if (controlToken?.trim()) {
      const parsed = parseVjTokenPayload(controlToken.trim());
      if (parsed?.role === 'operator' && parsed.sessionId) {
        if (!parsed.exp || parsed.exp >= Date.now()) {
          localStorage.setItem(`${CONTROL_KEY_PREFIX}${parsed.sessionId}`, controlToken.trim());
          localStorage.setItem(VJ_SESSION_STORAGE_KEY, parsed.sessionId);
          localStorage.removeItem(VJ_VIEW_ONLY_KEY);
          return parsed.sessionId;
        }
      }
    }
    const fromUrl = params.get('sessionId');
    if (fromUrl && fromUrl.trim()) {
      const sid = fromUrl.trim().slice(0, 64);
      localStorage.setItem(VJ_SESSION_STORAGE_KEY, sid);
      setVjViewOnlyMode(!hasControlTokenForSession(sid));
      return sid;
    }
  } catch {
    /* ignore */
  }
  return null;
}

export function vjSessionQuery(): string {
  return `sessionId=${encodeURIComponent(getVjSessionId())}`;
}
