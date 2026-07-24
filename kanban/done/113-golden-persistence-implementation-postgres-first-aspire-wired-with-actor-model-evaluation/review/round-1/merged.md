# Round 1 — merged findings
**Date:** 2026-07-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 1 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-infrastructure-tests/profile-postgres-persistence-tests.cs
- Description: Live Postgres tests soft-skipped without Docker; under CI that would green without exercising concurrency.
- Suggestion: Fail-closed when `CI` / `GITHUB_ACTIONS` is set; narrow Testcontainers catch filter.
- Source: general
- Disposition notes: Fixed 2026-07-23 — `IsCiEnvironment()` throws when unavailable under CI; dropped `InvalidOperationException` / `NotSupportedException` from soft-skip catch filter. Local soft-skip retained.

### M2 — Severity: suggestion — Status: wontfix
- File: profile-entity-type-configuration-infrastructure.cs; golden-db-context.cs
- Description: Host half of two-party Version contract (`.IsConcurrencyToken()`) is memory-only; no analyzer/auto-apply for mapped IAggregateRoot.
- Suggestion: Analyzer or auto-IsConcurrencyToken in GoldenDbContext.OnModelCreating.
- Source: general
- Disposition notes: **wontfix for 113 closeout** (orchestrator 2026-07-23). Two-party is intentional design (ADR-0009 negative consequences; Design regions). Auto-apply changes the contract surface; analyzer is a valid follow-on (file separately if desired) but not required to ship the golden path. Profile proves both halves; how-to documents the host obligation.

### M3 — Severity: nit — Status: fixed
- File: tests/foundation/foundation-infrastructure-tests/golden-db-context-tests.cs
- Description: Deleted-child / add-child after save Version paths untested.
- Suggestion: Harness cases for add owned child and delete owned child.
- Source: general
- Disposition notes: Fixed 2026-07-23 — two new tests; foundation-infrastructure-tests 9 passed.

## Duplicates / conflicts

- None
