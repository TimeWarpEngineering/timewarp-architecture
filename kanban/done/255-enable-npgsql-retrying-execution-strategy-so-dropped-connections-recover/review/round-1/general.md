# Round 1 — general
**Date:** 2026-09-29
**Scope reviewed:** same as framework

## Summary

Adds `PostgresRetryPolicy` (4 retries, 2s max delay, Npgsql default transient set) applied by
`PostgresDbModule`, wraps the sole explicit transaction (`EfPrincipalRoleStore.TryClaimFirstAdministratorAsync`)
in `CreateExecutionStrategy().ExecuteAsync` with an in-transaction re-read, and adds four
Postgres-backed recovery tests (control without policy, pooled-connection recovery, mid-transaction
kill, concurrent claims). Verified against the repo: no other `BeginTransaction` / `TransactionScope`
in `source/`; the only non-retry `UseNpgsql` left is the design-time factory (deliberate, documented)
and model-only mapping tests (no connection). Design regions reconciled. Risk is low.

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/web/features/admin/principals/ef-principal-role-store-infrastructure.cs:108
- Description: on a replay, `Db.ChangeTracker.Clear()` detaches every entity in the scoped
  `PostgresDbContext`, not only this claim's `PrincipalRoleAssignment` rows. Another store sharing
  the request scope would lose tracked state.
- Suggestion: detach only `PrincipalRoleAssignment` entries for `principalId`, or document why a
  full clear is safe.
- Status: open
