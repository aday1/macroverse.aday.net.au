import { fetchLocalStatus } from './api.js';
import { setGigUrlOriginOverride } from './gigQr.js';

let checked = false;
let inflight: Promise<void> | null = null;

function isLoopbackHost(host: string): boolean {
  const h = host.toLowerCase();
  return h === 'localhost' || h === '::1' || h === '[::1]' || /^127(?:\.|$)/.test(h);
}

export async function refreshGigUrlOriginOverride(): Promise<void> {
  if (checked) return;
  if (inflight) return inflight;
  inflight = (async () => {
    try {
      if (typeof window === 'undefined' || !isLoopbackHost(window.location.hostname)) {
        setGigUrlOriginOverride('');
        return;
      }
      const status = await fetchLocalStatus();
      const lanOrigin = status?.lanUrls?.find((url) => /^https?:\/\//i.test(url));
      setGigUrlOriginOverride(lanOrigin || '');
    } catch {
      setGigUrlOriginOverride('');
    } finally {
      checked = true;
      inflight = null;
    }
  })();
  return inflight;
}
