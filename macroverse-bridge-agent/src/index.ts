#!/usr/bin/env node

import { MacroverseBridgeClient } from './client.js';
import { loadConfig, parseArgv } from './config.js';

async function main(): Promise<void> {
  const config = loadConfig(parseArgv(process.argv.slice(2)));
  const client = new MacroverseBridgeClient(config);

  const shutdown = () => {
    console.log('[bridge] Shutting down');
    client.stop();
    process.exit(0);
  };
  process.on('SIGINT', shutdown);
  process.on('SIGTERM', shutdown);

  await client.start();
}

main().catch((err) => {
  console.error('[bridge] Fatal:', err);
  process.exit(1);
});
