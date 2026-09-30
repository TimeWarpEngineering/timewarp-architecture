// #region Purpose
// Ctrl-K / Cmd-K hotkey and appbar search-field trigger for the command palette, plus focus return on close.
// #endregion
//
// #region Design
// Task 239-003: the SPA had no global keydown. Register attaches one document listener and the
// trigger listeners for the element matching triggerSelector (the appbar search field), and asks
// .NET to open the palette (OpenCommandPalette). Ctrl-K is preventDefault-ed so the browser does
// not focus its own address/search bar. The element focused when the palette opened is kept so
// RestoreFocus can hand focus back on close; it is captured once per open, so a repeat Ctrl-K
// while the palette is open does not replace it with the palette's own input. While restoring,
// the trigger's own focusin is ignored so returning focus to the search field does not reopen the
// palette. The same keydown listener suppresses ArrowUp/ArrowDown caret movement on the palette
// input (inputSelector); .NET still receives the key and moves the highlight. ScrollIntoView keeps
// the highlighted row visible. Register returns a handle (IJSObjectReference in C#) whose Dispose
// removes every listener — one handle per TimeWarpPage.
// Imported on demand (CommandPaletteJsModule), not via window.Spa.
// #endregion

interface CommandPaletteHost {
  invokeMethodAsync(methodName: string, ...args: unknown[]): Promise<unknown>;
}

export interface CommandPaletteHandle {
  RestoreFocus(): void;
  ScrollIntoView(id: string): void;
  Dispose(): void;
}

export function Register(host: CommandPaletteHost, triggerSelector: string, inputSelector: string): CommandPaletteHandle {
  let returnFocusTo: HTMLElement | null = null;
  let restoring = false;
  const trigger = document.querySelector<HTMLElement>(triggerSelector);

  const open = (): void => {
    if (returnFocusTo === null || !returnFocusTo.isConnected) {
      const active = document.activeElement;
      returnFocusTo = active instanceof HTMLElement && active !== document.body ? active : null;
    }

    // The .NET side may already be disposed (page navigated away before Dispose ran).
    host.invokeMethodAsync("OpenCommandPalette").catch(() => undefined);
  };

  const onKeyDown = (event: KeyboardEvent): void => {
    if ((event.ctrlKey || event.metaKey) && !event.altKey && !event.shiftKey && event.key.toLowerCase() === "k") {
      event.preventDefault();
      open();
      return;
    }

    if ((event.key === "ArrowUp" || event.key === "ArrowDown")
      && event.target instanceof Element && event.target.matches(inputSelector)) {
      event.preventDefault();
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
    ScrollIntoView(id: string): void {
      document.getElementById(id)?.scrollIntoView({ block: "nearest" });
    },
    Dispose(): void {
      document.removeEventListener("keydown", onKeyDown);
      trigger?.removeEventListener("focusin", onTrigger);
      trigger?.removeEventListener("click", onTrigger);
      returnFocusTo = null;
    },
  };
}
