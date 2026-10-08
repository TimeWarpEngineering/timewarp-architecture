# Review framework

## Budget (by-diff)

- Lines changed: 1953
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 272

**Date:** 2026-10-09
**Host task:** kanban/to-do/272-upgrade-timewarp-architecture-to-net-11/
**Diff scope:** branch `task/272-upgrade-timewarp-architecture-to-net-11` vs `master` (commit bf7bb258c; 291 files, +1074/−879)
**Plan / brief:** .NET 10 → .NET 11 RC1 upgrade (SDK pin, 21 test global.json mirrors, TFM, CPM 11.x, CI setup-dotnet, NU1510/IDE0211 fallout, test timeouts, docs). See task.md Results.
**Effort:** 3
**Reviewer roster:** general
**Session IDs:** ganda task-work review oracle (Claude Opus 5.5 orchestrator; general reviewer as Claude subagent)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
