# Review framework

## Budget (by-diff)

- Lines changed: 3018
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 469-003

**Date:** 2026-11-04
**Host task:** kanban/in-progress/469-003-refactor-aspire-apphost-and-web-server-programcs-over-500-lines/
**Diff scope:** branch `task/469-003-refactor-aspire-apphost-and-web-server-programcs-o` (HEAD 8edf0ea9a) vs merge-base b16e2ce15 on master; product change is commit 15e93cb37.
**Plan / brief:** split the AppHost `program.cs` (942 lines) and the Web.Server `program.cs` (679 lines) into concern types, keeping behaviour, flag guards, and order (task.md Goal).
**Effort:** 3 (by-diff budget); roster axis general only.
**Reviewer roster:** general, run as two read-only general reviewers (general-apphost: AppHost equivalence and flag guards; general-web-server: Web.Server equivalence and order). The review oracle merged their findings and re-checked them.
**Session IDs:** review oracle (claude-opus-5-5, headless ganda task work); subagents a9681a5e9a4970e87 (apphost), ac84f0f4e80731943 (web-server).

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
