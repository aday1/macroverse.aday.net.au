/**
 * Command registry.
 *
 * Lightweight, in-memory registry of all user-facing actions. Each
 * panel registers its commands during init; the command palette
 * (Phase 3) reads from `allCommands()` and runs them via `run()`.
 *
 * Design goals:
 *   - Simple: no event bus, no DI. A Map<string, Command>.
 *   - Lazy: panels can register on demand (e.g. when their view
 *     opens) without forcing an upfront import of every panel.
 *   - Idempotent: re-registering an id replaces the previous entry.
 *   - Optional `when()` predicate so commands can hide themselves
 *     when context doesn't apply.
 */

export interface Command {
  id: string;
  /** Short label shown in the palette. */
  label: string;
  /** Optional fuzzy-search hint and tooltip text. */
  description?: string;
  /** Optional category used to group items (e.g. "Editor", "VJ"). */
  category?: string;
  /** Optional shortcut hint shown in the palette UI. */
  shortcut?: string;
  /** Optional predicate; if returns false, the command is hidden. */
  when?: () => boolean;
  /** Action to invoke. */
  run: () => void | Promise<void>;
  /** Optional keywords to widen fuzzy matching. */
  keywords?: string[];
}

const registry = new Map<string, Command>();

export function registerCommand(cmd: Command): void {
  if (!cmd || !cmd.id || !cmd.label || !cmd.run) return;
  registry.set(cmd.id, cmd);
}

export function unregisterCommand(id: string): void {
  registry.delete(id);
}

export function allCommands(): Command[] {
  const list: Command[] = [];
  for (const cmd of registry.values()) {
    if (typeof cmd.when === 'function') {
      try { if (!cmd.when()) continue; } catch (_) { continue; }
    }
    list.push(cmd);
  }
  // Stable sort: category, then label.
  list.sort((a, b) => {
    const ca = (a.category || 'zz').toLowerCase();
    const cb = (b.category || 'zz').toLowerCase();
    if (ca !== cb) return ca < cb ? -1 : 1;
    return a.label.toLowerCase() < b.label.toLowerCase() ? -1 : 1;
  });
  return list;
}

export function runCommand(id: string): boolean {
  const cmd = registry.get(id);
  if (!cmd) return false;
  if (typeof cmd.when === 'function') {
    try { if (!cmd.when()) return false; } catch (_) { return false; }
  }
  try {
    void cmd.run();
  } catch (e) {
    // eslint-disable-next-line no-console
    console.warn('[commandRegistry] command failed:', id, e);
    return false;
  }
  return true;
}

/**
 * Score a command against a query. Higher is better. Returns -1
 * when there is no match. Tiny custom matcher: case-insensitive
 * subsequence match with bonuses for prefix and word-boundary
 * matches.
 */
export function scoreCommand(cmd: Command, query: string): number {
  if (!query) return 1;
  const q = query.toLowerCase();
  const haystacks: string[] = [
    cmd.label,
    cmd.description || '',
    cmd.category || '',
    ...(cmd.keywords || [])
  ].map((s) => s.toLowerCase());

  let best = -1;
  for (const h of haystacks) {
    if (!h) continue;
    if (h === q) return 1000;
    if (h.startsWith(q)) {
      best = Math.max(best, 500);
      continue;
    }
    const idx = h.indexOf(q);
    if (idx >= 0) {
      best = Math.max(best, 200 - idx);
      continue;
    }
    // Subsequence match
    let i = 0;
    let matched = 0;
    let lastIdx = -1;
    let runLen = 0;
    let bestRun = 0;
    for (let j = 0; j < h.length && i < q.length; j++) {
      if (h[j] === q[i]) {
        if (j === lastIdx + 1) runLen++;
        else { bestRun = Math.max(bestRun, runLen); runLen = 1; }
        lastIdx = j;
        i++;
        matched++;
      }
    }
    bestRun = Math.max(bestRun, runLen);
    if (matched === q.length) {
      best = Math.max(best, 50 + bestRun * 5);
    }
  }
  return best;
}
