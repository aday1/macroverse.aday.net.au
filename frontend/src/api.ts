import type { SourcesResponse, Settings, VersionResponse, IndexEntry } from './types.js';
import { usesLocalBrowserStore } from './hostCapabilities.js';
import {
  applyLocalDelete,
  applyLocalMove,
  applyLocalRename,
  applyLocalUpdate,
  getLocalShaderContent,
  getLocalSettings,
  getLocalThumbnails,
  mergeIndexWithLocal,
  saveLocalSettings,
  saveLocalShader,
  saveLocalThumbnail,
} from './cloudLocalStore.js';

const INDEX_URL = '/api/index';
const SOURCES_URL = '/api/sources';
const SETTINGS_URL = '/api/settings';
const VERSION_URL = '/api/version';

export interface FetchOpts {
  signal?: AbortSignal;
}

export async function fetchSources(opts?: FetchOpts): Promise<SourcesResponse> {
  const res = await fetch(SOURCES_URL, { cache: 'no-store', signal: opts?.signal });
  if (!res.ok) throw new Error('sources: ' + res.status + ' ' + res.statusText);
  return res.json() as Promise<SourcesResponse>;
}

export async function fetchIndex(opts?: FetchOpts): Promise<IndexEntry[]> {
  const res = await fetch(INDEX_URL, { cache: 'no-store', signal: opts?.signal });
  if (!res.ok) throw new Error('index: ' + res.status + ' ' + res.statusText);
  const data = await res.json();
  const server = Array.isArray(data) ? data : [];
  if (usesLocalBrowserStore()) return mergeIndexWithLocal(server);
  return server;
}

export async function fetchSettings(opts?: FetchOpts): Promise<Settings | null> {
  const res = await fetch(SETTINGS_URL, { cache: 'no-store', signal: opts?.signal });
  if (!res.ok) throw new Error('settings: ' + res.status + ' ' + res.statusText);
  const server = (await res.json()) as Settings;
  if (!usesLocalBrowserStore()) return server;
  const local = await getLocalSettings();
  return { ...server, ...local };
}

export async function fetchVersion(opts?: FetchOpts): Promise<VersionResponse> {
  const res = await fetch(VERSION_URL, { cache: 'no-store', signal: opts?.signal });
  if (!res.ok) return {};
  return res.json() as Promise<VersionResponse>;
}

export async function fetchShader(path: string, opts?: FetchOpts): Promise<string> {
  const p = (path || '').replace(/\\/g, '|');
  if (usesLocalBrowserStore()) {
    const local = await getLocalShaderContent(p);
    if (local != null) return local;
  }
  const res = await fetch('/api/shader?path=' + encodeURIComponent(p), { cache: 'no-store', signal: opts?.signal });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(body && body.trim() ? body.trim() : (res.statusText || 'load failed'));
  }
  return res.text();
}

export interface TextTemplateItem {
  name: string;
  label: string;
}

export async function fetchTextTemplatesList(opts?: FetchOpts): Promise<TextTemplateItem[]> {
  const res = await fetch('/api/templates/text', { cache: 'no-store', signal: opts?.signal });
  if (!res.ok) return [];
  const data = await res.json();
  return Array.isArray(data) ? data : [];
}

export async function fetchTextTemplate(name: string, opts?: FetchOpts): Promise<string> {
  const res = await fetch('/api/templates/text?name=' + encodeURIComponent(name), { cache: 'no-store', signal: opts?.signal });
  if (!res.ok) throw new Error(await res.text() || res.statusText);
  return res.text();
}

export async function fetchAgentStatus(opts?: FetchOpts): Promise<{ online: boolean; cooldownRemainingSec?: number }> {
  const res = await fetch('/api/agent-status', { cache: 'no-store', signal: opts?.signal });
  if (!res.ok) return { online: false };
  const j = await res.json().catch(() => ({}));
  const obj = j as { online?: boolean; cooldownRemainingSec?: number };
  return {
    online: !!(obj && obj.online),
    cooldownRemainingSec: typeof obj.cooldownRemainingSec === 'number' ? obj.cooldownRemainingSec : 0
  };
}

