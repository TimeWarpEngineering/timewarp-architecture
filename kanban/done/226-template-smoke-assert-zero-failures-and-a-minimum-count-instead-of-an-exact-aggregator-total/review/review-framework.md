# Review framework — task 226

**Date:** 2026-09-16
**Host task:** kanban/to-do/226-template-smoke-assert-zero-failures-and-a-minimum-count-instead-of-an-exact-aggregator-total/
**Diff scope:** branch `task/226-template-smoke-assert-zero-failures-and-a-minimum` vs `origin/master` (commits `f47dfca5` product + `bc326aaa` Results). Product: replace template-smoke tier-3 exact MTP `ExpectedSucceeded` pin with a zero-failure + `MinimumSucceeded` floor gate.
**Plan / brief:** See `task.md` Requirements — parse `failed:`/`skipped:`, `failed == 0`, `succeeded >= MinimumSucceeded` (web 180 / api 9 / common 3), `total == succeeded + skipped`, unparsable summary fails, 2× floor warning, helper tests, docs if they still mention exact-count.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle: grok session 01a0a8b6-0fd6-7fd2-956f-674f4e6b5496 (2026-09-16). General reviewer (round 1): grok session 01a0a8b7-6b9d-7950-aeff-f03429c8648d (2026-09-16).

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
