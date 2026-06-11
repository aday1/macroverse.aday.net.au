export const CROSSFADE_TAP_MS = 600;

export interface CrossfadeAnim {
  active: boolean;
  startVal: number;
  target: number;
  startTime: number;
  durationMs: number;
}

export function startCrossfadeAnim(
  anim: CrossfadeAnim,
  current: number,
  target: number,
  durationMs: number,
  now: number
): void {
  anim.active = true;
  anim.startVal = current;
  anim.target = target;
  anim.startTime = now;
  anim.durationMs = Math.max(1, durationMs);
}

export function tickCrossfadeAnim(anim: CrossfadeAnim, now: number): number | null {
  if (!anim.active) return null;
  const elapsed = now - anim.startTime;
  if (elapsed < 0) return anim.startVal;
  if (elapsed >= anim.durationMs) {
    anim.active = false;
    return anim.target;
  }
  const t = elapsed / anim.durationMs;
  const smooth = 0.5 - 0.5 * Math.cos(t * Math.PI);
  return anim.startVal + (anim.target - anim.startVal) * smooth;
}
