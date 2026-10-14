// #region Purpose
// Ask Tag pick mode: highlight a marked element under the pointer or by keyboard and describe the
// one the person chooses.
// #endregion
//
// #region Design
// Task 346. A pick is named by the token StartPick mints. Start arms pick mode (the overlay and
// every listener are live when it returns, task 351), so the panel says "picking" only once a click
// or key is caught. Answer resolves with JSON for the chosen element, or "" when cancelled; a pick
// chosen between Start and Answer keeps its answer. CancelPick(token) ends only that pick, and when
// it arrives before that pick's Start, Start refuses it, so a cancel that overtakes the start never
// leaves an armed pick nobody waits for. A newer Start ends the older pick. Only elements
// the app marked with data-ask-tag (AskTagMark) inside the page body are candidates, never any
// <div>. A candidate must be visible to the person: it is skipped inside data-ask-tag-private,
// [hidden], [aria-hidden=true] or [inert], when it has no box, or when checkVisibility says it is
// not rendered. The Ask panel is never a candidate. The description is semantic and capped:
// kind, label (data-ask-tag-label, else the first heading, else the text), visible text (text
// nodes the browser renders, skipping private and hidden descendants; input values are not text
// nodes, so they are never read), data-qa, landmark (the enclosing marked element's label or
// the nearest labelled landmark) and data-ask-tag-ref. C# caps and checks it again.
// The overlay is a fixed, pointer-events:none outline plus label and a status hint, so the
// element under the pointer is found with elementFromPoint. Listeners run in the capture phase:
// a click picks instead of following a link or pressing a button. Clicks inside the Ask panel
// pass through, so its Cancel button still works. Keys: Tab / Down / Right next, Shift+Tab / Up /
// Left previous, Enter chooses, Esc cancels. Pick mode starts by blurring focus left in the Ask
// panel (the Tag button that started it), so those keys drive the pick at once. Esc cancels
// everywhere, but Enter and Tab with focus moved back into the Ask panel stay native, so its
// Cancel button is still pressable. The pick root is looked up on every use, because the page
// body is re-created on navigation. A hit anywhere in a grid row resolves to the mark inside
// that row, and its text is the whole row's. data-qa is the marked element's own, never an
// ancestor's. The first candidate is highlighted at start so the keyboard path needs no
// pointer. Styles are global (app.css): this markup is created here, so scoped CSS cannot reach
// it. Imported on demand by AskTagJsModule.
// #endregion

const kindAttribute = "data-ask-tag";
const labelAttribute = "data-ask-tag-label";
const referenceAttribute = "data-ask-tag-ref";
const hiddenSelector = "[data-ask-tag-private], [hidden], [aria-hidden='true'], [inert], .twe-agent-ask";
const landmarkSelector = "section, nav, main, aside, form, [role='region'], [role='main'], [role='navigation'], [role='form']";
const labelCap = 80;
const textCap = 300;

interface AskTagCandidate {
  kind: string;
  label: string;
  text: string;
  dataQa: string | null;
  landmark: string | null;
  reference: string | null;
}

let active: PickSession | null = null;
let latest: { token: string; answer: Promise<string> } | null = null;
// Tokens cancelled before their Start arrived; Start refuses (and forgets) them.
const refused = new Set<string>();

/** Arms pick mode for token; false when that pick was already cancelled. */
export function Start(rootSelector: string, token: string): boolean {
  if (refused.delete(token)) {
    return false;
  }

  active?.finish(null);
  const answer = new Promise<string>(resolve => {
    active = new PickSession(rootSelector, token, resolve);
  });
  latest = { token, answer };
  return true;
}

/** The chosen element's JSON for token's pick, or "" when it was cancelled or replaced. */
export function Answer(token: string): Promise<string> {
  return latest?.token === token ? latest.answer : Promise.resolve("");
}

export function CancelPick(token: string): void {
  if (active?.token === token) {
    active.finish(null);
  } else if (latest?.token !== token) {
    refused.add(token);
  }
}

class PickSession {
  readonly token: string;
  private readonly rootSelector: string;
  private readonly resolve: (json: string) => void;
  private readonly outline: HTMLDivElement;
  private readonly badge: HTMLDivElement;
  private readonly hint: HTMLDivElement;
  private current: Element | null = null;
  private done = false;

