# Include permitted static action-hosting pages in AgentSiteMap and AgentNavigate so off-page offers resolve in one hop

## Description

Live SigNoz GenAI traces (`3a26665d...`, `6297eba3...`) showed a direct contradiction between off-page tool offers and `navigate`:

1. When a signed-in admin on `/` asks Ask AI `"Create a new Role named Tester"`, Ask AI calls `Prefill.Role.CreateRole({ "name": "Tester" })`.
2. Because `RolePage` (`/Admin/Roles/New`) is declared with `[Page("/Admin/Roles/New", Policy = PermissionIds.AdminRolesManage, AgentActions = [typeof(RoleState.CreateRoleActionSet.Action)])]`, `Prefill.Role.CreateRole` returns `AgentNavigate.OfferFor`:
   `{"executed":false,"navigateTo":"/Admin/Roles/New","message":"Create role is on /Admin/Roles/New; I can take you to the /Admin/Roles/New page."}`.
3. `AgentAskInstructions` instructs the model: *"If a tool returns executed false and navigateTo, that action lives on another page: call navigate with that url"* AND *"Never navigate to a url that is not in the site map."*
4. However, `AgentSiteMap.DestinationsAsync` (`source/container-apps/web/projects/web-spa/features/application/agent/agent-site-map.cs`) only includes `NavigationDestinations.ForAsync` (pages shown in the navigation menu) plus `CommandPaletteRoster.DeepLinks`. Non-menu static pages that host `[Page(AgentActions = ...)]` — such as `/Admin/Roles/New` — are omitted from `AgentSiteMap` and therefore missing from `AgentNavigate`'s `url` JSON-schema enum and refused by `AgentNavigate.FindAsync`.
5. As a result, the model sees conflicting instructions and is forced to detour to `/Admin/Roles`, then call `Role.OpenNewRole`, instead of navigating directly to `/Admin/Roles/New` in one hop.

Every static route that an off-page `AgentNavigate.OfferFor` can return for the current principal must also be a permitted `AgentSiteMap` / `AgentNavigate` destination.

## Propagation

likely

## Requirements

1. **Derive permitted static action-hosting pages from `PageRegistry.ActionScopes` (no hand-coded route list):**
   - Extend `AgentSiteMap.DestinationsAsync` (or its helper) so that in addition to `NavigationDestinations.ForAsync` and `CommandPaletteRoster.DeepLinks`, it includes every static (non-parameterized) route from `PageRegistry.ActionScopes` that is **not** already in the menu destinations and that the current `ClaimsPrincipal` is authorized to access.
   - Enforce policy authorization identically to `NavigationDestinations` / `PageRegistry`: check the `[Page]` / `[Authorize]` policy for that page (e.g., `PermissionIds.AdminRolesManage` on `RolePage` `/Admin/Roles/New`) using `IAuthorizationService` so unauthorized users and anonymous visitors **never** see or navigate to restricted action pages.
   - Read title/description from `PageRegistry` / page metadata (no hardcoded route strings under `features/application/agent/`, preserving the existing no-hardcoded-routes guard test).
   - Keep menu pages (`NavMenu`) unchanged — this adds non-menu static action pages to `AgentSiteMap` / `AgentNavigate` only, not to the left navigation rail.

2. **Invariant between `CatalogAgentToolSet` / `FormPrefill` offers and `AgentNavigate`:**
   - Every non-null `OffPageRoute` returned by `CatalogAgentToolSet.SelectAsync` (including `FormPrefill.SelectAsync`) for a principal MUST appear in `AgentSiteMap.DestinationsAsync` for that same principal and succeed in `AgentNavigate.FindAsync` / `AgentNavigate.InvokeAsync`.
   - `AgentNavigate.Create` automatically includes those routes in its `url` schema enum and description because it reads `AgentSiteMap.DestinationsAsync`.
   - Verify `AgentAskInstructions.For` stays comfortably under `AgentAskInstructions.MaxLength` (7,000 chars) with the extra destination(s).

3. **Tests:**
   - Add an invariant test across the real `PageRegistry.All`, `PageRegistry.ActionScopes`, and `ActionCatalog.Entries` proving that for an authorized admin principal, **every** off-page tool's `OffPageRoute` (including `Prefill.Role.CreateRole` → `/Admin/Roles/New`) is present in `AgentSiteMap.DestinationsAsync` and accepted by `AgentNavigate.InvokeAsync`.
   - Add authorization negative tests proving that an anonymous user and a non-admin signed-in user do **not** receive `/Admin/Roles/New` in `AgentSiteMap.DestinationsAsync` and are refused by `AgentNavigate.InvokeAsync("/Admin/Roles/New")`.
   - Verify `./bin/dev build` and targeted `./bin/dev test` pass with 0 warnings and 0 errors.

## Goal (implementer restatement)

