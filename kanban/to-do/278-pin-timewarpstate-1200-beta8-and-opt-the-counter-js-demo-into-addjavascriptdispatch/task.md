# Pin TimeWarp.State 12.0.0-beta.8 and opt the Counter JS demo into AddJavaScriptDispatch

## Description

TimeWarp.State **12.0.0-beta.8** (timewarp-state task 096, PR #615, released 2026-10-05) makes
JavaScript dispatch **opt-in**. `JsonRequestHandler` no longer resolves arbitrary type names with
`Type.GetType`. Only actions the host allows are dispatchable from JS:

```csharp
services.AddJavaScriptDispatch(b => b.Allow<TAction>(alias?));
```

Anything else (an unknown name, a non-action type, bad JSON) is rejected with a logged warning and
`InvalidRequestTypeException`, and nothing is sent. The full type name, the assembly-qualified
name, or the optional alias resolve. Redux DevTools allows its own messages only when DevTools is
enabled.

This repo's only JS dispatcher is the Counter demo (task 265's timewarp-state shape):
`web-spa/source/features/counter.ts`, `Spa.Counter.DispatchIncrementCountAction`. It sends
`"TimeWarp.Architecture.Features.Counters.CounterState+IncrementCounterActionSet+Action, Web.Spa"`
through `timeWarpState.DispatchRequest`. **After the bump, that button breaks until the action
is opted in.**

## Requirements

1. Move every `TimeWarp.State.*` pin in `Directory.Packages.props` (`TimeWarp.State`,
   `TimeWarp.State.Plus`, and any others present) from 12.0.0-beta.7 to **12.0.0-beta.8**.
   Pins move forward only.
2. In `web-spa/program.cs`, next to `AddTimeWarpState` and `AddActionCatalog`, add
   `AddJavaScriptDispatch(b => b.Allow<CounterState.IncrementCounterActionSet.Action>(...))`.
   - Decide whether to use an alias, for example `"Counter.Increment"`, so `counter.ts` stops
     hard-coding the CLR type name. Prefer the alias if it removes that string agreement; record
     the choice in the Design regions of `counter.ts` and `program.cs`.
   - Mirror the registration in any SPA test host (for example the `aspire-spa-test-application.cs`
     pattern from task 239-002) where the Counter JS test runs.
3. Keep or update the existing Counter JS-dispatch test (`counter-js-dispatch-tests.cs`, task 265)
   so it proves the allowed action dispatches through the real handler: count 3 → 10. Add a test
   that a non-allowed name is rejected and the count is unchanged.
4. Check that nothing else in the SPA relies on JS dispatch, including Redux DevTools in
   Development. Record the result.
5. Remind the maintainer in Results: a package bump can leave stale `_framework` WASM files, so
   run `dev clean` and clear the site data after merging.

## Checklist

- [x] `TimeWarp.State.*` pins on 12.0.0-beta.8
- [x] `AddJavaScriptDispatch` allows the Counter increment action (alias decision recorded); test host mirrored
- [x] Counter JS test: allowed dispatch works (3 → 10); a non-allowed name is rejected
- [x] No other JS dispatchers (recorded)
- [x] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`, `dev check-version`
- [x] Do **not** start an AppHost; record the browser check as not performed
- [ ] Implementation review; host `open-pr`

## Session

- Created: 2026-10-05 (cockpit; follow-up to timewarp-state 096 / beta.8)
- 2026-10-05 implementer (ganda task work, headless): pins bumped, allow-list + alias wired, tests
  extended, skill/d.ts docs reconciled; all gates green. No AppHost started.

## Results

- **Pins:** `TimeWarp.State` and `TimeWarp.State.Plus` (the only `TimeWarp.State.*` pins) moved
  12.0.0-beta.7 → **12.0.0-beta.8** in `Directory.Packages.props`.
- **Allow-list:** `Web.Spa.Program.AllowJavaScriptDispatch(JavaScriptDispatchBuilder)` is the single
  allow-list; `ConfigureServices` calls `AddJavaScriptDispatch(AllowJavaScriptDispatch)` right after
  `AddActionCatalog`. It is public so test hosts reuse the same list rather than copying it.
- **Alias decision: use the alias.** `CounterState.IncrementCounterActionSet.JavaScriptAlias =
  "Counter.Increment"` (const on the ActionSet); `counter.ts` sends `"Counter.Increment"` instead of
  the assembly-qualified CLR type name. Renaming or moving the type no longer breaks the JS. The
  one remaining string agreement (TS literal ↔ C# const) is test-checked. Recorded in the Design
  regions of `program.cs` and `counter-state.increment-counter.cs` and the header of `counter.ts`.
- **Test hosts:** `AnalyticsSpaTestApplication` (hosts the Counter JS test) and
  `AspireSpaTestApplication` (mirrors the production catalog) both call
  `AddJavaScriptDispatch(Web.Spa.Program.AllowJavaScriptDispatch)`.
- **Tests (`counter-js-dispatch-tests.cs`):** the existing fact dispatches what counter.ts sends
  through the real `JsonRequestHandler` + allow-list (count 3 → 10) and asserts the name equals
  `JavaScriptAlias`. New `Reject_A_Name_Not_Allowed_And_Leave_The_Count_Unchanged` (3 inputs:
  `System.Object, System.Private.CoreLib`, unknown alias `Counter.Decrement`, and a real but
  non-allowed action `CounterState+ThrowExceptionActionSet+Action, Web.Spa`) asserts
  `InvalidRequestTypeException` and count stays 3. `--filter-class JsDispatch`: 5/5.
- **Other JS dispatchers: none.** `git grep DispatchRequest|JsonRequestHandler` finds only
  `counter.ts`; `Routes.razor` only calls `JsonRequestHandler.InitAsync()` (registers the JS
  bridge). Redux DevTools (Debug `ReduxDevToolsEnabled`) needs no entry: State beta.8 allows its own
  messages when DevTools is enabled.
- **Docs reconciled:** `skills/tw-blazor/SKILL.md` JS-dispatch paragraph now states the opt-in
  rule and alias convention; `time-warp-state.d.ts` param doc updated.
- **Gates:** `dev build` 0 warnings / 0 errors; `dev test` all suites passed (21 summaries, 0
  failed); `dev template-smoke` SUCCEEDED; `ganda repo audit` passes; `dev check-version`
  2.0.0-beta.20 vs NuGet 2.0.0-beta.19 — new, no bump needed.
- **Browser check: not performed** (workers do not start an AppHost).
- **Maintainer reminder:** a package bump can leave stale `_framework` WASM files — after merging,
  run `dev clean` and clear the site data before trying the Counter page.

### How to validate

Smoke: `cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class JsDispatch`
Expect: 5 passed, 0 failed (onclick/export agreement; alias dispatch 3 → 10; three rejected names leave count at 3).

Maintainer, after merge:
1. Run `dev clean`, then `dev run`, and clear the site data.
2. On the Counter page, the JS button increments.
3. In DevTools, `timeWarpState.DispatchRequest("System.Object, System.Private.CoreLib", {})` is
   rejected with a warning in the browser logs (`Web.Spa.Browser`).
