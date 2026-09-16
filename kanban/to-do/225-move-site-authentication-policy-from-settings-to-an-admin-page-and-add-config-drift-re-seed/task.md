# Move site authentication policy from Settings to an Admin page and add config-drift re-seed

## Description

Two corrections to 219-006 after the first live look (Steve, 2026-09-16, screenshot of `/Settings`).

1. **Wrong audience.** The "Authentication" section (offer Microsoft 365 sign-in, allow bootstrap,
   trusted tenants, passkey prompt mode) landed on the personal `/Settings` page next to "my
   passkeys" and "Link Microsoft 365". Those are **site policy** decided by an administrator, not
   user settings. Move them to a page under the existing **Admin** nav category
   (`NavMenu.razor` `FluentNavCategory Title="Admin"`, gated by `PermissionIds.AdminAccess`),
   alongside Principals and Roles. `/Settings` keeps only what belongs to the signed-in person.
   The 219-006 brief said "the existing Settings page gets an Authentication section" — that
   placement was the cockpit's mistake, not the implementer's.
2. **Config drift after first-run seed.** The site-settings store seeds `EntraSignInEnabled`,
   `EntraAllowBootstrap`, `EntraTrustedTenants` from `Authentication:Entra` **once**, then owns
   them. The tester then re-ran `dev entra setup --tenant "TimeWarp Enterprises LLC"`: user
   secrets now hold tenant `a16bcaef-ea01-44ad-be12-249a17658692`, but the persisted trusted
   tenants still list the earlier CrunchIt tenant `30f3971f-4719-4f20-9b6f-88916e0b95bd`, so
   bootstrap with the new tenant will be refused as "Untrusted tenant" with no obvious reason.
   The app must make this drift visible and give a sanctioned way to re-seed.

## Requirements

### A. Admin page for site authentication policy

- New SPA page under `web-spa/features/admin/site-settings/pages/` (e.g. `/Admin/Authentication`
  or `/Admin/SiteSettings`), `[Page(..., Policy = PermissionIds.SettingsWrite)]` or a new
  `admin.settings` permission consistent with how Principals/Roles pages are gated (read the
  admin permission conventions in `permission-ids-contracts.cs`: the UI lock is Administrator +
  prefix `admin`; decide whether `SettingsRead`/`SettingsWrite` should be renamed/moved into the
  `admin.*` set — document the decision). Nav entry inside the Admin category.
- Move the Authentication form, its state wiring (`SiteSettingsState`), and the
  `[CrossSliceReference]` from `SettingsPage.razor` to the new page. `/Settings` retains
  passkeys + Link Microsoft 365 only. Remove the "Configuration still registers the Microsoft 365
  scheme…" explanatory copy from Settings; on the admin page keep it and add the effective
  configuration tenant (display name + domain from `EntraAuthenticationOptions.TenantDisplayName`
  / `TenantDomain`, else the GUID) so an admin can see what configuration currently points at.
- Update the SPA integration tests that targeted the section on `/Settings` (219-006 added
  them) to the new route; add a test that a non-admin does not see the nav entry or the page.

### B. Config-drift detection and re-seed

- `SiteSettingsSeeder` (or the seed hosted service): after the store is non-empty, compare the
  persisted policy with configuration each boot. Log **Warning** (one line, `LoggerMessage.Define`)
  when `Authentication:Entra:TenantId` is not in the persisted `EntraTrustedTenants`, when
  `AllowBootstrap` differs, or when `Enabled` differs (extend the existing
  `LogConfiguredVsEnabled` pattern). Message must say what to do: "edit on /Admin/… or run
  `dev entra reseed`".
- On the admin page, surface the same drift as a banner ("Configuration tenant X is not in the
  trusted tenants list") with a one-click "Add configuration tenant" action (uses the normal
  `UpdateSiteSettings` command with version; no bypass).
- Sanctioned re-seed: add `Authentication:Entra:ReseedSiteSettings` (bool, default false, dev
  only) that makes the seeder overwrite the three policy fields from configuration at boot and
  log that it did; AND a dev-cli command `dev entra reseed` that sets that flag in user secrets
  for one run (or, simpler and explicit: the command prints the exact `dev db reset --yes`
  fallback and toggles the flag; worker decides, document it). The flag must never be honoured
  outside Development (check `IHostEnvironment.IsDevelopment()`).
- Tests: seeder drift warning (config tenant missing from trusted list); reseed flag overwrites
  in Development and is ignored otherwise; admin page banner + add-tenant action.

## Checklist

- [ ] Admin page + nav entry + permission gating decision documented
- [ ] `/Settings` reduced to personal items; section and state moved
- [ ] SPA tests moved/added (admin sees page; non-admin does not)
- [ ] Seeder drift Warning with remediation text
- [ ] Admin page drift banner + "Add configuration tenant"
- [ ] `ReseedSiteSettings` flag (Development only) + `dev entra reseed` (or documented fallback)
- [ ] Tests for drift warning, reseed gating, banner action
- [ ] `dotnet test -- --filter-class Entra` and `SiteSettings` green; `dev build` 0/0; `ganda repo audit` clean
- [ ] Results and How to validate (include the screenshot-equivalent steps: sign in as admin,
      open Admin → Authentication, see drift banner, click add, bootstrap with the new tenant)

## Session

- Created: cockpit (2026-09-16)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N

## Notes

- Prior: 219-006 (site settings store, policy seam, Settings page section), 223 (tenant name in
  options/user secrets), 221 (Entra iss fix). Existing admin pages:
  `web-spa/features/admin/principals/pages/PrincipalsPage.razor`, `admin/roles/pages/*`.
- Existing seeder: `source/container-apps/web/features/identity/site-settings-seeder-application.cs`
  (`LogConfiguredVsEnabled`), hosted service `site-settings-seed-hosted-service-server.cs` (retry
  on 42P01 from 219-006 CI fix — keep it).
- `dev db reset --yes` exists (`tools/dev-cli/endpoints/db-reset-command.cs`) and is the blunt
  fallback today; it drops everything, including principals and passkeys.
- Tester's current state: user secrets tenant `a16bcaef-…` (TimeWarp Enterprises LLC),
  persisted trusted tenants `30f3971f-…` (CrunchIt, LLC), controls appear disabled for the
  signed-in passkey user (likely lacks `SettingsWrite`).

## Results

_Pending._

### How to validate

_Pending._
