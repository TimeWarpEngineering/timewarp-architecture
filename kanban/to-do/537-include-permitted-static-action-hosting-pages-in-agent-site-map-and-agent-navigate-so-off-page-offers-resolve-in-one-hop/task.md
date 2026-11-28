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

## Notes

- Key files:
  - `source/container-apps/web/projects/web-spa/features/application/agent/agent-site-map.cs`
  - `source/container-apps/web/projects/web-spa/features/application/agent/agent-navigate.cs`
  - `source/container-apps/web/projects/web-spa/features/application/agent/page-agent-scope.cs`
  - `source/container-apps/web/projects/web-spa/features/admin/roles/pages/RolePage.razor.cs`
  - Page generator / `PageRegistry` / `PageActionScope` (check whether `PageActionScope` already exposes `Policy` / `Title` / `Description` or if the generator should include them).
