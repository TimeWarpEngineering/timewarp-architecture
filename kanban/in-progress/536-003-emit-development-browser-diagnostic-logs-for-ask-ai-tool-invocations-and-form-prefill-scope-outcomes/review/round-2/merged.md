# Round 2 — merged findings
**Date:** 2026-11-28
**Sources:** general
**Scope:** fix delta `3f0fd472f..f04ccf592`. The round-1 IDs carry forward. No new findings.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 3 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/components/FormPrefillScope.razor:85
- Description: `alreadyHandled` was always false, because every `ReportSeen` call was guarded by `HandledId`.
- Source: general (round 1)
- Disposition notes: Fixed in f04ccf592. The guard is gone from all three call sites. `OnAfterRenderAsync` reports the
  draft, then returns when the id was already handled, so the draft is not applied twice. Covered by
  `Prefill_Scope_Reports_Seen_Applied_And_Already_Handled`. `evidence/ask-diag.txt` shows `alreadyHandled=true`.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent/ask-diagnostics.cs:179
- Description: `ErrorCode` compared copies of English text.
- Source: general (round 1)
- Disposition notes: Fixed in f04ccf592. `WebMcpDispatcher.UnavailableError`, `FormPrefill.ArgumentsMustBeObjectError`,
  and `FormPrefill.NoFieldMatchedPrefix` are used by both the code that writes the error and `ErrorCode`. Tests cover
  each code through the constant.

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/components/FormPrefillScope.razor:153
- Description: The report helpers did snapshot and key work outside Development.
- Source: general (round 1)
- Disposition notes: Fixed in f04ccf592. `AskDiagnostics.IsEnabled` is public, and all five report helpers return
  first when it is false. Covered by `Report_Helpers_Skip_The_Snapshot_When_Diagnostics_Are_Off`.

### M4 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/application/ask-diagnostics-wiring-tests.cs
- Description: No test or recorded run covered the wiring.
- Source: general (round 1)
- Disposition notes: Fixed in f04ccf592. Five wiring tests drive the real tool function, the form scope, and the Ask
  panel. `## Results` records the targeted runs (9/9), and the lines are in `evidence/ask-diag.txt` with no typed values.

### M5 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent/catalog-agent-functions.cs:126
- Description: A tool body that threw wrote `tool-start` but no `tool-outcome`.
- Source: general (round 1)
- Disposition notes: Fixed in f04ccf592. A `catch` writes `tool-outcome completed=false error=<type name>` and rethrows.
  Covered by `Thrown_Tool_Writes_The_Exception_Type_And_Rethrows`.

### M6 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/browser-log-forwarder.ts:23
- Description: The script declared the `Window` member again.
- Source: general (round 1)
- Disposition notes: Fixed in f04ccf592. The local declaration is removed. The script uses `source/types/global.d.ts`.

## Duplicates / conflicts

- None. Single reviewer.

## Gates

Not re-run by the reviewer (recorded-gates contract). `## Results` records `./bin/dev build` 0 warnings and 0 errors,
both targeted `./bin/dev test` runs passing 9/9 for `AskDiagnostics_Should_`, and `ganda repo audit` passing with one
warning that was already there. No targeted command was run in this round.

## Evidence

I opened each screenshot and judged it as a product page.

- `evidence/form-prefill-scope.png` — route `/Admin/Roles/New`, signed in as Member. The "New Role" title matches the
  content: breadcrumb Home / Roles / New Role, "Create a custom role.", and a Role card with Name and Description fields
  and a Save button. The Admin nav section is open with Roles listed. Placement and copy are fine. This task changes no
  UI, and none is shown.
- `evidence/agent-ask.png` — route `/`, signed in as Member. The home page is titled "TimeWarp.Architecture" (Welcome,
  Built with, Signed in cards), and the Akili panel is open on the right with "New conversation". This capture shows the
  panel while it is still loading ("Starting Akili…"), not the starter prompts from round 1. That is a normal startup
  state, not a defect. Title, placement, and copy are fine. This task changes no UI.

A screenshot cannot show an `ask-diag` line. `evidence/ask-diag.txt` shows the change itself. It holds the tool-start,
tool-outcome (including `error=ArgumentException`), prefill-seen (`alreadyHandled=false`, then `true`),
prefill-outcome (`applied`, `cleared`), and agent-deferred lines. None of them contains the typed name or description.
