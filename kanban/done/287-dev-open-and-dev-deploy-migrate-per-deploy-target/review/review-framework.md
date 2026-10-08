# Review framework

## Budget (by-diff)

- Lines changed: 1947
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 287

**Date:** 2026-10-09
**Host task:** kanban/to-do/287-dev-open-and-dev-deploy-migrate-per-deploy-target/
**Diff scope:** branch `task/287-dev-open-and-dev-deploy-migrate-per-deploy-target` vs `master` (commit 95382d06b)
**Plan / brief:** `dev open` and `dev deploy migrate` per deploy target (compose / kubernetes / aca), shared preflight scopes, tests in `tests/tools/dev-cli-tests/deploy-operate-tests.cs`, `skills/tw-deploy` docs, AppHost Design region.
**Effort:** 3 (by-diff budget; roster axes general)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, ganda task work headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
- Reviewers must not touch any cluster, container, or Azure resource
