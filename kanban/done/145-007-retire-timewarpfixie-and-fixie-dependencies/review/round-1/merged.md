# Round 1 — merged findings
**Date:** 2026-08-02
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 3 | 0 |

## Issues

### M1 — Severity: nit — Status: fixed
- File: in-memory-principal-store-contract-tests.cs
- Description: Stale "still-Fixie EF fixture" comment
- Source: general
- Disposition notes: Reworded to dual-fixture static-wrapper rationale

### M2 — Severity: nit — Status: fixed
- File: ef-principal-store-contract-tests.cs
- Description: Stale "Identity-tests still Fixie" comment
- Source: general
- Disposition notes: Updated for completed identity migration

### M3 — Severity: nit — Status: fixed
- File: kanban/overview.md
- Description: "Integration Tests (Fixie)" label
- Source: general
- Disposition notes: Renamed to Jaribu

### M4 — Severity: suggestion — Status: fixed
- File: task gate evidence
- Description: Claim 5 not re-verified in review pass
- Source: general
- Disposition notes: Orchestrator recorded gates — `dev build` 0/0; `ganda repo audit` PASS (after cleaning smoke artifacts); `dotnet run tools/dev-cli/dev.cs -- template-smoke` SUCCEEDED (stale AOT bin/dev had expected 2; source expects 5). Host-free suites spot-checked green (domain 37, identity 169, web-infra 39, analyzers 102, testing-tests 3). Full `dev test` sequential not re-run end-to-end this session (integration suites long); MTP-only path verified via project-dir runs + smoke.

## Duplicates / conflicts

- None
