// #region Purpose
// Paste listener for the feedback details box. Image files go to the page upload method.
// #endregion
//
// #region Design
// FluentTextArea is a web component. Register binds paste in the capture phase on the host
// and, when the shadow tree is ready, on its textarea. A WeakSet keeps one paste from being
// uploaded twice. preventDefault runs only when the clipboard holds an image. An empty
// clipboard name becomes pasted-image.png. The page method is ReceivePastedImage.
// #endregion

interface FeedbackPasteHost {
  invokeMethodAsync(methodName: string, ...args: unknown[]): Promise<unknown>;
}

export interface FeedbackPasteHandle {
  Dispose(): void;
}

export function Register(host: FeedbackPasteHost, root: HTMLElement): FeedbackPasteHandle {
  const listeners: Array<{ target: EventTarget; listener: EventListener }> = [];
  const bound = new WeakSet<EventTarget>();

  const onPaste: EventListener = (event) => {
    const clipboard = (event as ClipboardEvent).clipboardData;
    const files = clipboard?.files;
    if (!files || files.length === 0) {
      return;
    }

    const images: File[] = [];
    for (const file of files) {
      if (file.type.startsWith("image/")) {
        images.push(file);
      }
    }

    if (images.length === 0) {
      return;
    }

    event.preventDefault();
    event.stopPropagation();
    for (const file of images) {
      void send(host, file);
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

  const attach = (): void => {
    const area = root.querySelector("fluent-text-area");
    if (area) {
      bind(area);
    }

    const inner = area?.shadowRoot?.querySelector("textarea");
    if (inner) {
      bind(inner);
    }
  };

  attach();
  const area = root.querySelector("fluent-text-area");
  if (area?.shadowRoot?.querySelector("textarea") == null) {
    void customElements.whenDefined("fluent-text-area").then(() => attach());
  }

  return {
    Dispose(): void {
      for (const entry of listeners) {
        entry.target.removeEventListener("paste", entry.listener, true);
      }

      listeners.length = 0;
    },
  };
}

async function send(host: FeedbackPasteHost, file: File): Promise<void> {
  const buffer = new Uint8Array(await file.arrayBuffer());
  const name = file.name.length > 0 ? file.name : "pasted-image.png";
  const type = file.type.length > 0 ? file.type : "image/png";
  await host.invokeMethodAsync("ReceivePastedImage", name, type, buffer);
}
