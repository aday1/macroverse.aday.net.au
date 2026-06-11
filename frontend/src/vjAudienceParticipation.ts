import { getVjSessionId } from './vjSession.js';
import { vjControlQuery, vjViewQuery } from './vjTokens.js';

const STORAGE_KEY = 'macroverse-vj-audience-participation';

export function getAudienceParticipationEnabled(): boolean {
  try {
    return localStorage.getItem(STORAGE_KEY) === '1';
  } catch {
    return false;
  }
}

export function setAudienceParticipationEnabled(enabled: boolean): void {
  try {
    if (enabled) localStorage.setItem(STORAGE_KEY, '1');
    else localStorage.removeItem(STORAGE_KEY);
  } catch {
    /* ignore */
  }
  window.dispatchEvent(
    new CustomEvent('macroverse-vj-audience-participation', { detail: { enabled } })
  );
}

export async function pushAudienceParticipation(enabled: boolean): Promise<void> {
  setAudienceParticipationEnabled(enabled);
  const q = vjControlQuery();
  await fetch(`/api/vj/session-config?${q}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ audienceParticipation: enabled }),
  });
}

export async function fetchAudienceParticipationConfig(): Promise<boolean> {
  const viewQ = vjViewQuery();
  const res = await fetch(`/api/vj/session-config?${viewQ}`);
  if (!res.ok) return false;
  const data = (await res.json()) as { audienceParticipation?: boolean };
  return Boolean(data.audienceParticipation);
}

export function audienceMouseQuery(): string {
  const viewQ = vjViewQuery();
  if (viewQ) return viewQ;
  return `sessionId=${encodeURIComponent(getVjSessionId())}`;
}
