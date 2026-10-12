# Review framework

## Budget (by-diff)

- Lines changed: 299
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 319

**Date:** 2026-10-12
**Host task:** kanban/to-do/319-closed-box-test-apphosts-use-dynamic-ingress-ports-instead-of-the-dev-run-pins/
**Diff scope:** branch task/319-closed-box-test-apphosts-use-dynamic-ingress-ports vs master (commit 31af31855)
**Plan / brief:** task.md Requirements — shared closed-box AppHost boot helper, dynamic ingress ports, guard, proof
**Effort:** 2 (by-diff budget); roster axes: general
**Reviewer roster:** general (review oracle, Claude Opus 5.5, headless ganda task work)
**Session IDs:** headless review oracle (ganda task work)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