export async function fetchAgentOutput(opts?: FetchOpts): Promise<{ output?: string }> {
  const res = await fetch('/api/agent-output', { cache: 'no-store', signal: opts?.signal });
  if (!res.ok) return { output: '' };
  const j = await res.json().catch(() => ({}));
  return { output: (j && (j as { output?: string }).output) || '' };
}

export async function fetchGithubStatus(opts?: FetchOpts): Promise<{ logged_in: boolean; user?: string; message?: string }> {
  const res = await fetch('/api/github/status', { cache: 'no-store', signal: opts?.signal });
  if (!res.ok) return { logged_in: false, message: res.statusText };
  const j = await res.json().catch(() => ({}));
  const o = j as { logged_in?: boolean; user?: string; message?: string };
  return {
    logged_in: !!(o && o.logged_in),
    user: o.user,
    message: o.message
  };
}

export async function postGithubRun(payload: { args: string[] }, opts?: FetchOpts): Promise<{ stdout: string; exitCode: number }> {
  const res = await fetch('/api/github/run', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
    cache: 'no-store',
    signal: opts?.signal
  });
  if (!res.ok) throw new Error(await res.text() || res.statusText);
  const j = await res.json();
  const o = j as { stdout?: string; exitCode?: number };
  return { stdout: o.stdout || '', exitCode: o.exitCode ?? 1 };
}

export async function postGithubAiFix(payload: { content: string; prompt?: string; path?: string; token?: string }, opts?: FetchOpts): Promise<{ content: string }> {
  const res = await fetch('/api/github/ai/fix', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
    cache: 'no-store',
    signal: opts?.signal
  });
  if (!res.ok) throw new Error(await res.text() || res.statusText);
  const j = await res.json();
  const o = j as { content?: string };
  return { content: o.content ?? '' };
}

export async function fetchThumbnails(opts?: FetchOpts): Promise<Record<string, string>> {
  const res = await fetch('/api/thumbnails', { cache: 'no-store', signal: opts?.signal });
  if (!res.ok) return {};
  const j = await res.json().catch(() => ({}));
  return (j && typeof j === 'object') ? (j as Record<string, string>) : {};
}

export async function fetchThumbnailsBatch(paths: string[], opts?: FetchOpts): Promise<Record<string, string>> {
  if (paths.length === 0) return {};
  const norm = paths.map((p) => (p || '').replace(/\\/g, '|')).filter(Boolean);
  if (norm.length === 0) return {};
  let out: Record<string, string> = {};
  if (usesLocalBrowserStore()) {
    out = await getLocalThumbnails(norm);
  }
  const res = await fetch('/api/thumbnails', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ paths: norm }),
    cache: 'no-store',
    signal: opts?.signal
  });
  if (res.ok) {
    const j = await res.json().catch(() => ({}));
    if (j && typeof j === 'object') out = { ...(j as Record<string, string>), ...out };
  }
  return out;
}

export async function postThumbnailSave(payload: { path: string; dataUrl: string }): Promise<{ ok: boolean }> {
  const path = (payload.path || '').replace(/\\/g, '|');
  if (usesLocalBrowserStore()) {
    await saveLocalThumbnail(path, payload.dataUrl || '');
    return { ok: true };
  }
  const res = await fetch('/api/thumbnail', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ path, dataUrl: payload.dataUrl || '' })
  });
  if (res.status === 403 || res.status === 401) {
    await saveLocalThumbnail(path, payload.dataUrl || '');
    return { ok: true };
  }
  if (!res.ok) throw new Error(res.statusText || 'thumbnail save failed');
  return res.json() as Promise<{ ok: boolean }>;
}

export interface SourcesPostReq {
  action: 'add' | 'remove' | 'replace';
  path?: string;
  index?: number;
  oldPath?: string;
  newPath?: string;
}

export async function postSources(req: SourcesPostReq): Promise<{ ok: boolean; paths: string[] }> {
  const res = await fetch(SOURCES_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(req)
  });
  if (!res.ok) {
    const err = await res.text();
    throw new Error(err || res.statusText);
  }
  return res.json() as Promise<{ ok: boolean; paths: string[] }>;
}

