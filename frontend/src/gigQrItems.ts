import {
  buildGigAudienceStreamUrl,
  buildGigJoinUrl,
  buildGigOutputUrl,
  buildGigVrAudienceUrl,
  buildGigVrControllerUrl,
} from './gigQr.js';

export interface GigQrItem {
  id: string;
  label: string;
  hint: string;
  buildUrl: (sessionId: string) => string;
}

export interface GigManualUrlItem extends GigQrItem {
  manualOnly?: boolean;
}

export const GIG_QR_ITEMS: GigQrItem[] = [
  {
    id: 'join',
    label: 'VJ collaboration',
    hint: 'Full desk on another laptop (controlToken)',
    buildUrl: (sid) => buildGigJoinUrl(sid),
  },
  {
    id: 'audience',
    label: 'Audience stream',
    hint: 'Phone/tablet flat stream + optional touch X/Y',
    buildUrl: (sid) => buildGigAudienceStreamUrl(sid),
  },
  {
    id: 'vr-audience',
    label: 'VR audience',
    hint: 'WebXR dome on Quest / headset',
    buildUrl: (sid) => buildGigVrAudienceUrl(sid, 'dome'),
  },
  {
    id: 'vr-vj',
    label: 'VR VJ controller',
    hint: 'Immersive remote desk + live code push',
    buildUrl: (sid) => buildGigVrControllerUrl(sid, 'dome'),
  },
];

export const GIG_MANUAL_URL_ITEMS: GigManualUrlItem[] = [
  {
    id: 'projector',
    label: 'Projector / OBS',
    hint: 'Clean view-only HDMI or browser source',
    buildUrl: (sid) => buildGigOutputUrl(sid),
    manualOnly: true,
  },
  ...GIG_QR_ITEMS,
];

/** Splash screen canvas element ids keyed by GIG_QR_ITEMS id */
export const SPLASH_QR_CANVAS_IDS: Record<string, string> = {
  join: 'splashVjQrJoin',
  audience: 'splashVjQrAudience',
  'vr-audience': 'splashVjQrVrAudience',
  'vr-vj': 'splashVjQrVrVj',
};

export function copyGigUrl(url: string, btn: HTMLButtonElement): void {
  void navigator.clipboard?.writeText(url).then(() => {
    const prev = btn.textContent;
    btn.textContent = 'Copied';
    window.setTimeout(() => {
      btn.textContent = prev;
    }, 1500);
  });
}
