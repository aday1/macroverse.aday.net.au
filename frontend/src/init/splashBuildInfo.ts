import { fetchVersion } from '../api.js';
import type { VersionResponse } from '../types.js';

export interface DeployMeta {
  last_build_at?: string;
  last_deployed_at?: string;
  build_date?: string;
  last_git_sha_short?: string;
  version?: string;
  branch?: string;
  track?: string;
}

let cachedMeta: DeployMeta | null = null;
let cachedVersion: VersionResponse | null = null;
let ageTicker: ReturnType<typeof setInterval> | null = null;

function isoMs(iso: string): number | null {
  if (!iso) return null;
  const normalized = iso.includes('T') ? iso : iso.replace(' ', 'T');
  const t = new Date(normalized).getTime();
  return Number.isNaN(t) ? null : t;
}

export function formatDeployTimestamp(iso: string): string {
  if (!iso) return '(unknown)';
  try {
    const normalized = iso.includes('T') ? iso : iso.replace(' ', 'T');
    const d = new Date(normalized);
    if (Number.isNaN(d.getTime())) return iso;
    const main = d.toLocaleString(undefined, {
      dateStyle: 'medium',
      timeStyle: 'short',
      timeZoneName: 'short',
    });
    const tz = Intl.DateTimeFormat().resolvedOptions().timeZone;
    return tz ? `${main} (${tz})` : main;
  } catch {
    return iso;
  }
}

export function ageSince(iso: string, kind: 'build' | 'deploy'): string {
  const t = isoMs(iso);
  if (t === null) return '';
  const tag = kind === 'deploy' ? 'deploy' : 'build';
  const sec = Math.round((Date.now() - t) / 1000);
  if (sec < -120) return '';
  if (sec < 45) return 'just now';
  if (sec < 3600) return `${Math.round(sec / 60)}m since ${tag}`;
  if (sec < 86400) return `${Math.round(sec / 3600)}h since ${tag}`;
  if (sec < 604800) return `${Math.round(sec / 86400)}d since ${tag}`;
  if (sec < 4233600) return `${Math.round(sec / 604800)}wk since ${tag}`;
  const mo = Math.round(sec / (30.4375 * 86400));
  if (mo < 24) return `${mo}mo since ${tag}`;
  return `${Math.round(sec / (365.2425 * 86400))}yr since ${tag}`;
}

export async function fetchDeployMeta(): Promise<DeployMeta | null> {
  try {
    const r = await fetch('/deploy-meta.json', { cache: 'no-store' });
    if (!r.ok) return null;
    return (await r.json()) as DeployMeta;
  } catch {
    return null;
  }
}

function buildIso(meta: DeployMeta | null, version?: VersionResponse | null): string {
  return meta?.last_build_at || meta?.build_date || version?.buildDate || '';
}

function deployIso(meta: DeployMeta | null, version?: VersionResponse | null): string {
  return meta?.last_deployed_at || meta?.last_build_at || meta?.build_date || version?.buildDate || '';
}

function shaShort(meta: DeployMeta | null, version?: VersionResponse | null): string {
  return meta?.last_git_sha_short || meta?.version || version?.gitRev || version?.version || '';
}

export function applyBuildInfoToUi(meta: DeployMeta | null, version: VersionResponse | null): void {
  cachedMeta = meta;
  cachedVersion = version;

  const buildIsoStr = buildIso(meta, version);
  const deployIsoStr = deployIso(meta, version);
  const sha = shaShort(meta, version);

  const verEl = document.getElementById('splashVersion');
  if (verEl && version) {
    const tag = version.releaseTag ? ` · ${version.releaseTag}` : '';
    verEl.textContent =
      (version.gitRev || version.version || 'dev') + (version.gitDirty ? ' [dirty]' : '') + tag;
  }

  const buildTimeEl = document.getElementById('splashBuildTime');
  if (buildTimeEl) {
    buildTimeEl.textContent = buildIsoStr ? formatDeployTimestamp(buildIsoStr) : '(unknown)';
  }

  const shaEl = document.getElementById('splashBuildSha');
  if (shaEl) {
    shaEl.textContent = sha ? sha.slice(0, 7) + (version?.gitDirty ? '+' : '') : '';
  }

  const ageEl = document.getElementById('splashDeployAge');
  if (ageEl) {
    const parts: string[] = [];
    const deployAge = ageSince(deployIsoStr, 'deploy');
    const buildAge = ageSince(buildIsoStr, 'build');
    if (deployAge) parts.push(deployAge);
    if (buildAge && buildAge !== deployAge) parts.push(buildAge);
    ageEl.textContent = parts.join(' · ');
  }

  const splashBuild = document.getElementById('splashBuild');
  if (splashBuild && buildIsoStr) {
    splashBuild.title =
      'Last build ' +
      formatDeployTimestamp(buildIsoStr) +
      (sha ? '\nCommit ' + sha + (version?.gitDirty ? ' (dirty)' : '') : '');
  }

  const tEl = document.getElementById('appBarBuildTime');
  const sEl = document.getElementById('appBarBuildSha');
  const bEl = document.getElementById('appBarBuild');
  if (tEl) {
    const raw = buildIsoStr || '';
    tEl.textContent = raw ? formatDeployTimestamp(raw) : new Date().toISOString().slice(0, 16).replace('T', ' ');
    if (bEl) {
      bEl.title =
        'Last build ' +
        (raw ? formatDeployTimestamp(raw) : 'unknown') +
        (sha ? '\nCommit ' + sha + (version?.gitDirty ? ' (dirty)' : '') : '');
    }
  }
  if (sEl && sha) sEl.textContent = sha.slice(0, 7) + (version?.gitDirty ? '+' : '');
}

function refreshAgesOnly(): void {
  if (document.getElementById('splashOverlay')?.classList.contains('hidden')) {
    if (ageTicker) {
      clearInterval(ageTicker);
      ageTicker = null;
    }
    return;
  }
  applyBuildInfoToUi(cachedMeta, cachedVersion);
}

export function startSplashBuildAgeTicker(): void {
  if (ageTicker) return;
  ageTicker = setInterval(refreshAgesOnly, 60000);
}

export async function initSplashBuildInfo(): Promise<VersionResponse | null> {
  const [meta, version] = await Promise.all([fetchDeployMeta(), fetchVersion().catch(() => null)]);
  applyBuildInfoToUi(meta, version);
  startSplashBuildAgeTicker();
  return version;
}