export async function postSettings(s: Partial<Settings>): Promise<{ ok: boolean }> {
  if (usesLocalBrowserStore()) {
    await saveLocalSettings(s);
    return { ok: true };
  }
  const res = await fetch(SETTINGS_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(s)
  });
  if (!res.ok) throw new Error(res.statusText || 'settings save failed');
  return res.json() as Promise<{ ok: boolean }>;
}

export async function postOutputSpout(enable: boolean): Promise<{ ok: boolean }> {
  const res = await fetch('/api/output/spout', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ enable })
  });
  if (!res.ok) return { ok: false };
  return res.json().catch(() => ({ ok: false })) as Promise<{ ok: boolean }>;
}

export async function postOutputNdi(enable: boolean): Promise<{ ok: boolean }> {
  const res = await fetch('/api/output/ndi', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ enable })
  });
  if (!res.ok) return { ok: false };
  return res.json().catch(() => ({ ok: false })) as Promise<{ ok: boolean }>;
}

export async function postOutputMacroCam(enable: boolean): Promise<{ ok: boolean; name?: string; pid?: number; streamUrl?: string }> {
  const res = await fetch('/api/output/macrocam', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ enable })
  });
  if (!res.ok) return { ok: false };
  return res.json().catch(() => ({ ok: false })) as Promise<{ ok: boolean; name?: string; pid?: number; streamUrl?: string }>;
}

let macroCamBaseUrl = '';

export function setMacroCamBaseUrl(url: string): void {
  macroCamBaseUrl = url.replace(/\/stream$/, '').replace(/\/+$/, '');
}

export async function postMacroCamFrame(jpegBlob: Blob): Promise<void> {
  const base = macroCamBaseUrl || '/api/output/macrocam';
  await fetch(base + '/frame', {
    method: 'POST',
    body: jpegBlob,
  });
}

export async function postBrowseFolder(): Promise<{ path: string }> {
  const res = await fetch('/api/browse-folder', { method: 'POST' });
  if (!res.ok) throw new Error(res.statusText || 'browse failed');
  return res.json() as Promise<{ path: string }>;
}

export interface LLMProviderConfig {
  name: string;
  enabled: boolean;
  priority: number;
  model?: string;
  endpoint?: string;
}

export async function fetchLLMStatus(opts?: FetchOpts): Promise<{
  providers: LLMProviderConfig[];
  ollamaOnline: boolean;
  ollamaModels: string[];
  cursorAgent: boolean;
}> {
  const res = await fetch('/api/llm/status', { cache: 'no-store', signal: opts?.signal });
  if (!res.ok) return { providers: [], ollamaOnline: false, ollamaModels: [], cursorAgent: false };
  const j = await res.json().catch(() => ({}));
  return j as { providers: LLMProviderConfig[]; ollamaOnline: boolean; ollamaModels: string[]; cursorAgent: boolean };
}

export async function postLLMConfig(providers: LLMProviderConfig[]): Promise<{ ok: boolean }> {
  const res = await fetch('/api/llm/config', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ providers })
  });
  if (!res.ok) throw new Error(res.statusText || 'llm config failed');
  return res.json() as Promise<{ ok: boolean }>;
}

export async function fetchLLMModels(endpoint?: string): Promise<{ models: string[] }> {
  const q = endpoint ? '?endpoint=' + encodeURIComponent(endpoint) : '';
  const res = await fetch('/api/llm/models' + q, { cache: 'no-store' });
  if (!res.ok) return { models: [] };
  const j = await res.json().catch(() => ({}));
  return { models: Array.isArray((j as { models?: string[] }).models) ? (j as { models: string[] }).models : [] };
}

export async function fetchListDirs(path: string): Promise<{ dirs: string[]; parent: string }> {
  const res = await fetch('/api/list-dirs', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ path: path || '' })
  });
  if (!res.ok) return { dirs: [], parent: '' };
  const j = await res.json().catch(() => ({}));
  return {
    dirs: Array.isArray((j as { dirs?: string[] }).dirs) ? (j as { dirs: string[] }).dirs : [],
    parent: (j as { parent?: string }).parent || ''
  };
}

