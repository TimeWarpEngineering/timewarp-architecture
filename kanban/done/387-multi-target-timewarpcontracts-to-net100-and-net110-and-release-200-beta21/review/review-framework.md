# Review framework

## Budget (by-diff)

- Lines changed: 118
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope — task 387

**Date:** 2026-10-18
**Host task:** kanban/to-do/387-multi-target-timewarpcontracts-to-net100-and-net110-and-release-200-beta21/
**Diff scope:** branch `task/387-multi-target-timewarpcontracts-to-net100-and-net11` vs `master` (commits 45e8bcb85, e2c532e78)
**Plan / brief:** multi-target `TimeWarp.Contracts` to `net10.0;net11.0`, bump repo version and platform pins to 2.0.0-beta.21, prove a net10.0 consumer against the packed nupkg
**Effort:** 1 (general only)
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** not reported by host

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
