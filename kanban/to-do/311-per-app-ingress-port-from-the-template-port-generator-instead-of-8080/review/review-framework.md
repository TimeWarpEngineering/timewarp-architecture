# Review framework

## Budget (by-diff)

- Lines changed: 455
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 311

**Date:** 2026-10-12
**Host task:** kanban/to-do/311-per-app-ingress-port-from-the-template-port-generator-instead-of-8080/
**Diff scope:** commit 13ed481f9 (task branch vs master), product files only
**Plan / brief:** task.md Requirements: committed `Parameters:ingress-port`, template port generator, dev open resolution, tests, docs
**Effort:** 2 (budget by-diff); roster axes: general
**Reviewer roster:** general (review oracle, Claude Opus 5.5, headless ganda task work)
**Session IDs:** headless review oracle (ganda task work)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
