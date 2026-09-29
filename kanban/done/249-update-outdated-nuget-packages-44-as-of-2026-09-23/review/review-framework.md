# Review framework — task 249

**Date:** 2026-09-29
**Host task:** kanban/to-do/249-update-outdated-nuget-packages-44-as-of-2026-09-23/
**Diff scope:** branch `task/249-update-outdated-nuget-packages-44-as-of-2026-09-23` vs `master` (commits 5b22d2aa, c77bd016)
**Plan / brief:** CPM pin refresh (53 pins), Microsoft.OpenApi 3.x deferred (task 257), SSH.NET lift removed, TimeWarp.State 12.0.0-beta.5 migration (drop `TestCaller` shim + `CamelCase` helper), dotnet-ef tool 10.0.12
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** ganda task work review oracle (Claude Opus 5.5, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
