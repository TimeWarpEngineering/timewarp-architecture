# Admin-configurable accepted tenants for Microsoft sign-in (allow any or a list)

## Goal (implementer restatement)

Administrators choose, at runtime, which Microsoft Entra tenants may sign in. The choice lives on the site-settings row and is edited on `/Admin/Authentication`. It is either a list of tenant ids (restricted; one id is the single-tenant case) or any tenant. The existing offer toggle still only decides whether Microsoft sign-in is available.

Acceptance means:

- The first human passkey registration already becomes Administrator through `TryClaimFirstAdministratorAsync`, and that role can open the Authentication page. Document that path. Do not add another bootstrap.
- Restricted mode refuses sign-in, link, and bootstrap when the token `tid` is missing from the accepted list (`Untrusted tenant`).
- Any-tenant mode accepts any non-empty tenant id. The token issuer is still pinned to that `tid`.
- Turning Microsoft sign-in off still refuses new challenges. Bootstrap off still refuses new accounts.
- `Authentication:Entra:TenantId` stays the OIDC authority (`organizations`, `common`, or a home-tenant GUID). It is not the trust list. Multi-tenant sign-in needs a multi-tenant app registration, documented with the security tradeoffs (including personal Microsoft accounts).
- Tests cover a refused unlisted tenant, an allowed different tenant in any-mode, and the unchanged availability toggle.

## Description

Steven, 2026-10-11 (voice). With Sign in with Microsoft we want an identity/ID; we do not inherently care which Entra tenant the person is in. Today trust is pinned to a single configured tenant. Add an **admin-configurable** tenant acceptance policy (runtime SiteSettings, not appsettings): the admin can accept a list of tenant ids, or allow any tenant. Keep the existing admin-panel on/off toggle for Microsoft sign-in availability.

**Startup concept:** whoever logs in first with a passkey becomes the admin. That admin uses `/Admin/Authentication` to set which tenant ids are accepted for Microsoft sign-in / sign-up, or to allow any tenant.

This is **not** about hard-coding "drop the single-tenant limit" in deploy config. Single-tenant remains available as one mode of the admin policy (restrict to a list of one or more tenants). "Any tenant" is the other mode.

## Requirements

