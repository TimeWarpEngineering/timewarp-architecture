# Round 1 — merged findings
**Date:** 2026-10-01
**Sources:** general, tests, plan-alignment

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 3 | 3 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/analyzers/timewarp-architecture-convention-analyzers/component-side-effect-analyzer.cs:173
- Description: Side-effect matching (display-string compares across base/interface chains) ran before the cheap "inside a component" check, for every operation in the SPA compilation.
- Suggestion: Check IsInComponent first; optionally resolve framework symbols once per compilation.
- Source: general
- Disposition notes: Fixed — `Analyze` now exits on `IsInComponent` first; Design region records the order and why targets stay name-matched (most targets are optional references per compilation, so per-compilation symbol resolution adds null-handling for little gain once the component filter runs first).

### M2 — Severity: suggestion — Status: fixed
- File: tests/analyzers/timewarp-architecture-analyzers-tests/component-side-effect-analyzer-tests.cs:271
- Description: HttpClient covered only GetStringAsync/DeleteAsync; Post/Put/Patch/Send unasserted.
- Suggestion: Add those invocations.
- Source: tests
- Disposition notes: Fixed — PostAsync, PutAsync, PatchAsync, SendAsync added and asserted.

### M3 — Severity: nit — Status: fixed
- File: tests/analyzers/timewarp-architecture-analyzers-tests/component-side-effect-analyzer-tests.cs:325
- Description: ILocalStorageService had no lifecycle case and no RemoveItem; IJSObjectReference.InvokeAsync (non-void) untested.
- Suggestion: Add Local.RemoveItemAsync in a lifecycle method; add Module.InvokeAsync.
- Source: tests
- Disposition notes: Fixed for local storage (`Local.RemoveItemAsync` in `OnInitializedAsync`). IJSObjectReference is already covered via `InvokeVoidAsync`; the analyzer's JS rule is receiver-type based ("every member except Dispose"), so a second member adds no distinct path.

### M4 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/spa.ts:6-9; web.spa.lib.module.ts:12
- Description: Design regions still described C# IJSRuntime interop as the Spa.Counter caller; the caller is now the JavaScript onclick.
- Suggestion: Reword; keep the plain-object rule.
- Source: plan-alignment
- Disposition notes: Fixed — both regions name the Counter page's JS onclick as the caller and keep the plain-object rule for any C# interop caller.

### M5 — Severity: nit — Status: fixed
- File: source/analyzers/timewarp-architecture-convention-analyzers/component-side-effect-analyzer.cs (GetReceiverType / marker check)
- Description: Member inherited from an unmarked base called through a marked [SideEffectService] type is not flagged; unreduced `this T` extension yields no receiver.
- Suggestion: Check invocation.Instance type, or document the limitation.
- Source: general
- Disposition notes: Fixed by documentation — known gaps recorded in the analyzer Design region (every marked service declares its own members today).

### M6 — Severity: nit — Status: wontfix
- File: source/analyzers/timewarp-architecture-attributes/direct-component-side-effect-attribute.cs:20
- Description: Opt-out attribute cannot target fields; field-initializer side effects opt out only at class level.
- Source: general
- Disposition notes: Wontfix (orchestrator) — matches documented scope; no field-initializer side effect exists; class-level opt-out covers it. Reviewer marked it acceptable as is.

### M7 — Severity: nit — Status: wontfix
- File: tests/analyzers/timewarp-architecture-analyzers-tests/component-side-effect-analyzer-tests.cs:92
- Description: IApiService subtype coverage uses an inherited member only.
- Source: tests
- Disposition notes: Wontfix (orchestrator) — the analyzer flags every member of any IApiService implementer; the subtype path (AllInterfaces) is what the test pins, and a subtype-declared member exercises the same branch.

### M8 — Severity: nit — Status: wontfix
- File: tests/container-apps/web/web-spa-integration-tests/features/counter/counter-js-dispatch-tests.cs:48
- Description: JS↔C# agreement is checked by regex over sources, not by executing JS; `window.Spa.Counter` wiring not asserted.
- Source: tests
- Disposition notes: Wontfix (orchestrator) — deliberate host-free trade-off documented in the test's Design region; the initializer wiring is a web-server build gate (missing initializer fails the build), and executing JS needs a browser lane the in-proc suite does not have.

## Duplicates / conflicts

- None; findings from the three reviewers did not overlap.
