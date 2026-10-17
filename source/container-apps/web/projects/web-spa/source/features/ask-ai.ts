// #region Purpose
// Inserts an @ resource token into the Ask textarea, says whether an @ starts a word, refocuses
// the textarea, copies the last answer's text, and sets the assistant avatar for the turns.
// #endregion
//
// #region Design
// The library MessageInput owns the draft. InsertReference splices at the caret and dispatches
// a bubbling input event so Blazor sees the change. Typing @ is what opens the picker, so when
// the token starts with @ and the character just before the caret is @, that @ is replaced
// instead of doubled. One space follows the token unless the next character is already
// whitespace, so text typed after it never joins the id. AtStartsWord answers, for the @ keydown,
// whether that @ starts a word: nothing or whitespace before it. The Blazor handler is async, so
// the @ has usually landed by the time this runs; an @ just before the caret is skipped. A mid-
// word @ (ada@example.com) stays plain text and does not open the picker or steal focus.
// FocusComposer returns focus to the textarea when the @ picker closes.
// Task 381 (focus on open, after a send, on close). The library MessageInput has no focus API, so
// focus goes through the same textarea selector. CaptureOpener runs when the panel opens: it keeps
// the focused element as the opener and claims the command palette's pending focus return
// (FocusReturnClaimEvent), so after the Ctrl-K row the opener is the element focused before Ctrl-K
// and the palette's return never lands on top of the composer. FocusComposerOnOpen focuses the
// textarea, retrying a few frames if the library has not attached it yet. FocusComposerInPanel is
// the soft form for after a send and after expand: it moves focus only when focus is in the panel
// or on nothing, so it never pulls focus back from somewhere the person went. RestoreOpener is
// one-shot on close: the opener if it is still in the document, else the page main region, else
// nothing focused, so focus never stays on a removed node. An opener handed over by the palette is
// restored through the palette's guarded restore, so focusing its search-field trigger does not
// reopen the palette.
// CopyText writes the text C# passes: the same last-answer text the thumbs file, not the whole
// transcript. A denied clipboard stays quiet: copy is presentational and must not break the
// panel. Imported on demand (AskAiJsModule), not via window.Spa.
// SetAssistantAvatar (task 361) puts the avatar on --twe-assistant-avatar on the document root, so
// AgentAsk.razor.css can draw it beside each assistant turn the library renders. Inline style
// attributes are banned (strict CSP), but the CSSOM is not. Only a base64 SVG data URI is
// accepted, so the value cannot break out of url(); anything else removes the property and the
// turns fall back to the brand gradient.
// #endregion

import { FocusReturnClaimEvent, type FocusReturnClaim } from "./focus-return-claim.js";

export function InsertReference(token: string): void {
  const area = document.querySelector<HTMLTextAreaElement>(".twe-agent-ask .sc-ai-input__textarea");
  if (area === null) {
    return;
  }

  let start = area.selectionStart ?? area.value.length;
  const end = area.selectionEnd ?? start;
  if (token.startsWith("@") && start > 0 && area.value[start - 1] === "@") {
    start -= 1;
  }

  const after = area.value.slice(end);
  const inserted = after.length > 0 && /^\s/.test(after) ? token : token + " ";
  area.value = area.value.slice(0, start) + inserted + after;
  const caret = start + inserted.length;
  area.selectionStart = caret;
  area.selectionEnd = caret;
  area.dispatchEvent(new Event("input", { bubbles: true }));
  area.focus();
}

export function AtStartsWord(): boolean {
  const area = document.querySelector<HTMLTextAreaElement>(".twe-agent-ask .sc-ai-input__textarea");
  if (area === null) {
    return false;
  }

  let start = area.selectionStart ?? area.value.length;
  if (start > 0 && area.value[start - 1] === "@") {
    start -= 1;
  }

  return start === 0 || /\s/.test(area.value[start - 1]);
}

export function FocusComposer(): void {
  document.querySelector<HTMLTextAreaElement>(ComposerSelector)?.focus();
}

const ComposerSelector = ".twe-agent-ask .sc-ai-input__textarea";
const PanelSelector = ".twe-agent-ask";
const OpenFocusFrames = 30;

let opener: HTMLElement | null = null;
let restoreOpener: (() => void) | null = null;

export function CaptureOpener(): void {
  const active = document.activeElement;
  const claim: FocusReturnClaim = {
    target: active instanceof HTMLElement && active !== document.body && active.closest(PanelSelector) === null
      ? active
      : null,
    restore: null,
  };
  document.dispatchEvent(new CustomEvent<FocusReturnClaim>(FocusReturnClaimEvent, { detail: claim }));
  opener = claim.target;
  restoreOpener = claim.restore;
}

export function FocusComposerOnOpen(): void {
  let frames = 0;
  const attempt = (): void => {
    const area = document.querySelector<HTMLTextAreaElement>(ComposerSelector);
    if (area !== null) {
      area.focus();
      return;
    }

    frames += 1;
    if (frames < OpenFocusFrames) {
      requestAnimationFrame(attempt);
    }
  };
  attempt();
}

export function FocusComposerInPanel(): void {
  const active = document.activeElement;
  if (active === null || active === document.body || active.closest(PanelSelector) !== null) {
    FocusComposer();
  }
}

export function RestoreOpener(): void {
  const target = opener;
  const restore = restoreOpener;
  opener = null;
  restoreOpener = null;
  if (target !== null && target.isConnected) {
    if (restore !== null) {
      restore();
    } else {
      target.focus();
    }

    return;
  }

  const main = document.querySelector<HTMLElement>("main");
  if (main !== null) {
    if (!main.hasAttribute("tabindex")) {
      main.setAttribute("tabindex", "-1");
    }

    main.focus({ preventScroll: true });
    return;
  }

  if (document.activeElement instanceof HTMLElement && !document.activeElement.isConnected) {
    document.activeElement.blur();
  }
}

export async function CopyText(text: string): Promise<void> {
  try {
    await navigator.clipboard.writeText(text);
  } catch {
    // The browser denied the clipboard. The answer stays on screen.
  }
}

const AvatarPrefix = "data:image/svg+xml;base64,";

export function SetAssistantAvatar(dataUri: string | null): void {
  const root = document.documentElement.style;
  const body = dataUri?.startsWith(AvatarPrefix) ? dataUri.slice(AvatarPrefix.length) : null;
  if (body === null || body === undefined || !/^[A-Za-z0-9+/]+={0,2}$/.test(body)) {
    root.removeProperty("--twe-assistant-avatar");
    return;
  }

  root.setProperty("--twe-assistant-avatar", `url("${AvatarPrefix}${body}")`);
}
