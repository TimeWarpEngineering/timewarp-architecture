# Round 1 — general
**Date:** 2026-09-17
**Scope reviewed:** branch `task/235-dev-entra-setup-mint-a-new-client-secret-automatic` vs `origin/master` (product commit `10798f9c`; kanban results `46cfd46d`)

## Summary

`dev entra setup` now re-mints when stored `ClientId` or `TenantId` differs from the target app (GUID-equal via `EntraTenants.TenantIdsEqual`), while `--new-secret` remains same-app rotation and still short-circuits listing without reporting `appRegistrationChanged`. Dry-run executes read-only app list + `user-secrets list`, prints the mint/keep decision, and only prints the masked `az ad app credential reset` invocation on the mint path; failed list still aborts (never fail-open). Status warns on stored-vs-found-by-name `ClientId` mismatch and points at `dev entra setup`. Decision-matrix and message/transcript helper tests cover the brief; `auth.md` documents tenant-switch re-mint vs `--new-secret`. Risk is low: pure decision helper plus thin command wiring, secret never printed.

## Issues
