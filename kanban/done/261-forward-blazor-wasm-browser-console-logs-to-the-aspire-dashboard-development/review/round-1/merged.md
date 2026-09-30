# Round 1 — merged findings
**Date:** 2026-10-01
**Sources:** general

## Counts (final, after the fix loop)

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 3 | 0 |
| suggestion | 0 | 3 | 1 |
| nit | 0 | 3 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/browser-log-forwarder.ts
- Description: `keepalive: true` on every flush. Browsers reject keepalive bodies over 64 KiB,
  so large stack-trace batches were dropped silently.
- Source: general
- Disposition notes: Regular flushes no longer set keepalive. Only the new `flushOnUnload`
  (pagehide) uses keepalive, and it halves its batch until a conservative UTF-8 estimate is
  under 60 KB.

### M2 — Severity: bug — Status: fixed
- File: browser-log-forwarder.ts
- Description: A whitespace-only message failed the server's NotEmpty rule and the whole batch
  was rejected with a 400.
- Source: general
- Disposition notes: `truncate()` maps a message that is empty after trim to `'(empty)'`.

### M3 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/browser-logs/forward-browser-logs/forward-browser-logs-contracts.cs
- Description: `entries: null` threw a NullReferenceException in the count rule (500), in
  Production too.
- Source: general
- Disposition notes: Added `.Cascade(CascadeMode.Stop)` on Entries and `.NotNull()` on each
  element. New test `ValidationError_Given_Null_Entries`.

### M4 — Severity: suggestion — Status: wontfix (partially addressed)
- File: forward-browser-logs-handler-application.cs / contracts
- Description: In Production the endpoint is still mapped; binding and validation run before the
  handler's 404, and the 404 body hints that the feature exists.
- Source: general
- Disposition notes:
  - The 404 body is now generic (no Detail).
  - M3 bounds validation.
  - Keeping the endpoint mapped is accepted: the generated FastEndpoints have no
    environment-gated registration, the 400 shape exposes only a public template contract, and
    the body-size posture is the same as every other anonymous endpoint.
  - The rationale is recorded in the contract's Design region.
  - Decided by: review oracle (orchestrator).

### M5 — Severity: suggestion — Status: fixed
- File: browser-log-rate-limiter-application.cs
- Description: A second limiter sits beside `platform/abuse` `AbuseRateLimitingModule` without
  a stated reason.
- Source: general
- Disposition notes: Took the reviewer's option (b). The Design region now says why: the abuse
  module is a Production per-IP request-window ring, while this is a dev-only volume cap
  weighted by entries, which a request-counting limiter cannot express.

### M6 — Severity: suggestion — Status: fixed
- File: forward-browser-logs-tests.cs
- Description: Missing validation tests.
- Source: general
- Disposition notes: Added tests for null entries, 51 entries, an unknown level, an unknown
  source, and an over-length PagePath. The runfile now passes 15/15.

### M7 — Severity: suggestion — Status: fixed
- File: browser-log-redactor-application.cs
- Description: Secrets in URL query or fragment parameters were not redacted, and PagePath was
  not redacted.
- Source: general
- Disposition notes: `SecretParameterPattern` redacts the values of `access_token`, `id_token`,
  `refresh_token`, `token`, `code`, `client_secret` and `password`, and keeps the keys.
  PagePath now goes through `Redact`. New test `Redacts_Secret_Query_Parameters`.

### M8 — Severity: nit — Status: fixed
- File: browser-log-forwarder.ts
- Description: Truncation could split a surrogate pair.
- Source: general
- Disposition notes: `truncate()` backs off one code unit when the cut would land on a high
  surrogate.

### M9 — Severity: nit — Status: fixed
- File: browser-log-forwarder.ts
- Description: The pagehide flush did nothing while a send was in flight or during a 429
  backoff.
- Source: general
- Disposition notes: `flushOnUnload` ignores both conditions. It is a last chance, and the
  in-flight batch was already spliced out, so nothing is sent twice.

### M10 — Severity: nit — Status: fixed
- File: forward-browser-logs-handler-application.cs
- Description: CR/LF in PagePath could forge lines in the console sink.
- Source: general
- Disposition notes: CR/LF in PagePath are replaced with spaces. Message keeps its newlines,
  because stack traces need them.

## Duplicates / conflicts

- None. There was a single reviewer.
