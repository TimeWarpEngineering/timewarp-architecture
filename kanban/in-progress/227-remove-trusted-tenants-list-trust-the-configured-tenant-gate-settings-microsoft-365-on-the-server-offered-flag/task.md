# Remove trusted tenants list: trust the configured tenant, gate Settings Microsoft 365 on the server offered flag

## Description

Decisions from the live review on 2026-09-16 (Steve):

1. **We support exactly one Entra tenant today** — the tenant that hosts the app registration
   (`Authentication:Entra:TenantId`; `dev entra setup` creates single-tenant registrations).
   The `TrustedTenants` allowlist (configuration + persisted site setting + admin textarea) exists
   for a multi-tenant mode nobody has, and it is what produced the "configuration drift" the
   tester hit (persisted CrunchIt GUID after switching the app to the TimeWarp tenant). Remove it.
   Trust = token `tid` equals the configured tenant. Re-introduce an allowlist only when a real
   multi-tenant registration exists.
2. **Drift banner shows the bare GUID**, so it aligns with what an admin sees elsewhere. With (1)
   the tenant part of drift disappears entirely; keep only the informational "App registration
   tenant" line with name, domain (when known) and GUID.
3. **`/Settings` lost its Microsoft 365 section** on the tester's machine. Cause: the section is
   gated by the SPA's client-side configuration (`wwwroot/appsettings*.json`
   `Authentication:Entra:Enabled` via `MockAuthenticationDefaults.IsEntraEnabled`, from 219-002),
   which nothing writes — `dev entra setup` writes server user secrets only. The login page
   already gates on the server (`GetEntraSignInOffered`). Settings must use the same server flag;
   delete the client-side Entra keys.
4. Heading cleanup on `/Admin/Authentication` (title + section heading both "Authentication").
   **Do not** touch the breadcrumb component (stack-based, out of scope).

## Requirements

### A. Remove TrustedTenants everywhere (inventory below)

- `TimeWarp.Identity`: drop `SiteSettings.EntraTrustedTenants` (aggregate, store port, in-memory
  store, tests). Keep `EntraSignInEnabled`, `EntraAllowBootstrap`, `PasskeyPromptMode`.
- Web features: remove from `GetSiteSettings` / `UpdateSiteSettings` contracts, handlers, details,
  validators, EF configuration; add a migration that drops the column (`identity.site_settings`).
- `EntraAuthenticationOptions`: remove `TrustedTenants`; validator **rejects** the key if present
  in configuration (fail loudly with "TrustedTenants was removed by task 227; trust is TenantId").
