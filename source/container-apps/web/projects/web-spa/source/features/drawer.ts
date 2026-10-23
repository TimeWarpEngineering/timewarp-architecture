// #region Purpose
// Presentational open, close, and Tab wrap for one Drawer dialog.
// #endregion
//
// #region Design
// Bind takes the dialog and a .NET host. The dialog stays in the Blazor render tree.
// showModal puts it in the top layer with an ancestor .twe-shell of display:contents.
// In HeadlessChrome 153.0.8010.12 that dialog matches :modal, paints above the app bar,
// and opener.focus() leaves focus inside the panel. The dialog is left in place.
// The same Chrome does not keep Tab inside a modal dialog. A dialog that holds only
// native buttons, under a display:contents ancestor, still leaves on Tab from the last
// button, for both display:block and display:flex. A capture-phase keydown wraps Tab
// and Shift+Tab between the first and last tab stop. The walk enters open shadow
// roots, so a fluent-button host counts as its inner control.
// fluent-button is registered in FluentUI beforeStart, before Show, so Show calls
// showModal on the same turn. Close cannot be overtaken by a later showModal.
// Escape fires cancel. preventDefault keeps the browser from closing the dialog: the
// caller owns IsOpen, and Close runs after that flag drops, which returns focus.
// A click whose target is the dialog is the backdrop. Both paths call RequestClose.
// close() queues the close event, so a flag cleared when close() returns is already
// clear in the listener. pendingModuleClose counts Close. The listener takes one and
// returns. Any other close, such as a form with method dialog, calls RequestClose.
// Show and Close no-op when the dialog is already in that state. Show also returns
// once Dispose has run.
// Dispose removes the listeners, then closes an open dialog so navigation returns
// focus. The close listener is already gone, so that close does not call RequestClose.
// Imported on demand (DrawerJsModule), not via window.Spa.
// #endregion

interface DrawerHost {
  invokeMethodAsync(methodName: string, ...args: unknown[]): Promise<unknown>;
}

export interface DrawerHandle {
  Show(): void;
  Close(): void;
  Dispose(): void;
}

export function Bind(dialog: HTMLDialogElement, host: DrawerHost): DrawerHandle {
  let disposed = false;
  let pendingModuleClose = 0;

  const closeFromModule = (): void => {
    if (!dialog.open) {
      return;
    }

    pendingModuleClose += 1;
    dialog.close();
  };

  const onCancel = (event: Event): void => {
    event.preventDefault();
    host.invokeMethodAsync("RequestClose").catch(() => undefined);
  };

  const onClick = (event: MouseEvent): void => {
    if (event.target === dialog) {
      host.invokeMethodAsync("RequestClose").catch(() => undefined);
    }
  };

  const isFocusStop = (node: HTMLElement): boolean => {
    if (node === dialog || node.tabIndex < 0 || node.hasAttribute("disabled")) {
      return false;
    }

    if (node.getClientRects().length === 0) {
      return false;
    }

    const tag = node.tagName;
    if (tag === "INPUT" && (node as HTMLInputElement).type === "hidden") {
      return false;
    }

    if (tag === "A") {
      return node.hasAttribute("href");
    }

    if (tag === "BUTTON" || tag === "INPUT" || tag === "SELECT" || tag === "TEXTAREA") {
      return true;
    }

    return node.hasAttribute("tabindex");
  };

  const tabStops = (): HTMLElement[] => {
    const found: HTMLElement[] = [];
    const walk = (node: Element): void => {
      if (node instanceof HTMLElement && isFocusStop(node)) {
        found.push(node);
        return;
      }

      if (node.shadowRoot) {
        for (const child of node.shadowRoot.children) {
          walk(child);
        }
      }

      for (const child of node.children) {
        walk(child);
      }
    };

    walk(dialog);
    return found;
  };

  const isWithin = (stop: HTMLElement, active: Element | null): boolean => {
    let node: Node | null = active;
    while (node) {
      if (node === stop) {
        return true;
      }

      const root = node.getRootNode();
      node = root instanceof ShadowRoot ? root.host : (node as Element).parentElement;
    }

    return false;
  };

  const onKeyDown = (event: KeyboardEvent): void => {
    if (event.key !== "Tab") {
      return;
    }

    const stops = tabStops();
    if (stops.length === 0) {
      return;
    }

    const first = stops[0];
    const last = stops[stops.length - 1];
    const active = document.activeElement;
    if (event.shiftKey && isWithin(first, active)) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && isWithin(last, active)) {
      event.preventDefault();
      first.focus();
    }
  };

  const onDialogClose = (): void => {
    if (pendingModuleClose > 0) {
      pendingModuleClose -= 1;
      return;
    }

    host.invokeMethodAsync("RequestClose").catch(() => undefined);
  };

  dialog.addEventListener("cancel", onCancel);
  dialog.addEventListener("click", onClick);
  dialog.addEventListener("keydown", onKeyDown, true);
  dialog.addEventListener("close", onDialogClose);

  return {
    Show(): void {
      if (disposed || !dialog.isConnected || dialog.open) {
        return;
      }

      dialog.showModal();
    },
    Close(): void {
      closeFromModule();
    },
    Dispose(): void {
      disposed = true;
      dialog.removeEventListener("cancel", onCancel);
      dialog.removeEventListener("click", onClick);
      dialog.removeEventListener("keydown", onKeyDown, true);
      dialog.removeEventListener("close", onDialogClose);
      if (dialog.open) {
        dialog.close();
      }
    },
  };
}
