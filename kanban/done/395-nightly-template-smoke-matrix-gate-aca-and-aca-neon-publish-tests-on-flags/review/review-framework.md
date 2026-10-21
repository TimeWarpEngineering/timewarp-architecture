# Review framework

## Budget (by-diff)

- Lines changed: 150
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope (round 1)

**Date:** 2026-10-21
**Host task:** kanban/to-do/395-nightly-template-smoke-matrix-gate-aca-and-aca-neon-publish-tests-on-flags/
**Diff scope:** branch `task/395-nightly-template-smoke-matrix-gate-aca-and-aca-neo` vs `master` (commit 0fb733d9e). Product files: `source/container-apps/aspire/projects/aspire-app-host/program.cs`, `tests/container-apps/aspire/aspire-tests/aca-neon-publish-tests.cs`, `tests/container-apps/aspire/aspire-tests/aca-publish-tests.cs`
**Plan / brief:** gate the aca and aca-neon publish facts on the `web` / `postgres` / `yarp` flags that emit the resources they assert on, so the nightly flag-off matrix passes
**Effort:** 1 (general only)
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle session not exposed to the agent

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit. Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