export async function postIndexBackup(): Promise<{ message: string; path: string }> {
  const res = await fetch('/api/index/backup', { method: 'POST' });
  if (!res.ok) throw new Error(await res.text() || res.statusText);
  return res.json() as Promise<{ message: string; path: string }>;
}

export async function postIndexClear(): Promise<void> {
  const res = await fetch('/api/index/clear', { method: 'POST' });
  if (!res.ok) throw new Error(await res.text() || res.statusText);
  await res.text();
}

export async function postPipelineScan(): Promise<string> {
  const res = await fetch('/api/pipeline/scan', { method: 'POST' });
  if (!res.ok) throw new Error(await res.text() || res.statusText);
  return res.text();
}

export async function postNativeScan(): Promise<{ added: number; removed: number }> {
  if (usesLocalBrowserStore()) return { added: 0, removed: 0 };
  const res = await fetch('/api/native-scan', { method: 'POST' });
  if (!res.ok) throw new Error(await res.text() || res.statusText);
  const data = await res.json().catch(() => ({}));
  return { added: (data as { added?: number }).added ?? 0, removed: (data as { removed?: number }).removed ?? 0 };
}

export async function postShaderSave(payload: { path: string; content: string }): Promise<{ ok: boolean; local?: boolean }> {
  const path = (payload.path || '').replace(/\\/g, '|');
  if (usesLocalBrowserStore()) {
    await saveLocalShader(path, payload.content);
    return { ok: true, local: true };
  }
  const res = await fetch('/api/shader/save', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ path, content: payload.content })
  });
  if (!res.ok) throw new Error(res.statusText || 'save failed');
  return res.json() as Promise<{ ok: boolean }>;
}

export async function postGitCommit(path: string): Promise<{ ok: boolean; message?: string }> {
  const res = await fetch('/api/git-commit', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ path: (path || '').replace(/\\/g, '|') })
  });
  if (!res.ok) return { ok: false };
  return res.json() as Promise<{ ok: boolean; message?: string }>;
}

export async function postGitRollback(path: string): Promise<{ ok: boolean }> {
  const p = (path || '').replace(/\\/g, '|');
  const res = await fetch('/api/git/rollback?path=' + encodeURIComponent(p), { method: 'POST' });
  if (!res.ok) throw new Error(await res.text() || res.statusText);
  return res.json() as Promise<{ ok: boolean }>;
}

export async function postUpdate(payload: {
  id?: number;
  name?: string;
  tags?: string[];
  sets?: string[];
  notes?: string;
  category?: string;
  favorite?: boolean;
  color?: string;
  paramRanges?: Array<{ min: number; max: number }>;
}): Promise<{ ok: boolean }> {
  if (usesLocalBrowserStore()) {
    await applyLocalUpdate(payload);
    return { ok: true };
  }
  const res = await fetch('/api/update', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });
  if (!res.ok) throw new Error(res.statusText || 'update failed');
  return res.json() as Promise<{ ok: boolean }>;
}

export async function postShaderRename(payload: { id: number; newName: string }): Promise<{ ok: boolean; path?: string; name?: string }> {
  if (usesLocalBrowserStore()) {
    const result = await applyLocalRename(payload.id, payload.newName);
    return { ok: true, ...result };
  }
  const res = await fetch('/api/shader/rename', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });
  if (!res.ok) throw new Error(await res.text() || res.statusText);
  return res.json() as Promise<{ ok: boolean; path?: string; name?: string }>;
}

export async function postShaderMove(payload: { id: number; category: string }): Promise<{ ok: boolean; path?: string; category?: string }> {
  if (usesLocalBrowserStore()) {
    const result = await applyLocalMove(payload.id, payload.category);
    return { ok: true, ...result };
  }
  const res = await fetch('/api/shader/move', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });
  if (!res.ok) throw new Error(await res.text() || res.statusText);
  return res.json() as Promise<{ ok: boolean; path?: string; category?: string }>;
}

