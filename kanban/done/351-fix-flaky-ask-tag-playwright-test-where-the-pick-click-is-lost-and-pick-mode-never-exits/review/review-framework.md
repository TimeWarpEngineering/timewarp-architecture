# Review framework

## Budget (by-diff)

- Lines changed: 348
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 351

**Date:** 2026-10-14
**Host task:** kanban/to-do/351-fix-flaky-ask-tag-playwright-test-where-the-pick-click-is-lost-and-pick-mode-never-exits/
**Diff scope:** branch `task/351-…` vs `master` (commit 80a00365f)
**Plan / brief:** arm Tag pick mode in ask-tag.ts before IsPicking turns on; token-named picks so a
cancel that overtakes its Start never arms; boyscout Pkcs advisory pin.
**Effort:** 2 (by-diff budget)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), session not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
