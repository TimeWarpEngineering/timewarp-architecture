// #region Purpose
// Ctrl-K / Cmd-K hotkey and appbar search-field trigger for the command palette, plus focus return on close.
// #endregion
//
// #region Design
// Task 239-003: the SPA had no global keydown. Register attaches one document listener and the
// trigger listeners for the element matching triggerSelector (the appbar search field), and asks
// .NET to open the palette (OpenCommandPalette). Ctrl-K is preventDefault-ed so the browser does
// not focus its own address/search bar. The element focused when the palette opened is kept so
// RestoreFocus can hand focus back on close; while restoring, the trigger's own focusin is ignored
// so returning focus to the search field does not reopen the palette. Register returns a handle
// (IJSObjectReference in C#) whose Dispose removes every listener — one handle per TimeWarpPage.
// Imported on demand (CommandPaletteJsModule), not via window.Spa.
// #endregion

interface CommandPaletteHost {
  invokeMethodAsync(methodName: string, ...args: unknown[]): Promise<unknown>;
}

export interface CommandPaletteHandle {
  RestoreFocus(): void;
  Dispose(): void;
}

export function Register(host: CommandPaletteHost, triggerSelector: string): CommandPaletteHandle {
  let returnFocusTo: HTMLElement | null = null;
  let restoring = false;
  const trigger = document.querySelector<HTMLElement>(triggerSelector);

  const open = (): void => {
    const active = document.activeElement;
    returnFocusTo = active instanceof HTMLElement && active !== document.body ? active : null;
    void host.invokeMethodAsync("OpenCommandPalette");
  };

  const onKeyDown = (event: KeyboardEvent): void => {
    if ((event.ctrlKey || event.metaKey) && !event.altKey && !event.shiftKey && event.key.toLowerCase() === "k") {
      event.preventDefault();
      open();
    }
  };

  const onTrigger = (): void => {
    if (!restoring) {
      open();
    }
  };

  document.addEventListener("keydown", onKeyDown);
  trigger?.addEventListener("focusin", onTrigger);
  trigger?.addEventListener("click", onTrigger);

  return {
    RestoreFocus(): void {
      const target = returnFocusTo;
      returnFocusTo = null;
      if (target === null || !target.isConnected) {
        return;
      }

      restoring = true;
      try {
        target.focus();
      } finally {
        restoring = false;
      }
    },
    Dispose(): void {
      document.removeEventListener("keydown", onKeyDown);
      trigger?.removeEventListener("focusin", onTrigger);
      trigger?.removeEventListener("click", onTrigger);
      returnFocusTo = null;
    },
  };
}