export async function postOpenInCursor(payload: { path: string }): Promise<void> {
  const res = await fetch('/api/open-in-cursor', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });
  if (!res.ok) throw new Error(res.statusText || 'open in Cursor failed');
}

export async function postOpenAgent(payload: { path: string }): Promise<void> {
  const res = await fetch('/api/open-agent', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ path: (payload.path || '').replace(/\\/g, '|') })
  });
  if (!res.ok) {
    const err = new Error(await res.text() || res.statusText || 'open agent failed') as Error & { rateLimit?: boolean };
    if (res.status === 429) err.rateLimit = true;
    throw err;
  }
}

export async function postOpenInExplorer(payload: { path: string }): Promise<void> {
  const res = await fetch('/api/open-in-explorer', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });
  if (!res.ok) throw new Error(res.statusText || 'open in explorer failed');
}

export async function postOpenInNotepad(payload: { path: string }): Promise<void> {
  const res = await fetch('/api/open-in-notepad', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ path: (payload.path || '').replace(/\\/g, '|') })
  });
  if (!res.ok) throw new Error(res.statusText || 'open in Notepad failed');
}

export async function postOpenInWire(payload: { path: string }): Promise<void> {
  const res = await fetch('/api/open-in-wire', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ path: (payload.path || '').replace(/\\/g, '|') })
  });
  if (!res.ok) throw new Error(res.statusText || 'open in Wire failed');
}

export interface SeedWireResult {
  generated: Array<{ set: string; file: string; shaders: number }>;
  skipped: string[];
  errors: string[];
}

export async function postSeedWire(payload: {
  set?: string;
  autoSeed?: boolean;
  dryRun?: boolean;
  mode?: string;
  features?: {
    fft?: boolean;
    webcam?: boolean;
    glitch?: boolean;
    midi?: boolean;
  };
  fxLevel?: string;
}): Promise<SeedWireResult> {
  const res = await fetch('/api/seed-wire', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error(await res.text() || res.statusText);
  return res.json() as Promise<SeedWireResult>;
}

export async function postBulkRename(renames: Array<{ id: number; newName: string }>): Promise<{
  ok: boolean;
  renamed: number;
  errors: string[];
  results: Array<{ id: number; oldName: string; newName: string; error?: string }>;
}> {
  const res = await fetch('/api/shader/bulk-rename', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ renames }),
  });
  if (!res.ok) throw new Error(await res.text() || res.statusText);
  return res.json();
}

export async function postSeedAvenue(payload: {
  name?: string;
  set?: string;
  dryRun?: boolean;
}): Promise<string> {
  const res = await fetch('/api/seed-avenue', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error(await res.text() || res.statusText);
  return res.text();
}

export async function fetchISFPath(payload: { id?: number; path?: string }): Promise<{ path: string; ok: boolean }> {
  const q = new URLSearchParams();
  if (payload.id != null) q.set('id', String(payload.id));
  if (payload.path) q.set('path', payload.path.replace(/\\/g, '|'));
  const res = await fetch('/api/isf-path?' + q.toString());
  if (!res.ok) throw new Error(res.statusText || 'isf-path failed');
  return res.json() as Promise<{ path: string; ok: boolean }>;
}

export async function postConvertAndOpenInWire(payload: { id: number; path: string; content: string }): Promise<{ ok: boolean; path?: string; error?: string }> {
  const res = await fetch('/api/convert-and-open-in-wire', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      id: payload.id,
      path: (payload.path || '').replace(/\\/g, '|'),
      content: payload.content || ''
    })
  });
  const j = await res.json().catch(() => ({})) as { ok?: boolean; path?: string; error?: string };
  if (!res.ok) throw new Error(j.error || res.statusText || 'convert and open failed');
  return j;
}

export async function fetchGitRepoStatus(path: string, opts?: FetchOpts): Promise<{ isRepo: boolean; root: string }> {
  const p = (path || '').replace(/\\/g, '|');
  const res = await fetch('/api/git/repo-status?path=' + encodeURIComponent(p), { cache: 'no-store', signal: opts?.signal });
  if (!res.ok) return { isRepo: false, root: '' };
  const j = await res.json().catch(() => ({}));
  return { isRepo: !!(j as { isRepo?: boolean }).isRepo, root: (j as { root?: string }).root || '' };
}

