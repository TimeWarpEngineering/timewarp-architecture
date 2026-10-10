// #region Purpose
// Bounded text summary of the page body for page_context. No screenshots.
// #endregion
//
// #region Design
// One walk of .twe-page__body, including open shadow roots, so every page gets headings,
// a clipped summary, forms, buttons, and items without a per-page list. Caps match
// PageAgentContext: 40 headings, 2,000 summary characters, 30 buttons, 10 forms,
// 20 fields, 40 items, and 500 elements visited. Card titles are h2 elements in light DOM.
// screenshot is not produced here; the C# document reserves a null field for a later opt-in.
// #endregion

const maxHeadings = 40;
const maxSummaryCharacters = 2000;
const maxButtons = 30;
const maxForms = 10;
const maxFields = 20;
const maxItems = 40;
const maxElements = 500;

export interface PageSurfaceForm {
  name: string;
  fields: string[];
}

export interface PageSurfaceItem {
  id: string;
  text: string;
}

export interface PageSurface {
  headings: string[];
  summary: string;
  forms: PageSurfaceForm[];
  buttons: string[];
  items: PageSurfaceItem[];
}

export function summarizeJson(selector: string): string {
  const root = document.querySelector(selector);
  return JSON.stringify(summarize(root));
}

export function summarize(root: ParentNode | null): PageSurface {
  const surface: PageSurface = { headings: [], summary: "", forms: [], buttons: [], items: [] };
  if (!(root instanceof Element)) {
    return surface;
  }

  const elements = collect(root);
  for (const element of elements) {
    const tag = element.tagName;
    if (/^H[1-6]$/.test(tag)) {
      pushText(surface.headings, textOf(element), maxHeadings, 200);
    }
  }

  surface.summary = textOf(root).slice(0, maxSummaryCharacters);

  for (const element of elements) {
    const tag = element.tagName;
    if (tag === "BUTTON" || element.getAttribute("role") === "button") {
      pushText(surface.buttons, textOf(element), maxButtons, 120);
    }
  }

  let forms = 0;
  for (const element of elements) {
    if (element.tagName !== "FORM" || forms >= maxForms) {
      continue;
    }

    forms++;
    const fields: string[] = [];
    for (const field of element.querySelectorAll("input, textarea, select")) {
      if (fields.length >= maxFields) {
        break;
      }

      const name = field.getAttribute("name")
        || field.getAttribute("aria-label")
        || field.getAttribute("id")
        || "";
      if (name) {
        fields.push(name.slice(0, 80));
      }
    }

    const legend = element.querySelector("legend");
    surface.forms.push({ name: legend ? textOf(legend).slice(0, 120) : "", fields });
  }

  for (const element of elements) {
    if (surface.items.length >= maxItems) {
      break;
    }

    if (element.tagName !== "LI" && element.getAttribute("role") !== "listitem") {
      continue;
    }

    const id = element.id || element.getAttribute("data-id") || "";
    const value = textOf(element);
    if (!id && !value) {
      continue;
    }

    surface.items.push({ id: id.slice(0, 80), text: value.slice(0, 200) });
  }

  return surface;
}

function collect(root: Element): Element[] {
  const found: Element[] = [];
  const visit = (node: ParentNode): void => {
    const children = node.querySelectorAll("*");
    for (const child of children) {
      if (found.length >= maxElements) {
        return;
      }

      found.push(child);
      if (child.shadowRoot) {
        visit(child.shadowRoot);
      }
    }
  };

  visit(root);
  return found;
}

function textOf(node: Element): string {
  return (node.textContent ?? "").replace(/\s+/g, " ").trim();
}

function pushText(target: string[], value: string, cap: number, itemCap: number): void {
  if (!value || target.length >= cap) {
    return;
  }

  target.push(value.slice(0, itemCap));
}
