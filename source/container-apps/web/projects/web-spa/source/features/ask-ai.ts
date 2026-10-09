// #region Purpose
// Inserts an @ resource token into the Ask textarea and copies the rendered answer.
// #endregion
//
// #region Design
// The library MessageInput owns the draft. InsertReference splices at the caret and dispatches
// a bubbling input event so Blazor sees the change. CopyAnswer reads the rendered answer text.
// A denied clipboard stays quiet: copy is presentational and must not break the panel.
// Imported on demand (AskAiJsModule), not via window.Spa.
// #endregion

export function InsertReference(token: string): void {
  const area = document.querySelector<HTMLTextAreaElement>(".twe-agent-ask .sc-ai-input__textarea");
  if (area === null) {
    return;
  }

  const start = area.selectionStart ?? area.value.length;
  const end = area.selectionEnd ?? start;
  area.value = area.value.slice(0, start) + token + area.value.slice(end);
  const caret = start + token.length;
  area.selectionStart = caret;
  area.selectionEnd = caret;
  area.dispatchEvent(new Event("input", { bubbles: true }));
  area.focus();
}

export async function CopyAnswer(): Promise<void> {
  const root = document.querySelector<HTMLElement>(".twe-agent-ask .sc-ai-root");
  const text = root?.innerText ?? "";
  try {
    await navigator.clipboard.writeText(text);
  } catch {
    // The browser denied the clipboard. The answer stays on screen.
  }
}