export async function postGitInit(path: string): Promise<{ ok: boolean; message?: string; error?: string }> {
  const res = await fetch('/api/git/init', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ path: (path || '').replace(/\\/g, '|') })
  });
  const j = (await res.json()) as { ok: boolean; message?: string; error?: string };
  if (!res.ok) throw new Error(j.error || res.statusText);
  return j;
}

export async function fetchGitInfo(path: string, opts?: FetchOpts): Promise<{
  tracked: boolean; revisions: number; firstDate: string; lastDate: string; firstSubject: string; lastSubject: string;
}> {
  const p = (path || '').replace(/\\/g, '|');
  const res = await fetch('/api/git/info?path=' + encodeURIComponent(p), { cache: 'no-store', signal: opts?.signal });
  if (!res.ok) return { tracked: false, revisions: 0, firstDate: '', lastDate: '', firstSubject: '', lastSubject: '' };
  const j = await res.json().catch(() => ({}));
  return j as { tracked: boolean; revisions: number; firstDate: string; lastDate: string; firstSubject: string; lastSubject: string };
}

export async function fetchGitLog(path: string, opts?: FetchOpts): Promise<Array<{ sha: string; date: string; subject: string }>> {
  const p = (path || '').replace(/\\/g, '|');
  const res = await fetch('/api/git/log?path=' + encodeURIComponent(p), { cache: 'no-store', signal: opts?.signal });
  if (!res.ok) return [];
  const data = await res.json().catch(() => []);
  return Array.isArray(data) ? data : [];
}

export async function postGitRevertVersion(payload: { path: string; sha: string }): Promise<{ ok: boolean }> {
  const res = await fetch('/api/git/revert-version', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ path: (payload.path || '').replace(/\\/g, '|'), sha: payload.sha || '' })
  });
  if (!res.ok) throw new Error(res.statusText || 'revert failed');
  return res.json() as Promise<{ ok: boolean }>;
}

export async function postGitHardResetShaders(payload?: { ref?: string }): Promise<{
  ok: boolean;
  backupPath?: string;
  ref?: string;
  message?: string;
  error?: string;
}> {
  const res = await fetch('/api/git/hard-reset-shaders', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload || {})
  });
  const data = (await res.json()) as { ok: boolean; backupPath?: string; ref?: string; message?: string; error?: string };
  if (!res.ok) throw new Error(data.error || res.statusText);
  return data;
}

export async function postShaderDelete(payload: { id: number; paths: string[]; confirm: boolean }): Promise<{ message: string }> {
  if (usesLocalBrowserStore()) {
    await applyLocalDelete(payload.id);
    return { message: 'Removed from this browser (server copy unchanged)' };
  }
  const res = await fetch('/api/shader/delete', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      id: payload.id,
      paths: (payload.paths || []).map((p) => p.replace(/\\/g, '|')),
      confirm: payload.confirm
    })
  });
  if (!res.ok) throw new Error(await res.text() || res.statusText);
  return res.json() as Promise<{ message: string }>;
}

