# Review framework

## Budget (by-diff)

- Lines changed: 84
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

# Review framework — task 359

**Date:** 2026-10-14
**Host task:** kanban/to-do/359-ci-runs-on-feature-branch-prs-and-pushes-and-editing-a-pr-description-no-longer-re-runs-ci/
**Diff scope:** branch `task/359-…` vs `ae03c2774` (master merge base): `.github/workflows/workflow.yml` + task kanban
**Plan / brief:** add `feature/**` to push/pull_request branches, drop `pull_request` `edited`, verify publish stays release-only
**Effort:** 1 (general only)
**Reviewer roster:** general (review oracle, Claude Opus 5.5)
**Session IDs:** not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
