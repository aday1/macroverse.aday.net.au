export interface VjTokenPayloadParsed {
  role: string;
  sessionId: string;
  exp: number;
}

export function parseVjTokenPayload(token: string): VjTokenPayloadParsed | null {
  const dot = token.indexOf('.');
  if (dot <= 0) return null;
  try {
    let b64 = token.slice(0, dot).replace(/-/g, '+').replace(/_/g, '/');
    while (b64.length % 4) b64 += '=';
    const p = JSON.parse(atob(b64)) as { role?: string; sessionId?: string; exp?: number };
    if (!p.sessionId) return null;
    return {
      role: p.role || '',
      sessionId: p.sessionId.trim().slice(0, 64) || 'default',
      exp: p.exp || 0,
    };
  } catch {
    return null;
  }
}
