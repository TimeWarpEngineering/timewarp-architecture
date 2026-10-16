// #region Purpose
// Draws the page body, or one element, to a PNG for ScreenshotState.CaptureScreenshot.
// #endregion
//
// #region Design
// DOM-to-image, written here so there is no vendored library (task 303-001 Notes record the
// spike: getDisplayMedia needs a picker on every capture and would include the Ask panel).
// The target is cloned into an inert document, so custom elements are not upgraded again.
// Open shadow roots are flattened: a host's clone takes its shadow children, and a slot takes
// its assigned nodes (or its fallback). Each element keeps only the computed properties that
// differ from the UA default for its tag, plus the inherited text properties, so the SVG stays
// small. Defaults come from a hidden same-origin iframe that is removed after the capture.
// data-no-capture elements and password inputs become a solid box of the same size, counted in
// masked. Form values are copied. Same-origin images and canvases become data URLs; anything
// else (cross-origin images, iframes, video) is a blank box. The clone goes into an SVG
// foreignObject, which is drawn on a canvas filled with the first opaque ancestor background.
// The PNG is scaled down until it fits maxBytes, and both sides are capped at MaxSide.
// capture returns the size and a token; takeBytes(token) hands the PNG to .NET once as a
// Uint8Array, which .NET reads as an IJSStreamReference, so an InteractiveServer circuit
// streams it instead of sending one SignalR message over the hub's size limit.
// Limits: web fonts fall back to system fonts, ::before/::after and closed shadow roots are
// not drawn, and CSS url() backgrounds stay empty. A tainted canvas reports "tainted".
// #endregion

export interface ScreenshotCapture {
  token: string;
  width: number;
  height: number;
  byteLength: number;
  masked: number;
  error: string | null;
}

const MaxSide = 4096;
const MaskColor = "rgb(96, 96, 96)";
const BlankColor = "rgb(224, 224, 224)";
const XhtmlNamespace = "http://www.w3.org/1999/xhtml";
const SkippedTags = new Set(["SCRIPT", "STYLE", "NOSCRIPT", "TEMPLATE", "LINK", "META", "BASE", "TITLE"]);
const BlankTags = new Set(["IFRAME", "VIDEO", "AUDIO", "OBJECT", "EMBED"]);
const InheritedAlways = [
  "color",
  "font-family",
  "font-size",
  "font-style",
  "font-weight",
  "line-height",
  "letter-spacing",
  "text-align",
  "text-transform",
  "white-space",
  "word-spacing",
  "visibility",
  "direction",
];

const pending = new Map<string, Blob>();
let nextToken = 0;

interface CaptureContext {
  inert: Document;
  defaults: DefaultStyles;
  masked: number;
}

export async function capture(selector: string, maxBytes: number): Promise<ScreenshotCapture> {
  let target: Element | null;
  try {
    target = document.querySelector(selector);
  } catch {
    return failure("invalid-selector");
  }

  if (!(target instanceof Element)) {
    return failure("not-found");
  }

  const defaults = new DefaultStyles();
  try {
    const context: CaptureContext = {
      inert: document.implementation.createHTMLDocument(""),
      defaults,
      masked: 0,
    };
    const rect = target.getBoundingClientRect();
    const width = Math.min(MaxSide, Math.max(1, Math.ceil(Math.max(rect.width, target.scrollWidth))));
    const height = Math.min(MaxSide, Math.max(1, Math.ceil(Math.max(rect.height, target.scrollHeight))));
    const root = cloneElement(target, context);
    if (!(root instanceof Element)) {
      return failure("not-found");
    }

    pinRoot(root, width, height);
    const image = await render(root, width, height);
    const blob = await encode(image, width, height, backgroundOf(target), maxBytes);
    if (blob === null) {
      return failure("too-large");
    }

    const token = `shot-${++nextToken}`;
    pending.set(token, blob.data);
    return {
      token,
      width: blob.width,
      height: blob.height,
      byteLength: blob.data.size,
      masked: context.masked,
      error: null,
    };
  } catch (error) {
    console.error("Screenshot capture failed", error);
    return failure(error instanceof DOMException && error.name === "SecurityError" ? "tainted" : "render-failed");
  } finally {
    defaults.dispose();
  }
}

export async function takeBytes(token: string): Promise<Uint8Array> {
  const blob = pending.get(token);
  pending.delete(token);
  if (blob === undefined) {
    return new Uint8Array(0);
  }

  return new Uint8Array(await blob.arrayBuffer());
}

function failure(error: string): ScreenshotCapture {
  return { token: "", width: 0, height: 0, byteLength: 0, masked: 0, error };
}

