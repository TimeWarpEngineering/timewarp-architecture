# Round 1 — tests
**Date:** 2026-10-01
**Scope reviewed:** `git diff master...HEAD` test files: component-side-effect-analyzer-tests.cs (analyzers-tests), counter-js-dispatch-tests.cs and sign-in-state-tests.cs (web-spa-integration-tests; sign-in-state-tests.cs is picked up by the same project, filter `SignInActions`). Suites run serially: analyzers `--filter-class Direct_Side_Effects` 14/14 pass; web-spa-integration `--filter-class JsDispatch` 2/2 pass; `--filter-class SignInActions` 13/13 pass.

## Summary
Requirement 6 is covered: each category has a handler plus lifecycle case, and the handler/service/nested-class negative, opt-out (class, member, property-lambda), empty reason (class and member, still flags), razor-generated tree, other `.g.cs` exempt, and non-WASM (absent and `false`) cases are all present. Assertions use exact locations and arguments, so they are not tautological. Requirement 7's test is a good host-free approach: it feeds the exact name and payload from counter.ts to `JsonRequestHandler.Handle` and asserts `Type.GetType(name) == IncrementCounterActionSet.Action` and count 3 -> 10. No bugs. A few minor breadth gaps below. New actions have real effect assertions (navigation tuples incl. open-redirect case; session RemoveItemAsync called). No Fixie/xUnit/mocking-of-first-party issues (FakeItEasy on the storage externality is fine).

## Issues
### Issue 1 — Severity: suggestion
- File: tests/analyzers/timewarp-architecture-analyzers-tests/component-side-effect-analyzer-tests.cs:271
- Description: HttpClient coverage only exercises GetStringAsync and DeleteAsync. The requirement lists send/get/post/put/delete; PostAsync/PutAsync/PatchAsync/SendAsync are never asserted, so a regression in the analyzer's member list for those would pass.
- Suggestion: add Post/Put/Send (and Patch) invocations to the existing test.
- Status: open

### Issue 2 — Severity: nit
- File: tests/analyzers/timewarp-architecture-analyzers-tests/component-side-effect-analyzer-tests.cs:325
- Description: ILocalStorageService is covered only by SetItemAsStringAsync (handler); no lifecycle-method case and no RemoveItem/Clear on local storage, though the brief says session AND local writes in both positions. IJSObjectReference.InvokeAsync (non-void) is likewise untested.
- Suggestion: add `Local.RemoveItemAsync` in OnInitializedAsync and `Module.InvokeAsync<...>` in the click handler.
- Status: open

### Issue 3 — Severity: nit
- File: tests/analyzers/timewarp-architecture-analyzers-tests/component-side-effect-analyzer-tests.cs:92
- Description: IApiService subtype coverage uses a stub IWebServerApiService and an inherited member only; the real IApiServerApiService and a subtype-declared member are not exercised.
- Suggestion: optional: add a subtype-declared method to the stub and call it.
- Status: open

### Issue 4 — Severity: nit
- File: tests/container-apps/web/web-spa-integration-tests/features/counter/counter-js-dispatch-tests.cs:48
- Description: The JS-to-C# agreement is verified by regex over counter.ts/CounterPage.razor, not by executing JS; it also does not check that `Counter` is reachable as `window.Spa.Counter` (TS entry wiring), where the earlier interop breakage occurred. Deliberate trade-off documented in the Design region; failures are descriptive, so low risk.
- Suggestion: optionally assert the TS entry that assembles `window.Spa` includes `Counter`, or note Playwright e2e covers it.
- Status: open
