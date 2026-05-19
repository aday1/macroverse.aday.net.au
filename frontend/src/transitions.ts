/**
 * Shader preview transition system.
 * Captures a snapshot of the current preview before switching,
 * then animates it out with the chosen effect.
 */

import { appSettings } from './state.js';

export type TransitionType = 'none' | 'crossfade' | 'wipe-left' | 'wipe-right' | 'wipe-down' | 'dissolve' | 'zoom-in' | 'zoom-out' | 'glitch' | 'slide-left';

export const TRANSITION_TYPES: { id: TransitionType; label: string }[] = [
  { id: 'none', label: 'None (instant)' },
  { id: 'crossfade', label: 'Crossfade' },
  { id: 'wipe-left', label: 'Wipe Left' },
  { id: 'wipe-right', label: 'Wipe Right' },
  { id: 'wipe-down', label: 'Wipe Down' },
  { id: 'dissolve', label: 'Dissolve' },
  { id: 'zoom-in', label: 'Zoom In' },
  { id: 'zoom-out', label: 'Zoom Out' },
  { id: 'glitch', label: 'Glitch' },
  { id: 'slide-left', label: 'Slide Left' },
];

let overlay: HTMLImageElement | null = null;
let animationId = 0;

function getTransitionType(): TransitionType {
  return (appSettings as Record<string, unknown>).transition as TransitionType || 'crossfade';
}

function getTransitionDuration(): number {
  const d = (appSettings as Record<string, unknown>).transitionDuration;
  return typeof d === 'number' ? d : 400;
}

export function captureSnapshot(canvas: HTMLCanvasElement | null): void {
  if (!canvas || getTransitionType() === 'none') return;
  try {
    const url = canvas.toDataURL('image/jpeg', 0.85);
    if (!overlay) {
      overlay = document.createElement('img');
      overlay.id = 'transition-overlay';
      overlay.style.cssText = 'position:absolute;top:0;left:0;width:100%;height:100%;pointer-events:none;z-index:5;object-fit:contain;';
    }
    overlay.src = url;
    overlay.style.opacity = '1';
    overlay.style.transform = '';
    overlay.style.clipPath = '';
    overlay.style.filter = '';
    overlay.style.transition = '';

    const wrap = canvas.parentElement;
    if (wrap && !wrap.contains(overlay)) {
      wrap.style.position = 'relative';
      wrap.appendChild(overlay);
    }
  } catch (_) {}
}

export function playTransition(): void {
  if (!overlay || getTransitionType() === 'none') {
    removeOverlay();
    return;
  }

  const type = getTransitionType();
  const duration = getTransitionDuration();
  const id = ++animationId;

  overlay.style.transition = '';
  void overlay.offsetHeight; // force reflow

  switch (type) {
    case 'crossfade':
      overlay.style.transition = `opacity ${duration}ms ease-in-out`;
      overlay.style.opacity = '0';
      break;

    case 'wipe-left':
      overlay.style.transition = `clip-path ${duration}ms ease-in-out`;
      overlay.style.clipPath = 'inset(0 0 0 100%)';
      break;

    case 'wipe-right':
      overlay.style.transition = `clip-path ${duration}ms ease-in-out`;
      overlay.style.clipPath = 'inset(0 100% 0 0)';
      break;

    case 'wipe-down':
      overlay.style.transition = `clip-path ${duration}ms ease-in-out`;
      overlay.style.clipPath = 'inset(100% 0 0 0)';
      break;

    case 'dissolve': {
      let start: number | null = null;
      const step = (ts: number) => {
        if (id !== animationId) return;
        if (!start) start = ts;
        const p = Math.min((ts - start) / duration, 1);
        if (overlay) {
          overlay.style.opacity = String(1 - p);
          overlay.style.filter = `blur(${p * 8}px) brightness(${1 + p * 0.5})`;
        }
        if (p < 1) requestAnimationFrame(step);
        else removeOverlay();
      };
      requestAnimationFrame(step);
      return;
    }

    case 'zoom-in':
      overlay.style.transition = `opacity ${duration}ms ease-in, transform ${duration}ms ease-in`;
      overlay.style.opacity = '0';
      overlay.style.transform = 'scale(2)';
      break;

    case 'zoom-out':
      overlay.style.transition = `opacity ${duration}ms ease-in, transform ${duration}ms ease-in`;
      overlay.style.opacity = '0';
      overlay.style.transform = 'scale(0.1)';
      break;

    case 'glitch': {
      let start: number | null = null;
      const step = (ts: number) => {
        if (id !== animationId) return;
        if (!start) start = ts;
        const p = Math.min((ts - start) / duration, 1);
        if (overlay) {
          const shake = (1 - p) * 10;
          const rx = (Math.random() - 0.5) * shake;
          const ry = (Math.random() - 0.5) * shake;
          overlay.style.transform = `translate(${rx}px, ${ry}px)`;
          overlay.style.opacity = String(1 - p);
          overlay.style.filter = p > 0.5 ? `hue-rotate(${(p - 0.5) * 720}deg)` : '';
        }
        if (p < 1) requestAnimationFrame(step);
        else removeOverlay();
      };
      requestAnimationFrame(step);
      return;
    }

    case 'slide-left':
      overlay.style.transition = `transform ${duration}ms ease-in-out, opacity ${duration * 0.8}ms ease-in`;
      overlay.style.transform = 'translateX(-100%)';
      overlay.style.opacity = '0';
      break;

    default:
      overlay.style.transition = `opacity ${duration}ms ease-in-out`;
      overlay.style.opacity = '0';
  }

  setTimeout(() => {
    if (id === animationId) removeOverlay();
  }, duration + 50);
}

function removeOverlay(): void {
  if (overlay && overlay.parentElement) {
    overlay.parentElement.removeChild(overlay);
  }
}