export async function postCursorSuggestParamsStream(
  payload: { path: string; content: string; cursorApiKey?: string; useAgent?: boolean },
  onLine: (line: string) => void
): Promise<{ params?: string[]; literals?: Array<{ value: number; line: number }> }> {
  const res = await fetch('/api/cursor-suggest-params-stream', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });
  if (!res.ok) {
    const j = await res.json().catch(() => ({}));
    throw new Error((j && (j as { error?: string }).error) || res.statusText || 'suggest params failed');
  }
  const reader = res.body!.getReader();
  const decoder = new TextDecoder();
  let buffer = '';
  let fullStream = '';
  let result: { params?: string[]; literals?: Array<{ value: number; line: number }>; raw?: string } = {};
  while (true) {
    const { done, value } = await reader.read();
    if (done) break;
    const chunk = decoder.decode(value, { stream: true });
    buffer += chunk;
    fullStream += chunk;
    const parts = buffer.split('\n\n');
    buffer = parts.pop() || '';
    for (const part of parts) {
      if (part.startsWith('data: ')) {
        const data = part.slice(6);
        try {
          const j = JSON.parse(data);
          if (j.done) result = j;
        } catch {
          if (onLine) onLine(data.replace(/\\n/g, '\n'));
        }
      }
    }
  }
  if (buffer.startsWith('data: ')) {
    const data = buffer.slice(6);
    try {
      const j = JSON.parse(data);
      if (j.done) result = j;
    } catch {
      if (onLine) onLine(data.replace(/\\n/g, '\n'));
    }
  }
  if (result.literals && !Array.isArray(result.literals)) result.literals = undefined;
  if (result.literals) {
    result.literals = result.literals.filter((x) => x && typeof x.value === 'number' && typeof x.line === 'number');
  }
  if ((!result.params || result.params.length === 0) && (!result.literals || result.literals.length === 0) && fullStream.length > 0) {
    const lastDataIdx = fullStream.lastIndexOf('\n\ndata: ');
    const payloadStart = lastDataIdx === -1 ? (fullStream.startsWith('data: ') ? 0 : -1) : lastDataIdx + 2;
    if (payloadStart >= 0) {
      const payload = fullStream.slice(payloadStart).replace(/^data: /, '').split('\n\n')[0].trim();
      try {
        const j = JSON.parse(payload);
        if (j.done && Array.isArray(j.params)) result = j;
      } catch {
        /* ignore */
      }
    }
    if ((!result.params || result.params.length === 0) && (result.raw || fullStream)) {
      const raw = (result.raw || fullStream) as string;
      const arrMatch = raw.match(/\[[\s\S]*?\]/);
      if (arrMatch) {
        try {
          const arr = JSON.parse(arrMatch[0]);
          if (Array.isArray(arr)) result = { ...result, params: arr.filter((x): x is string => typeof x === 'string') };
        } catch {
          /* ignore */
        }
      }
    }
  }
  return result && (result.params || result.literals) ? result : { params: result.params || [], literals: result.literals || [] };
}

export async function postCursorAssist(payload: {
  path: string;
  content: string;
  prompt: string;
  cursorApiKey?: string;
}): Promise<void> {
  const res = await fetch('/api/cursor-assist', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });
  const j = await res.json().catch(() => ({}));
  if (!res.ok) throw new Error((j && (j as { error?: string }).error) || res.statusText || 'cursor assist failed');
}

export async function postCursorAssistVisual(payload: {
  path: string;
  content: string;
  prompt: string;
  screenshot: string;
  cursorApiKey?: string;
}): Promise<{ message?: string }> {
  const res = await fetch('/api/cursor-assist-visual', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });
  const j = await res.json().catch(() => ({}));
  if (!res.ok) throw new Error((j && (j as { error?: string }).error) || res.statusText || 'visual assist failed');
  return j as { message?: string };
}

export async function postVibeCreate(payload: {
  name: string;
  genre: string;
  description: string;
  cursorApiKey?: string;
}): Promise<{ ok: boolean; id: number; path: string; name: string }> {
  const res = await fetch('/api/vibe-create', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });
  const j = await res.json().catch(() => ({}));
  if (!res.ok) throw new Error((j && (j as { error?: string }).error) || res.statusText || 'vibe create failed');
  return j as { ok: boolean; id: number; path: string; name: string };
}

export async function postCursorRefactorParams(payload: {
  path: string;
  content: string;
  cursorApiKey?: string;
}): Promise<{ content: string }> {
  const res = await fetch('/api/cursor-refactor-params', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      path: (payload.path || '').replace(/\\/g, '|'),
      content: payload.content || '',
      cursorApiKey: payload.cursorApiKey || undefined
    })
  });
  const j = (await res.json().catch(() => ({}))) as { content?: string; error?: string };
  if (!res.ok) {
    if (res.status === 429) throw new Error(j.error || 'Agent cooldown. Wait a moment and try again.');
    throw new Error(j.error || res.statusText || 'refactor failed');
  }
  if (!j.content || typeof j.content !== 'string') throw new Error(j.error || 'No refactored content returned');
  return { content: j.content };
}

