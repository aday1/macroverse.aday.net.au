export const SHADER_PATH_DROP_TYPE = 'application/x-macroverse-shader-path';

export type ShaderDropTarget = {
  el: HTMLElement;
  onDrop: (path: string) => void;
};

const targets: ShaderDropTarget[] = [];
let ghostEl: HTMLElement | null = null;

const DRAG_THRESHOLD_PX = 8;

export function registerShaderDropTarget(target: ShaderDropTarget): () => void {
  targets.push(target);
  target.el.classList.add('shader-drop-target');
  return () => {
    const i = targets.indexOf(target);
    if (i >= 0) targets.splice(i, 1);
    target.el.classList.remove('shader-drop-target', 'shader-drop-highlight');
  };
}

function hitTarget(clientX: number, clientY: number): ShaderDropTarget | null {
  for (const t of targets) {
    const r = t.el.getBoundingClientRect();
    if (clientX >= r.left && clientX <= r.right && clientY >= r.top && clientY <= r.bottom) {
      return t;
    }
  }
  return null;
}

function clearHighlights(): void {
  for (const t of targets) t.el.classList.remove('shader-drop-highlight');
}

function ensureGhost(): HTMLElement {
  if (!ghostEl) {
    ghostEl = document.createElement('div');
    ghostEl.className = 'shader-drag-ghost';
    document.body.appendChild(ghostEl);
  }
  return ghostEl;
}

function isInteractiveChild(el: EventTarget | null): boolean {
  if (!(el instanceof HTMLElement)) return false;
  return !!el.closest(
    '.star, .swatch, .tag-pill, .tag-pill-x, .tag-pill-add, .set-pill, .set-pill-x, .set-pill-add, .emoji-badge, .vj-shader-carousel-actions, .vj-shader-carousel-load, button, input, select, a'
  );
}

export function attachShaderListDrag(item: HTMLElement, path: string, label: string): void {
  item.draggable = true;
  item.addEventListener('dragstart', (ev: DragEvent) => {
    if (!ev.dataTransfer) return;
    ev.dataTransfer.setData('text/plain', path);
    ev.dataTransfer.setData(SHADER_PATH_DROP_TYPE, path);
    ev.dataTransfer.effectAllowed = 'copy';
  });

  item.addEventListener('pointerdown', (ev: PointerEvent) => {
    if (ev.button !== 0 || !path || isInteractiveChild(ev.target)) return;

    let dragging = false;
    let activePath: string | null = path;
    const startX = ev.clientX;
    const startY = ev.clientY;

    const onMove = (e: PointerEvent) => {
      if (!activePath) return;
      const dx = e.clientX - startX;
      const dy = e.clientY - startY;
      if (!dragging) {
        if (dx * dx + dy * dy < DRAG_THRESHOLD_PX * DRAG_THRESHOLD_PX) return;
        dragging = true;
        item.setPointerCapture(e.pointerId);
        item.dataset.shaderPointerDrag = '1';
        const g = ensureGhost();
        g.textContent = label.slice(0, 48);
        g.style.display = 'block';
      }
      const g = ensureGhost();
      g.style.left = `${e.clientX + 12}px`;
      g.style.top = `${e.clientY + 12}px`;
      clearHighlights();
      const hit = hitTarget(e.clientX, e.clientY);
      if (hit) hit.el.classList.add('shader-drop-highlight');
    };

    const finish = (e: PointerEvent) => {
      document.removeEventListener('pointermove', onMove);
      document.removeEventListener('pointerup', finish);
      document.removeEventListener('pointercancel', finish);
      if (ghostEl) ghostEl.style.display = 'none';
      clearHighlights();
      if (dragging && activePath) {
        const hit = hitTarget(e.clientX, e.clientY);
        if (hit) hit.onDrop(activePath);
        e.preventDefault();
        e.stopPropagation();
        window.setTimeout(() => {
          delete item.dataset.shaderPointerDrag;
        }, 0);
      } else {
        delete item.dataset.shaderPointerDrag;
      }
      activePath = null;
      dragging = false;
      try {
        item.releasePointerCapture(e.pointerId);
      } catch {
        /* ignore */
      }
    };

    document.addEventListener('pointermove', onMove);
    document.addEventListener('pointerup', finish);
    document.addEventListener('pointercancel', finish);
  });
}

export function dispatchVjDeckLoad(deck: 'A' | 'B', path: string): void {
  window.dispatchEvent(
    new CustomEvent('macroverse-vj-load-path', { detail: { deck, path } })
  );
}
