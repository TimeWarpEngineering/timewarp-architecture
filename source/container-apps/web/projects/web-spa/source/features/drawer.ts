// #region Purpose
// Presentational open, close, and Tab wrap for one Drawer dialog.
// #endregion
//
// #region Design
// Bind takes the dialog element and a .NET host. The app shell is display:contents, and a
// modal dialog under that ancestor does not go into the top layer, so Tab leaves the panel.
// Show moves the dialog onto document.body before showModal. Blazor keeps the element
// reference, so later renders update it there. Show waits until the dismiss button's custom
// element is defined, so the first focusable control exists when the browser moves focus in.
// That wait can outlive a close. wantOpen records the caller's last request: Show sets it,
// Close and Dispose clear it, and a native close this module did not start clears it too.
// The deferred show returns without showModal when the flag is clear.
// Close calls close, which returns focus to the opener. Both no-op when the dialog is already
// in that state. The browser queues the close event, so a flag held only while close() runs
// is already clear when the listener runs. pendingModuleClose counts closes this module
// starts (Close, Dispose, or the move onto document.body). The listener takes one and
// returns. Any other close, such as a form with method dialog, still calls RequestClose.
// Chromium still does not wrap Tab when the focused control is a closed-shadow host
// (fluent-button), even after the dialog matches :modal on document.body. A capture-phase
// keydown wraps Tab and Shift+Tab between the first and last tab stop in the dialog.
// Escape fires cancel. preventDefault keeps the browser from closing the dialog itself: the
// caller owns IsOpen, and Close runs only after that flag drops, which is what restores focus.
// A click whose target is the dialog is a backdrop click. The panel box is only the drawer, so
// a click inside the panel hits a child. Both paths call RequestClose and do not close here.
// Dispose removes the listeners and closes an open dialog so a navigation still returns focus.
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
  let wantOpen = false;
  let pendingModuleClose = 0;

  const closeFromModule = (): void => {
    if (!dialog.open) {
      return;
    }

    pendingModuleClose += 1;
    dialog.close();
  };

  const placeOnBody = (): void => {
    if (dialog.parentElement !== document.body) {
      closeFromModule();
      document.body.appendChild(dialog);
    }
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
    if (event.key !== "Tab" || disposed) {
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

    if (disposed) {
      return;
    }

    wantOpen = false;
    host.invokeMethodAsync("RequestClose").catch(() => undefined);
  };

  dialog.addEventListener("cancel", onCancel);
  dialog.addEventListener("click", onClick);
  dialog.addEventListener("keydown", onKeyDown, true);
  dialog.addEventListener("close", onDialogClose);

  const show = (): void => {
    if (disposed || !wantOpen || !dialog.isConnected || dialog.open) {
      return;
    }

    placeOnBody();
    dialog.showModal();
  };

  return {
    Show(): void {
      wantOpen = true;
      placeOnBody();
      if (customElements.get("fluent-button")) {
        show();
        return;
      }

      customElements.whenDefined("fluent-button").then(() => {
        requestAnimationFrame(show);
      }).catch(() => undefined);
    },
    Close(): void {
      wantOpen = false;
      closeFromModule();
    },
    Dispose(): void {
      disposed = true;
      wantOpen = false;
      dialog.removeEventListener("cancel", onCancel);
      dialog.removeEventListener("click", onClick);
      dialog.removeEventListener("keydown", onKeyDown, true);
      dialog.removeEventListener("close", onDialogClose);
      closeFromModule();
    },
  };
}