An off-page tool such as `Prefill.Role.CreateRole` tells Ask to call `navigate` with `/Admin/Roles/New`, and the instructions also say never to navigate to a url that is missing from the site map. That page hosts the create action and is not a menu row, so `AgentSiteMap` and `AgentNavigate` omit it. Ask then has to detour through the roles list. Every static route an off-page offer can return for this principal must be a destination that same principal may open, in one hop.

Acceptance:

- `AgentSiteMap.DestinationsAsync` keeps the menu pages and Ctrl-K deep links, and also adds each static route from `PageRegistry.ActionScopes` that is not already a menu destination, when `IAuthorizationService` allows that page's policy. Anonymous and non-admin principals never see `/Admin/Roles/New`. The nav rail stays the menu.
- Title and description come from the generated page metadata. Agent code does not grow a hand-written route list.
- For an administrator, every non-null `OffPageRoute` from `CatalogAgentToolSet.SelectAsync`, including `Prefill.Role.CreateRole` → `/Admin/Roles/New`, is in the site map and `AgentNavigate.InvokeAsync` accepts it. Ask's instructions stay under 7,000 characters.
- `./bin/dev build` and the targeted tests pass with 0 warnings and 0 errors.

## Notes

- Key files:
  - `source/container-apps/web/projects/web-spa/features/application/agent/agent-site-map.cs`
  - `source/container-apps/web/projects/web-spa/features/application/agent/agent-navigate.cs`
  - `source/container-apps/web/projects/web-spa/features/application/agent/page-agent-scope.cs`
  - `source/container-apps/web/projects/web-spa/features/admin/roles/pages/RolePage.razor.cs`
  - Page generator / `PageRegistry` / `PageActionScope` (check whether `PageActionScope` already exposes `Policy` / `Title` / `Description` or if the generator should include them).

## Results

`PageActionScope` now carries the page's Policy, Title, and Description. `AgentSiteMap.DestinationsAsync` adds each static action-hosting route that is not already a menu or deep-link row, after the same `IAuthorizationService` check the menu uses. `/Admin/Roles/New` is that row for an administrator (`New Role`, description "Create a custom role."). Anonymous visitors and a signed-in non-admin do not receive it, and `navigate` refuses it. The nav rail and the palette page rows stay the menu. Ask instructions for an administrator, including that extra destination, stay more than 1,000 characters under the 7,000 cap.

`RolePage.razor.cs` only gained the `[Page]` Description. That string is not rendered. Opening `/Admin/Roles/New` signed out shows the passkey sign-in gate. This worktree's AppHost could not bind `https://127.0.0.1:22124` because a Development AppHost was already listening, so the shot is that running ingress.

- [evidence/role-page.png](evidence/role-page.png) route `/Admin/Roles/New` principal signed-out look at the passkey sign-in gate in front of New Role

### How to validate

Smoke: Sign in as an administrator, open Ask on `/`, and ask it to create a role named Tester. The `Prefill.Role.CreateRole` result has `navigateTo` `/Admin/Roles/New`. Call `navigate` with that url.

Expect: The move opens New Role in one hop. The left nav does not gain a New Role item. Signed out, or signed in without `AdminRolesManage`, the site map omits `/Admin/Roles/New` and `navigate` returns "That page is not available." Ask's instructions stay under 7,000 characters.

Gates:

- `./bin/dev build` — passed, 0 warnings, 0 errors
- `./bin/dev test --no-build tests/container-apps/web/web-spa-integration-tests/features/application/navigation-destinations-tests.cs tests/container-apps/web/web-spa-integration-tests/features/application/navigation-destinations-test-fixture.cs` — passed, 10/10 (`NavigationDestinations_.Destinations_Should_`). Both files are parts of that class. Not standalone runfiles (no `#if !JARIBU_MULTI`); CI runs the integration project, and this command runs the class once.
- `./bin/dev test --no-build tests/analyzers/timewarp-architecture-sourcegenerator-tests/page-source-generator-registry-tests.cs` — passed, 15/15 (`PageSourceGenerator_Registry_Tests`). Not a standalone runfile (no `#if !JARIBU_MULTI`); CI runs it.
- `ganda repo audit` — passed, 43 passed, 0 failed, 1 warning (`vscode-window-icon`, pre-existing, non-blocking). The commit hook then refused to sign: `package-pin-currency` saw `TimeWarp.Amuru` and `TimeWarp.Amuru.Tools` at `3.0.0-beta.10` while nuget.org had `3.0.0-beta.11` (published the same day, cache younger than a minute). Pins moved to `3.0.0-beta.11` in `Directory.Packages.props`. `./bin/dev build` passed again, and `ganda repo audit` passed again with the same 43/0/1 counts.
- `./bin/dev lint-skills` — passed, 11/11 (skills/tw-blazor changed)

## Session

- Implement oracle: implemented by implementer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 500), Done — 2026-11-28T15:18:33Z
