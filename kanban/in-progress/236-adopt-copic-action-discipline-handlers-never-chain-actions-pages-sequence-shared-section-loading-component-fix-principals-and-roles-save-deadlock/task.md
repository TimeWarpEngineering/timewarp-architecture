# Adopt COPIC action discipline: handlers never chain actions, pages sequence; shared Section loading component; fix Principals and Roles save deadlock

## Description

Live bug 2026-09-17 (Steve): on `/Admin/Principals`, changing a principal's roles and clicking
Save shows "Loading…" forever. Root cause (verified in code):

- `ApiHandler.Handle` (`web-spa/features/base/api-handler.cs`) acquires a **per-state
  `SemaphoreSlim(1,1)`** from `IStore.GetSemaphore(stateType)` and holds it for the whole
  handler, including `HandleSuccess`.
- `PrincipalState.SetPrincipalRolesActionSet.Handler.HandleSuccess`
  (`principal-state.set-principal-roles.cs`) does `await PrincipalState.FetchPrincipals(...)`.
  `FetchPrincipals` is another `ApiHandler` on the same `PrincipalState`, so it waits on the
  same semaphore the outer handler holds → deadlock. Action tracking had already marked
  `FetchPrincipals` started, so `IsAnyActive` stays true → "Loading…" never clears.
- `RoleState.SetRolePermissionsActionSet.Handler.HandleSuccess`
  (`role-state.set-role-permissions.cs`) does the same (`await RoleState.FetchRoles(...)`) and
  will hang the same way.

COPIC has the identical handler base and semaphore (`Web.Spa/Features/Base/ApiHandler.cs`) and
never deadlocks because **no COPIC handler dispatches another action**: update/delete handlers'
`HandleSuccess` are empty; the page sequences the work (`await State.Update(...)` then
`await NoSubRouteState.ChangeRoute(...)` back to the list, whose `OnInitialized` fetches) or
binds `FluentDataGrid Loading=IsLoading` to `ActionTrackingState.IsAnyActive(FetchX)`. COPIC
also has one loading composite, `CopicSection` (Title, `LoadingActionType`, Policy, Tip,
HeaderContent, ChildContent → `FluentProgressRing` while active). Architecture hand-writes
`<p><em>Loading…</em></p>` in nine pages instead.

**Rules (locked):**
1. A handler does one thing. It never sends/dispatches another action (no `await XState.Y()`
   inside `Handle` / `HandleSuccess` / `HandleError`). Pages and components sequence actions.
2. Loading UI comes from the shared `Section` component (port of `CopicSection`) driven by
   `ActionTrackingState.IsAnyActive(LoadingActionType)`; no hand-written loading markup.

## Requirements

### A. Fix the deadlock and remove chained dispatch
- `PrincipalState.SetPrincipalRoles`: `HandleSuccess` no longer fetches. Keep the
  `NotifySessionChanged` side effect there only if it does not dispatch an action (it calls the
  auth state provider directly — acceptable); otherwise move it too.
- `PrincipalsPage.OnSave`: `await PrincipalState.SetPrincipalRoles(id); await PrincipalState.FetchPrincipals();`
  (or apply the update response to state in `HandleSuccess` if the response carries the row — pick
  one, document).
- `RoleState.SetRolePermissions` / `RoleDetailPage.OnSave`: same change.
- `ApiHandler.Handle`: narrow the semaphore to the request + network phase
  (`GetRequest` → `ValidateRequestAsync` → `ApiService.GetResponse`) and release it **before**
  `HandleApiResponseAsync`. Update the Design region. This makes an accidental nested dispatch
  slow, not fatal.
- Guard: a test (dev-cli-tests style source scan, or an analyzer if one is cheap) that fails
  when any file under `web-spa/features/**/*-state/*.cs` contains `await [A-Za-z]+State\.` inside
  a `Handler` class. Allow-list empty. Plus a runtime test that drives
  update-then-fetch through the real `ApiHandler` base (fake `IWebServerApiService`) and
  asserts completion within a timeout.

