/**
 * Touch-aware layout tiers for phone / tablet / desktop shells.
 *
 * Width-only media queries miss iPad Pro landscape and Steam Deck
 * (touch + wide). This module sets html.layout-{phone,tablet,desktop}
 * so CSS and JS can share one layout mode.
 */

export type LayoutTier = 'phone' | 'tablet' | 'desktop';

const TIER_CLASSES = ['layout-phone', 'layout-tablet', 'layout-desktop', 'layout-compact'] as const;

export function getLayoutTier(): LayoutTier {
  const root = document.documentElement;
  if (root.classList.contains('force-mobile')) return 'phone';

  const w = window.innerWidth;
  const h = window.innerHeight;
  const coarse = window.matchMedia('(pointer: coarse)').matches;

  if (w <= 640 || (coarse && h <= 520 && w <= 980)) return 'phone';
  if (w <= 1024 || (coarse && w <= 1366)) return 'tablet';
  return 'desktop';
}

export function isLayoutPhone(): boolean {
  return getLayoutTier() === 'phone';
}

export function isLayoutTablet(): boolean {
  return getLayoutTier() === 'tablet';
}

export function isLayoutDesktop(): boolean {
  return getLayoutTier() === 'desktop';
}

/** Phone or tablet drawer shell (not desktop grid). */
export function isCompactLayout(): boolean {
  const t = getLayoutTier();
  return t === 'phone' || t === 'tablet';
}

type TierChangeCallback = (tier: LayoutTier, prev: LayoutTier) => void;
const listeners: TierChangeCallback[] = [];
let currentTier: LayoutTier | null = null;

export function onLayoutTierChange(cb: TierChangeCallback): () => void {
  listeners.push(cb);
  return () => {
    const idx = listeners.indexOf(cb);
    if (idx >= 0) listeners.splice(idx, 1);
  };
}

export function applyLayoutTier(): LayoutTier {
  const tier = getLayoutTier();
  const root = document.documentElement;
  for (const c of TIER_CLASSES) root.classList.remove(c);
  root.classList.add(`layout-${tier}`);
  root.classList.toggle('layout-compact', tier !== 'desktop');

  const prev = currentTier;
  currentTier = tier;
  if (prev !== null && prev !== tier) {
    for (const cb of listeners) cb(tier, prev);
  }
  return tier;
}

export function initLayoutTier(): LayoutTier {
  applyLayoutTier();
  const onChange = () => applyLayoutTier();
  window.addEventListener('resize', onChange);
  window.addEventListener('orientationchange', onChange);
  return currentTier!;
}
