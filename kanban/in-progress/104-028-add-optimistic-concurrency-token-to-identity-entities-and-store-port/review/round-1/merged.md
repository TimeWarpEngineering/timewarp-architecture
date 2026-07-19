# Round 1 — merged findings
**Date:** 2026-07-19
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 2 | 0 | 0 |

Full descriptions: `general.md` (issue numbers match M numbers). Reviewer verified all
hard-scrutiny areas clean (lock coverage, snapshot completeness/aliasing, conflict orientation,
rehydration ctor safety, dual-mode csproj, port-contract/implementation agreement) and re-ran
identity 88/88 + foundation-domain 37/37.

## Issues

### M1 — Severity: suggestion — Status: open
- File: tests/libraries/timewarp-identity-tests/in-memory-principal-store-concurrency-tests.cs
- Description: Two documented MUST-level port clauses untested: (a) caller-AHEAD version conflict (all tests are caller-behind; a `!=` → `<` regression would pass), (b) Add persists nonzero Version as-is (a store resetting to 0 on Add would pass).
- Suggestion: Two deterministic cases — two-store flow asserting `ExpectedVersion > ActualVersion` on ahead-conflict; add a v1 snapshot to a fresh store and assert Get returns Version 1.
- Source: general
- Disposition notes:

### M2 — Severity: nit — Status: open
- File: source/libraries/timewarp-identity/in-memory-principal-store.cs:37-42, 211-218
- Description: Type/handle-immutability branch in UpdateCredentialAsync and the TryAdd-fails rollback branch in AddCredentialAsync are unreachable via the public surface; Design region presents the check order as caller-observable.
- Suggestion: One Design-region sentence labeling both branches defensive (unreachable via public API today).
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: source/libraries/timewarp-identity/i-principal-store.cs (Design region)
- Description: Sync-throw vs faulted-Task divergence between in-memory and future EF stores undocumented, despite cross-store parity being an explicit design goal.
- Suggestion: One port Design-region sentence: exceptions may surface synchronously or via the returned Task; callers must not assume faulted-task delivery.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- Single source — no collapsing needed.
