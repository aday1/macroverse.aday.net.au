import { entries } from './state.js';
import type { IndexEntry } from './types.js';
import {
  applyVjLibraryFilter,
  getVjLibraryFilter,
  setVjLibraryFilter,
  toggleVjFilterCategory,
  toggleVjFilterFormat,
  toggleVjFilterSet,
  toggleVjFilterTag,
  vjFilterSetChipNames,
  vjFilterTagChipNames,
  type VjFormatFilter,
  type VjLibraryFilter,
} from './vjLibraryFilter.js';

const COLLAPSE_KEY = 'macroverse-vj-filter-strip-collapsed';

function allEntryTags(): string[] {
  const tags: string[] = [];
  for (const e of entries) {
    for (const t of e.tags || []) tags.push(t);
  }
  return tags;
}

function allCategories(): string[] {
  const names = new Set<string>();
  entries.forEach((e) => {
    const c = (e.category || '').trim();
    if (c) names.add(c);
  });
  return [...names].sort((a, b) => a.localeCompare(b));
}

function makeChip(
  label: string,
  onClick: (ev: MouseEvent) => void,
  state: 'off' | 'on' | 'exclude',
  title?: string
): HTMLButtonElement {
  const btn = document.createElement('button');
  btn.type = 'button';
  btn.className = 'vj-filter-chip';
  btn.textContent = label;
  if (state === 'on') btn.classList.add('is-on');
  if (state === 'exclude') btn.classList.add('is-exclude');
  if (title) btn.title = title;
  btn.addEventListener('click', onClick);
  return btn;
}

function chipRow(label: string): { row: HTMLElement; chips: HTMLElement } {
  const row = document.createElement('div');
  row.className = 'vj-filter-row';
  const lab = document.createElement('span');
  lab.className = 'vj-filter-row-label';
  lab.textContent = label;
  const chips = document.createElement('div');
  chips.className = 'vj-filter-chips';
  row.appendChild(lab);
  row.appendChild(chips);
  return { row, chips };
}

function makeToggle(
  label: string,
  checked: boolean,
  onChange: (on: boolean) => void,
  title?: string
): HTMLLabelElement {
  const wrap = document.createElement('label');
  wrap.className = 'vj-filter-toggle';
  const input = document.createElement('input');
  input.type = 'checkbox';
  input.checked = checked;
  if (title) wrap.title = title;
  input.addEventListener('change', () => onChange(input.checked));
  const span = document.createElement('span');
  span.textContent = label;
  wrap.appendChild(input);
  wrap.appendChild(span);
  return wrap;
}

function tagChipState(f: VjLibraryFilter, tag: string): 'off' | 'on' | 'exclude' {
  if (f.tagsExclude.includes(tag)) return 'exclude';
  if (f.tagsInclude.includes(tag)) return 'on';
  return 'off';
}

function setChipState(f: VjLibraryFilter, setName: string): 'off' | 'on' | 'exclude' {
  if (f.setsExclude.includes(setName)) return 'exclude';
  if (f.setsInclude.includes(setName)) return 'on';
  return 'off';
}

