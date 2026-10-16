# Review framework

## Budget (by-diff)

- Lines changed: 430
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 375

**Date:** 2026-10-16
**Host task:** kanban/to-do/375-run-only-the-default-template-smoke-variant-per-pr-and-the-flag-off-variants-nightly/
**Diff scope:** branch `task/375-run-only-the-default-template-smoke-variant-per-pr` vs `origin/feature/template-smoke-nightly` (work-set base; commits 3f1237546, ee9f57131)
**Plan / brief:** task.md Requirements 1–6 and `## Goal (implementer restatement)`
**Effort:** 2 (by-diff budget) — roster axes: general
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), session id not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
