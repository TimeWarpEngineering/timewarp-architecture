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

- [x] `SiteSettings` aggregate + `ISiteSettingsStore` (in-memory + EF) + seed-once
- [x] `IEntraSignInPolicy` + default impl; challenge endpoint and ticket processor use it
- [x] `GetSiteSettings` / `UpdateSiteSettings` contracts, handlers, `SettingsWrite` permission
- [x] Anonymous "Entra sign-in offered" read for the login page; login button hidden when off
- [x] Settings page "Authentication" section (toggle, bootstrap, tenants, passkey prompt mode)
- [x] Tests listed above green; `dotnet test -- --filter-class Entra` still green
- [x] `dev build` 0/0; `ganda repo audit` clean; TWA0009 slice placement respected
      (`tw-slice-isolation`: settings is its own slice — documented)
- [x] Docs + Design regions
- [x] Results and How to validate

## Session

- Created: 99473 (2026-09-15)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N
- Implementer: grok (2026-09-15)
- Review oracle: grok session 01a0a562-c060-7110-bb10-a0ec9995d2b5 (2026-09-15)

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

Site settings is a singleton aggregate (`Entity<SiteSettingsId>` with well-known `SingletonId`)
plus `ISiteSettingsStore` (in-memory default, EF behind postgres). First-run seed copies
`Authentication:Entra:Enabled` / `AllowBootstrap` / `TrustedTenants` once; after that
`IEntraSignInPolicy` reads the store, not those three options. Configuration `Enabled` remains
the scheme-registration gate only; boot logs when the two disagree.

**Slice placement:** own product slice `Features.Settings` under `web/features/settings/`
(SPA state under `web-spa/features/settings/`). Identity policy reads `ISiteSettingsStore` from
`TimeWarp.Identity` (other assembly, TWA0009-free). Settings page is Applications chrome with
`[CrossSliceReference]` to `SiteSettingsState`. Table is `identity.site_settings` (alongside
principals, never inside them).

**Key decisions**
- `IEntraSignInPolicy.EvaluateAsync(mode, tenantId?)` is the only method — products (crunchit
  008-004) replace it in DI without touching the named `entra` scheme.
- Challenge is the only mode that returns 403 `Sign-in disabled`; existing sessions stay.
- `SettingsWrite` is Administrator seed only (not `admin.*` protected-core).
- Anonymous `GET api/identity/entra/offered` returns `{ offered }` only.
- Passkey prompt `Required` hides `TimeWarpPage` body until a passkey exists.
- Review M1: Settings Get/Update do not insert on an empty store (503 `Site settings not initialized`); only `SiteSettingsSeeder` writes the first row, in `StartingAsync` before Kestrel accepts requests.

**Tests (this session)**
- In-memory store: 5 passed
- EF store: 1 passed (ephemeral Postgres)
- Policy / seeder / offered / Get / Update runfiles: all passed
- `cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class Entra` — 36 passed
- Site settings endpoints: 6 passed (401/403/409/200 + anonymous offered boolean)
- `dev build` 0/0
- `ganda repo audit` passes (2 pre-existing advisory warnings: memsearch hooks, vscode peacock)
- Review M1 runfiles: get-site-settings 3 passed; update-site-settings 5 passed; seeder 3 passed

**Review**
- Rounds: 2 · Effort: 1 · Roster: general
- Counts (final): bug 0 open / 1 fixed / 0 wontfix; suggestion 0 / 0 / 0; nit 0 / 0 / 0
- Disposition: **clean** (M1 fixed on this task id; no exceptions)
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/round-2/general.md`, `review/round-2/merged.md`, `review/disposition.md`

**Not in this commit:** live Entra ID round-trip; `dev entra setup` still writes configuration
only (first-run seed copies into settings).

### How to validate

**Automated**
```bash
# from repo root
dev build
# expect: 0 Warning(s), 0 Error(s)

cd tests/libraries/timewarp-identity-tests && dotnet test -c Release -- --filter-class SiteSettings
# expect: 5 passed

cd tests/container-apps/web/web-infrastructure-tests && dotnet test -c Release -- --filter-class SiteSettings
# expect: 1 passed (or skip when Postgres is unavailable locally; CI requires it)

