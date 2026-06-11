/** Gradient range sliders (jkantner-style) with per-control colors. */

const SLIDER_PALETTE = [
  '#5588cc',
  '#cc7744',
  '#2ecc71',
  '#9b59b6',
  '#e74c3c',
  '#1abc9c',
  '#f39c12',
  '#e91e63',
  '#00bcd4',
  '#8bc34a',
  '#ff5722',
  '#673ab7'
];

export function sliderColorForKey(key: string): string {
  let h = 0;
  for (let i = 0; i < key.length; i++) {
    h = (h * 31 + key.charCodeAt(i)) | 0;
  }
  return SLIDER_PALETTE[Math.abs(h) % SLIDER_PALETTE.length];
}

function percentFromInput(input: HTMLInputElement): number {
  const min = parseFloat(input.min);
  const max = parseFloat(input.max);
  const val = parseFloat(input.value);
  if (!Number.isFinite(min) || !Number.isFinite(max) || max <= min) return 0;
  const p = ((val - min) / (max - min)) * 100;
  return Math.max(0, Math.min(100, p));
}

/** Sync --mv-percent and --mv-slider-color on the input (and wrapper if present). */
export function updateSliderFill(input: HTMLInputElement): void {
  const pct = percentFromInput(input);
  input.style.setProperty('--mv-percent', pct + '%');
  const wrap = input.closest('.mv-rich-slider') as HTMLElement | null;
  const colorKey = input.dataset.mvColorKey || wrap?.dataset.mvColorKey || input.id || input.name || 'slider';
  const color = sliderColorForKey(colorKey);
  input.style.setProperty('--mv-slider-color', color);
  if (wrap) {
    wrap.style.setProperty('--mv-slider-color', color);
    wrap.style.setProperty('--mv-percent', pct + '%');
  }
}

export interface RichSliderOptions {
  label: string;
  min: number;
  max: number;
  step: number;
  value: number;
  colorKey?: string;
  className?: string;
  showValue?: boolean;
  formatValue?: (v: number) => string;
  onInput?: (v: number) => void;
  inputClassName?: string;
  hideLabel?: boolean;
  compact?: boolean;
}

export interface RichSliderHandle {
  root: HTMLElement;
  input: HTMLInputElement;
  valueEl: HTMLElement | null;
  setValue: (v: number) => void;
  setLabel: (text: string) => void;
  sync: () => void;
}

function attachFillListeners(input: HTMLInputElement): void {
  const sync = () => updateSliderFill(input);
  input.addEventListener('input', sync);
  input.addEventListener('change', sync);
  sync();
}

/** Tap or drag anywhere on the track to set value (touch + VJ fader feel). */
export function attachTouchTrackScrub(input: HTMLInputElement): void {
  const track = input.closest('.mv-rich-slider__track') as HTMLElement | null;
  const root = input.closest('.mv-rich-slider') as HTMLElement | null;
  if (!track) return;

  const valueFromClientX = (clientX: number): number => {
    const rect = track.getBoundingClientRect();
    if (rect.width <= 0) return parseFloat(input.value);
    const min = parseFloat(input.min);
    const max = parseFloat(input.max);
    const stepRaw = parseFloat(input.step);
    const step = Number.isFinite(stepRaw) && stepRaw > 0 ? stepRaw : (max - min) / 200;
    const t = Math.max(0, Math.min(1, (clientX - rect.left) / rect.width));
    let v = min + t * (max - min);
    if (step > 0) v = Math.round(v / step) * step;
    return Math.max(min, Math.min(max, v));
  };

  const apply = (clientX: number): void => {
    input.value = String(valueFromClientX(clientX));
    input.dispatchEvent(new Event('input', { bubbles: true }));
    updateSliderFill(input);
  };

  track.addEventListener(
    'pointerdown',
    (e) => {
      if (e.pointerType === 'mouse' && e.button !== 0) return;
      // Clicks on the range input must use native drag/scrub; preventDefault on
      // bubbled events breaks smooth intermediate values (snaps to 0 or 100%).
      if (e.target === input) return;
      e.preventDefault();
      root?.classList.add('mv-rich-slider--active');
      track.setPointerCapture(e.pointerId);
      apply(e.clientX);
    },
    { passive: false }
  );
  track.addEventListener('pointermove', (e) => {
    if (!track.hasPointerCapture(e.pointerId)) return;
    e.preventDefault();
    apply(e.clientX);
  });
  const end = (e: PointerEvent): void => {
    if (!track.hasPointerCapture(e.pointerId)) return;
    track.releasePointerCapture(e.pointerId);
    root?.classList.remove('mv-rich-slider--active');
    input.dispatchEvent(new Event('change', { bubbles: true }));
  };
  track.addEventListener('pointerup', end);
  track.addEventListener('pointercancel', end);
}

