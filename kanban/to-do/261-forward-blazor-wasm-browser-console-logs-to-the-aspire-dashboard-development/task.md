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

- [ ] Findings on the built-in `WithBrowserLogs` recorded in Notes (capabilities, limits,
      whether it sees the maintainer's own browser)
- [ ] Implementation (built-in preferred; fallback only with a recorded reason)
- [ ] Development-only; Production unaffected (a test or assertion that proves it)
- [ ] Tests: AppHost model test (Aspire.Hosting.Testing closed-box lane, or a model inspection)
      showing the browser-log wiring is present in Development and absent otherwise. If the
      fallback is used: endpoint validation, cap and rate-limit tests, and a Production
      fail-closed test
- [ ] Skill note (where the logs appear; the stale-asset Mono assertion symptom)
- [ ] Purpose/Design regions reconciled
- [ ] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`
- [ ] Do **not** start the AppHost manually (`dev run`, `aspire run`, or `dotnet run` of the
      AppHost): it shares the maintainer's user secrets. Closed-box Aspire.Hosting.Testing
      suites that `dev test` already runs are fine. Record the manual dashboard check as not
      performed.
- [ ] Implementation review; host `open-pr`

## Session

- Created: 571175 (2026-10-01)

## Notes

- Memory discipline: task 260 may be building at the same time. Run builds and tests serially
  and call `dotnet build-server shutdown` before finishing.

## Results

*(fill when done)*

### How to validate

*(required before done)*

Maintainer, after merge: run `dev run`, open the app, and trigger a console error (for example
`console.error("probe")` in DevTools). Confirm it appears in the Aspire dashboard's logs.
