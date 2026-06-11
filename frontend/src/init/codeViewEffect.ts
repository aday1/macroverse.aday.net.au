/** Terminal CRT "code view" on the shader editor (codepen.io/cauners/pen/ExMaqOW). */

export const CODE_VIEW_STORAGE_KEY = 'macroverse-crt-code';

export function isCodeViewEnabled(): boolean {
  const stored = localStorage.getItem(CODE_VIEW_STORAGE_KEY);
  if (stored === null) return true;
  return stored === 'true';
}

export function setCodeViewEnabled(on: boolean): void {
  localStorage.setItem(CODE_VIEW_STORAGE_KEY, String(on));
  applyCodeViewState();
}

export function toggleCodeView(): boolean {
  const next = !isCodeViewEnabled();
  setCodeViewEnabled(next);
  return next;
}

export function applyCodeViewState(): void {
  const wrap = document.getElementById('codeWrap');
  const on = isCodeViewEnabled();
  if (wrap) {
    wrap.classList.toggle('crt-on', on);
    wrap.classList.toggle('code-view-on', on);
  }
  const cb = document.getElementById('crtCodeToggle') as HTMLInputElement | null;
  if (cb) cb.checked = on;
}