export interface UpdateCheckResult {
  hasUpdates: boolean;
  commits: string[];
  localHead?: string;
  remoteHead?: string;
  error?: string;
}

export async function fetchUpdateCheck(opts?: FetchOpts): Promise<UpdateCheckResult> {
  const res = await fetch('/api/update/check', { cache: 'no-store', signal: opts?.signal });
  if (!res.ok) return { hasUpdates: false, commits: [], error: res.statusText };
  const j = await res.json().catch(() => ({}));
  return j as UpdateCheckResult;
}

export async function postTagScan(): Promise<{
  ok: boolean;
  scanned: number;
  mouseTagged: number;
  roliTagged: number;
  uniformsFilled: number;
}> {
  const res = await fetch('/api/shader/tag-scan', { method: 'POST' });
  if (!res.ok) throw new Error(await res.text() || res.statusText);
  return res.json();
}

/* ── Wire Pipeline Hub API ─────────────────────────────────────────────── */

export interface WireLibraryEntry {
  name: string;
  displayName: string;
  fileName: string;
  setName: string;
  category: string;
  shaderCount: number;
  fileSizeBytes: number;
  path: string;
}

export async function postWireClassifyEffects(): Promise<{
  ok: boolean;
  scanned: number;
  effectsTagged: number;
  sourcesTagged: number;
}> {
  const res = await fetch('/api/wire/classify-effects', { method: 'POST' });
  if (!res.ok) throw new Error(await res.text() || res.statusText);
  return res.json();
}

export async function fetchWireLibrary(opts?: FetchOpts): Promise<WireLibraryEntry[]> {
  const res = await fetch('/api/wire/library', { cache: 'no-store', signal: opts?.signal });
  if (!res.ok) return [];
  const data = await res.json().catch(() => []);
  return Array.isArray(data) ? data : [];
}

export async function fetchWirePatch(name: string, opts?: FetchOpts): Promise<string> {
  const res = await fetch('/api/wire/patch?name=' + encodeURIComponent(name), { cache: 'no-store', signal: opts?.signal });
  if (!res.ok) throw new Error(await res.text() || res.statusText);
  return res.text();
}

export async function postWireGenerate(payload: {
  shaderIds: number[];
  topology: string;
  midiPreset?: string;
  outputName?: string;
  features?: { fft?: boolean; webcam?: boolean; midi?: boolean };
}): Promise<{ ok: boolean; file: string; shaderCount: number }> {
  const res = await fetch('/api/wire/generate', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error(await res.text() || res.statusText);
  return res.json();
}

export async function postWireGenerateEffects(): Promise<{
  ok: boolean;
  generated: number;
  errors: string[];
}> {
  const res = await fetch('/api/wire/generate-effects', { method: 'POST' });
  if (!res.ok) throw new Error(await res.text() || res.statusText);
  return res.json();
}

export async function postWireDelete(payload: { path: string }): Promise<{ ok: boolean }> {
  const res = await fetch('/api/wire/delete', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error(await res.text() || res.statusText);
  return res.json();
}

export async function postWireCompile(payload?: {
  author?: string;
  vendor?: string;
  url?: string;
  mail?: string;
}): Promise<{
  ok: boolean;
  total: number;
  updated: number;
  compiled: number;
  errors: string[];
  wireFiles: string[];
}> {
  const res = await fetch('/api/wire/compile', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload || {}),
  });
  if (!res.ok) throw new Error(await res.text() || res.statusText);
  return res.json();
}

export async function postOpenInResolume(payload: { path: string }): Promise<void> {
  const res = await fetch('/api/open-in-resolume', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error(await res.text() || res.statusText);
}

export async function postUpdateApply(): Promise<{ success: boolean; message: string; error?: string }> {
  const res = await fetch('/api/update/apply', { method: 'POST' });
  const j = await res.json().catch(() => ({})) as { success?: boolean; message?: string; error?: string };
  if (!res.ok) return { success: false, message: j.error || res.statusText };
  return { success: !!(j.success), message: j.message || '', error: j.error };
}
