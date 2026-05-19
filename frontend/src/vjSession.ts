const VJ_SESSION_STORAGE_KEY = 'macroverse-vj-session-id';

export function getVjSessionId(): string {
  try {
    const fromUrl = typeof window !== 'undefined'
      ? new URLSearchParams(window.location.search).get('sessionId')
      : null;
    if (fromUrl && fromUrl.trim()) {
      return fromUrl.trim().slice(0, 64);
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
  } catch {
    /* ignore */
  }
}

export function vjSessionQuery(): string {
  return `sessionId=${encodeURIComponent(getVjSessionId())}`;
}
