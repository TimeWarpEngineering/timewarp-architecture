# Review framework

## Budget (by-diff)

- Lines changed: 106
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

# Review framework — task 298

**Date:** 2026-10-10
**Host task:** kanban/to-do/298-upgrade-to-timewarpstate-1200-beta10-add-timewarpstateblazor/
**Diff scope:** branch task/298-upgrade-to-timewarpstate-1200-beta10-add-timewarps vs master (53d5c0a09)
**Plan / brief:** Bump TimeWarp.State / Plus to 12.0.0-beta.10, add TimeWarp.State.Blazor, handle beta.10 breaking changes
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Claude review oracle (ganda task work, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
