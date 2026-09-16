# Review framework — task 228

**Date:** 2026-09-16
**Host task:** kanban/in-progress/228-template-smoke-drop-per-family-test-floors-require-at-least-one-test-and-zero-failures/
**Diff scope:** branch `task/228-template-smoke-drop-per-family-test-floors-require` vs `origin/master` (commits `e38408e9` product + `2607d5f5` Results). Product: drop per-family `MinimumSucceeded` floors and the 2× stale-floor warning; gate generated Jaribu aggregators on exit 0 (harness precondition), `failed == 0`, `total > 0`, and `total == succeeded + skipped`.
**Plan / brief:** See `task.md` Requirements — `Decide` is failed==0 / total>0 / total==succeeded+skipped; unparsable still fails; remove `MinimumSucceeded` from `JaribuFamilyAggregators`; keep totals Info line; tests for zero total / any failed / mismatch / unparsable / `1/1` / large totals; no contributor-facing "raise the floor" text.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle: grok session 01a0aa1a-4b39-7432-9276-a8fa6059416a (2026-09-16). General reviewer (round 1): grok session 01a0aa1c-714b-75d1-8261-84aad6f3c2aa (2026-09-16).

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
