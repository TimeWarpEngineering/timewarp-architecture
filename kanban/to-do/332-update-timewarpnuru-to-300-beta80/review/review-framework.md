# Review framework

## Budget (by-diff)

- Lines changed: 66
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

# Review framework — task 332

**Date:** 2026-10-13
**Host task:** kanban/to-do/332-update-timewarpnuru-to-300-beta80/
**Diff scope:** branch `task/332-update-timewarpnuru-to-300-beta80` vs `master` (commit 4d7ac270c: `Directory.Packages.props` + task.md)
**Plan / brief:** Bump `TimeWarp.Nuru` and `TimeWarp.Nuru.DevCli` from 3.0.0-beta.79 to 3.0.0-beta.80 so the outdated-package audit clears and master can be attested.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle — Claude Opus 5.5 (ganda task work, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
