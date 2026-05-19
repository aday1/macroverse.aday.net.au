import fs from 'fs';
import os from 'os';
import path from 'path';

export interface BridgeConfig {
  cloudUrl: string;
  token: string;
  bridgeId: string;
  sessionId: string;
  linkEnabled: boolean;
  oscListenPort: number;
  hdmiUrl: string;
}

const DEFAULT_CONFIG: BridgeConfig = {
  cloudUrl: 'https://macroverse.aday.net.au',
  token: '',
  bridgeId: `mv-bridge-${os.hostname()}`,
  sessionId: 'default',
  linkEnabled: true,
  oscListenPort: 0,
  hdmiUrl: ''
};

function configPath(): string {
  return (
    process.env.MACROVERSE_BRIDGE_CONFIG ||
    path.join(os.homedir(), '.macroverse', 'bridge.json')
  );
}

export function loadConfig(argv: Record<string, string | boolean>): BridgeConfig {
  const cfg: BridgeConfig = { ...DEFAULT_CONFIG };
  const file = configPath();

  if (fs.existsSync(file)) {
    try {
      const parsed = JSON.parse(fs.readFileSync(file, 'utf-8'));
      Object.assign(cfg, parsed);
    } catch (err) {
      console.warn('[bridge] Failed to read config:', err);
    }
  }

  if (process.env.BRIDGE_TOKEN) cfg.token = process.env.BRIDGE_TOKEN;
  if (process.env.CLOUD_URL) cfg.cloudUrl = process.env.CLOUD_URL;
  if (process.env.BRIDGE_ID) cfg.bridgeId = process.env.BRIDGE_ID;
  if (process.env.SESSION_ID) cfg.sessionId = process.env.SESSION_ID;
  if (process.env.BRIDGE_SESSION_ID) cfg.sessionId = process.env.BRIDGE_SESSION_ID;

  if (typeof argv.token === 'string' && argv.token) cfg.token = argv.token;
  if (typeof argv['cloud-url'] === 'string') cfg.cloudUrl = argv['cloud-url'];
  if (typeof argv['bridge-id'] === 'string') cfg.bridgeId = argv['bridge-id'];
  if (typeof argv['session-id'] === 'string') cfg.sessionId = argv['session-id'];
  if (argv['no-link'] === true) cfg.linkEnabled = false;

  if (!cfg.hdmiUrl) {
    const base = cfg.cloudUrl.replace(/\/$/, '');
    const sid = encodeURIComponent(cfg.sessionId || 'default');
    cfg.hdmiUrl = `${base}/vj-output.html?remote=1&sessionId=${sid}`;
  }

  return cfg;
}

export function parseArgv(args: string[]): Record<string, string | boolean> {
  const out: Record<string, string | boolean> = {};
  for (let i = 0; i < args.length; i++) {
    const a = args[i];
    if (a === '--no-link') {
      out['no-link'] = true;
      continue;
    }
    if (a.startsWith('--')) {
      const key = a.slice(2);
      const next = args[i + 1];
      if (next && !next.startsWith('--')) {
        out[key] = next;
        i++;
      } else {
        out[key] = true;
      }
    }
  }
  return out;
}