dotnet run source/container-apps/web/features/settings/get-site-settings/get-site-settings-tests.cs
# expect: 3 passed — empty store GET is 503 and does not insert

dotnet run source/container-apps/web/features/settings/update-site-settings/update-site-settings-tests.cs
# expect: 5 passed — empty store PUT is 503 and does not insert; 409 on stale Version

dotnet run source/container-apps/web/features/identity/site-settings-seeder-tests.cs
# expect: 3 passed — empty Get then seeder still copies Authentication:Entra

dotnet run source/container-apps/web/features/identity/entra-sign-in-policy-tests.cs
# expect: 7 passed (disabled → 403 Sign-in disabled; bootstrap; untrusted; allowed)

cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class Entra
# expect: 36 passed

cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class SiteSettings
# expect: 6 passed — anonymous GET api/settings 401; GET api/identity/entra/offered 200
# with JSON containing offered and not tenant/bootstrap; Member PUT 403; stale Version 409
```

**Smoke**
```bash
dev run
# 1. Open /Login as anonymous. GET https://localhost:7000/api/identity/entra/offered
#    expect: 200 {"offered":false} (template default) and no "Continue with Microsoft 365".
# 2. Sign in as bootstrap Administrator. Open /Settings.
#    expect: Authentication section with Entra toggle, allow bootstrap, trusted tenants,
#    passkey prompt. Save with SettingsWrite.
# 3. With scheme registered (dev entra setup) and EntraSignInEnabled true, /Login shows
#    Continue with Microsoft 365. Toggle it off and save; /Login hides the button.
#    New challenge GET /api/identity/entra/challenge?mode=bootstrap → 403 Sign-in disabled.
#    Existing Entra-linked session still works.
```

**Expect**
- `GET api/settings` without a session → 401
- `PUT api/settings` as Member → 403
- `PUT api/settings` with a Version that does not match the stored row → 409 `Concurrency conflict`
- Offered JSON is a single boolean; no tenant list

**Depends on:** `dev run` for the SPA smoke; Postgres only for the EF store test.

**Not in scope:** live Microsoft 365 login against a real tenant (needs `dev entra setup` + Azure).

### CI fix (post-review)

CI run 34981521704 on PR #365 failed two ways: (A) `template-smoke-harness.cs`'s
`JaribuFamilyAggregators` still expected 148 for `web-jaribu-tests`; the aggregator now totals
170 (confirmed with a local `dotnet test -c Release` run), so the expectation was bumped 148 → 170.
(B) `SiteSettingsSeedHostedService.StartingAsync` (runs before Kestrel accepts requests) could hit
Npgsql 42P01 ("relation identity.site_settings does not exist") because the AppHost has no wait
edge between web-server and web-migrations by design (task 155). Fixed by giving the seed a
bounded retry (1s backoff, up to 30 attempts) on that specific undefined-table error, matching the
same first-boot race the rest of the postgres path already tolerates; the in-memory path is
unaffected since it never throws that error. Verified locally: `aspire-tests` (7/7, all
`*ThroughIngress*` cases) green against real Postgres via Docker.

CI run 34983768899: `ci` job and `SmokeDefault` passed, but `SmokeNoPostgres` failed to build
(CS0234/CS0246) because `ef-site-settings-store-infrastructure.cs` and
`site-settings-entity-type-configuration-infrastructure.cs` were still emitted into the
no-postgres generated app. Added both to the `(!postgres)` exclude list in
`.template.config/template.json`, alongside the other `ef-*-store-infrastructure.cs` /
`*-entity-type-configuration-infrastructure.cs` pairs. `InMemoryIdentityStoresModule` already
registers `ISiteSettingsStore -> InMemorySiteSettingsStore` unconditionally, so the no-postgres
app still boots. Verified locally: `dev build` (0/0) and a full local `dev template-smoke` run —
`SmokeDefault`, `SmokeNoPostgres`, `SmokeNoApi` all built 0 errors, ending "Template smoke
SUCCEEDED"; `ganda repo audit` unchanged (2 pre-existing advisory warnings).