- `IEntraSignInPolicy` / `SiteSettingsEntraSignInPolicy` / `EntraTicketProcessor`: the tenant
  check becomes `claims.TenantId == options.TenantId` (GUID compare; `organizations` / `common`
  authority is not supported for bootstrap — refuse with the existing `Untrusted tenant` 403 and
  document). Keep the `EntraIssuerValidator` pin (issuer must match the token's own `tid`).
- Seeder: no tenant seeding; drift detection reduced to `Enabled` / `AllowBootstrap` mismatch
  warnings (keep the 42P01 retry). `ReseedSiteSettings` flag and `dev entra reseed` stay but no
  longer mention tenants; if they become pointless after this, remove them and say so in Results.
- `dev entra setup` / `status` / `entra-setup.cs`: stop writing/listing `TrustedTenants:0`;
  status warns if the obsolete key is still present in user secrets and prints the
  `dotnet user-secrets remove` line.
- `appsettings.json` (web-server): remove the `TrustedTenants` placeholder. `auth.md` and
  `overview.md` updated.
- Tests: update every test in the inventory; add one asserting a token whose `tid` differs from
  the configured tenant is refused on bootstrap and on link.

### B. Admin page (`/Admin/Authentication`)

- Remove the trusted tenants textarea, the drift banner for tenants, and "Add configuration
  tenant". Show a read-only "App registration tenant" line: `{DisplayName} ({Domain}) — {GUID}`
  or just the GUID when name/domain are unknown. Keep the Enabled/AllowBootstrap drift banner if
  it survives (A).
- One "Authentication" heading: keep the page title, rename the section heading to
  "Microsoft 365 sign-in policy" (or drop it).

### C. Settings page gate

- `/Settings` shows the Microsoft 365 section when the **server** says sign-in is offered
  (`GetEntraSignInOffered.Offered`, same source as the login page), independent of client config.
  Delete `MockAuthenticationDefaults.EntraEnabledKey` / `UseEntraKey` / `IsEntraEnabled` /
  `UsedObsoleteUseEntraKey` and the `Authentication:Entra` / `UseEntra` blocks in both
  `web-spa/wwwroot/appsettings*.json` if nothing else consumes them (grep; `UseMock` stays).
- SPA integration test: with the server reporting offered = true the section renders (link CTA
  present); with offered = false it does not.

## Checklist

- [x] A: library + web features + migration + options/validator + policy + seeder + dev-cli + docs
- [x] A: refusal test for foreign `tid` on bootstrap and link
- [x] B: admin page read-only tenant line with GUID; textarea/banner/add removed; single heading
- [x] C: Settings gated on server offered flag; client-side Entra config keys deleted; SPA test
- [x] `dotnet test -- --filter-class Entra` and `SiteSettings` green; `dev build` 0/0;
      `ganda repo audit` clean; `dev template-smoke` passes (floor gate, no count bump needed)
- [x] Results and How to validate
- [x] Implementation review disposition (clean)

## Session

- Created: cockpit (2026-09-16)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N
- Implementer: Grok 4.6 (2026-09-16)
- Resume: Grok 4.6 (2026-09-16) — drop padded tests, set web smoke floor to 177, commit
- Review oracle: grok session 01a0aa03-4bbd-7d21-aa21-400c840ccc7a (2026-09-16)
- Reviewer (round 1 general): grok session 01a0aa04-ced5-72d1-99fd-7588ae6b7516 (2026-09-16)

## Notes

- Inventory (`grep -rln "TrustedTenants\|EntraTrustedTenants"` on master 2026-09-16):
  identity options/validator/ticket-processor/policy/seeder(+tests); settings contracts/handlers/
  details/EF config/tests; postgres migration `20260915135643_AddSiteSettings` + snapshot;
  web-server `appsettings.json`; SPA `AuthenticationPage.razor`, `site-settings-state*.cs`,
  `auth.md`; identity library `site-settings.cs`, `i-site-settings-store.cs`, `overview.md`;
  tests `entra-challenge`, `entra-oidc-handler-round-trip`, `entra-public-origin`,
  `entra-scheme-registration`, `entra-ticket-processor`, `site-settings-endpoint`,
  `in-memory-site-settings-store-tests`; dev-cli `entra-reseed`, `entra-setup`, `entra-status`,
  `services/entra-setup.cs`.
- Client-side gate: `web-spa/services/mocks/mock-authentication-defaults.cs` (`EntraEnabledKey`,
  `UseEntraKey`, `IsEntraEnabled`), consumed by `SettingsPage.razor` L28-30 only.
- Prior: 219-002 (gate + TrustedTenants introduced), 219-006 (persisted), 225 (admin page, drift).
- RFC 219 "pin the tenant" guardrail is preserved: the pin is `TenantId`, enforced in one place.
- Out of scope: breadcrumb trail behaviour.

## Results

Removed the TrustedTenants allowlist. Trust is `claims.TenantId == Authentication:Entra:TenantId` (GUID compare) in `SiteSettingsEntraSignInPolicy`, used for bootstrap, sync-hit, and link. `organizations` / `common` is not a GUID, so those tickets refuse 403 Untrusted tenant. EntraIssuerValidator still pins `iss` to the token's own `tid`.

`SiteSettings` no longer stores tenants. Seeder copies only Enabled / AllowBootstrap; drift warnings are those two fields. `ReseedSiteSettings` and `dev entra reseed` stay (still useful for Enabled/AllowBootstrap). `dev entra setup` no longer writes `TrustedTenants:0`. `dev entra status` warns if the obsolete key is present and prints `dotnet user-secrets remove "Authentication:Entra:TrustedTenants:0" --project source/container-apps/web/projects/web-server`. Options validator fails boot with `TrustedTenants was removed by task 227; trust is TenantId` when the key is still in configuration. Migration `20260916120000_DropSiteSettingsEntraTrustedTenants` drops `identity.site_settings.EntraTrustedTenants`.

`/Admin/Authentication` keeps the page title Authentication; section heading is "Microsoft 365 sign-in policy". Read-only "App registration tenant" line is `{DisplayName} ({Domain}) — {GUID}` or the bare GUID. Trusted-tenants textarea, tenant drift banner, and "Add configuration tenant" are gone. Enabled/AllowBootstrap mismatch still shows a warning banner.

`/Settings` Microsoft 365 section (including Link CTA) is gated on `GetEntraSignInOffered.Offered`, same as Login. HTML proof: `Settings_Microsoft365_Section_Should_Follow_Server_Offered_Flag` in web-server-integration prerender. SPA suite pins the offered query (`settings-page-microsoft-365-tests.cs`). SPA `wwwroot/appsettings*.json` no longer has `Authentication:Entra` / `UseEntra` (`UseMock` stays). `MockAuthenticationDefaults.EntraEnabledKey` / `UseEntraKey` / `IsEntraEnabled` remain because Web.Server scheme registration still uses them.

**Key decisions:** Reseed kept. Client Entra helpers kept for the server. Web co-located count dropped with the trusted-tenants tests; `MinimumSucceeded` for web is **177** (real count). Did not pad tests to hold 180.

**Tests:** Entra filter 52 (web-server-integration) + 12 (web-jaribu) + 43 (dev-cli); SiteSettings 6 (endpoint) + 25 (web-jaribu, class-name overlap with policy); identity InMemory SiteSettings 5; Returns (Settings HTML + offered gate) 113; SPA SettingsPage 1 + AuthenticationPage 2. web-jaribu-tests 177/177. `dev build` 0/0. `ganda repo audit` pass (2 pre-existing advisory warnings). `dotnet run tools/dev-cli/dev.cs -- template-smoke` SUCCEEDED (web 177/177 floor 177; api 9/9; common 3/3).

### How to validate

**Smoke**

```bash
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class Entra
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class SiteSettings
cd tests/container-apps/web/web-jaribu-tests && dotnet test -c Release -- --filter-class Entra
cd tests/container-apps/web/web-jaribu-tests && dotnet test -c Release -- --filter-class SiteSettings
./bin/dev build
ganda repo audit
dotnet run tools/dev-cli/dev.cs -- template-smoke
```

Rebuild `./bin/dev` (`dotnet run tools/dev-cli/dev.cs -- self-install`) before `./bin/dev template-smoke` if the AOT binary still has floor 180.

With a Development host (`./bin/dev run`), sign in as Administrator:

1. Open `/Admin/Authentication`. Expect title Authentication, one section heading "Microsoft 365 sign-in policy", a read-only "App registration tenant" line that includes the GUID, and no trusted-tenants textarea / Add configuration tenant.
2. Open `/Settings` with Entra offered (scheme registered and Offer Microsoft 365 sign-in saved). Expect Microsoft 365 section and `data-qa="LinkMicrosoft365"`.
3. Turn off offer (or disable Entra) and reload `/Settings`. Expect no Microsoft 365 section.

**Expect**

- Entra/SiteSettings filters: all passed.
- `dev build`: 0 Warning(s), 0 Error(s).
- `ganda repo audit`: exit 0 (advisory memsearch/vscode warnings only).
- template-smoke: `Template smoke SUCCEEDED`; web-jaribu-tests 177/177 (floor 177).
- Foreign `tid` bootstrap and link: 403 title `Untrusted tenant`.
- Config still containing `Authentication:Entra:TrustedTenants`: options validation fails with `TrustedTenants was removed by task 227; trust is TenantId`.

**Automated gate**

```bash
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class Entra
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class SiteSettings
cd tests/container-apps/web/web-jaribu-tests && dotnet test -c Release -- --filter-class Entra
cd tests/container-apps/web/web-jaribu-tests && dotnet test -c Release -- --filter-class SiteSettings
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class SettingsPage
./bin/dev build
```

**Depends on:** Development host for the UI smoke; postgres for the drop-column migration on a live database (`dev db update`). Stale AOT `./bin/dev` still asserts floor 180 — use `dotnet run tools/dev-cli/dev.cs -- template-smoke` or self-install first.

**Not in scope:** live Entra round-trip against a real tenant; breadcrumb trail.

### Review disposition

- **Outcome:** clean
- **Rounds:** 1
- **Effort / roster:** 1, general only
- **Final counts:** bug 0/0/0, suggestion 0/0/0, nit 0/0/0 (open/fixed/wontfix)
- **Wontfix / escalations:** none
- **Paths:**
  - `review/review-framework.md`
  - `review/round-1/general.md`
  - `review/round-1/merged.md`
  - `review/disposition.md`

## Implementation Notes

- Cockpit note (2026-09-16, before resume): the first implementer pass hit the turn budget after
  `dev template-smoke` passed and before committing. **Do not pad tests to satisfy the smoke
  floor.** Removing the trusted-tenants tests legitimately lowers the web co-located count; set
  `MinimumSucceeded` for `web` in `tools/dev-cli/services/template-smoke-harness.cs` to the real
  count (177 per the smoke log) and remove any filler tests that were added only to reach 180.
  Then write Results, tick the checklist, and commit everything on the task branch.
