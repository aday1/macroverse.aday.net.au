export type HostMode = 'cloud' | 'desktop';

export type HostCapabilities = {
  desktopShell?: boolean;
  cursorAgent?: boolean;
  localAgents?: boolean;
  wirePipeline?: boolean;
  videoOutput?: boolean;
};

export type ServerConfig = {
  readonly?: boolean;
  demo?: boolean;
  localBrowserStore?: boolean;
  hostMode?: HostMode;
  capabilities?: HostCapabilities;
};

let hostMode: HostMode = 'desktop';
let readonlyHost = false;
let localBrowserStore = false;
let capabilities: HostCapabilities = {
  desktopShell: true,
  cursorAgent: true,
  localAgents: true,
  wirePipeline: true,
  videoOutput: true,
};
let configLoaded = false;

export function isHostConfigLoaded(): boolean {
  return configLoaded;
}

export function getHostMode(): HostMode {
  return hostMode;
}

export function isDesktopHost(): boolean {
  return hostMode === 'desktop';
}

export function isCloudHost(): boolean {
  return hostMode === 'cloud';
}

export function isReadonlyHost(): boolean {
  return readonlyHost;
}

/** Server filesystem writes blocked; edits go to IndexedDB in this browser. */
export function usesLocalBrowserStore(): boolean {
  return localBrowserStore;
}

export function hasDesktopShell(): boolean {
  return !!capabilities.desktopShell;
}

export function hasCursorAgent(): boolean {
  return !!capabilities.cursorAgent;
}

export function hasLocalAgents(): boolean {
  return !!capabilities.localAgents;
}

export function hasWirePipeline(): boolean {
  return !!capabilities.wirePipeline;
}

/** Spout, NDI, MacroCam MJPEG — local wire-output / desktop only. */
export function hasVideoOutput(): boolean {
  return !!capabilities.videoOutput;
}

function syncViewSelectForHost(): void {
  const sel = document.getElementById('viewSelect') as HTMLSelectElement | null;
  if (!sel) return;
  for (const opt of Array.from(sel.options)) {
    if (opt.value === 'pipeline' || opt.value === 'wire') {
      const hide = isCloudHost();
      opt.hidden = hide;
      opt.disabled = hide;
    }
  }
  if (isCloudHost() && (sel.value === 'pipeline' || sel.value === 'wire')) {
    sel.value = 'split';
    sel.dispatchEvent(new Event('change', { bubbles: true }));
  }
}

export function applyHostModeClasses(): void {
  const root = document.documentElement;
  root.classList.toggle('host-cloud', isCloudHost());
  root.classList.toggle('host-desktop', isDesktopHost());
}

function initHostBannerExpand(): void {
  const cloudBanner = document.getElementById('cloudLocalBanner');
  if (!cloudBanner || cloudBanner.dataset.bannerExpandInit === '1') return;
  cloudBanner.dataset.bannerExpandInit = '1';
  const toggle = () => cloudBanner.classList.toggle('host-banner--expanded');
  cloudBanner.addEventListener('click', () => toggle());
  cloudBanner.addEventListener('keydown', (e) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      toggle();
    }
  });
}

export function applyDesktopOnlyUI(): void {
  applyHostModeClasses();
  syncViewSelectForHost();
  const cloudBanner = document.getElementById('cloudLocalBanner');
  if (cloudBanner) {
    cloudBanner.style.display = isCloudHost() ? 'flex' : 'none';
    if (isCloudHost()) initHostBannerExpand();
  }
  const demoBanner = document.getElementById('demoBanner');
  if (demoBanner && usesLocalBrowserStore()) {
    demoBanner.style.display = 'flex';
  }
}

export async function loadHostCapabilities(): Promise<ServerConfig> {
  const fallback: ServerConfig = {};
  try {
    const r = await fetch('/api/config', { cache: 'no-store' });
    if (!r.ok) return fallback;
    const cfg = (await r.json()) as ServerConfig;
    if (cfg.hostMode === 'cloud' || cfg.hostMode === 'desktop') {
      hostMode = cfg.hostMode;
    }
    if (cfg.capabilities) {
      capabilities = { ...capabilities, ...cfg.capabilities };
    }
    readonlyHost = !!cfg.readonly;
    localBrowserStore = cfg.localBrowserStore !== false && (!!cfg.readonly || cfg.hostMode === 'cloud');
    configLoaded = true;
    applyDesktopOnlyUI();

    const banner = document.getElementById('demoBanner');
    if (banner && cfg.demo && !usesLocalBrowserStore()) {
      banner.style.display = 'flex';
    }
    return cfg;
  } catch {
    return fallback;
  }
}
