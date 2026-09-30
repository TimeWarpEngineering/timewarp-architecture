# Forward Blazor WASM browser console logs to the Aspire dashboard (Development)

## Description

web-spa runs in the browser (Blazor WebAssembly), so its errors reach only the browser DevTools
console. They never reach the Aspire dashboard. On 2026-10-01 a Mono assembly-load assertion
(`metadata/assembly.c:1673`, from stale `_framework` assets) was invisible in Aspire. The
structured and console logs of every resource were clean, and the maintainer had to paste the
error from F12.

Goal: in Development, browser console output (errors at minimum; warnings and the app's ILogger
output if cheap) shows up in the Aspire dashboard, where both the maintainer and agents using
the Aspire MCP tools already look.

## Requirements

1. **Investigate Aspire's built-in browser log capture first** (`WithBrowserLogs`; the pinned
   Aspire is 13.5.x). Confirm what it needs (browser automation? a resource wrapping the ingress
   or web-server URL?), what it captures (console errors, warnings, unhandled exceptions,
   network?), and whether it works with the real browser the maintainer uses or only with a
   browser Aspire launches. Record the findings in Notes. Use the `aspire` skills and the Aspire
   docs MCP (`search_docs` / `get_doc`); don't guess the API.
2. **Choose and implement.**
   - **Preferred:** built-in `WithBrowserLogs` in the AppHost. It must be Development-only and
     must not change Production topology.
   - **Fallback, if the built-in feature can't capture the maintainer's own browser session:**
     forward the SPA's logs over OpenTelemetry. That means a WASM `ILoggerProvider`, or a
     JS console hook, that posts to a web-server endpoint. The endpoint relays into the
     server's ILogger / OTLP pipeline under a distinct category (for example
     `Web.Spa.Browser`).
   - Fallback constraints:
     - Development / Testing only, fail-closed in Production (same posture as mock auth,
       task 145-009).
     - Rate-limited and size-capped.
     - Anonymous-safe: no tokens, cookies or PII in payloads.
     - A server endpoint must follow the contracts rules: an `[ApiEndpoint]` contract with an
       explicit `[EndpointAllowAnonymous(reason)]` or `[EndpointAuthorize]`, or a
       documented browser-protocol exception if it truly isn't contract JSON.
     - Browser-side sending lives in a TimeWarp.State action or a logging provider, never a
       page method. See the rule that every SPA user interaction is a State action, task 260.
   - Record the choice and the reason in the Design region of the file that owns it.
3. **Template:** the change ships to generated apps. Keep the flag regions (`web`, and others)
   intact, and make sure `dev template-smoke` passes.
4. **Docs:** add a short rule or how-to to the owning skill: where browser logs appear, and
   that stale `_framework` assets show up as a Mono assembly assertion, fixed by `dev clean` and
   clearing site data.

## Checklist

