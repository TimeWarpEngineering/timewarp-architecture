# Round 1 — general
**Date:** 2026-09-08
**Scope reviewed:** product + tests vs `origin/master` (commits `e2626588`, `50946bdd`); kitchen Results cited only to re-verify. Same scope as `review/review-framework.md`.

## Summary

Mechanical SPA rehome collapsing `web-spa/features/authentication/` and `web-spa/features/account/` into `web-spa/features/identity/` (namespaces → `Features.Identity`), with dead `AccountState` deleted and no fourth wallet slice. Risk is low: naming/placement only; routes `/authentication/{action}`, `/Login`, `/Logout`, type names, Entra/passkey/mock registration paths, authorization slice, and `services/identity-session-*` / mock auth registration are unchanged. Re-verification found no leftover `Features.Account` / `Features.Authentication` in `source/` or `tests/`, no dangling `AccountState` / `NoSubAccountState` consumers, no server-contract or `GetCurrentUser` moves, and Design regions on the listener/factory match the same-slice `CredentialsState` opt-out drop.

## Issues
