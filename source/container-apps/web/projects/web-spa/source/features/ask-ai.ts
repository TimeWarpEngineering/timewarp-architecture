// #region Purpose
// Inserts an @ resource token into the Ask textarea and copies the last answer's text.
// #endregion
//
// #region Design
// The library MessageInput owns the draft. InsertReference splices at the caret and dispatches
// a bubbling input event so Blazor sees the change. Typing @ is what opens the menu, so when the
// token starts with @ and the character just before the caret is @, that @ is replaced instead of
// doubled. CopyText writes the text C# passes: the same last-answer text the thumbs file, not the
// whole transcript. A denied clipboard stays quiet: copy is presentational and must not break
// the panel. Imported on demand (AskAiJsModule), not via window.Spa.
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

  area.value = area.value.slice(0, start) + token + area.value.slice(end);
  const caret = start + token.length;
  area.selectionStart = caret;
  area.selectionEnd = caret;
  area.dispatchEvent(new Event("input", { bubbles: true }));
  area.focus();
}

export async function CopyText(text: string): Promise<void> {
  try {
    await navigator.clipboard.writeText(text);
  } catch {
    // The browser denied the clipboard. The answer stays on screen.
  }
}
