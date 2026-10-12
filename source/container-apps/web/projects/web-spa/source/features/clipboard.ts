// #region Purpose
// Writes plain text to the system clipboard for the shell notification region's Copy button.
// #endregion
//
// #region Design
// Unlike ask-ai.ts CopyText, a failure is NOT swallowed: the C# caller (CopyMessage handler)
// reports a denied clipboard or an insecure context as a Warning bar, so the rejection must reach
// it as a JSException. Imported on demand (ClipboardJsModule), not via window.Spa.
// #endregion

export async function WriteText(text: string): Promise<void> {
  if (navigator.clipboard === undefined) {
    throw new Error("Clipboard access is unavailable in this context.");
  }

  await navigator.clipboard.writeText(text);
}
