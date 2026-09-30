// #region Purpose
// Development/Testing hook that forwards browser console errors/warnings, window errors and unhandled
// rejections to web-server (POST api/browser-logs) so they appear in the Aspire dashboard logs.
// #endregion
//
// #region Design
// Classic script (NOT an ES module — no import/export, wrapped in an IIFE): loaded by a plain
// <script src> BEFORE blazor.web.js so it sees failures that happen before the .NET runtime boots
// (e.g. a Mono assembly-load assertion from stale _framework assets). The host page only emits the
// tag in Development/Testing; the endpoint answers 404 elsewhere and this script then disables itself.
// Safety: originals are always called first; a re-entrancy guard and silent fetch failures prevent
// log loops; credentials are omitted (anonymous endpoint); only location.pathname is sent (never the
// query string or hash). Buffer caps at 200 entries; batches are at most 50 and flush every 1s or as
// soon as 50 are buffered. 429 backs the forwarder off; 404 disables it permanently.
// keepalive only on the pagehide flush: browsers reject keepalive bodies over 64 KiB, so that flush
// also trims its batch to KEEPALIVE_BYTES; it ignores an in-flight send or 429 backoff (last chance).
// Messages that are blank after trim become '(empty)' (server NotEmpty rejects whitespace), and
// truncation never splits a UTF-16 surrogate pair (a lone surrogate fails server JSON binding).
// Wire shape mirrors ForwardBrowserLogs.Command (web-contracts).
// #endregion
(function (): void {
  'use strict';

  interface LogEntry { level: string; source: string; message: string; }

  const MAX_MESSAGE = 4096;
  const MAX_BUFFER = 200;
  const MAX_BATCH = 50;
  const FLUSH_MS = 1000;
  const BACKOFF_MS = 10000;
  const KEEPALIVE_BYTES = 60000;

  const buffer: LogEntry[] = [];
  let forwarding = false;
  let inHook = false;
  let disabled = false;
  let backoffUntil = 0;
  let timer: number | undefined;

  function stringify(value: unknown): string {
    if (value instanceof Error) return value.stack || value.message;
    if (typeof value === 'string') return value;
    if (value !== null && typeof value === 'object') {
      try { return JSON.stringify(value); } catch { return String(value); }
    }
    return String(value);
  }

  function enqueue(level: string, source: string, message: string): void {
    if (disabled || inHook || buffer.length >= MAX_BUFFER) return;
    inHook = true;
    try { push(level, source, message); } finally { inHook = false; }
  }

  function truncate(message: string): string {
    if (message.trim() === '') return '(empty)';
    if (message.length <= MAX_MESSAGE) return message;
    const code = message.charCodeAt(MAX_MESSAGE - 1);
    return message.substring(0, code >= 0xD800 && code <= 0xDBFF ? MAX_MESSAGE - 1 : MAX_MESSAGE);
  }

  function push(level: string, source: string, message: string): void {
    buffer.push({ level, source, message: truncate(message) });
    if (buffer.length >= MAX_BATCH) {
      void flush();
    } else if (timer === undefined) {
      timer = window.setTimeout(() => { void flush(); }, FLUSH_MS);
    }
  }

  function unloadBody(): string {
    let count = Math.min(buffer.length, MAX_BATCH);
    let body = JSON.stringify({ entries: buffer.slice(0, count), pagePath: location.pathname });
    while (count > 1 && body.length * 3 > KEEPALIVE_BYTES) {
      count = Math.max(1, Math.floor(count / 2));
      body = JSON.stringify({ entries: buffer.slice(0, count), pagePath: location.pathname });
    }
    buffer.splice(0, count);
    return body;
  }

  function flushOnUnload(): void {
    if (disabled || buffer.length === 0) return;
    try {
      void fetch(new URL('api/browser-logs', document.baseURI).toString(), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: unloadBody(),
        credentials: 'omit',
        keepalive: true
      }).catch(() => { /* swallow: logging here would feed back into the console hook */ });
    } catch { /* never throw from the unload handler */ }
  }

  async function flush(): Promise<void> {
    if (timer !== undefined) { window.clearTimeout(timer); timer = undefined; }
    if (disabled || forwarding || buffer.length === 0) return;
    if (Date.now() < backoffUntil) {
      timer = window.setTimeout(() => { void flush(); }, backoffUntil - Date.now());
      return;
    }

    forwarding = true;
    const batch = buffer.splice(0, MAX_BATCH);
    try {
      const response = await fetch(new URL('api/browser-logs', document.baseURI).toString(), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ entries: batch, pagePath: location.pathname }),
        credentials: 'omit'
      });
      if (response.status === 404) {
        disabled = true;
        buffer.length = 0;
      } else if (response.status === 429) {
        backoffUntil = Date.now() + BACKOFF_MS;
      }
    } catch {
      // Swallow silently: logging here would feed back into the console hook.
    } finally {
      forwarding = false;
      if (!disabled && buffer.length > 0 && timer === undefined) {
        timer = window.setTimeout(() => { void flush(); }, FLUSH_MS);
      }
    }
  }

  function wrap(method: 'error' | 'warn', level: string): void {
    const original = console[method].bind(console);
    console[method] = (...args: unknown[]): void => {
      original(...args);
      try { enqueue(level, 'console', args.map(stringify).join(' ')); } catch { /* never throw from the hook */ }
    };
  }

  wrap('error', 'error');
  wrap('warn', 'warn');

  window.addEventListener('error', (event: ErrorEvent) => {
    const where = event.filename ? ` (${event.filename}:${event.lineno}:${event.colno})` : '';
    enqueue('error', 'window.error', (event.error ? stringify(event.error) : event.message) + where);
  });

  window.addEventListener('unhandledrejection', (event: PromiseRejectionEvent) => {
    enqueue('error', 'unhandledrejection', stringify(event.reason));
  });

  window.addEventListener('pagehide', flushOnUnload);
})();
