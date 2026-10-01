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

- [x] TWA0026 analyzer + `[DirectComponentSideEffect(reason)]` + empty-reason diagnostic
- [x] Registered: descriptor SSOT, AnalyzerReleases.Unshipped, AGENTS.md table and package row,
      `tw-blazor` skill
- [x] Analyzer tests (categories × handler/lifecycle, exemptions, opt-out, gate)
- [x] Counter demo dispatches from JS (timewarp-state shape); no C#→JS handler; test proves the
      count changes
- [x] web-spa builds with 0 TWA0026; every opt-out carries a real reason (list in Results)
- [x] Gates: `dev build` 0/0 (analyzer registry change ⇒ full rebuild), `dev test`,
      `dev template-smoke`, `ganda repo audit`. If the Analyzers package ships, check
      `dev check-version`, and bump the version and pins in the same commit if required
- [x] Do **not** start an AppHost; record the browser check as not performed
- [ ] Implementation review; host `open-pr`

## Session

- Created: 20701 (2026-10-01)
- 2026-10-01: implement oracle — verified prior session's work; full `--no-incremental` rebuild 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`, `dev check-version` all green; TWA0026 confirmed firing in the real web-spa build (temporary NavigateTo injected, reverted).

## Notes

- Origin: task 260's open questions (analyzer proposal and the CounterPage JS-interop demo).
- Memory discipline: other workers may build at the same time. Run builds serially and call
  `dotnet build-server shutdown` before finishing.

## Results

**Analyzer.** `ComponentSideEffectAnalyzer` (`source/analyzers/timewarp-architecture-convention-analyzers/component-side-effect-analyzer.cs`)
reports **TWA0026** for any member of a `ComponentBase` type (lifecycle included; invocations and
method groups) calling `NavigationManager.NavigateTo/NavigateToLogin/Refresh`, `IJSRuntime` /
`IJSObjectReference` invokes (extensions included), `IApiService` + subtypes, `HttpClient`
Send/Get/Post/Put/Delete/Patch, Blazored session/local storage writes, or a `[SideEffectService]`
type. **TWA0027**: `[DirectComponentSideEffect]` with an empty/whitespace reason (does not opt out).
Gate copies TWA0022/0025 (WASM SDK only; razor-generated trees analyzed, other `.g.cs` exempt).
Descriptors live in the analyzer (same SSOT pattern as TWA0025).

**First-party services: marker attribute** `[SideEffectService]` (Attributes package), applied to
`PasskeyCeremonyClient`, `WebAuthnJsModule`, `SignOutJsModule`, `CommandPaletteJsModule`. Choice
recorded in the analyzer Design region (more JS modules/ceremony clients expected; framework types
stay a closed list in the analyzer). Opt-out attribute `[DirectComponentSideEffect(reason)]` in the
Attributes package.

**Registered:** AnalyzerReleases.Unshipped.md (TWA0026, TWA0027), AGENTS.md table + Analyzers row
(TWA0020–0027), `tw-blazor` skill "User interactions are actions" (now compiler-checked; JS dispatch).

**Tests:** 14 analyzer tests (`Should_Ban_Direct_Side_Effects_In_Components`) — each category in
handler and lifecycle, method group/lambda, handler/service/nested non-component not flagged,
class/member opt-out, empty reason, razor-generated tree, other generated tree, gate absent/false.

**web-spa applied (0 TWA0026):**
- Counter: `fluent-button` with JS `onclick="Spa.Counter.DispatchIncrementCountAction()"` →
  `timeWarpState.DispatchRequest`; `IJSRuntime` + C# handler removed. `counter-js-dispatch-tests.cs`
  pins onclick↔export agreement and feeds counter.ts's action name/payload to `JsonRequestHandler`
  → count 3 → 10.
- LoginPage already-signed-in redirect → `RouteState.ChangeRoute` (converted, no opt-out).
- RedirectToLogin → new `SignInState.RedirectToLogin` action (forceLoad in handler; `LoginPage.GetLoginUrl`
  shared by link and handler). Test `RedirectToLogin_ForceLoad_Login_With_A_Safe_Return`.
- AuthenticationStateListener sessionStorage removal → new `CredentialsState.ForgetPasskeySoftPromptLater`
  action. Test `ForgetPasskeySoftPromptLater_Remove_The_Session_Key`.

**Final opt-out list (1):** `CommandPalette.razor` — "Hotkey registration, focus restore and
scroll-into-view are presentational JS bound to this component's DOM and lifetime; there is no
store state to dispatch."

**Gates:** full `dotnet build timewarp-architecture.slnx -c Release --no-incremental` 0/0; `dev build`
0/0; `dev test` all suites pass; `dev template-smoke` SUCCEEDED; `ganda repo audit` pass;
`dev check-version` beta.20 > NuGet beta.19 (no bump needed). Browser check **not performed**
(no AppHost in workers).

### How to validate

**Smoke:**
```bash
dotnet build timewarp-architecture.slnx -c Release --no-incremental
cd tests/analyzers/timewarp-architecture-analyzers-tests && dotnet test -c Release -- --filter-class Direct_Side_Effects
cd ../../container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class JsDispatch
```
Optional: add `@inject NavigationManager Nav` + `private void Bad() => Nav.NavigateTo("/");` to
`CounterPage.razor` and build web-spa.

**Expect:** build 0 warnings / 0 errors; 14/14 analyzer tests and 2/2 JsDispatch tests pass; the
optional edit fails the web-spa build with `TWA0026: Component calls NavigationManager.NavigateTo
directly; dispatch a TimeWarp.State action whose handler does it`. In a browser (not run here), the
Counter "Increment Count by 7 via JavaScript" button adds 7 to the count.