function cloneNode(node: Node, context: CaptureContext): Node | null {
  if (node.nodeType === Node.TEXT_NODE) {
    return context.inert.createTextNode(node.textContent ?? "");
  }

  if (node instanceof Element) {
    return cloneElement(node, context);
  }

  return null;
}

function cloneElement(element: Element, context: CaptureContext): Node | null {
  if (SkippedTags.has(element.tagName.toUpperCase())) {
    return null;
  }

  const style = getComputedStyle(element);
  if (style.display === "none") {
    return null;
  }

  if (element.hasAttribute("data-no-capture") || isPassword(element)) {
    context.masked++;
    return box(element, style, context, MaskColor);
  }

  if (BlankTags.has(element.tagName.toUpperCase())) {
    return box(element, style, context, BlankColor);
  }

  const clone = context.inert.importNode(element, false) as Element;
  clone.removeAttribute("id");
  copyStyle(element, style, clone, context);
  copyValue(element, clone);
  if (element instanceof HTMLImageElement) {
    copyImage(element, clone);
    return clone;
  }

  if (element instanceof HTMLCanvasElement) {
    return canvasImage(element, style, context);
  }

  const children = element.shadowRoot ? element.shadowRoot.childNodes : element.childNodes;
  for (const child of children) {
    appendFlattened(child, clone, context);
  }

  return clone;
}

function appendFlattened(child: Node, parent: Element, context: CaptureContext): void {
  if (child instanceof HTMLSlotElement) {
    const assigned = child.assignedNodes({ flatten: true });
    const nodes = assigned.length > 0 ? assigned : Array.from(child.childNodes);
    for (const node of nodes) {
      const clone = cloneNode(node, context);
      if (clone !== null) {
        parent.appendChild(clone);
      }
    }

    return;
  }

  const clone = cloneNode(child, context);
  if (clone !== null) {
    parent.appendChild(clone);
  }
}

function isPassword(element: Element): boolean {
  return element instanceof HTMLInputElement && element.type.toLowerCase() === "password";
}

function box(element: Element, style: CSSStyleDeclaration, context: CaptureContext, color: string): Element {
  const rect = element.getBoundingClientRect();
  const replacement = context.inert.createElementNS(XhtmlNamespace, "div");
  const display = style.display.startsWith("inline") ? "inline-block" : "block";
  replacement.setAttribute(
    "style",
    `display:${display};box-sizing:border-box;width:${rect.width}px;height:${rect.height}px;`
      + `margin:${style.margin};border-radius:${style.borderRadius};background-color:${color};`
      + `vertical-align:${style.verticalAlign};flex:${style.flex};`);
  return replacement;
}

function copyStyle(element: Element, style: CSSStyleDeclaration, clone: Element, context: CaptureContext): void {
  const defaults = context.defaults.for(element);
  let text = "";
  for (let index = 0; index < style.length; index++) {
    const name = style[index];
    const value = style.getPropertyValue(name);
    if (value === defaults.get(name) && !InheritedAlways.includes(name)) {
      continue;
    }

    text += `${name}:${value};`;
  }

  // Animations would freeze mid-frame or replay inside the image.
  text += "animation:none;transition:none;";
  clone.setAttribute("style", text);
}

function copyValue(element: Element, clone: Element): void {
  if (element instanceof HTMLInputElement) {
    if (element.type === "checkbox" || element.type === "radio") {
      if (element.checked) {
        clone.setAttribute("checked", "");
      } else {
        clone.removeAttribute("checked");
      }
    } else {
      clone.setAttribute("value", element.value);
    }
  } else if (element instanceof HTMLTextAreaElement) {
    clone.textContent = element.value;
  } else if (element instanceof HTMLSelectElement) {
    clone.setAttribute("data-selected-index", String(element.selectedIndex));
  } else if (element instanceof HTMLOptionElement) {
    if (element.selected) {
      clone.setAttribute("selected", "");
    } else {
      clone.removeAttribute("selected");
    }
  }
}

function copyImage(element: HTMLImageElement, clone: Element): void {
  clone.removeAttribute("srcset");
  clone.removeAttribute("sizes");
  clone.removeAttribute("loading");
  const source = imageSource(element);
  if (source === null) {
    clone.removeAttribute("src");
  } else {
    clone.setAttribute("src", source);
  }
}

function imageSource(element: HTMLImageElement): string | null {
  const source = element.currentSrc || element.src;
  if (!source) {
    return null;
  }

  if (source.startsWith("data:")) {
    return source;
  }

  if (!element.complete || element.naturalWidth === 0) {
    return null;
  }

  try {
    const canvas = document.createElement("canvas");
    canvas.width = element.naturalWidth;
    canvas.height = element.naturalHeight;
    const context = canvas.getContext("2d");
    if (context === null) {
      return null;
    }

    context.drawImage(element, 0, 0);
    return canvas.toDataURL("image/png");
  } catch {
    // A cross-origin image taints the canvas; it is drawn blank.
    return null;
  }
}

