# Review framework

## Budget (by-diff)

- Lines changed: 465
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 342

**Date:** 2026-10-13
**Host task:** kanban/to-do/342-update-nuget-packages-to-latest-timewarpmediator-1400-beta5-dogfood/
**Diff scope:** branch task/342-… vs origin/master (commits c2081f7ec, 0e1bd5c92; kanban excluded)
**Plan / brief:** NuGet bumps (Mediator 14.0.0-beta.5, State 12.0.0-beta.12, others) plus TimeWarp.State beta.11 migration (Redux DevTools removal, ApiHandler-owned semaphores, ICloneable DTOs, StateTestOptions)
**Effort:** 2 (by-diff budget); roster axes: general
**Reviewer roster:** general (review oracle, Claude Opus 5.5)
**Session IDs:** review oracle session not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
