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

- [x] Admin page + nav entry + permission gating decision documented
- [x] `/Settings` reduced to personal items; section and state moved
- [x] SPA tests moved/added (admin sees page; non-admin does not)
- [x] Seeder drift Warning with remediation text
- [x] Admin page drift banner + "Add configuration tenant"
- [x] `ReseedSiteSettings` flag (Development only) + `dev entra reseed` (or documented fallback)
- [x] Tests for drift warning, reseed gating, banner action
- [x] `dotnet test -- --filter-class Entra` and `SiteSettings` green; `dev build` 0/0; `ganda repo audit` clean
- [x] Results and How to validate (include the screenshot-equivalent steps: sign in as admin,
      open Admin → Authentication, see drift banner, click add, bootstrap with the new tenant)

## Session

- Created: cockpit (2026-09-16)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N
- Implementer: Grok session (2026-09-16)

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

Site authentication policy is an Admin concern. `/Settings` keeps passkeys and Link Microsoft 365. Config-vs-store drift is a boot Warning with a one-click add-tenant on `/Admin/Authentication`, plus a Development-only reseed flag and `dev entra reseed`.

**Permission decision:** keep `settings.read` (self-service: personal Settings + GetSiteSettings for passkey prompt / Link Microsoft 365) and `settings.write` (Administrator seed; gates `/Admin/Authentication` and UpdateSiteSettings). Not renamed into `admin.*` — SettingsRead is not an admin permission, and moving write-only would require a `role_permissions` data migration plus a catalog break. Protected-core stays prefix `admin` (roles/principals lockout). The Admin nav category (`admin.access`) is the audience lock. Documented in `permission-ids-contracts.cs` Design region.

**`dev entra reseed`:** writes `Authentication:Entra:ReseedSiteSettings=true` in Web.Server user secrets. The next Development boot overwrites EntraSignInEnabled, EntraAllowBootstrap, and EntraTrustedTenants from configuration (PasskeyPromptMode kept). `--clear` writes false. Prints `dev db reset --yes` as the blunt fallback that drops principals and passkeys. The seeder ignores the flag unless the hosted service passes `IHostEnvironment.IsDevelopment()`.

### Files changed

- Admin page: `web-spa/features/admin/site-settings/pages/AuthenticationPage.razor` (+ `.cs`), NavMenu + `_Imports` + global usings
- `/Settings` stripped of the Authentication section; still reads `EntraSignInEnabled` for Link Microsoft 365
- GetSiteSettings response projects bound `Authentication:Entra` tenant/enabled snapshot (CrossSliceReference on the handler)
- `SiteSettingsConfigurationDrift` helper; seeder drift Warnings + Development reseed; `EntraAuthenticationOptions.ReseedSiteSettings`
- `dev entra reseed` / `--clear`; entra status lists the flag
- Tests: seeder drift/reseed, drift helper + add-tenant merge, GetSiteSettings mapping, SPA route/policy, Member 403 / Admin 200 HTML, Settings HTML no longer contains AuthenticationSettings

### Test outcomes

- `dotnet run tools/dev-cli/dev.cs -- build` — 0 Warning / 0 Error
- Host-free: seeder 6/6, drift helper 6/6, GetSiteSettings 4/4, UpdateSiteSettings 5/5
- `cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class Entra` — 48 passed
- `cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class SiteSettings` — 6 passed
- SPA `AuthenticationPage` 2/2; HTML deep-link Authentication 4/4 (Member 403 after pinning Member; Admin 200)
- `ganda repo audit` — pass (2 pre-existing advisory warnings: memsearch-scaffold, vscode-window-icon)

### How to validate

**Depends on:** local Aspire (`dev run`), a passkey Administrator, and Entra user secrets from `dev entra setup`. Postgres volume already seeded with an older trusted tenant.

**Smoke**

1. Sign in as an Administrator (passkey or Entra).
2. Confirm `/Settings` shows Passkeys and (if Entra is offered) Link Microsoft 365 only — no Authentication section, no "Configuration still registers the Microsoft 365 scheme".
3. Open Admin → Authentication (`/Admin/Authentication`).
4. See "Configuration tenant: …" (display name + domain when `dev entra setup` wrote them, else the GUID).
5. If user secrets tenant is not in trusted tenants (e.g. TimeWarp Enterprises LLC `a16bcaef-…` vs persisted CrunchIt `30f3971f-…`): warning banner "Configuration tenant … is not in the trusted tenants list" and **Add configuration tenant**.
6. Click **Add configuration tenant**. Banner clears; the GUID appears in Trusted tenants. Save is the normal UpdateSiteSettings path (version CAS).
7. Bootstrap / sign in with the new tenant — no "Untrusted tenant".
8. Sign in as Member: Admin category hidden; GET `/Admin/Authentication` is 403.

**Expect**

- Member never sees site policy controls on `/Settings`.
- Admin page is gated by `settings.write` (Administrator seed) inside the Admin nav (`admin.access`).
- Boot log Warning when config tenant is missing from trusted tenants, when Enabled differs, or when AllowBootstrap differs — text includes `edit on /Admin/Authentication or run \`dev entra reseed\``.
- `dev entra reseed` writes the flag; next **Development** `dev run` overwrites the three policy fields; `dev entra reseed --clear` stops later overwrites. Flag ignored outside Development.

**Automated**

```bash
dotnet run tools/dev-cli/dev.cs -- build
# expect: 0 Warning(s) 0 Error(s)

dotnet run source/container-apps/web/features/identity/site-settings-seeder-tests.cs
dotnet run source/container-apps/web/features/settings/site-settings-configuration-drift-tests.cs
dotnet run source/container-apps/web/features/settings/get-site-settings/get-site-settings-tests.cs

cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class Entra
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class SiteSettings
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class AuthenticationPage
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-method Authentication

ganda repo audit
```

**Not in scope:** live WebAuthn ceremony against a real authenticator; production honoring of `ReseedSiteSettings`.
