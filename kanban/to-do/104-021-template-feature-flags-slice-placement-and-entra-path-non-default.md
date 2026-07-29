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

- [ ] Flags/symbols
- [ ] Slice placement
- [ ] Entra non-default
- [ ] Analyzer clean

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
