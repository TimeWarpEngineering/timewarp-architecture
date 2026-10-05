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

- [ ] `TimeWarp.State.*` pins on 12.0.0-beta.8
- [ ] `AddJavaScriptDispatch` allows the Counter increment action (alias decision recorded); test host mirrored
- [ ] Counter JS test: allowed dispatch works (3 → 10); a non-allowed name is rejected
- [ ] No other JS dispatchers (recorded)
- [ ] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`, `dev check-version`
- [ ] Do **not** start an AppHost; record the browser check as not performed
- [ ] Implementation review; host `open-pr`

## Session

- Created: 2026-10-05 (cockpit; follow-up to timewarp-state 096 / beta.8)

## Results

*(fill when done)*

### How to validate

*(required before done)*

Maintainer, after merge:
1. Run `dev clean`, then `dev run`, and clear the site data.
2. On the Counter page, the JS button increments.
3. In DevTools, `timeWarpState.DispatchRequest("System.Object, System.Private.CoreLib", {})` is
   rejected with a warning in the browser logs (`Web.Spa.Browser`).
