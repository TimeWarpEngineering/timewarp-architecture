# TWA0026 components only dispatch actions, and the Counter JS demo dispatches from JavaScript

## Description

Enforce the rule that task 260 adopted and wrote into the `tw-blazor` skill: **every web-spa user
interaction is a TimeWarp.State action, and components only dispatch.** Steve decided this on
2026-10-01 from 260's open questions:

- **Scope = every component member** (not only event-handler methods).
- **The Counter JS-interop demo copies timewarp-state's shape.** JavaScript dispatches the action
  directly from a plain HTML button. No C# handler calls `IJSRuntime`, so the demo needs no
  exemption.

## Requirements

### A. Analyzer TWA0026

1. **Gate.** Same as TWA0022 / TWA0025: SPA client code only (`UsingMicrosoftNETSdkBlazorWebAssembly`).
   Razor-generated trees ARE analyzed; other `.g.cs` trees are exempt.
2. **Report.** Report an invocation in any member of a type deriving from `ComponentBase`
   (lifecycle overrides included) whose target is:
   - `NavigationManager.NavigateTo`, `NavigateToLogin` or `Refresh`;
   - `IJSRuntime` / `IJSObjectReference` `InvokeAsync` / `InvokeVoidAsync`, including the
     extension overloads;
   - `IApiService` and subtypes (`IWebServerApiService`, `IApiServerApiService`);
   - `HttpClient` send/get/post/put/delete members;
   - `ISessionStorageService` / `ILocalStorageService` writes (`SetItem*`, `RemoveItem*`, `Clear*`);
   - first-party ceremony / JS-module services such as `PasskeyCeremonyClient`.

   Message: "Component calls {member} directly; dispatch a TimeWarp.State action whose handler
   does it."

   Decide how first-party services are listed (a list in the analyzer, or a marker attribute on
   the service types) and record the choice in the Design region. Prefer a marker attribute if
   more services are expected.
3. **Out of scope.** Never flagged: handlers (`BaseHandler<T>` / `ActionHandler` descendants),
   services, static helpers, and any non-component type. That is where the work belongs.
4. **Opt-out.** `[DirectComponentSideEffect("reason")]` on the component class or member. The
   reason is required and non-empty; an empty reason is a second diagnostic, the same pattern as
   `[CrossSliceReference]` / `[PageLocalMessageBar]`. The attribute lives in the Attributes
   package with the others.
5. **Registration.** Register TWA0026 everywhere a TWA id is listed:
   - the descriptor SSOT;
   - `AnalyzerReleases.Unshipped.md`;
   - the AGENTS.md enforcement table and the Analyzers package row (TWA0002–0016, TWA0020–0026);
   - the `tw-blazor` skill section "User interactions are actions", which should now say the
     rule is compiler-checked.
6. **Tests.** Analyzer tests in the existing convention-analyzer test suite:
   - each target category flagged in an event handler AND in a lifecycle method;
   - handler and service code not flagged;
   - the opt-out suppresses the diagnostic;
   - an empty reason is reported;
   - the razor-generated tree is analyzed;
   - non-WASM projects are not analyzed.

### B. Apply it to web-spa (0 new diagnostics, every opt-out with a reason)

7. **Counter demo.** Copy timewarp-state's `test-app` shape
   (`Test.App.Client.lib.module.js` → `timeWarpState.DispatchRequest("<action type name>", { … })`).
   - The Counter page's JS button becomes plain markup whose JavaScript `onclick` calls
     `Spa.Counter.DispatchIncrementCountAction`, which dispatches `IncrementCount` through
     `timeWarpState.DispatchRequest`.
   - Remove the `JsRuntime.InvokeVoidAsync` call and its C# handler from `CounterPage`.
   - Keep the demo's purpose: JavaScript dispatching into the store.
   - Keep the TS → `wwwroot/js` pipeline and the plain-object `window.Spa` namespace. Intermediate
     segments of `window.Spa` must be plain objects, not classes, or the interop lookup fails.
   - Update the existing Counter JS-interop test, or add one, so it still proves the store count
     changes.
8. **Opt-outs (expected; use real reasons).**
   - `CommandPalette` focus, scroll and hotkey registration (presentational JS).
   - Login's "already signed in" lifecycle redirect, and `RedirectToLogin`, unless either can
     reasonably become an action (for example dispatching `RouteState.ChangeRoute` from
     `OnInitialized`). Prefer converting to an action over opting out.

   Record the final opt-out list in Results.
9. Reconcile Purpose/Design regions on every touched file.

## Checklist

- [ ] TWA0026 analyzer + `[DirectComponentSideEffect(reason)]` + empty-reason diagnostic
- [ ] Registered: descriptor SSOT, AnalyzerReleases.Unshipped, AGENTS.md table and package row,
      `tw-blazor` skill
- [ ] Analyzer tests (categories × handler/lifecycle, exemptions, opt-out, gate)
- [ ] Counter demo dispatches from JS (timewarp-state shape); no C#→JS handler; test proves the
      count changes
- [ ] web-spa builds with 0 TWA0026; every opt-out carries a real reason (list in Results)
- [ ] Gates: `dev build` 0/0 (analyzer registry change ⇒ full rebuild), `dev test`,
      `dev template-smoke`, `ganda repo audit`. If the Analyzers package ships, check
      `dev check-version`, and bump the version and pins in the same commit if required
- [ ] Do **not** start an AppHost; record the browser check as not performed
- [ ] Implementation review; host `open-pr`

## Session

- Created: 20701 (2026-10-01)

## Notes

- Origin: task 260's open questions (analyzer proposal and the CounterPage JS-interop demo).
- Memory discipline: other workers may build at the same time. Run builds serially and call
  `dotnet build-server shutdown` before finishing.

## Results

*(fill when done)*

### How to validate

*(required before done)*