- Keep `EntraSignInEnabled` (and related offer/bootstrap toggles) on the admin Authentication page; do not remove or redefine that availability control.
- Add runtime SiteSettings fields for tenant acceptance, e.g. allow-any vs accepted-tenant-id list (shape TBD). Persist via existing `ISiteSettingsStore` / UpdateSiteSettings — not `Authentication:Entra:TenantId` appsettings as the trust policy.
- Update `SiteSettingsEntraSignInPolicy` (and ticket SyncHit / Link / BootstrapCreate paths) so:
  - **Restricted:** token `tid` must be in the accepted list; otherwise refuse Untrusted tenant (behavior analogous to today's pin).
  - **Any tenant:** accept any non-empty GUID `tid` (still pin `iss` to that tid via `EntraIssuerValidator`).
- Document security implications: any-tenant means any org account that can complete OIDC against the app registration can sign up/link (subject to bootstrap/link policy); whether personal Microsoft accounts (`consumers`) are included; optional future blocklist if needed.
- Entra **app registration** must use multi-tenant supported account types for "any" (and for accepting tenants other than the home tenant). Note in docs / setup (`dev entra setup` / overview).
- Authority / OIDC: today `organizations` / `common` as `Authentication:Entra:TenantId` never matches GUID pin and bootstrap/link refuse Untrusted. Multi-tenant auth will need authority + issuer validation adjusted so OIDC can succeed while **policy** (SiteSettings) decides acceptance — do not leave trust solely on the configured TenantId GUID equality.
- First-admin: confirm and document that first human passkey create already claims Administrator via `IPrincipalRoleStore.TryClaimFirstAdministratorAsync` (already in code). If gaps exist for the "first passkey user can reach Admin Authentication" story, note them; do not invent a second bootstrap path unless missing.
- Tests: restricted list refuses unlisted tid; any-tenant allows different tid; availability toggle still gates offer/challenge; regression that off/disabled behavior is unchanged.

**Acceptance**

- First passkey user becomes admin (existing `TryClaimFirstAdministratorAsync` path; document if already sufficient).
- Admin can set accepted tenants **or** any-tenant on `/Admin/Authentication`.
- Sign-in/link/bootstrap from an unlisted tenant is refused when restricted.
- Any-tenant works when allowed.
- Microsoft sign-in availability toggle unchanged in meaning.
- Automated tests cover restricted vs any.

## Checklist

- [x] SiteSettings (+ migration / UpdateSiteSettings contracts / AuthenticationPage) carry allow-any or accepted-tenant list
- [x] `SiteSettingsEntraSignInPolicy` trusts SiteSettings list/any instead of only `Authentication:Entra:TenantId` GUID equality
- [x] OIDC authority / issuer path supports multi-tenant tickets when policy allows
- [x] Docs: security implications + Entra app registration supported account types
- [x] Confirm first-passkey → Administrator (`TryClaimFirstAdministratorAsync`) is sufficient for admin to configure tenants
- [x] Tests: restricted refuse, any allow, availability toggle unchanged

## Notes

### Code grounding (read-only, master ~2026-10-11)

**Trust today is single-tenant config pin**

- `source/libraries/timewarp-identity/overview.md` — trust is `Authentication:Entra:TenantId` (token `tid` must GUID-equal); `organizations` / `common` not supported for bootstrap/link (403 Untrusted tenant). Runtime offer/bootstrap live on SiteSettings; secrets/authority stay in config.
- `source/libraries/timewarp-identity/settings/site-settings.cs` — `EntraSignInEnabled`, `EntraAllowBootstrap`, `PasskeyPromptMode` only. Explicit design note (task 227): aggregate does **not** store a tenant allowlist; trust is config TenantId.
- `source/container-apps/web/features/identity/entra-authentication-options-application.cs` — `Authentication:Entra` bind; `TenantId` is the trust pin; task 227 removed `TrustedTenants` allowlist from options (validator rejects that key if present).
- `source/container-apps/web/features/identity/site-settings-entra-sign-in-policy-application.cs` — `MatchesConfiguredTenant`: SyncHit/Link/BootstrapCreate refuse unless `tid` GUID-equals configured TenantId; non-GUID TenantId (`organizations`/`common`) never matches.
- `source/container-apps/web/features/identity/entra-sign-in-policy-tests.cs` — covers disabled / bootstrap disabled / untrusted tenant / allowed / organizations refuse.
- Migration `20260916120000_DropSiteSettingsEntraTrustedTenants` dropped a prior `EntraTrustedTenants` column from SiteSettings — this task reintroduces a **runtime** accepted-tenants (or any) concept under admin control, not a return to deploy-time TrustedTenants on options.

**Offer / Link Microsoft 365 / ticket processor**

- `source/container-apps/web/features/identity/entra-sign-in-offer-application.cs` + `get-entra-sign-in-offered/` — `GetEntraSignInOffered`; scheme `Enabled` AND settings `EntraSignInEnabled`.
- GetCredentials `CanLinkMicrosoft365` uses the same offer rule (task 282).
- `source/container-apps/web/features/identity/entra-ticket-processor-application.cs` — bootstrap create/existing, link; policy evaluates with tenant id from claims; handle = `EntraAccountHandle.Encode(tenantId, objectId)`.
- `source/container-apps/web/features/identity/entra-issuer-validator-server.cs` — for multi-tenant authorities, pins `iss` to `EntraIssuerMaterial.FromTenantId(tid)` (does not replace the policy trust pin).

**Admin UI (where the new control belongs)**

- `source/container-apps/web/projects/web-spa/features/admin/site-settings/pages/AuthenticationPage.razor` (+ `.cs`) — admin Authentication panel.
- `source/container-apps/web/features/settings/update-site-settings/` + `get-site-settings/` — API for runtime policy.
- SPA state: `source/container-apps/web/projects/web-spa/features/settings/site-settings-state/`.

**First passkey user → admin (already exists)**

- `IPrincipalRoleStore.TryClaimFirstAdministratorAsync` — atomic empty-deployment → first human passkey create gets Administrator + Member (`source/container-apps/web/features/admin/principals/i-principal-role-store-application.cs`; EF + in-memory implementations).
- Called from CompletePasskeyRegistration (see `BootstrapAdministratorOptions` design comment).
- `Authentication:BootstrapAdministratorPrincipalIds` remains optional break-glass only (`bootstrap-administrator-options-application.cs`).

**Aspire note**

- `aspire-app-host/program.cs` currently defaults parameter `entra-tenant-id` to `"organizations"` in one place — that string fails the GUID trust pin today; relevant when wiring multi-tenant authority vs policy.

## Session

- Implementation: grok task-work (2026-10-20). Tenant acceptance is site settings (`Restricted` list or `Any`), edited on `/Admin/Authentication`. `Authentication:Entra:TenantId` stays the OIDC authority. First passkey registration already claims Administrator through `TryClaimFirstAdministratorAsync` (settings.write + admin.access); no second bootstrap was added.
- Docs: grok task-work (2026-10-21). `wwwroot/auth.md` and the policy, mode, and ticket-processor design notes still described the task 227 tenant pin. They now describe site-settings acceptance (restricted list or any tenant), the multi-tenant app registration, personal Microsoft accounts, and the first-passkey administrator path.

## Results

Administrators set accepted Entra tenants on the site-settings row. Restricted mode accepts only listed tenant ids (an empty list accepts none). Any mode accepts every non-empty `tid`. The offer and bootstrap toggles are unchanged. A GUID `Authentication:Entra:TenantId` is copied into the restricted list once on first seed or on a row that has not been initialized; `organizations` and `common` seed an empty list. Issuer pinning is unchanged: `EntraIssuerValidator` still pins `iss` to the token `tid`. A single-tenant app registration still completes OIDC only for its home tenant; other tenants need `organizations` or `common` plus a multi-tenant supported account type (`dev entra setup` prints that note). Personal Microsoft accounts use tenant `9188040d-6c67-4c5b-b112-36a304b66dad` and only arrive when the authority is `common` or `consumers`. There is no tenant blocklist.

`wwwroot/auth.md` states the same policy, including personal Microsoft accounts and the first-passkey administrator path. The Authentication page was compiled with the SPA. It was not clicked through in a browser in this walk.

### How to validate

Smoke: `dotnet run --file source/container-apps/web/features/identity/entra-sign-in-policy-tests.cs`

Expect: 10 passed. Restricted mode refuses an unlisted `tid` with title `Untrusted tenant`. Any mode allows a different `tid` on link. An empty guid is refused even in Any mode. Challenge with sign-in disabled returns `Sign-in disabled`; challenge with sign-in enabled allows without a tenant. Bootstrap stays refused when the tenant is accepted and bootstrap is off.

Smoke: `dotnet run --file source/container-apps/web/features/identity/site-settings-seeder-tests.cs`

Expect: 8 passed. A GUID authority seeds that one id. `organizations` seeds an empty restricted list. An uninitialized row receives the GUID once and keeps its passkey mode and assistant name. Development reseed rewrites offer and bootstrap and keeps Any mode.

Smoke: `dotnet test` in `tests/libraries/timewarp-identity-tests` and `dotnet run --file source/container-apps/web/features/settings/update-site-settings/update-site-settings-tests.cs`

Expect: identity tests passed (292). Update handler persists Any mode and the collapsed tenant list, and an empty guid fails validation.
