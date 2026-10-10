// #region Purpose
// Paste listener for the feedback details box. Image files go to the page upload method.
// #endregion
//
// #region Design
// Fluent UI v5 renders the details box as fluent-textarea, not fluent-text-area. The page
// may pass that element or a wrapper around it. Register binds paste in the capture phase
// on the control and on the textarea inside its open shadow root. A paste on the inner
// control does not cross the shadow boundary unless the event is composed, so the inner
// listener is the one the details box hits. A MutationObserver binds that textarea when it
// appears and again if the control replaces it. customElements.whenDefined covers a control
// that is not upgraded yet; one animation frame covers a shadow tree that lands just after
// upgrade. The host listener stops a composed paste so the file is uploaded once.
// preventDefault runs only when the clipboard holds an image. Files are read from
// clipboardData.files, then from items when files is empty. An empty clipboard name becomes
// pasted-image.png. The page method is ReceivePastedImage. maxBytes comes from .NET
// (FeedbackAttachmentRules.MaxBytes) at register time; a larger file is refused before it
// is read. A refusal or a failed read/send goes to the page's ReceivePasteRejected so the
// user sees the normal error notification; if that call fails too, the error goes to the
// console. The file goes to .NET as a JS stream reference (DotNet.createJSStreamReference),
// not a byte array, so an InteractiveServer circuit reads it in chunks instead of one
// SignalR message over the hub's size limit.
// #endregion

declare const DotNet: {
  createJSStreamReference(data: Blob): unknown;
};

interface FeedbackPasteHost {
  invokeMethodAsync(methodName: string, ...args: unknown[]): Promise<unknown>;
}

export interface FeedbackPasteHandle {
  Dispose(): void;
}

const ControlTag = "fluent-textarea";

export function Register(host: FeedbackPasteHost, root: HTMLElement, maxBytes: number): FeedbackPasteHandle {
  const listeners: Array<{ target: EventTarget; listener: EventListener }> = [];
  const bound = new WeakSet<EventTarget>();
  const observed = new WeakSet<Node>();
  const observers: MutationObserver[] = [];
  let disposed = false;
  let retried = false;
  let retryHandle = 0;

  const onPaste: EventListener = (event) => {
    const images = imageFiles((event as ClipboardEvent).clipboardData);
    if (images.length === 0) {
      return;
    }

    event.preventDefault();
    event.stopPropagation();
    for (const file of images) {
      void send(host, file, maxBytes);
    }
  };

  const bind = (target: EventTarget): void => {
    if (bound.has(target)) {
      return;
    }

    bound.add(target);
    target.addEventListener("paste", onPaste, true);
    listeners.push({ target, listener: onPaste });
  };

  const watch = (node: Node): void => {
    if (disposed || observed.has(node)) {
      return;
    }

    observed.add(node);
    const observer = new MutationObserver(() => {
      attach();
    });
    observer.observe(node, { childList: true, subtree: true });
    observers.push(observer);
  };

  const attach = (): void => {
    if (disposed) {
      return;
    }

    watch(root);
    const area = controlOf(root);
    if (!area) {
      return;
    }

    bind(area);
    const shadow = area.shadowRoot;
    if (!shadow) {
      scheduleRetry();
      return;
    }

    watch(shadow);
    const inner = shadow.querySelector("textarea");
    if (!inner) {
      scheduleRetry();
      return;
    }

    bind(inner);
  };

  const scheduleRetry = (): void => {
    if (retried || disposed) {
      return;
    }

    retried = true;
    retryHandle = requestAnimationFrame(() => {
      retryHandle = 0;
      attach();
    });
  };

  attach();
  void customElements.whenDefined(ControlTag).then(() => {
    attach();
  });

  return {
    Dispose(): void {
      disposed = true;
      if (retryHandle !== 0) {
        cancelAnimationFrame(retryHandle);
        retryHandle = 0;
      }

      for (const observer of observers) {
        observer.disconnect();
      }

      observers.length = 0;
      for (const entry of listeners) {
        entry.target.removeEventListener("paste", entry.listener, true);
      }

      listeners.length = 0;
    },
  };
}

function controlOf(root: HTMLElement): HTMLElement | null {
  if (root.localName === ControlTag) {
    return root;
  }

  const found = root.querySelector(ControlTag);
  return found instanceof HTMLElement ? found : null;
}

function imageFiles(clipboard: DataTransfer | null): File[] {
  if (clipboard == null) {
    return [];
  }

  const images: File[] = [];
  const take = (file: File | null): void => {
    if (file != null && file.type.startsWith("image/")) {
      images.push(file);
    }
  };

  if (clipboard.files != null) {
    for (const file of clipboard.files) {
      take(file);
    }
  }

  if (images.length === 0 && clipboard.items != null) {
    for (const item of clipboard.items) {
      if (item.kind === "file") {
        take(item.getAsFile());
      }
    }
  }

  return images;
}

async function send(host: FeedbackPasteHost, file: File, maxBytes: number): Promise<void> {
  if (file.size > maxBytes) {
    await reject(host, "too-large");
    return;
  }

  try {
    const name = file.name.length > 0 ? file.name : "pasted-image.png";
    const type = file.type.length > 0 ? file.type : "image/png";
    await host.invokeMethodAsync("ReceivePastedImage", name, type, DotNet.createJSStreamReference(file));
  } catch (error) {
    console.error("Feedback paste failed", error);
    await reject(host, "failed");
  }
}

async function reject(host: FeedbackPasteHost, reason: string): Promise<void> {
  try {
    await host.invokeMethodAsync("ReceivePasteRejected", reason);
  } catch (error) {
    console.error("Feedback paste could not report a rejection", error);
  }
}