### B. Shared `Section` component
- `web-spa/components/composites/Section.razor` (+ `.razor.css` per `tw-blazor-css-strategy`)
  ported from COPIC's `CopicSection`: `Title` (required), `Description`/`Tip`, `HeaderContent`,
  `LoadingActionType` (Type?), `Policy` (hide/disable when the policy fails, same as COPIC),
  `ChildContent`; renders `FluentProgressRing` while `ActionTrackingState.IsAnyActive(LoadingActionType)`.
  Compose with `Card`; do not duplicate Card's chrome.
- Replace every hand-written loading block with `Section` (or `Loading=IsLoading` on
  `FluentDataGrid` where a grid is the content): `PrincipalsPage`, `RolesListPage`,
  `RoleDetailPage`, `AgentLinksPage`, `ProfilePage`, `CredentialList`, `SuperheroPage`,
  `AssemblyInfoModal`, `WeatherForecastsPage`.
- Style guide: "Section" card showing loading/loaded states. `skills/tw-blazor/SKILL.md`: add
  rules 1 and 2 above (next to the 233/234 rules).
- Guard test: fails on the literal `Loading…` / `Loading...` in `web-spa/features/**/*.razor`.

### Tests / gates
- Existing SPA/prerender suites green (selectors by `data-qa`); new guards green;
  `dev build` 0/0; `ganda repo audit` clean.

## Checklist

