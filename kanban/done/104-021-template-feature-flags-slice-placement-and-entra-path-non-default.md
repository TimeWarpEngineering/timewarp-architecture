# Template feature flags slice placement and Entra path non-default

## Parent

104

## Description

Integrate packages into template safely: feature flags/symbols (TWA0008/0010), slice placement (TWA0009), Entra/MSAL not default happy path.

## Requirements

- Default `dev build` green
- Slices isolation clean
- Entra decision implemented (flag off or remove from primary docs/path)

## Checklist

- [x] Flags/symbols
- [x] Slice placement
- [x] Entra non-default
- [x] Analyzer clean

## Notes

Template is the product delivery vehicle.

### From task 131 disposition (F-009 / F-010)

- **F-009 interim done on 131:** `MOCK_AUTHENTICATION` is defined for **all** configurations
  so Debug and Release agree (was Debug-only → Release compiled MSAL against placeholder
  AzureAdB2C). This task still owns long-term Entra/MSAL non-default posture and AzureAd
  appsettings residue.
- **F-010:** B2C/PWA fossil regions removed on 131; MSAL script still not in template output.
  Coordinate with **104-016** on Passwordless CDN + tenant key removal from `App.razor`.
- Bare `Features` substrate for RoleIds/ModuleIds is documented (placement skill); rehome
  authorization constants only if still desired under slice cleanup below.

### Depends on

104-016, packages exist

## Session

- Created: 2026-07-16
- Implementation: 2026-08-04

### Addendum: auth-slice fragmentation (Steve review, 2026-07-23 — parked here)

`web/features/` carries three near-empty auth-era vestiges alongside the living `identity/`
slice; consolidate as part of this task's slice-placement work. Direction leaning (not final):
**identity as the umbrella** — fold or delete the vestiges rather than a principled
authn/authz/identity three-way split (which would fight the 104 program's shape for a
distinction nothing currently needs).

- `auth/` — Passwordless-era `get-sign-in-token` (+ feature-annotations). Task 110 removed its
  route as a security hazard (mints a token for arbitrary UserId); verify NO remaining consumer
  (mock-auth flows?) and DELETE the contract+handler — finish what 110 started.
- `authentication/` — single file: legacy `get-current-user-contracts.cs`, overlapping
  identity's `get-current-session`. Kill one "who am I" contract (likely GetCurrentUser) or
  migrate it into identity; reconcile SPA consumers and the hand-maintained YARP
  /api/GetCurrentUser carve-out (or let 107's route generation absorb that).
- `authorization/` — single file of role-ID constants; not a slice, shared contract data.
  Rehome (identity contracts or platform location) per TWA0009 sharing rules.

Words authn/authz/identity are fine as concepts; the smell is single-file slices whose
boundaries the contents don't justify (three eras preserved side by side by the 114-002
mechanical migration, as scoped).

## Results

### Flags / symbols

- **No new `template.json` symbols** for identity or x402 — both ship with the template
  (not optional product surfaces). Avoids TWA0008/0010 dual compile paths.
- **Entra is runtime config**, not DefineConstants: `Authentication:UseEntra` (default
  false) beside existing `Authentication:UseMock` (145-009). MSAL / Identity.Web packages
  remain referenced so the opt-in path compiles; they are not registered unless the flag is true.

### Slice placement (auth-era vestiges)

| Vestige | Disposition |
|---------|-------------|
| `features/auth/` | Already removed on **104-016** (GetSignInToken deleted) |
| `features/authentication/` | Folded into **identity**: `GetCurrentUser` (client-only grants, not who-am-I) + `MockUserIds` → `Features.Identity` |
| `features/authorization/` | `RoleIds` rehomed to `features/admin/roles/role-ids-contracts.cs`; still bare `Features` substrate (TWA0009) |

- GetCurrentUser remains **[ClientOnlyContract]** (mock SPA grants only) — no server endpoint;
  ingress tests already assert `api/GetCurrentUser` is not a generated prefix.
- SPA `web-spa/features/authentication|authorization` kept (policies, claims factory, state) —
  those are real SPA concerns; naming review remains **132** if desired.
- Placement skill substrate example path updated to `admin/roles/role-ids-contracts.cs`.

### Entra non-default

SPA auth branch (after mock gate fails):

1. `UseEntra=true` → MSAL + `AzureAdB2C` + `AccountClaimsPrincipalFactoryWithRoles`
2. else → **IdentitySessionAuthenticationRegistration** (GetCurrentSession ASP + NoOp token provider)

Server:

1. `UseEntra=true` → `AddMicrosoftIdentityWebAppAuthentication` (default scheme Entra) + named identity-session
2. else → `AddAuthentication(identity-session)` as default — **no AzureAd required to boot**

Also:

- `RedirectToLogin` → `/Login` (passkey CTA), not MSAL `authentication/login`
- PasskeyCeremonyClient notifies `IdentitySessionAuthenticationStateProvider` after successful ceremony
- Base appsettings: `Authentication:UseEntra=false`; AzureAd / AzureAdB2C placeholders documented as Entra-only
- Dev still `UseMock=true` via appsettings.Development.json
- `auth.md` documents Entra as opt-in only

### Verification

- `dev build`: **0 warnings / 0 errors**
- `web-server-integration-tests` happy-path filter (`--filter-method Ok_`): **19/19**
- Mock composition (`Real_Development*`, Production fail-closed): **pass**
- Full suite can still surface **429** on principal-register under 104-015 rate limits when many
  identity tests share a host — not introduced by this task

### Disposition

- **Done.** Default template path does not require Entra; identity is the umbrella for
  vestigial web/features auth folders; identity + x402 need no template feature flags.
- SPA folder naming glossary (authentication vs authorization UI) → optional **132**.
- Package-level removal of MSAL / Microsoft.Identity.Web from template output (smaller WASM)
  deferred — opt-in compile-time symbol would be a follow-on if package weight becomes a product
  concern.

### How to validate

**Automated**
```bash
./bin/dev build
# expect: 0/0 without AzureAd required at boot
# Happy-path integration filter used at close (identity session / mock gates)
```

**Config / UX**
1. Default SPA (non-mock, `UseEntra` false): passkey identity-session — no MSAL login wall
2. Unauthorized → `/Login` (passkey), not RemoteAuthenticatorView
3. Set `Authentication:UseEntra` true only when intentionally testing Entra

**Slice cleanup**
```bash
test ! -d source/container-apps/web/features/auth || echo 'auth vestige still present'
# authentication/authorization product vestiges consolidated per Results (identity umbrella / admin roles)
```

**Not in scope:** dropping MSAL packages entirely; SPA folder rename glossary (132).

