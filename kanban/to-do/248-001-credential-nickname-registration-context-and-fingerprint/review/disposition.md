# Disposition — task 248-001

**Date:** 2026-09-23
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

One general-effort round on the 55-file branch diff found no bugs in the new server paths (rename ownership/404 parity, validator/domain/SPA agreement on 1–64 trimmed, EF mapping = migration = snapshot, UA classifier never stores the raw string, fingerprint on the wire but never the handle). Four suggestions and two nits were fixed in the same worktree: a migration data step for legacy agent-key labels, a single-owner rule for the pending nickname editor (no double editor on Settings), draft retention on a failed auto-opened rename, a `CredentialList` render test, a store-contract round-trip for the new columns on both backends, and a documented `label` → `nickname` wire break. Gates after fixes: `dev build` 0/0, `ganda repo audit` clean, identity 228/228, infrastructure (real Postgres, `Migrate()`) 57/57, SPA 50/50.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M7 | nit | Interactive click coverage for the inline editor / two-step revoke and the rename handler's state effects needs bUnit or a Playwright flow; the repo has neither for Blazor components and adding a test dependency is the maintainer's call. State → markup is pinned by the new render test; the endpoint is pinned by server integration tests. | orchestrator (review oracle), flagged for human |

## Escalations

- None blocking. Human decision requested (non-blocking): adopt bUnit (or a Playwright identity flow) for interactive Blazor component tests — see M7.
