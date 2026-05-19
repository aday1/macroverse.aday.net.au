/**
 * Dock system.
 *
 * Lets the index panel (Library) and right column (Params) behave
 * as either docked columns (desktop) or as overlay drawers
 * (tablet, phone). The CSS does the actual layout; this module
 * adds the body-class flags and the API for opening / closing
 * drawers.
 *
 * Body classes used:
 *   .dock-left-open   - left drawer (index/library) is open
 *   .dock-right-open  - right drawer (params) is open
 *
 * On desktop (>= 1025px), opening / closing a drawer has no visible
 * effect because CSS keeps the columns docked anyway. We still set
 * the classes so commands and tab-bar buttons report the right
 * intent.
 *
 * The drawers reuse the existing #indexPanel and #rightColumn DOM,
 * promoted to `position: fixed` via CSS when the open class is
 * present. This avoids duplicating the panels in the DOM.
 */

export type DockSide = 'left' | 'right';

let scrimEl: HTMLDivElement | null = null;

function ensureScrim(): HTMLDivElement {
  if (scrimEl) return scrimEl;
  scrimEl = document.createElement('div');
  scrimEl.id = 'dockScrim';
  scrimEl.className = 'dock-scrim';
  scrimEl.setAttribute('aria-hidden', 'true');
  scrimEl.addEventListener('click', () => closeAllDocks());
  document.body.appendChild(scrimEl);
  return scrimEl;
}

export function isDockOpen(side: DockSide): boolean {
  return document.body.classList.contains(
    side === 'left' ? 'dock-left-open' : 'dock-right-open'
  );
}

export function openDock(side: DockSide): void {
  ensureScrim();
  // Only one drawer open at a time on small screens.
  if (side === 'left') {
    document.body.classList.add('dock-left-open');
    document.body.classList.remove('dock-right-open');
  } else {
    document.body.classList.add('dock-right-open');
    document.body.classList.remove('dock-left-open');
  }
  document.body.classList.add('dock-any-open');
  // Make sure the underlying panel is "uncollapsed" so the drawer
  // shows its contents (the existing collapse classes hide them).
  if (side === 'left') {
    document.getElementById('indexPanel')?.classList.remove('collapsed');
    document.getElementById('mainGrid')?.classList.remove('index-collapsed');
  } else {
    document.getElementById('rightColumn')?.classList.remove('collapsed');
    document.getElementById('mainGrid')?.classList.remove('right-collapsed');
  }
}

export function closeDock(side: DockSide): void {
  document.body.classList.remove(
    side === 'left' ? 'dock-left-open' : 'dock-right-open'
  );
  if (!isDockOpen('left') && !isDockOpen('right')) {
    document.body.classList.remove('dock-any-open');
  }
}

export function closeAllDocks(): void {
  document.body.classList.remove('dock-left-open', 'dock-right-open', 'dock-any-open');
}

export function toggleDock(side: DockSide): void {
  if (isDockOpen(side)) closeDock(side);
  else openDock(side);
}

export function initDockSystem(): void {
  ensureScrim();

  // Esc closes any open drawer.
  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') closeAllDocks();
  });

  // When a list item is tapped on the index drawer, close the
  // drawer so the user sees the preview (mobile only).
  const list = document.getElementById('list');
  if (list) {
    list.addEventListener('click', (ev) => {
      // Only auto-close on small viewports where the drawer
      // overlays the canvas; on desktop the panel is docked and
      // closing it would be jarring.
      if (window.matchMedia('(max-width: 1024px)').matches) {
        const target = ev.target as HTMLElement | null;
        if (target?.closest('.list-item')) {
          // Tiny delay so the click on the item registers first.
          setTimeout(() => closeDock('left'), 120);
        }
      }
    }, true);
  }

  // Resize listener: if we cross from mobile to desktop, close any
  // drawer to avoid a stuck overlay.
  window.addEventListener('resize', () => {
    if (window.matchMedia('(min-width: 1025px)').matches) {
      closeAllDocks();
    }
  });
}
