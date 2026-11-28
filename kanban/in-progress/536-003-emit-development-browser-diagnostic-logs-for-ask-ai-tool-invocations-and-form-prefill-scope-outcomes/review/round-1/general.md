# Round 1 — general
**Date:** 2026-11-28
**Scope reviewed:** task commits `d178e9460..3f0fd472f` (see review-framework.md), plus call sites in
`page-agent-route.cs`, `page-agent-scope.cs`, `agent-navigate.cs`, `agent-site-map.cs`, `form-prefill.cs`,
`form-prefill.values.cs`, `web-mcp-dispatcher.cs`, and `web-server/components/App.razor`.

## Summary

The change adds `window.__twBrowserDiag` to the browser-log forwarder, an `AskDiagnostics` service
gated on `environmentName == "Development"`, and breadcrumb calls in `AgentAsk`, `CatalogAgentFunction.InvokeCoreAsync`,
and `FormPrefillScope`. The service and the forwarder hook match Requirement 1. Paths are safe: `PageAgentRoute.PathOr`
and `PageAgentScope.FromNavigation` both run `Normalize`, which drops the query string and hash, so `path=` and `route=`
never carry query values even though `timeWarpPagePath` returns `pathname + search + hash`. The tests cover formatting,
the non-Development no-op, swallowed JS failures, and the forwarder under node. The dominant problem is in
`FormPrefillScope`: the required `alreadyHandled` flag is always false, and the already-handled skip leaves no line.
The rest are maintainability items.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/application/components/FormPrefillScope.razor:85
- Description: All three `ReportSeen` call sites (lines 85, 97, 109) run only when `draft.Id != HandledId`.
  `ReportSeen` then computes `alreadyHandled = draft.Id == HandledId` (line 149), which is therefore always `false`.
  Requirement 2 lists `alreadyHandled` as a logged flag, and the task Description names "FormPrefillScope skipping a
  draft" as a case with zero trace. The already-handled skip at line 104 (`draft.Id == HandledId` → return) still
  writes nothing, and the flag that would name it can never be `true`.
- Suggestion: Report the waiting draft whenever `FormSuggestionState.For(Form)` is non-null, without the
  `HandledId` guard; `SeenKey` already dedupes repeats, so a draft that stays waiting after it was handled writes one
  `prefill-seen ... alreadyHandled=true` line. Alternatively, write a `prefill-outcome outcome=skipped-handled` line
  on the line-104 return. Either way the flag must be able to be `true`.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/agent/ask-diagnostics.cs:176
- Description: `ErrorCode` matches error text by copied English literals. `"The action is not available on this page."`
  already exists as `WebMcpDispatcher.UnavailableError` and is repeated as a literal four times in
  `catalog-agent-functions.cs`. `"Prefill arguments must be a JSON object."` (line 206) and the `"No field matched."`
  prefix (line 211) live only as literals in `form-prefill.values.cs:58` and `:198`. If a source string changes, the code
  silently falls back to `Failed` or `Type`. The tests pin `NotObject` and `NoMatch` but not `NotAvailable`. The repo
  hard rule is "When two things must agree, generate one from the other or add a build check."
- Suggestion: Hoist the prefill error strings to constants on `FormPrefill` and use one constant for the
  not-available text, then reference those constants from `ErrorCode` and from `CatalogAgentFunctions`.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/components/FormPrefillScope.razor:150
- Description: Outside Development the service is a no-op, but the component still does the work first.
  `ReportSeen` calls `IsDirty()`, which JSON-serializes `Model` (`Snapshot`), and it builds an interpolated key string.
  It runs from `OnParametersSet` and from `OnAfterRenderAsync` on each render while a draft is waiting, in
  Production too. `AgentAsk.ReportDeferred` builds a key string on each `EnsureAgentAsync` as well. Requirement 1 asks
  for a complete no-op outside Development.
- Suggestion: Expose `AskDiagnostics.IsEnabled` (the existing `Enabled` field) and return early from the `Report*`
  helpers in `FormPrefillScope` and `AgentAsk` when it is false.
- Status: open

### Issue 4 — Severity: suggestion
- File: kanban/in-progress/536-003-emit-development-browser-diagnostic-logs-for-ask-ai-tool-invocations-and-form-prefill-scope-outcomes/task.md
- Description: The recorded gates cover `AskDiagnostics` formatting, the service gate, and the forwarder script. No
  test and no recorded run cover the wiring: the `Trace` mapping in `CatalogAgentFunctions`, and the `Report*` calls in
  `AgentAsk` and `FormPrefillScope`. The `### How to validate` smoke (filter `Web.Spa.Browser` for `ask-diag`) is
  written as a step, but `## Results` does not say it was run or what lines appeared. The four screenshots show the
  unchanged UI and cannot show a log line.
- Suggestion: Run the recorded smoke once and record the observed `ask-diag` lines (event names and flags, with no
  typed values) under `## Results`, or add a dashboard or structured-log capture to `evidence/`.
- Status: open

### Issue 5 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/agent/catalog-agent-functions.cs:263
- Description: `ToolStart` is written on entry, but when `current.Entry.Execute`, `FormPrefill.DispatchAsync`,
  `AgentNavigate.InvokeAsync`, or `AgentSiteMap.InvokeAsync` throws, no `tool-outcome` line is written. A start line
  with no outcome is a signal, but the exception type is lost from the breadcrumb.
- Suggestion: Wrap the body in a `try`/`catch (Exception)` that writes `tool-outcome completed=false error=Threw`
  (or the exception type name) and rethrows.
- Status: open

### Issue 6 — Severity: nit
- File: source/container-apps/web/projects/web-spa/source/browser-log-forwarder.ts:25
- Description: The file declares `interface Window { __twBrowserDiag?: ... }`, and the same member is added to
  `source/types/global.d.ts`, which is in the same compilation (`tsconfig.json` `rootDir: ./source`). The local
  declaration is redundant.
- Suggestion: Keep the `global.d.ts` declaration and drop the local `interface Window`.
- Status: open
