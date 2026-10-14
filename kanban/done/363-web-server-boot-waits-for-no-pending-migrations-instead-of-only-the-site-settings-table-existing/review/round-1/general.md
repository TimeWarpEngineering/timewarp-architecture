# Round 1 — general
**Date:** 2026-10-14
**Scope reviewed:** same as framework

## Summary

The change does what the spec asks. The probe returns EF's pending list. A `to_regclass` guard on
`__EFMigrationsHistory` keeps a fresh database from logging Error. The seed waits with
Information/Warning logs and throws a named `InvalidOperationException` when the budget runs out.
Registration stays Postgres-only, and the Design regions (AppHost, probe port and adapter, seed,
module, `tw-aggregate-pattern`) and the template excludes are updated. Main risk: timing in the new
regression test.

Checked and found correct: history-table name/schema come from relational options (the
partially-migrated test would fail if `to_regclass` missed the table); `pending[^1]` is the latest
(EF orders oldest-first); the last attempt with pending migrations throws before any EF read. The
42P01 catch still covers the `dev db reset` race. Not dropping the per-test databases matches the
other Postgres suites.

## Issues

### Issue 1 — Severity: suggestion
- File: tests/container-apps/web/web-server-integration-tests/features/settings/site-settings-boot-pending-migrations-tests.cs:66
- Description: The regression test waits a fixed 3s, then asserts that the pending-migration wait log already exists. Host construction (WebApplication build, module registration, DI) happens on the same background task before the seed's first probe. On a slow or loaded CI runner that can take longer than 3s, so the test would fail spuriously even though the behavior is correct.
- Suggestion: Poll for the wait log (or for boot completion) up to a generous deadline. Then hold a short window and assert the boot is still blocked.
- Status: open
