# Review framework

## Budget (by-diff)

- Lines changed: 668
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 096

**Date:** 2026-10-03
**Host task:** kanban/to-do/096-page-generator-multi-route-support-for-multi-tab-pages/
**Diff scope:** branch `task/096-page-generator-multi-route-support-for-multi-tab-p` vs `master` (92953267, f04cbc13)
**Plan / brief:** multi-route `[Page("/primary", "/alias", …)]`; primary owns GetPageUrl/IStaticRoute/PageRegistry/TWE009; new TWE010 (duplicate route) / TWE011 (conflicting declaration)
**Effort:** 2
**Reviewer roster:** general, tests (sonnet subagents); orchestrator: review oracle (claude-opus-5-5)
**Session IDs:** general a290d6e3e0afcb35e · tests a56df310aff93c596 · fix pass a6f0374eef2db9dc5

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
