# Round 1 — merged findings
**Date:** 2026-11-28
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 3 | 0 | 0 |
| nit | 2 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/container-apps/web/projects/web-spa/features/application/components/FormPrefillScope.razor:85
- Description: Every `ReportSeen` call site (lines 85, 97, 109) is guarded by `draft.Id != HandledId`, so the
  required `alreadyHandled` flag (line 149) is always `false`. The already-handled skip at line 104 writes no line.
  The task Description names a skipped draft as a case that has no trace, and Requirement 2 lists `alreadyHandled`.
- Suggestion: Report any waiting draft without the `HandledId` guard (`SeenKey` dedupes), or write a
  `prefill-outcome outcome=skipped-handled` line on the line-104 return. The flag must be able to be `true`.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/application/agent/ask-diagnostics.cs:176
- Description: `ErrorCode` matches copied English literals. The not-available text already exists as
  `WebMcpDispatcher.UnavailableError` and is repeated four times in `catalog-agent-functions.cs`. The prefill
  `NotObject` and `NoMatch` texts exist only as literals in `form-prefill.values.cs`. If the source text drifts, the code
  falls back to `Failed` without failing a test (`NotAvailable` is not tested). This breaks the AGENTS.md rule "When two
  things must agree, generate one from the other or add a build check."
- Suggestion: Hoist the strings to constants and reference them from `ErrorCode` and the error sites.
- Source: general
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/application/components/FormPrefillScope.razor:150
- Description: Outside Development, `ReportSeen` still runs `IsDirty()` (a JSON snapshot of `Model`) and builds a
  key string on each render while a draft is waiting. `AgentAsk.ReportDeferred` also builds a key on each
  `EnsureAgentAsync`. Requirement 1 asks for a complete no-op outside Development.
- Suggestion: Expose `AskDiagnostics.IsEnabled` and return early from the `Report*` helpers when it is false.
- Source: general
- Disposition notes:

### M4 — Severity: suggestion — Status: open
- File: kanban/in-progress/536-003-emit-development-browser-diagnostic-logs-for-ask-ai-tool-invocations-and-form-prefill-scope-outcomes/task.md
- Description: No test and no recorded run cover the wiring (the `Trace` mapping in `CatalogAgentFunctions`, and the
  `Report*` calls in `AgentAsk` and `FormPrefillScope`). `### How to validate` describes the `ask-diag` smoke, but
  `## Results` does not record that it was run or which lines appeared. The screenshots show unchanged UI.
- Suggestion: Run the smoke once and record the observed `ask-diag` events and flags (with no typed values) in
  `## Results`, or add a structured-log capture to `evidence/`.
- Source: general
- Disposition notes:

### M5 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/application/agent/catalog-agent-functions.cs:263
- Description: When a tool body throws, `tool-start` is written but no `tool-outcome`, and the exception type is not
  in the breadcrumb.
- Suggestion: Catch, write `tool-outcome completed=false error=Threw` (or the exception type name), and rethrow.
- Source: general
- Disposition notes:

### M6 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/source/browser-log-forwarder.ts:25
- Description: The local `interface Window { __twBrowserDiag?: ... }` duplicates the member added to
  `source/types/global.d.ts`, which is in the same compilation.
- Suggestion: Drop the local declaration.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. Single reviewer.

## Checked and not raised

- Query strings in `path=` / `route=`: `timeWarpPagePath` returns `pathname + search + hash`, but
  `PageAgentRoute.PathOr` and `PageAgentScope.FromNavigation` pass it through `PageAgentScope.Normalize`, which drops
  `?…` and `#…`. No leak.
- Prerender and missing JavaScript: `InvokeVoidAsync` runs inside `SendAfter`'s `try`, so the synchronous
  `InvalidOperationException` thrown during prerender is caught.
- Forwarder load: `App.razor` loads `browser-log-forwarder.js` only when `BrowserLogForwarding.IsEnabled`
  (Development/Testing). In Testing the C# side never calls the hook; in Development a call before the script loads is
  a `JSException` and is swallowed.
- `Trace` switch covers every result type `InvokeCoreAsync` returns (`CatalogAgentCallResult`, `FormPrefill.Result`,
  `AgentNavigate.Offer`, `AgentNavigate.Result`, `AgentSiteMap.Result`).
- Goal restatement vs task: consistent. No misread.

## Evidence

Each screenshot was opened and judged as a product page.

- `evidence/form-prefill-scope.png` — route `/Admin/Roles/New`, signed in as Member. Title "New Role" matches the
  content (breadcrumb Home / Roles / New Role, "Create a custom role.", a Role card with Name and Description fields and
  Save). Placement and copy are fine. No visible change is expected from this task, and none is shown.
- `evidence/agent-ask.png` — route `/`, signed in as Member. Home page titled "TimeWarp.Architecture" with the Akili
  panel open on the right ("Hi, I'm Akili — what can I help with?", four starter prompts, composer). Title, placement,
  and copy are fine. No visible change is expected.

Neither screenshot can show an `ask-diag` log line, so they do not demonstrate the change itself (see M4).
