import { mountGigAudienceDisplayPage } from './gigAudienceDisplay.js';
import { getVjSessionId, persistVjSessionFromUrl } from './vjSession.js';

persistVjSessionFromUrl();

const params = new URLSearchParams(window.location.search);
const sessionId = params.get('sessionId')?.trim().slice(0, 64) || getVjSessionId();
const mixParam = params.get('mix');
const mix = mixParam != null ? Math.max(0, Math.min(1, Number(mixParam) / 100)) : undefined;

const host = document.getElementById('gigAudienceHost');
if (host) {
  mountGigAudienceDisplayPage(host, sessionId, {
    mix,
    qrVisible: true,
  });
}

window.addEventListener('load', () => {
  void document.documentElement.requestFullscreen?.().catch(() => {});
});
