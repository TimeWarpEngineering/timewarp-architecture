# Review framework — task 132-001

**Date:** 2026-09-08
**Host task:** kanban/to-do/132-001-fold-spa-authentication-and-account-login-ux-into-identity/
**Diff scope:** branch `task/132-001-fold-spa-authentication-and-account-login-ux-into` vs `origin/master` (product commits `e2626588`, `50946bdd`; kitchen Results `a18f5110`). Product + tests; exclude kitchen `task.md` from product review (reviewer may cite Results claims to re-verify).
**Plan / brief:** Mechanical SPA rehome: collapse `web-spa/features/authentication/` and `web-spa/features/account/` into `web-spa/features/identity/` so the WASM client matches the server identity umbrella (104-021 / 132). Naming + namespace only. Do not change Entra vs passkey vs mock behavior. Routes `/authentication/{action}`, `/Login`, `/Logout` stay. Delete dead `AccountState` if unused.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:**
- Round 1: Grok review oracle session 01a07fa2-542c-7343-9250-95311498d308 (2026-09-08)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Product files in scope

- `source/container-apps/web/projects/web-spa/_Imports.razor`
- `source/container-apps/web/projects/web-spa/global-usings.cs`
- `source/container-apps/web/projects/web-spa/features/identity/` (moved adapters, pages, login-page)
- Deleted: `features/account/account-state/*`, `features/authentication/*`, `pages/Authentication.razor`, `pages/RedirectToLogin.razor`
- `source/container-apps/web/projects/web-spa/features/application/pages/HomePage.razor.cs` (CrossSliceReference reason text)
- `tests/container-apps/web/web-spa-integration-tests/features/identity/login-return-url-tests.cs`

## Surrounding call sites to re-verify

- No leftover `Features.Account` / `Features.Authentication` in `source/` or `tests/`
- Empty `features/authentication/` and `features/account/` gone; `features/authorization/` kept
- `web-spa/services/identity-session-*` and mock auth registration still in artifact `services/`
- Routes still `/authentication/{action}`, `/Login`, `/Logout`
- TWA0009: listener dropped same-slice `CredentialsState` opt-out; Profile + Authorization remain; factory Authorization opt-out retargeted
- `AccountState` / `NoSubAccountState` have no remaining consumers
- Type names unchanged (`LoginPage`, `AuthenticationStateListener`, `AccountClaimsPrincipalFactoryWithRoles`)
- No server contract moves, no `GetCurrentUser` rename, no `features/auth/` resurrection