export function buildVjLibraryFilterStrip(onChange: () => void): { root: HTMLElement; refresh: () => void } {
  const root = document.createElement('div');
  root.className = 'vj-filter-strip';

  let collapsed = true;
  try {
    const stored = localStorage.getItem(COLLAPSE_KEY);
    if (stored === '1') collapsed = true;
    else if (stored === '0') collapsed = false;
  } catch {
    collapsed = true;
  }

  const head = document.createElement('div');
  head.className = 'vj-filter-head';

  const collapseBtn = document.createElement('button');
  collapseBtn.type = 'button';
  collapseBtn.className = 'vj-filter-collapse-btn';
  collapseBtn.title = 'Show or hide filters';

  const textInput = document.createElement('input');
  textInput.type = 'text';
  textInput.className = 'vj-filter-text';
  textInput.placeholder = 'Filter shaders...';
  textInput.autocomplete = 'off';

  const countEl = document.createElement('span');
  countEl.className = 'vj-filter-count';

  const clearBtn = document.createElement('button');
  clearBtn.type = 'button';
  clearBtn.className = 'vj-filter-clear-btn';
  clearBtn.textContent = 'Clear';
  clearBtn.title = 'Reset all VJ library filters';

  head.appendChild(collapseBtn);
  head.appendChild(textInput);
  head.appendChild(countEl);
  head.appendChild(clearBtn);

  const body = document.createElement('div');
  body.className = 'vj-filter-body';

  const formatRow = chipRow('Format');
  const tagRow = chipRow('Tags');
  const setRow = chipRow('Sets');
  const catRow = chipRow('Category');

  const modeRow = document.createElement('div');
  modeRow.className = 'vj-filter-row vj-filter-mode-row';

  const tagModeWrap = document.createElement('span');
  tagModeWrap.className = 'vj-filter-mode-group';
  const tagModeLabel = document.createElement('span');
  tagModeLabel.className = 'vj-filter-row-label';
  tagModeLabel.textContent = 'Tags';
  const tagModeAny = document.createElement('button');
  tagModeAny.type = 'button';
  tagModeAny.className = 'vj-filter-chip';
  tagModeAny.textContent = 'ANY';
  const tagModeAll = document.createElement('button');
  tagModeAll.type = 'button';
  tagModeAll.className = 'vj-filter-chip';
  tagModeAll.textContent = 'ALL';
  tagModeWrap.appendChild(tagModeLabel);
  tagModeWrap.appendChild(tagModeAny);
  tagModeWrap.appendChild(tagModeAll);

  const setModeWrap = document.createElement('span');
  setModeWrap.className = 'vj-filter-mode-group';
  const setModeLabel = document.createElement('span');
  setModeLabel.className = 'vj-filter-row-label';
  setModeLabel.textContent = 'Sets';
  const setModeAny = document.createElement('button');
  setModeAny.type = 'button';
  setModeAny.className = 'vj-filter-chip';
  setModeAny.textContent = 'ANY';
  const setModeAll = document.createElement('button');
  setModeAll.type = 'button';
  setModeAll.className = 'vj-filter-chip';
  setModeAll.textContent = 'ALL';
  setModeWrap.appendChild(setModeLabel);
  setModeWrap.appendChild(setModeAny);
  setModeWrap.appendChild(setModeAll);

  modeRow.appendChild(tagModeWrap);
  modeRow.appendChild(setModeWrap);

  const toggleRow = document.createElement('div');
  toggleRow.className = 'vj-filter-row vj-filter-toggle-row';

  root.appendChild(head);
  root.appendChild(body);
  body.appendChild(formatRow.row);
  body.appendChild(tagRow.row);
  body.appendChild(modeRow);
  body.appendChild(setRow.row);
  body.appendChild(catRow.row);
  body.appendChild(toggleRow);

  function notify(): void {
    onChange();
    refresh();
  }

  function setCollapsed(on: boolean): void {
    collapsed = on;
    root.classList.toggle('is-collapsed', collapsed);
    collapseBtn.textContent = collapsed ? '>' : 'v';
    try {
      localStorage.setItem(COLLAPSE_KEY, collapsed ? '1' : '0');
    } catch {
      /* ignore */
    }
  }

  collapseBtn.addEventListener('click', () => setCollapsed(!collapsed));

  let textDebounce: ReturnType<typeof setTimeout> | null = null;
  textInput.addEventListener('input', () => {
    if (textDebounce) clearTimeout(textDebounce);
    textDebounce = setTimeout(() => {
      textDebounce = null;
      setVjLibraryFilter({ text: textInput.value });
      notify();
    }, 120);
  });

  clearBtn.addEventListener('click', () => {
    setVjLibraryFilter({
      text: '',
      formats: [],
      tagsInclude: [],
      tagsExclude: [],
      tagMode: 'any',
      categories: [],
      setsInclude: [],
      setsExclude: [],
      setMode: 'any',
      favoritesOnly: false,
      hideDead: true,
      hideTrash: true,
      syncWithMainList: false,
    });
    textInput.value = '';
    notify();
  });

  tagModeAny.addEventListener('click', () => {
    setVjLibraryFilter({ tagMode: 'any' });
    notify();
  });
  tagModeAll.addEventListener('click', () => {
    setVjLibraryFilter({ tagMode: 'all' });
    notify();
  });
  setModeAny.addEventListener('click', () => {
    setVjLibraryFilter({ setMode: 'any' });
    notify();
  });
  setModeAll.addEventListener('click', () => {
    setVjLibraryFilter({ setMode: 'all' });
    notify();
  });

  function renderFormatChips(f: VjLibraryFilter): void {
    formatRow.chips.replaceChildren();
    (['glsl', 'isf'] as VjFormatFilter[]).forEach((fmt) => {
      const on = f.formats.includes(fmt);
      formatRow.chips.appendChild(
        makeChip(
          fmt.toUpperCase(),
          () => {
            toggleVjFilterFormat(fmt);
            notify();
          },
          on ? 'on' : 'off',
          'Toggle ' + fmt.toUpperCase() + ' format'
        )
      );
    });
  }

  function renderTagChips(f: VjLibraryFilter): void {
    tagRow.chips.replaceChildren();
    const tags = vjFilterTagChipNames(20, allEntryTags());
    for (const tag of tags) {
      const state = tagChipState(f, tag);
      tagRow.chips.appendChild(
        makeChip(
          tag,
          (ev) => {
            toggleVjFilterTag(tag, ev.shiftKey);
            notify();
          },
          state,
          'Click include, Shift+click exclude'
        )
      );
    }
    if (tags.length === 0) {
      const empty = document.createElement('span');
      empty.className = 'vj-filter-empty-hint';
      empty.textContent = '(no tags)';
      tagRow.chips.appendChild(empty);
    }
  }

  function renderSetChips(f: VjLibraryFilter): void {
    setRow.chips.replaceChildren();
    const extraSets: string[] = [];
    entries.forEach((e) => (e.sets || []).forEach((s) => extraSets.push(s)));
    for (const setName of vjFilterSetChipNames(extraSets)) {
      const state = setChipState(f, setName);
      const short = setName.replace(/^vj-/, '');
      setRow.chips.appendChild(
        makeChip(
          short,
          (ev) => {
            toggleVjFilterSet(setName, ev.shiftKey);
            notify();
          },
          state,
          setName + ' — click include, Shift+click exclude'
        )
      );
    }
  }

  function renderCategoryChips(f: VjLibraryFilter): void {
    catRow.chips.replaceChildren();
    const cats = allCategories();
    for (const cat of cats) {
      const on = f.categories.some((c) => c.toLowerCase() === cat.toLowerCase());
      catRow.chips.appendChild(
        makeChip(
          cat,
          () => {
            toggleVjFilterCategory(cat);
            notify();
          },
          on ? 'on' : 'off',
          'Filter category ' + cat
        )
      );
    }
    if (cats.length === 0) {
      const empty = document.createElement('span');
      empty.className = 'vj-filter-empty-hint';
      empty.textContent = '(no categories)';
      catRow.chips.appendChild(empty);
    }
  }

  function renderToggles(f: VjLibraryFilter): void {
    toggleRow.replaceChildren();
    toggleRow.appendChild(
      makeToggle('Favorites', f.favoritesOnly, (on) => {
        setVjLibraryFilter({ favoritesOnly: on });
        notify();
      }, 'Show favorites only')
    );
    toggleRow.appendChild(
      makeToggle('Hide dead', f.hideDead, (on) => {
        setVjLibraryFilter({ hideDead: on });
        notify();
      }, 'Hide shaders with dead in tags')
    );
    toggleRow.appendChild(
      makeToggle('Hide trash', f.hideTrash, (on) => {
        setVjLibraryFilter({ hideTrash: on });
        notify();
      }, 'Hide trash category')
    );
    toggleRow.appendChild(
      makeToggle('Sync main list', f.syncWithMainList, (on) => {
        setVjLibraryFilter({ syncWithMainList: on });
        notify();
      }, 'Also apply main list format and set filters')
    );
  }

  function renderModeButtons(f: VjLibraryFilter): void {
    const multiTags = f.tagsInclude.length > 1;
    const multiSets = f.setsInclude.length > 1;
    tagModeWrap.style.display = multiTags ? '' : 'none';
    setModeWrap.style.display = multiSets ? '' : 'none';
    tagModeAny.classList.toggle('is-on', f.tagMode === 'any');
    tagModeAll.classList.toggle('is-on', f.tagMode === 'all');
    setModeAny.classList.toggle('is-on', f.setMode === 'any');
    setModeAll.classList.toggle('is-on', f.setMode === 'all');
  }

  function updateCount(filtered: IndexEntry[], total: number): void {
    countEl.textContent = filtered.length + ' / ' + total;
  }

  function refresh(): void {
    const f = getVjLibraryFilter();
    if (textInput.value !== f.text) textInput.value = f.text;
    const filtered = applyVjLibraryFilter(entries);
    updateCount(filtered, entries.length);
    renderFormatChips(f);
    renderTagChips(f);
    renderSetChips(f);
    renderCategoryChips(f);
    renderToggles(f);
    renderModeButtons(f);
  }

  setCollapsed(collapsed);
  refresh();

  return { root, refresh };
}