function canvasImage(element: HTMLCanvasElement, style: CSSStyleDeclaration, context: CaptureContext): Element {
  let source: string;
  try {
    source = element.toDataURL("image/png");
  } catch {
    return box(element, style, context, BlankColor);
  }

  const image = context.inert.createElementNS(XhtmlNamespace, "img");
  copyStyle(element, style, image, context);
  image.setAttribute("src", source);
  return image;
}

function pinRoot(root: Element, width: number, height: number): void {
  const style = root.getAttribute("style") ?? "";
  root.setAttribute(
    "style",
    `${style}margin:0;position:static;transform:none;overflow:hidden;box-sizing:border-box;`
      + `width:${width}px;height:${height}px;max-width:none;max-height:none;`);
}

async function render(root: Element, width: number, height: number): Promise<HTMLImageElement> {
  const markup = new XMLSerializer().serializeToString(root);
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}">`
    + `<foreignObject x="0" y="0" width="100%" height="100%">${markup}</foreignObject></svg>`;
  const image = new Image();
  image.decoding = "sync";
  image.src = "data:image/svg+xml;charset=utf-8," + encodeURIComponent(svg);
  await image.decode();
  return image;
}

interface Encoded {
  data: Blob;
  width: number;
  height: number;
}

async function encode(
  image: HTMLImageElement,
  width: number,
  height: number,
  background: string,
  maxBytes: number): Promise<Encoded | null> {
  for (let scale = 1; scale >= 0.2; scale *= 0.7) {
    const canvas = document.createElement("canvas");
    canvas.width = Math.max(1, Math.round(width * scale));
    canvas.height = Math.max(1, Math.round(height * scale));
    const context = canvas.getContext("2d");
    if (context === null) {
      throw new Error("No 2d canvas context.");
    }

    context.fillStyle = background;
    context.fillRect(0, 0, canvas.width, canvas.height);
    context.drawImage(image, 0, 0, canvas.width, canvas.height);
    const data = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, "image/png"));
    if (data === null) {
      throw new Error("The canvas produced no PNG.");
    }

    if (data.size <= maxBytes) {
      return { data, width: canvas.width, height: canvas.height };
    }
  }

  return null;
}

function backgroundOf(element: Element): string {
  for (let node: Element | null = element; node !== null; node = node.parentElement) {
    const color = getComputedStyle(node).backgroundColor;
    if (color && color !== "transparent" && !/rgba\(.*,\s*0\)$/.test(color)) {
      return color;
    }
  }

  return "rgb(255, 255, 255)";
}

/** UA default computed styles per tag, read from a hidden same-origin iframe. */
class DefaultStyles {
  private readonly cache = new Map<string, Map<string, string>>();
  private frame: HTMLIFrameElement | null = null;

  for(element: Element): Map<string, string> {
    const key = `${element.namespaceURI}|${element.localName}`;
    const cached = this.cache.get(key);
    if (cached !== undefined) {
      return cached;
    }

    const values = new Map<string, string>();
    const sandbox = this.document();
    if (sandbox !== null) {
      const probe = sandbox.createElementNS(element.namespaceURI ?? XhtmlNamespace, element.localName);
      const host = element.namespaceURI === XhtmlNamespace
        ? sandbox.body
        : sandbox.body.appendChild(sandbox.createElementNS("http://www.w3.org/2000/svg", "svg"));
      host.appendChild(probe);
      const style = sandbox.defaultView?.getComputedStyle(probe);
      if (style) {
        for (let index = 0; index < style.length; index++) {
          const name = style[index];
          values.set(name, style.getPropertyValue(name));
        }
      }

      if (host === sandbox.body) {
        probe.remove();
      } else {
        host.remove();
      }
    }

    this.cache.set(key, values);
    return values;
  }

  dispose(): void {
    this.frame?.remove();
    this.frame = null;
  }

  private document(): Document | null {
    if (this.frame === null) {
      const frame = document.createElement("iframe");
      frame.setAttribute("aria-hidden", "true");
      frame.tabIndex = -1;
      frame.style.position = "fixed";
      frame.style.left = "-10000px";
      frame.style.top = "0";
      frame.style.width = "10px";
      frame.style.height = "10px";
      frame.style.border = "0";
      frame.style.visibility = "hidden";
      document.body.appendChild(frame);
      this.frame = frame;
    }

    return this.frame.contentDocument;
  }
}
