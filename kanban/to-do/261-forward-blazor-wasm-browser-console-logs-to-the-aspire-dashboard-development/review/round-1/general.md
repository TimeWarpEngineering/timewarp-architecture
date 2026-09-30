# Round 1 — general
**Date:** 2026-10-01
**Scope reviewed:** branch task/261 vs origin/master

## Summary
The fallback design is sound and the slice follows the conventions: an `[ApiEndpoint]` contract with
`[EndpointAllowAnonymous]`, co-located handler, Purpose/Design regions, and filename grammar. The
emitted JS is a classic script loaded before `blazor.web.js`. The runfile passes 9/9 locally. Three
defects break the "capture the error" goal or the Production posture:
- `keepalive: true` on every flush drops large batches.
- A whitespace-only console message rejects the whole batch.
- `"entries": null` throws in the validator. The validator runs before the environment gate, so
  this also happens in Production.

The rest are hardening and coverage suggestions.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/source/browser-log-forwarder.ts:77
- Description: Every flush uses `keepalive: true`, not just the unload flush. The Fetch spec, as
  Chrome and Edge enforce it, rejects a keepalive request with a network error (a TypeError) when
  its body plus other in-flight keepalive bodies exceed 64 KiB. A batch can hold up to 50 entries
  of 4096 chars each, and JSON escaping of stack-trace newlines and quotes adds more, so bodies
  reach roughly 200 KB. A burst of about 16 or more stack traces (typical for a Blazor unhandled
  exception or a WASM boot failure) makes `fetch` throw. The `catch` swallows the error, and the
  batch was already `splice`d, so those entries are lost silently. This hits the exact scenario
  the feature exists for.
- Suggestion: Use `keepalive` only for the `pagehide` flush. Alternatively, cap each batch by
  serialized byte size (well under 64 KiB) as well as by count.
- Status: open

### Issue 2 — Severity: bug
- File: source/container-apps/web/projects/web-spa/source/browser-log-forwarder.ts:51 (with forward-browser-logs-contracts.cs:62)
- Description: The client replaces only an empty string with `'(empty)'`. The server validates
  `Message` with `NotEmpty()`, which in FluentValidation also rejects whitespace-only strings. So
  `console.error(' ')`, `console.error('\n')` or `console.error('', '')` (joined to `" "`) produce
  an entry the server rejects. The whole batch, up to 49 real errors, then gets a 400 and is dropped.
- Suggestion: On the client, use `message.trim() === '' ? '(empty)' : …`. Or relax the server rule
  to `NotNull()` plus a max length, but keep the client and server rules in agreement.
- Status: open

### Issue 3 — Severity: bug
- File: source/container-apps/web/features/browser-logs/forward-browser-logs/forward-browser-logs-contracts.cs:55-57
- Description: `RuleFor(c => c.Entries).NotEmpty().Must(entries => entries.Count <= MaxEntriesPerBatch)`
  has no `Cascade(CascadeMode.Stop)`, and there is no global stop cascade in the repo. A body of
  `{"entries":null}` deserializes `Entries` as null, because System.Text.Json overwrites the `[]`
  initializer. `NotEmpty` then records a failure, and `Must` still runs and throws a
  NullReferenceException, so the response is a 500 instead of a 400. I verified this with
  FluentValidation 12 in a scratch runfile. `FluentValidationBehavior` runs before the handler's
  environment gate, so an anonymous caller can trigger this in **Production** too: a 500 plus an
  error log entry, instead of the intended fail-closed 404.
- Suggestion: Add `.Cascade(CascadeMode.Stop)` to the `Entries` rule, or make the lambda
  null-safe (`entries is null || entries.Count <= Max`). Add a validation test for a null
  `entries` value.
- Status: open

### Issue 4 — Severity: suggestion
- File: source/container-apps/web/features/browser-logs/forward-browser-logs/forward-browser-logs-handler-application.cs:36 (with web-server/program.cs:206)
- Description: "Fail-closed in Production" is only a handler-level 404. The FastEndpoint is still
  mapped anonymously in Production. Request binding, which takes the whole JSON body up to
  Kestrel's 30 MB default, and validation (`RuleForEach` over an unbounded list before the
  `Count <= 50` check matters) both run before the gate. Validation failures answer 400 with a
  field-level error shape, so the endpoint and its contract are discoverable in Production. The
  404 body also reads "only available in Development and Testing". The mock-auth precedent
  (145-009) gates registration, not just behavior.
- Suggestion: Reject before binding outside Development/Testing, for example with a small
  path-classified check or FastEndpoints `Configure` / `DontAutoSend` gated on the environment,
  or with a request-body size limit on this route. At minimum, return a generic 404 body. Accept
  and document this in the contract's Design region if registration-time gating is not possible
  with the generator.
- Status: open

