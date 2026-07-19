# Round 1 — merged findings
**Date:** 2026-07-19
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 2 | 0 |

Full descriptions: `general.md` (issue numbers match M numbers). Reviewer verified all
hard-scrutiny areas clean (lock coverage, snapshot completeness/aliasing, conflict orientation,
rehydration ctor safety, dual-mode csproj, port-contract/implementation agreement) and re-ran
identity 88/88 + foundation-domain 37/37.

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: tests/libraries/timewarp-identity-tests/in-memory-principal-store-concurrency-tests.cs
- Description: Two documented MUST-level port clauses untested: (a) caller-AHEAD version conflict (all tests are caller-behind; a `!=` → `<` regression would pass), (b) Add persists nonzero Version as-is (a store resetting to 0 on Add would pass).
- Suggestion: Two deterministic cases — two-store flow asserting `ExpectedVersion > ActualVersion` on ahead-conflict; add a v1 snapshot to a fresh store and assert Get returns Version 1.
- Source: general
- Disposition notes: Added both. `AddPersistsVersionAsIs.Add_of_nonzero_version_snapshot_persists_that_version` moves a Get-returned (version-1) snapshot from one store into an unrelated fresh store and asserts the reload still shows Version 1. `CallerAheadConflict.Ahead_of_store_throws_with_expected_greater_than_actual` uses a two-store flow (store A advances a principal to version 1; that snapshot is presented to store B, which only ever saw version 0) and asserts `ExpectedVersion(1) > ActualVersion(0)` on the thrown `ConcurrencyConflictException`. `timewarp-identity-tests` now 90/90 (was 88).

### M2 — Severity: nit — Status: fixed
- File: source/libraries/timewarp-identity/in-memory-principal-store.cs:37-42, 211-218
- Description: Type/handle-immutability branch in UpdateCredentialAsync and the TryAdd-fails rollback branch in AddCredentialAsync are unreachable via the public surface; Design region presents the check order as caller-observable.
- Suggestion: One Design-region sentence labeling both branches defensive (unreachable via public API today).
- Source: general
- Disposition notes: Added a paragraph to the Design region (after the check-order explanation) stating both branches are defensive, not currently reachable via the public API — no mutators on Type/Handle, CredentialId minted only in Create, and a duplicate id implies a duplicate handle already caught by HandleIndex.TryAdd first — so a future reader doesn't hunt for the missing test or the caller who can trigger them.

### M3 — Severity: nit — Status: fixed
- File: source/libraries/timewarp-identity/i-principal-store.cs (Design region)
- Description: Sync-throw vs faulted-Task divergence between in-memory and future EF stores undocumented, despite cross-store parity being an explicit design goal.
- Suggestion: One port Design-region sentence: exceptions may surface synchronously or via the returned Task; callers must not assume faulted-task delivery.
- Source: general
- Disposition notes: Added a bullet to the port contract stating exception delivery is not specified to be synchronous (in-memory throws before returning a Task; an EF-backed store will fault the Task instead), and that callers must always `await` directly in a try/catch rather than separating task acquisition from awaiting.

## Duplicates / conflicts

- Single source — no collapsing needed.
