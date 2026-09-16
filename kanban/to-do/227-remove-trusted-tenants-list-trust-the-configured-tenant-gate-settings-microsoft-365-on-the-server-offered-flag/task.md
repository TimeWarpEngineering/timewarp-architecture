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

- [ ] A: library + web features + migration + options/validator + policy + seeder + dev-cli + docs
- [ ] A: refusal test for foreign `tid` on bootstrap and link
- [ ] B: admin page read-only tenant line with GUID; textarea/banner/add removed; single heading
- [ ] C: Settings gated on server offered flag; client-side Entra config keys deleted; SPA test
- [ ] `dotnet test -- --filter-class Entra` and `SiteSettings` green; `dev build` 0/0;
      `ganda repo audit` clean; `dev template-smoke` passes (floor gate, no count bump needed)
- [ ] Results and How to validate

## Session

- Created: cockpit (2026-09-16)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N

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

_Pending._

### How to validate

_Pending._