  constructor(rootSelector: string, token: string, resolve: (json: string) => void) {
    this.token = token;
    this.rootSelector = rootSelector;
    this.resolve = resolve;
    this.outline = element("twe-ask-tag-outline");
    this.badge = element("twe-ask-tag-badge");
    this.hint = element("twe-ask-tag-hint");
    this.hint.setAttribute("role", "status");
    this.hint.setAttribute("data-qa", "AskTagHint");
    this.outline.setAttribute("data-qa", "AskTagOutline");
    this.outline.appendChild(this.badge);
    document.body.append(this.outline, this.hint);
    window.addEventListener("pointermove", this.onPointerMove, true);
    window.addEventListener("pointerdown", this.onPointerDown, true);
    window.addEventListener("mousedown", this.onPointerDown, true);
    window.addEventListener("click", this.onClick, true);
    window.addEventListener("keydown", this.onKeyDown, true);
    window.addEventListener("scroll", this.onLayout, true);
    window.addEventListener("resize", this.onLayout, true);
    if (insideAsk(document.activeElement) && document.activeElement instanceof HTMLElement) {
      document.activeElement.blur();
    }

    const first = this.candidates()[0] ?? null;
    this.highlight(first);
    if (first === null) {
      this.hint.textContent = "Nothing on this page can be tagged. Press Esc to cancel.";
    }
  }

  finish(candidate: AskTagCandidate | null): void {
    if (this.done) {
      return;
    }

    this.done = true;
    window.removeEventListener("pointermove", this.onPointerMove, true);
    window.removeEventListener("pointerdown", this.onPointerDown, true);
    window.removeEventListener("mousedown", this.onPointerDown, true);
    window.removeEventListener("click", this.onClick, true);
    window.removeEventListener("keydown", this.onKeyDown, true);
    window.removeEventListener("scroll", this.onLayout, true);
    window.removeEventListener("resize", this.onLayout, true);
    this.outline.remove();
    this.hint.remove();
    if (active === this) {
      active = null;
    }

    this.resolve(candidate === null ? "" : JSON.stringify(candidate));
  }

  /** The page body is re-created on navigation, so it is looked up each time, never cached. */
  private root(): Element | null {
    return document.querySelector(this.rootSelector);
  }

  private candidates(): Element[] {
    const root = this.root();
    if (root === null) {
      return [];
    }

    return Array.from(root.querySelectorAll(`[${kindAttribute}]`)).filter(isPickable);
  }

  private candidateAt(x: number, y: number): Element | null {
    const root = this.root();
    const hit = document.elementFromPoint(x, y);
    // A grid row is marked in one cell, so a hit anywhere in the row is that mark.
    const row = hit?.closest("tr, [role='row']") ?? null;
    const inRow = row?.querySelector(`[${kindAttribute}]`) ?? null;
    let marked = inRow ?? hit?.closest(`[${kindAttribute}]`) ?? null;
    while (marked !== null) {
      if (root?.contains(marked) && isPickable(marked)) {
        return marked;
      }

      marked = marked.parentElement?.closest(`[${kindAttribute}]`) ?? null;
    }

    return null;
  }

  private highlight(target: Element | null, scroll = false): void {
    this.current = target;
    if (target === null) {
      this.outline.style.display = "none";
      this.hint.textContent = "Pick an element for Ask. Tab or arrows to move, Enter to choose, Esc to cancel.";
      return;
    }

    const chip = chipText(target);
    this.badge.textContent = chip;
    this.hint.textContent = `${chip}. Enter to choose, Esc to cancel.`;
    if (scroll) {
      boxOf(target).scrollIntoView({ block: "nearest", inline: "nearest" });
    }

    this.place();
  }

  private place(): void {
    if (this.current === null) {
      return;
    }

    const rect = boxOf(this.current).getBoundingClientRect();
    this.outline.style.display = "block";
    this.outline.style.top = `${rect.top}px`;
    this.outline.style.left = `${rect.left}px`;
    this.outline.style.width = `${rect.width}px`;
    this.outline.style.height = `${rect.height}px`;
  }

  private move(step: number): void {
    const candidates = this.candidates();
    if (candidates.length === 0) {
      return;
    }

    const index = this.current === null ? -1 : candidates.indexOf(this.current);
    const next = index < 0
      ? (step > 0 ? 0 : candidates.length - 1)
      : (index + step + candidates.length) % candidates.length;
    this.highlight(candidates[next], true);
  }

  private choose(target: Element | null): void {
    if (target !== null && isPickable(target)) {
      this.finish(describe(target));
    }
  }

  private readonly onPointerMove = (event: PointerEvent): void => {
    const target = this.candidateAt(event.clientX, event.clientY);
    if (target !== null && target !== this.current) {
      this.highlight(target);
    }
  };

  private readonly onPointerDown = (event: MouseEvent): void => {
    if (insideAsk(event.target)) {
      return;
    }

    event.preventDefault();
    event.stopPropagation();
  };