- [x] Findings on the built-in `WithBrowserLogs` recorded in Notes (capabilities, limits,
      whether it sees the maintainer's own browser)
- [x] Implementation (built-in preferred; fallback only with a recorded reason)
- [x] Development-only; Production unaffected (a test or assertion that proves it)
- [x] Tests: AppHost model test (Aspire.Hosting.Testing closed-box lane, or a model inspection)
      showing the browser-log wiring is present in Development and absent otherwise. If the
      fallback is used: endpoint validation, cap and rate-limit tests, and a Production
      fail-closed test
- [x] Skill note (where the logs appear; the stale-asset Mono assertion symptom)
- [x] Purpose/Design regions reconciled
- [x] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`
- [x] Do **not** start the AppHost manually (`dev run`, `aspire run`, or `dotnet run` of the
      AppHost): it shares the maintainer's user secrets. Closed-box Aspire.Hosting.Testing
      suites that `dev test` already runs are fine. Record the manual dashboard check as not
      performed.
- [x] Implementation review (disposition: accepted-exceptions, see `review/disposition.md`)
- [ ] Host `open-pr`

## Session

- Created: 571175 (2026-10-01)
- Implemented: ganda task work implement oracle (2026-10-01). Fallback path; AppHost never started.
- Reviewed: ganda task work review oracle (2026-10-01), effort 2, general reviewer (Claude Opus
  subagent a62c6400b2388d870); fixes applied on this id.

## Notes

- **Built-in `WithBrowserLogs` findings** (aspire.dev `browser-logs`, `aspirebrowserlogs001`):
  - Ships in a separate, preview hosting package, `Aspire.Hosting.Browsers` (13.5.3-preview). The
    API is experimental and gated by `ASPIREBROWSERLOGS001`.
  - Adds a `<parent>-browser-logs` child resource with dashboard commands: Open tracked browser,
    Configure tracked browser, and Capture screenshot.
  - Captures over the Chrome DevTools Protocol: Runtime (console messages), Log, Page lifecycle,
    and Network (requests and failures).
  - Needs Edge or Chrome installed where the AppHost runs. Here the AppHost runs in WSL, so that
    means a Linux Chromium or an explicit executable path.
  - **It only sees a tracked browser that Aspire launches**, which uses an Aspire-managed user
    data dir (Shared or Isolated) and never the user's normal profile. It cannot see the
    maintainer's own browser session, so the brief's fallback condition is met.
- **Choice: fallback.** A classic JS hook (`web-spa/source/browser-log-forwarder.ts` →
  `js/browser-log-forwarder.js`) is loaded by `App.razor` before `blazor.web.js`, only when
  `BrowserLogForwarding.IsEnabled` (Development/Testing). It POSTs batches to the
  `[ApiEndpoint]` contract `ForwardBrowserLogs` (`POST api/browser-logs`,
  `[EndpointAllowAnonymous]`). The handler logs under category `Web.Spa.Browser`.
  - JS, not a WASM `ILoggerProvider`: the target failure (a Mono assembly assertion) happens
    before the .NET runtime boots. WASM ILogger warnings and errors reach console.warn/error, so
    the hook captures them too.
  - The hook is a pre-runtime logging provider, not a page method or a user interaction, so the
    task 260 State-action rule doesn't apply.
  - The reasons are recorded in the Design regions of the contract, the handler and the TS file.
- Posture:
  - Fail-closed 404 outside Development/Testing, and no script tag in Production.
  - Global token bucket: 200 entries, refilled at 100 per 10s → 429.
  - Size caps: ≤ 50 entries per batch, 4096 characters per message, 512-character page path.
  - `credentials: 'omit'`, pathname only (no query or hash), and bearer/JWT redaction server-side.
- No AppHost change, so no AppHost model test. The Production-absent proof is the host-free
  `IsEnabled` test plus the handler run under a Production environment, which returns 404.

- Memory discipline: task 260 may be building at the same time. Run builds and tests serially
  and call `dotnet build-server shutdown` before finishing.

## Results

- New slice `source/container-apps/web/features/browser-logs/forward-browser-logs/`:
  - contract, handler, `BrowserLogForwarding` gate, `BrowserLogRateLimiter`, `BrowserLogRedactor`
  - Jaribu runfile `forward-browser-logs-tests.cs`, 9 tests: happy path, empty entries,
    over-length message, 429, Production/Development/Testing gate, Production handler 404, and
    redaction
- `web-spa/source/browser-log-forwarder.ts` (TS pipeline → `wwwroot/js`).
- `App.razor` emits the script tag in Development/Testing only. `program.cs` registers the
  limiter singleton.
- `web-application` gains `Microsoft.Extensions.Hosting.Abstractions` and
  `System.Threading.RateLimiting` (CPM 10.0.12).
- Skill note: `skills/tw-blazor/SKILL.md`, "Browser console logs in the Aspire dashboard".
- The ingress carve-out for `api/browser-logs` is generated automatically (task 107 generator).
- Gates (2026-10-01): `dev build` 0/0; `dev test` green, 0 failed; `dev template-smoke`
  SUCCEEDED; `ganda repo audit` passes. The first smoke run collided with task 260's concurrent
  smoke on the fixed port base 17000; the re-run was clean.
- Manual dashboard check: **not performed**, because the AppHost may not be started by workers.

### Review disposition

- Rounds: 2. Round 1 was the general reviewer; round 2 was the oracle re-verifying the fixes.
  Effort 2, roster: general.
- Final counts:
  - bug: 3 fixed
  - suggestion: 3 fixed, 1 wontfix
  - nit: 3 fixed
  - open: 0
- Outcome: **accepted-exceptions**. The one exception is M4: the generated endpoint stays mapped
  in Production, so binding and validation run before the 404. The 404 body is now generic.
  The rationale is in the contract's Design region.
- Fixes:
  - keepalive only for the pagehide flush, with a byte cap
  - blank messages become `(empty)`
  - stop cascade for a null `entries`
  - URL secret parameter redaction, which PagePath also goes through
  - PagePath CR/LF replaced with spaces
  - surrogate-safe truncation
  - the unload flush ignores an in-flight send and the 429 backoff
  - the limiter's Design region explains why it is separate from the abuse module
  - 6 new tests; the runfile now passes 15/15
- Post-fix gates: `dev build` 0/0; web-jaribu-tests 227/227; `ganda repo audit` passes.
- Artifacts: `review/review-framework.md`, `review/round-1/{general,merged}.md`,
  `review/round-2/{general,merged}.md`, `review/disposition.md`.

### How to validate

Smoke: `dotnet run source/container-apps/web/features/browser-logs/forward-browser-logs/forward-browser-logs-tests.cs`
Expect: 15/15 pass. That covers POST `api/browser-logs` → Accepted, validation rejections, a 429
after the bucket drains, `IsEnabled` false in Production, the handler returning 404 in
Production, and bearer/JWT redaction.

Smoke: `dev build` and `cd tests/container-apps/web/web-jaribu-tests && dotnet test -c Release`
Expect: build 0/0; the aggregator is green, with the new runfile included via its `features/**` glob.

Maintainer, after merge: run `dev run`, open the app, and trigger a console error (for example
`console.error("probe")` in DevTools). Confirm it appears in the Aspire dashboard's
structured logs for web-server, under category `Web.Spa.Browser`. In a Production build,
the page source has no `browser-log-forwarder.js` tag.
