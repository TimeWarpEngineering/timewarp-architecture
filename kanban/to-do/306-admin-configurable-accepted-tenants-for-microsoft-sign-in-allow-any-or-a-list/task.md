# Admin-configurable accepted tenants for Microsoft sign-in (allow any or a list)

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

- [ ] SiteSettings (+ migration / UpdateSiteSettings contracts / AuthenticationPage) carry allow-any or accepted-tenant list
- [ ] `SiteSettingsEntraSignInPolicy` trusts SiteSettings list/any instead of only `Authentication:Entra:TenantId` GUID equality
- [ ] OIDC authority / issuer path supports multi-tenant tickets when policy allows
- [ ] Docs: security implications + Entra app registration supported account types
- [ ] Confirm first-passkey → Administrator (`TryClaimFirstAdministratorAsync`) is sufficient for admin to configure tenants
- [ ] Tests: restricted refuse, any allow, availability toggle unchanged

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

## Results

### How to validate

