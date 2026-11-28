# Round 2 — general
**Date:** 2026-11-28
**Scope reviewed:** fix delta `3f0fd472f..f04ccf592` (commit f04ccf592). Product files: `ask-diagnostics.cs`,
`catalog-agent-functions.cs`, `form-prefill.cs`, `form-prefill.values.cs`, `FormPrefillScope.razor`, `AgentAsk.razor`,
`browser-log-forwarder.ts`. Tests: `ask-diagnostics-tests.cs` and the new `ask-diagnostics-wiring-tests.cs`. Kitchen:
`evidence/ask-diag.txt`, the re-captured screenshots, and `task.md` `## Results`.

## Summary

The fix pass covers all six round-1 findings. No new defect was found in the delta. `alreadyHandled` can now be true,
error codes read shared constants, the report helpers return early when diagnostics are off, a thrown tool body writes
`tool-outcome` and rethrows, and the duplicate `Window` declaration is gone. New in-proc wiring tests drive the real
`CatalogAgentFunction`, `FormPrefillScope`, and `AgentAsk` and check the lines. The printed lines are recorded in
`evidence/ask-diag.txt`. Overall risk is low: the changes are Development-only, and the one control-flow change in the
tool function (a try/catch that rethrows) keeps the same behavior.

## Round-1 findings re-verified

- **M1 (bug) — fixed.** `FormPrefillScope.razor:85,97` now report any waiting draft. In `OnAfterRenderAsync` (lines
  106–116), `ReportSeen` runs before the `HandledId` early return, so the return still happens and the apply does not
  run again. `SeenKey` (line 161) includes `alreadyHandled`, so the repeat render writes one new line, then dedupes.
  `Prefill_Scope_Reports_Seen_Applied_And_Already_Handled` puts the same draft id back, asserts
  `alreadyHandled=true`, and asserts `outcome=applied` appears once. The evidence file shows that line.
- **M2 (suggestion) — fixed.** `FormPrefill.ArgumentsMustBeObjectError` and `FormPrefill.NoFieldMatchedPrefix`
  (`form-prefill.cs:31,34`) are written by `form-prefill.values.cs:60,200` and compared in `AskDiagnostics.ErrorCode`.
  The four not-available literals in `catalog-agent-functions.cs` now use `WebMcpDispatcher.UnavailableError`. A search
  of `source/` for the sentence finds only the constant. The tests now cover `NotAvailable`, `NotObject`, and `NoMatch`
  through the constants.
- **M3 (suggestion) — fixed.** `AskDiagnostics.IsEnabled` is public. `ReportSeen` and `ReportOutcome` in
  `FormPrefillScope`, and `ReportBuilt`, `ReportDeferred`, and `ReportStatus` in `AgentAsk`, return before the snapshot or
  the key. `Report_Helpers_Skip_The_Snapshot_When_Diagnostics_Are_Off` counts model reads and shows the disabled host
  reads fewer times.
- **M4 (suggestion) — fixed.** `ask-diagnostics-wiring-tests.cs` covers the `Trace` mapping, the thrown-tool path, the
  prefill seen, applied, cleared, and already-handled lines, and an `AgentAsk` deferral. `## Results` records the two
  targeted test runs (9/9 for the class) and links `evidence/ask-diag.txt`. The typed name and description do not appear
  in it (`AssertNoSecrets` checks both).
- **M5 (nit) — fixed.** `catalog-agent-functions.cs` wraps the body after `ToolStart` in `try`. The `catch` writes
  `tool-outcome completed=false error=<exception type name>` and rethrows. `ErrorCode` passes through only an ASCII
  alphanumeric word that ends in `Exception` (`IsExceptionTypeName`), so raw error text still collapses to a code.
  `Thrown_Tool_Writes_The_Exception_Type_And_Rethrows` covers it.
- **M6 (nit) — fixed.** `browser-log-forwarder.ts` drops the local `interface Window` and points to
  `source/types/global.d.ts` in its Design region.

## Issues

None.

## Checked and not raised

- The `catch (Exception)` also sees `OperationCanceledException`. It writes `error=OperationCanceledException` (or
  `TaskCanceledException`) and rethrows. Cancellation behavior does not change.
- `AskDiagnostics_Should_` is now `partial` across two files. The new file adds its own host (`DiagnosticHost`,
  `InteractiveHtmlRenderer`), which fits the repo's rule for partial Jaribu test classes.
- The already-handled proof calls the private `FormSuggestionState.Put` through reflection, because the public action
  assigns a new id. If the method is renamed, the test throws `InvalidOperationException`, so it cannot pass silently.
- `catalog-agent-functions.cs` has a blank first line inside the new `try` block (a leftover from re-indenting). The
  recorded build is 0 warnings, and this is cosmetic, so it is not raised.
- Goal restatement vs task: the round-1 paragraph added to the restatement matches the round-1 ledger. No misread.
