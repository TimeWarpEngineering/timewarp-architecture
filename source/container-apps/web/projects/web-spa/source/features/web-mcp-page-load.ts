// #region Purpose
// Registers the site-wide navigate tool during HTML parse, before Blazor boots.
// #endregion
//
// #region Design
// Classic script (NOT an ES module — no import/export, wrapped in an IIFE). The host page emits
// it at the start of <body>, before blazor.web.js. The payload is the JSON element
// #timewarp-webmcp-page-load: the real navigate tool for this principal (enum of permitted URLs,
// Ask before editing so the person confirms). Feature-detect document.modelContext, then
// navigator.modelContext. registerTool is preferred. provideContext is the older one-shot API.
// When neither exists this script does nothing and does not install a polyfill. execute refuses
// a url that is not in the schema enum, asks with window.confirm, and location.assign only when
// the resolved URL stays on this origin. The abort handle is window.__timewarpWebMcpPageLoad so
// the later Blazor publish can unregister this navigate before it registers the same name.
// #endregion
(function (): void {
  'use strict';

  interface PageLoadTool {
    name: string;
    description: string;
    inputSchema: InputSchema;
    refusal: string;
  }

  interface InputSchema {
    properties?: {
      url?: {
        enum?: string[];
      };
    };
  }

  interface ModelContext {
    registerTool?: (tool: RegisteredTool, options?: { signal?: AbortSignal }) => void | Promise<void>;
    provideContext?: (context: { tools: RegisteredTool[] }) => void;
  }

  interface RegisteredTool {
    name: string;
    description: string;
    inputSchema: InputSchema;
    execute: (input: { url?: unknown } | undefined) => Promise<NavigateResult>;
  }

  interface NavigateResult {
    navigated: boolean;
    url?: string;
    error?: string;
  }

  const element = document.getElementById('timewarp-webmcp-page-load');
  const text = element?.textContent;
  if (!text) {
    return;
  }

  let payload: PageLoadTool;
  try {
    payload = JSON.parse(text) as PageLoadTool;
  } catch {
    return;
  }

  if (payload.name !== 'navigate' || !payload.inputSchema) {
    return;
  }

  const modelContext = resolveModelContext();
  if (!modelContext) {
    return;
  }

  const controller = new AbortController();
  const tool: RegisteredTool = {
    name: payload.name,
    description: payload.description,
    inputSchema: payload.inputSchema,
    execute: (input) => execute(payload, input),
  };

  if (typeof modelContext.registerTool === 'function') {
    const pending = modelContext.registerTool(tool, { signal: controller.signal });
    if (pending && typeof pending.then === 'function') {
      void pending.catch(() => undefined);
    }
  } else if (typeof modelContext.provideContext === 'function') {
    modelContext.provideContext({ tools: [tool] });
  } else {
    return;
  }

  window.__timewarpWebMcpPageLoad = {
    abort: () => {
      controller.abort();
    },
  };

  function resolveModelContext(): ModelContext | undefined {
    const documentContext = (document as Document & { modelContext?: ModelContext }).modelContext;
    if (documentContext) {
      return documentContext;
    }

    return (navigator as Navigator & { modelContext?: ModelContext }).modelContext;
  }

  function execute(spec: PageLoadTool, input: { url?: unknown } | undefined): Promise<NavigateResult> {
    const url = typeof input?.url === 'string' ? input.url : '';
    const allowed = spec.inputSchema.properties?.url?.enum ?? [];
    if (!allowed.includes(url)) {
      return Promise.resolve({ navigated: false, error: spec.refusal });
    }

    const allowedByPerson = window.confirm('Allow navigate to ' + url + '?');
    if (!allowedByPerson) {
      return Promise.resolve({ navigated: false, error: 'The person rejected the move.' });
    }

    let target: URL;
    try {
      target = new URL(url, document.baseURI);
    } catch {
      return Promise.resolve({ navigated: false, error: spec.refusal });
    }

    if (target.origin !== location.origin) {
      return Promise.resolve({ navigated: false, error: spec.refusal });
    }

    location.assign(target.href);
    return Promise.resolve({ navigated: true, url });
  }
})();
