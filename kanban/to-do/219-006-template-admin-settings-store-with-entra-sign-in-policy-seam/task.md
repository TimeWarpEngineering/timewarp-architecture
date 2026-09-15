# Template admin settings store with Entra sign-in policy seam

## Description

Give the template a persisted, admin-editable **site settings** aggregate and use it for the
first runtime policy: whether Entra sign-in is offered to users. Configuration keeps
owning what is *configured* (client id, secret, tenant, `PublicOrigin`, scheme registration at
startup — 219-002/219-004). Settings own what is *allowed* at runtime and will grow as the
architecture example app grows (Steve, 2026-09-15: "the one value will surely grow").

Decision (cockpit, 2026-09-15): split "configured" (deploy-time configuration + secrets) from
"enabled for users" (runtime admin policy). The template gets the settings store now so
products (crunchit 008-004) inherit the seam instead of inventing one.

## Parent

219

## Depends on

- 219-004

## Requirements

### Domain / store (identity-style ports, `IPrincipalStore` is the pattern)

- `SiteSettings` aggregate (single row / singleton id) with `Version` concurrency token
  (`Entity<SiteSettingsId>` or a dedicated singleton type — worker decides, document in Design
  region). First fields, all Entra sign-in policy:
  - `EntraSignInEnabled` (bool, default **false** — configuration `Enabled` says the scheme
    exists; this says users may use it)
  - `EntraAllowBootstrap` (bool, default false)
  - `EntraTrustedTenants` (list of tenant ids; empty = refuse bootstrap)
  - `PasskeyPromptMode` (Soft | Required — Required blocks app use after Entra session until a
    passkey is registered; template default Soft, matching 219-003)
- `ISiteSettingsStore` port: `GetAsync`, `UpdateAsync` (throws `ConcurrencyConflictException`
  on version mismatch). In-memory (default template output) and EF (postgres flag) implementations,
  schema alongside `identity` (worker decides table/schema; keep it out of `principals`).
- Seed: when the store is empty, seed from configuration once (`Authentication:Entra:Enabled`,
  `AllowBootstrap`, `TrustedTenants`) so existing dev setups keep working, then configuration
  stops being consulted for those three. Keep `Authentication:Entra:Enabled` as the
  scheme-registration gate only; log once at boot if configuration says enabled but settings
  say disabled (or vice versa) so the two are never silently confused.

### Policy seam

- `IEntraSignInPolicy` (application layer): `Task<EntraSignInDecision> EvaluateAsync(mode,
  tenantId?)` returning allow / refuse-with-problem (`SignInDisabled`, `BootstrapDisabled`,
  `UntrustedTenant`). Default implementation reads `ISiteSettingsStore`. The 219-002 challenge
  endpoint and the ticket processor call this instead of reading `EntraAuthenticationOptions`
  for `AllowBootstrap` / `TrustedTenants`. Problem titles must match 219-002 tests where they
  exist (`Untrusted tenant`), add `Sign-in disabled` (403) for the new case.
- A product may replace `IEntraSignInPolicy` (DI) without touching the scheme.

### Contracts / endpoints (web-contracts + web-server, `tw-web-api-contracts` conventions)

- `GetSiteSettings` query (policy `PermissionIds.SettingsRead`) and `UpdateSiteSettings`
  command (new `PermissionIds.SettingsWrite`; grant to the bootstrap administrator role) with
  `Version` for optimistic concurrency (409 on conflict). Validator: tenant ids are GUIDs.
- SPA: the login page asks the server whether Entra sign-in is offered (public, unauthenticated
  read of the *offered* flag only — do not expose tenant list anonymously) and hides
  "Continue with Microsoft 365" when disabled. Settings page (`/Settings`, existing
  `SettingsRead` policy) gets an "Authentication" section: Entra sign-in toggle, allow
  bootstrap, trusted tenants editor, passkey prompt mode; edits require `SettingsWrite`.
- Disabling at runtime refuses **new** challenges only; existing Entra credentials and sessions
  stay (lockout is directory-sync policy per RFC 219 D9).

### Tests

- Store tests (in-memory + EF via existing test host pattern): get/update/concurrency/seed-once.
- Policy tests: disabled → 403 `Sign-in disabled`; bootstrap disabled; untrusted tenant; allowed.
- Endpoint tests: read requires `SettingsRead`; write requires `SettingsWrite`; 409 on stale
  version; anonymous "offered" read exposes only the boolean.
- Existing `Entra*` tests updated to seed settings rather than configuration where they
  asserted `AllowBootstrap` / `TrustedTenants`.

### Docs

- Identity guide: "Configured vs enabled" section; `dev entra setup` (219-005) still writes
  configuration; first-run seed copies into settings; after that use the Settings page.
- Design regions on the aggregate, port, policy, and the challenge endpoint.

## Checklist

- [ ] `SiteSettings` aggregate + `ISiteSettingsStore` (in-memory + EF) + seed-once
- [ ] `IEntraSignInPolicy` + default impl; challenge endpoint and ticket processor use it
- [ ] `GetSiteSettings` / `UpdateSiteSettings` contracts, handlers, `SettingsWrite` permission
- [ ] Anonymous "Entra sign-in offered" read for the login page; login button hidden when off
- [ ] Settings page "Authentication" section (toggle, bootstrap, tenants, passkey prompt mode)
- [ ] Tests listed above green; `dotnet test -- --filter-class Entra` still green
- [ ] `dev build` 0/0; `ganda repo audit` clean; TWA0009 slice placement respected
      (`tw-slice-isolation`: settings is its own slice or under identity — decide and document)
- [ ] Docs + Design regions
- [ ] Results and How to validate

## Session

- Created: 99473 (2026-09-15)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N

## Notes

- Why a store and not more configuration: configuration is deploy-time and secret-bearing;
  policy is edited by an administrator at runtime and will accumulate (Entra today; later
  registration open/closed, agent key registration, payment toggles from `TimeWarp.402`,
  maintenance banners). One aggregate with a concurrency token is the pattern to copy.
- Existing seams to reuse: `PermissionIds` / `IPermissionEvaluator`
  (`source/container-apps/web/platform/authorization/`), `SettingsPage.razor` (`/Settings`,
  `SettingsRead`), `IPrincipalStore` in-memory/EF pair, `ConcurrencyConflictException`,
  `BootstrapAdministratorOptions` (program.cs ~214) for the admin grant.
- Crunchit 008-004 will replace `IEntraSignInPolicy` (or extend the aggregate) with the
  portal's own "Microsoft 365 sign-in" toggle; keep the interface small.
- Do not fold `PublicOrigin`, client id, secret, or tenant id into settings — those stay
  configuration (219-004 / 219-005).

## Results

_Pending._

### How to validate

_Pending._
