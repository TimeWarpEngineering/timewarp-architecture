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
// CopyText writes the text C# passes: the same last-answer text the thumbs file, not the whole
// transcript. A denied clipboard stays quiet: copy is presentational and must not break the
// panel. Imported on demand (AskAiJsModule), not via window.Spa.
// SetAssistantAvatar (task 361) puts the avatar on --twe-assistant-avatar on the document root, so
// AgentAsk.razor.css can draw it beside each assistant turn the library renders. Inline style
// attributes are banned (strict CSP), but the CSSOM is not. Only a base64 SVG data URI is
// accepted, so the value cannot break out of url(); anything else removes the property and the
// turns fall back to the brand gradient.
// #endregion

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
  document.querySelector<HTMLTextAreaElement>(".twe-agent-ask .sc-ai-input__textarea")?.focus();
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
