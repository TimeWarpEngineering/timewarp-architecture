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

- [ ] A: Principals + Roles save no longer chain a fetch; pages sequence; semaphore scoped to network phase
- [ ] A: nested-dispatch source guard + runtime completion test
- [ ] B: `Section` component; nine loading blocks replaced; style guide; skill rules
- [ ] B: loading-literal guard test
- [ ] Gates green; Results and How to validate (manual: change a principal's roles → Save → list refreshes)

## Session

- Created: cockpit (2026-09-17)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N

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

_Pending._

### How to validate

_Pending._
