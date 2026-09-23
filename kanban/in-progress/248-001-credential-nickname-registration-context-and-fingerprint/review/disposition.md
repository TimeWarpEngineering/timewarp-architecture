# Disposition — task 248-001

**Date:** 2026-09-23
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 (general, effort 1) reviewed the 55-file branch diff and found no bugs in the new server paths (rename ownership/404 parity, validator/domain/SPA agreement on 1–64 trimmed, EF mapping = migration = snapshot, UA classifier never stores the raw string, fingerprint on the wire but never the handle). Four suggestions and two nits were fixed in the same worktree: a migration data step for legacy agent-key labels, a single-owner rule for the pending nickname editor, draft retention on a failed auto-opened rename, a `CredentialList` render test, a store-contract round-trip for the new columns on both backends, and a documented `label` → `nickname` wire break. Round 2 (general, effort 1) re-reviewed the merge of `origin/master` (task 246: `Revoke*` rename + last-credential guard) into this branch — merge commit `3aeed9b1` — and confirmed both 246 and 248-001 landed intact, with `RevokeDisabled` gating both steps of the revoke confirmation and Confirm raising `OnRevoke`; M1–M6 fixes survived the merge; no new findings. Gates re-run by the review oracle after round 2: `dev build` 0/0; `web-spa-integration-tests` 55/55 (includes 246's revoke-guard + deep-link facts and this task's `credential-list-render-tests.cs`). Fix-loop gates (`dev test` all suites, `dev template-smoke`, `ganda repo audit`) are recorded in task Results; CI on the merge commit `3aeed9b1` was green, and the run on head `d7088bf6` (docs-only commit) was still in progress at disposition time.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M7 | nit | Interactive click coverage for the inline editor / two-step revoke and the rename handler's state effects needs bUnit or a Playwright flow; the repo has neither for Blazor components and adding a test dependency is the maintainer's call. State → markup is pinned by the render test; the endpoint is pinned by server integration tests. Re-confirmed unchanged in round 2. | orchestrator (review oracle), flagged for human |

## Escalations

- None blocking. Human decision requested (non-blocking): adopt bUnit (or a Playwright identity flow) for interactive Blazor component tests — see M7.