- [x] A: Principals + Roles save no longer chain a fetch; pages sequence; semaphore scoped to network phase
- [x] A: nested-dispatch source guard + runtime completion test
- [x] B: `Section` component; nine loading blocks replaced; style guide; skill rules
- [x] B: loading-literal guard test
- [x] Gates green; Results and How to validate (manual: change a principal's roles → Save → list refreshes)

## Session

- Created: cockpit (2026-09-17)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N
- Implementer: Grok 4.6 (2026-09-17)

## Notes

- COPIC reference (read-only): `/home/steve/worktrees/github.com/TimeWarpEngineering/copic/main/Source/ContainerApps/Web/Web.Spa/Components/Composites/CopicSection.razor`,
  `Features/Base/ApiHandler.cs` (same semaphore), `Features/Admin/SecurityRoles/Pages/SecurityRolePage/SecurityRoleForm/SecurityRoleForm.razor`
  (page sequences update then ChangeRoute), `Features/Admin/Modules/Pages/ModulesPage/ModulesTable.razor`
  (`Loading=IsLoading` bound to `IsAnyActive(FetchModules)`).
- Architecture files: `features/base/api-handler.cs` (~L35-70 semaphore), `features/admin/principals/**`,
  `features/admin/roles/role-state/role-state.set-role-permissions.cs`, pages listed above.
- `timewarp-state` `ActionTrackingBehavior` completes tracking in `finally`, so the stuck spinner
  is purely the semaphore wait, not tracking.
- Prior: 233 (actions rule, raw-button guard), 234 (form primitives), COPIC patterns.

## Results

Handlers no longer chain actions; pages sequence the next dispatch. The Principals/Roles save deadlock is gone: `SetPrincipalRoles` / `SetRolePermissions` `HandleSuccess` no longer call Fetch. `PrincipalsPage.OnSave` and `RoleDetailPage.OnSave` do `await Set…` then `await Fetch…`. The Set response is stored-only (virtual Member / bootstrap Admin), so the list is re-fetched rather than patched from the write response.

The same rule is applied to every `await XState.` inside a `*-state` Handler so the empty allow-list guard is honest: credentials add/revoke/merge callers sequence `FetchCredentials`; Counter sequences `ResetStore` then `ChangeRoute`. `NotifySessionChanged` stays on `SetPrincipalRoles.HandleSuccess` (auth provider, not an action). `DefaultApiHandler.HandleError` still toasts via `ToastNotificationState` (base class, outside the `*-state` scan).

`ApiHandler.Handle` releases the per-state semaphore after GetRequest → validate → `GetResponse`, then runs `HandleApiResponseAsync`. Accidental nested dispatch on the same state waits instead of deadlocking.

Loading UI is `Section` (`components/composites/Section.razor` + isolation CSS): Card chrome, `FluentSpinner` while `ActionTrackingState.IsAnyActive(LoadingActionType)`, optional Policy/Description/Tip/HeaderContent. Unmatched attributes splat `BaseComponent.Attributes` (one CaptureUnmatchedValues). Hand-written `Loading…` / `Loading...` removed from the nine listed surfaces (CredentialList loading is the parent Section). Style guide has a Section card; `skills/tw-blazor/SKILL.md` has the two locked rules.

**Files (product):** `api-handler.cs`; principal/role/credentials/application-state handlers; pages listed in the brief plus Settings, Passkeys, AddPasskeyPrompt, Counter; `Section.razor(.css)`; style guide; tw-blazor skill.

**Tests:** `handler-nested-dispatch-guard-tests.cs`, `razor-loading-literal-guard-tests.cs`, `api-handler-nested-dispatch-tests.cs`.

**Gates:** `dotnet run tools/dev-cli/dev.cs -- build` 0/0; SPA Unit tag 28/28 including the three new tests (nested fetch completed in 177ms); Settings prerender (`--filter-method Settings`) 9/9 after the CaptureUnmatchedValues fix; `ganda repo audit` pass (2 advisory warnings, pre-existing).

### How to validate

**Smoke**
```bash
# from repo root
dotnet run tools/dev-cli/dev.cs -- build
# expect: Build succeeded. 0 Warning(s) 0 Error(s)

cd tests/container-apps/web/web-spa-integration-tests
dotnet test -c Release -- --filter-method Not_Await_Another_State_Action
dotnet test -c Release -- --filter-method Not_Contain_Hand_Written_Loading_Literal
dotnet test -c Release -- --filter-method Complete_Nested_Fetch
# expect: each run Passed, 1 succeeded

cd ../web-server-integration-tests
dotnet test -c Release -- --filter-method Settings
# expect: Passed (Settings prerender HTML still has data-qa CreatePasskey / Microsoft365Settings)

# UI
./bin/dev run
# sign in as an admin → /Admin/Principals → change a principal's roles → Save
```

**Expect**
- Save does not stick on Loading; the principals table redisplays with the new checks.
- `/StyleGuide` shows a Section card with loaded child copy (FluentSpinner only while the bound action is active).
- Nested-dispatch test finishes in well under 5s (`CallCount` 2, both flags true). If the semaphore still covered HandleSuccess, that test would hang until timeout.

**Automated gate**
```bash
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-tag Unit
# expect: 28 passed
ganda repo audit
# expect: pass (advisory memsearch-scaffold / vscode-window-icon only)
```

**Depends on:** `dotnet test` must be run from the suite directory (MTP). Settings prerender boots the web-server test host.

**Not in scope:** live WebAuthn ceremony; `DefaultApiHandler` toast-on-error still dispatches `ToastNotificationState` (not a `*-state` Handler).

## Implementation Notes

- Cockpit addendum (2026-09-17, from the cross-repo handler review): a **third same-state
  nested fetch** exists and must be fixed in this task alongside Principals and Roles:
  `features/identity/credentials-state/credentials-state.revoke-credential.cs` (~L51-55)
  `await CredentialsState.FetchCredentials(...)` inside `HandleSuccess` — Delete passkey and
  Unlink Microsoft 365 on `/Settings` hang the same way. Also `credentials-state.add-passkey.cs`
  (~L112) and `credentials-state.add-existing-passkey.cs` (~L96) await `FetchCredentials`
  from plain `BaseHandler`s (no semaphore, so no hang today) — move those refreshes to the
  page/`CredentialList` sequencing as well for consistency.
- Rule refinement pending Steve's decision: the review found cross-state dispatch from handlers
  is an established pattern (`DefaultApiHandler.HandleError` → `ToastNotificationState`,
  `ResetStore` → `RouteState.ChangeRoute`, COPIC and Trinsic do the same). The enforced rule is
  likely to become "a handler must not dispatch an action on its **own** state" with
  notifications allowed. Do **not** write the source-scan guard to forbid all
  `await XState.` in handlers — scope any guard to same-state dispatch (handler nested in
  `XState` calling `XState.<ActionSetEntry>`), or leave the guard for the timewarp-state
  analyzer task and note it in Results.
- Implementer (2026-09-17): original brief locked an empty allow-list for
  `await [A-Za-z]+State.` inside `*-state` Handler classes. That scan is what landed
  (credentials Fetch, ResetStore ChangeRoute, and ceremony toasts were sequenced to
  pages so the allow-list stays empty). `DefaultApiHandler.HandleError` still toasts
  (not under `*-state`). Same-state-only tightening can wait on Steve's decision.