function defaultFormat(v: number): string {
  return Number.isInteger(v) ? String(v) : v.toFixed(2);
}

export function createRichSlider(opts: RichSliderOptions): RichSliderHandle {
  const colorKey = opts.colorKey ?? opts.label;
  const color = sliderColorForKey(colorKey);
  const format = opts.formatValue ?? defaultFormat;
  const showValue = opts.showValue !== false;

  const root = document.createElement('div');
  root.className = 'mv-rich-slider' + (opts.className ? ' ' + opts.className : '');
  if (opts.compact) root.classList.add('mv-rich-slider--compact');
  root.dataset.mvColorKey = colorKey;
  root.style.setProperty('--mv-slider-color', color);

  let labelEl: HTMLElement | null = null;
  if (!opts.hideLabel) {
    labelEl = document.createElement('span');
    labelEl.className = 'mv-rich-slider__label';
    labelEl.textContent = opts.label;
    labelEl.style.color = color;
    root.appendChild(labelEl);
  }

  const trackWrap = document.createElement('div');
  trackWrap.className = 'mv-rich-slider__track';

  const input = document.createElement('input');
  input.type = 'range';
  input.min = String(opts.min);
  input.max = String(opts.max);
  input.step = String(opts.step);
  input.value = String(opts.value);
  input.dataset.mvColorKey = colorKey;
  if (opts.inputClassName) {
    for (const c of opts.inputClassName.split(/\s+/)) {
      if (c) input.classList.add(c);
    }
  }

  let valueEl: HTMLElement | null = null;

  const onInput = () => {
    const v = parseFloat(input.value);
    if (valueEl) valueEl.textContent = format(v);
    opts.onInput?.(v);
  };
  input.addEventListener('input', onInput);

  trackWrap.appendChild(input);
  root.appendChild(trackWrap);
  if (showValue) {
    valueEl = document.createElement('span');
    valueEl.className = 'mv-rich-slider__value';
    valueEl.textContent = format(opts.value);
    root.appendChild(valueEl);
  }
  attachFillListeners(input);
  attachTouchTrackScrub(input);

  const setValue = (v: number) => {
    input.value = String(v);
    if (valueEl) valueEl.textContent = format(v);
    updateSliderFill(input);
  };
  const setLabel = (text: string) => {
    if (labelEl) labelEl.textContent = text;
  };
  const sync = () => updateSliderFill(input);

  return { root, input, valueEl, setValue, setLabel, sync };
}

export interface EnhanceRangeOptions {
  label?: string;
  colorKey?: string;
  className?: string;
  valueEl?: HTMLElement | null;
  formatValue?: (v: number) => string;
}

/** Wrap an existing range input in .mv-rich-slider (replaces input in DOM). */
export function enhanceRangeInput(
  input: HTMLInputElement,
  opts: EnhanceRangeOptions = {}
): RichSliderHandle {
  const colorKey = opts.colorKey || input.dataset.param || input.id || input.className || 'slider';
  const color = sliderColorForKey(colorKey);
  const format = opts.formatValue ?? defaultFormat;
  const parent = input.parentElement;
  if (!parent) {
    attachFillListeners(input);
    return {
      root: input,
      input,
      valueEl: opts.valueEl ?? null,
      setValue: (v) => {
        input.value = String(v);
        updateSliderFill(input);
      },
      setLabel: () => {},
      sync: () => updateSliderFill(input)
    };
  }

  const root = document.createElement('div');
  root.className = 'mv-rich-slider mv-rich-slider--inline' + (opts.className ? ' ' + opts.className : '');
  root.dataset.mvColorKey = colorKey;
  root.style.setProperty('--mv-slider-color', color);

  if (opts.label) {
    const labelEl = document.createElement('span');
    labelEl.className = 'mv-rich-slider__label';
    labelEl.textContent = opts.label;
    labelEl.style.color = color;
    root.appendChild(labelEl);
  }

  const trackWrap = document.createElement('div');
  trackWrap.className = 'mv-rich-slider__track';
  input.dataset.mvColorKey = colorKey;
  parent.insertBefore(root, input);
  trackWrap.appendChild(input);
  root.appendChild(trackWrap);
  attachFillListeners(input);
  attachTouchTrackScrub(input);

  const valueEl = opts.valueEl ?? null;
  const setValue = (v: number) => {
    input.value = String(v);
    if (valueEl) valueEl.textContent = format(v);
    updateSliderFill(input);
  };
  const setLabel = () => {};
  const sync = () => updateSliderFill(input);

  return { root, input, valueEl, setValue, setLabel, sync };
}