  private readonly onClick = (event: MouseEvent): void => {
    if (insideAsk(event.target)) {
      return;
    }

    event.preventDefault();
    event.stopPropagation();
    const target = this.candidateAt(event.clientX, event.clientY);
    if (target === null) {
      this.finish(null);
      return;
    }

    this.choose(target);
  };

  private readonly onKeyDown = (event: KeyboardEvent): void => {
    switch (event.key) {
      case "Escape":
        this.finish(null);
        break;
      case "Enter":
        if (insideAsk(event.target)) {
          return;
        }

        this.choose(this.current);
        break;
      case "Tab":
        if (insideAsk(event.target)) {
          return;
        }

        this.move(event.shiftKey ? -1 : 1);
        break;
      case "ArrowDown":
      case "ArrowRight":
        this.move(1);
        break;
      case "ArrowUp":
      case "ArrowLeft":
        this.move(-1);
        break;
      default:
        return;
    }

    event.preventDefault();
    event.stopPropagation();
  };

  private readonly onLayout = (): void => {
    this.place();
  };
}

function element(className: string): HTMLDivElement {
  const div = document.createElement("div");
  div.className = className;
  return div;
}

function insideAsk(target: EventTarget | null): boolean {
  return target instanceof Element && target.closest(".twe-agent-ask") !== null;
}

function isPickable(candidate: Element): boolean {
  if (candidate.closest(hiddenSelector) !== null) {
    return false;
  }

  const box = candidate.getBoundingClientRect();
  if (box.width === 0 || box.height === 0) {
    return false;
  }

  const visible = (candidate as Element & { checkVisibility?: (options?: object) => boolean })
    .checkVisibility?.({ checkOpacity: true, checkVisibilityCSS: true });
  return visible !== false;
}

/** A grid row's mark sits in a cell; highlight the whole row. */
function boxOf(target: Element): Element {
  if (target.closest("td, [role='gridcell']") === null) {
    return target;
  }

  return target.closest("tr, [role='row']") ?? target;
}

function squash(text: string | null | undefined): string {
  return (text ?? "").replace(/\s+/g, " ").trim();
}

function clip(text: string, cap: number): string {
  return text.length <= cap ? text : text.slice(0, cap - 1) + "…";
}

/** Text nodes the browser renders, skipping private and hidden descendants. */
function visibleText(target: Element, cap: number): string {
  const parts: string[] = [];
  let length = 0;
  const walker = document.createTreeWalker(target, NodeFilter.SHOW_TEXT);
  for (let node = walker.nextNode(); node !== null && length < cap; node = walker.nextNode()) {
    const parent = node.parentElement;
    if (parent === null || parent.closest(hiddenSelector) !== null) {
      continue;
    }

    if (parent.tagName === "SCRIPT" || parent.tagName === "STYLE" || parent.tagName === "TEMPLATE") {
      continue;
    }

    const visible = (parent as Element & { checkVisibility?: (options?: object) => boolean })
      .checkVisibility?.({ checkOpacity: true, checkVisibilityCSS: true });
    if (visible === false) {
      continue;
    }

    const text = squash(node.textContent);
    if (text.length > 0) {
      parts.push(text);
      length += text.length + 1;
    }
  }

  return clip(parts.join(" "), cap);
}

function labelOf(target: Element): string {
  const explicit = squash(target.getAttribute(labelAttribute));
  if (explicit.length > 0) {
    return clip(explicit, labelCap);
  }

  const heading = target.querySelector("h1, h2, h3, h4, h5, h6");
  if (heading !== null && heading.closest(hiddenSelector) === null) {
    const text = squash(heading.textContent);
    if (text.length > 0) {
      return clip(text, labelCap);
    }
  }

  return visibleText(target, labelCap);
}

function chipText(target: Element): string {
  const kind = squash(target.getAttribute(kindAttribute));
  const label = labelOf(target);
  return label.length === 0 ? kind : `${kind}: ${label}`;
}

function landmarkOf(target: Element): string | null {
  const marked = target.parentElement?.closest(`[${kindAttribute}]`) ?? null;
  if (marked !== null) {
    return chipText(marked);
  }

  const landmark = target.parentElement?.closest(landmarkSelector) ?? null;
  if (landmark === null) {
    return null;
  }

  const name = squash(landmark.getAttribute("aria-label"));
  return name.length > 0 ? clip(name, labelCap) : landmark.tagName.toLowerCase();
}

function describe(target: Element): AskTagCandidate {
  const dataQa = target.getAttribute("data-qa");
  return {
    kind: squash(target.getAttribute(kindAttribute)),
    label: labelOf(target),
    text: visibleText(boxOf(target), textCap),
    dataQa,
    landmark: landmarkOf(target),
    reference: target.getAttribute(referenceAttribute),
  };
}
