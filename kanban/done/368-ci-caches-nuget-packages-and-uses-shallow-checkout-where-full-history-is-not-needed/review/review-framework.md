# Review framework

## Budget (by-diff)

- Lines changed: 149
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Framework — task 368

**Date:** 2026-10-15
**Host task:** kanban/to-do/368-ci-caches-nuget-packages-and-uses-shallow-checkout-where-full-history-is-not-needed/
**Diff scope:** branch `task/368-ci-caches-nuget-packages-and-uses-shallow-checkout` vs `master` (commit 2bd5031d9; product change is `.github/workflows/workflow.yml` only)
**Plan / brief:** shallow checkout in every job (detect-paths fetches base/head by SHA), CPM-keyed `~/.nuget/packages` cache for ci-group and template-smoke, read-only restore in release, 2.0.0-smoke eviction before save; runfile build cache declined.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), session id not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