### Issue 5 — Severity: suggestion
- File: source/container-apps/web/features/browser-logs/forward-browser-logs/browser-log-rate-limiter-application.cs:1-33
- Description: The repo already has an app-ring rate limiting mechanism,
  `platform/abuse/abuse-rate-limiting-module-server.cs`. It is an ASP.NET `GlobalLimiter` that is
  path-classified, partitioned per IP, and runs before FastEndpoints binding. It returns a
  structured 429 with `Retry-After`. This change adds a second, divergent mechanism: an in-handler
  global token bucket. That bucket runs after deserialization and validation, is shared by all
  callers, and returns no `Retry-After`. Its Design region does not mention why the existing
  module was not used.
- Suggestion: Either add a `browser-logs` policy to `AbuseRateLimitingModule` (it could use a
  token bucket, since entry-weighted permits are the reason for this class), or record in the
  Design region why the platform limiter does not fit.
- Status: open

### Issue 6 — Severity: suggestion
- File: source/container-apps/web/features/browser-logs/forward-browser-logs/forward-browser-logs-tests.cs:95-125
- Description: The brief asks for "endpoint validation, cap and rate-limit tests". The tests cover
  only empty entries and an over-length message. There are no tests for:
  - a batch over `MaxEntriesPerBatch` (51 entries), which is the size cap
  - an unknown `Level` or `Source`, which are the allow-lists
  - `PagePath` over 512 characters
  - `entries: null` (Issue 3)
- Suggestion: Add these validation cases, all cheap against the same host.
- Status: open

### Issue 7 — Severity: suggestion
- File: source/container-apps/web/features/browser-logs/forward-browser-logs/browser-log-redactor-application.cs:19-26
- Description: Redaction covers only `Bearer …` values and JWT-shaped strings in `Message`.
  Browser console messages from failed fetches and navigations often contain full URLs with query
  strings or fragments: OAuth `code=` / `state=`, `access_token=` / `id_token=` in the fragment,
  and similar. The brief's constraint is "no tokens, cookies or PII in payloads". The client
  strips the query only from `pagePath`, not from message text. `pagePath` is also logged without
  redaction.
- Suggestion: Also redact common secret query and fragment parameters (`access_token`,
  `id_token`, `refresh_token`, `code`, `token`), or strip query strings and fragments from URLs
  found in messages. Keep the Design region's "best-effort" wording.
- Status: open

### Issue 8 — Severity: nit
- File: source/container-apps/web/projects/web-spa/source/browser-log-forwarder.ts:51
- Description: `message.substring(0, MAX_MESSAGE)` can split a UTF-16 surrogate pair.
  `JSON.stringify` then emits a lone `\udXXX` escape. System.Text.Json rejects invalid UTF-16 in
  escaped strings, so the whole batch fails to bind (400) and is dropped. This is rare: it needs
  an emoji or other astral character exactly at the 4096 boundary.
- Suggestion: Back off one code unit when the character at index `MAX_MESSAGE - 1` is a high
  surrogate.
- Status: open

### Issue 9 — Severity: nit
- File: source/container-apps/web/projects/web-spa/source/browser-log-forwarder.ts:59-62,113
- Description: The `pagehide` flush returns early when a flush is already in flight
  (`forwarding`) or during a 429 backoff, which schedules a timer that will never fire. Entries
  buffered at unload are lost in those cases. This is acceptable for a dev tool but worth noting
  in the Design region.
- Suggestion: On `pagehide`, send any remaining buffer with `navigator.sendBeacon` (it cannot set
  `Content-Type: application/json` without a Blob, so use a JSON-typed Blob), or document the
  limitation.
- Status: open

### Issue 10 — Severity: nit
- File: source/container-apps/web/features/browser-logs/forward-browser-logs/forward-browser-logs-handler-application.cs:79-80
- Description: `Message` and `PagePath` are client-controlled and can contain CR/LF. Structured
  OTLP attributes are safe, but the plain console log sink (the Aspire "Console logs" view)
  renders forged-looking lines. The endpoint is dev-only and anonymous.
- Suggestion: Optionally replace `\r` and `\n` in `PagePath`, since it is a single-line field;
  leave `Message` multi-line for stack traces.
- Status: open

## Verified OK (no action)
- `App.razor` emits the script only when `BrowserLogForwarding.IsEnabled`, and before
  `_framework/blazor.web.js`. There are no template flag regions in the touched razor or
  program.cs lines.
- The emitted `wwwroot/js/browser-log-forwarder.js` is a classic IIFE with no `export {}`
  (TS 7 emit), and it is gitignored like the other TS outputs.
- There is no recursion loop:
  - The hook calls the original method first and has a re-entrancy guard.
  - Fetch failures are swallowed without console output.
  - Browser-native "Failed to load resource" messages do not go through the `console.*` API.
  - The window `error` listener is bubble-phase, so resource-load errors are not captured.
- The rate limiter is correct:
  - `AttemptAcquire(n)` with n ≤ 50 < `TokenLimit` 200 never throws.
  - The lease is disposed.
  - The singleton is disposed by the DI container.
  - `QueueLimit = 0`.
- `SharedProblemDetails.Status` drives the HTTP status in `BaseFastEndpoint` (404/429 as intended).
- The contract's nullability agrees with the validator (TWA0002/0003). The handler follows the
  `TrackEvent` pattern.
- `dotnet run …/forward-browser-logs-tests.cs` passes 9/9.
