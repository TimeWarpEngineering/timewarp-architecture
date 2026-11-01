# Review framework

## Budget (by-diff)

- Lines changed: 1312
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope — task 459

**Date:** 2026-11-01
**Host task:** kanban/in-progress/459-consume-timewarpcontracts-from-timewarp-contracts-and-remove-the-in-repo-copy/
**Diff scope:** branch `task/459-consume-timewarpcontracts-from-timewarp-contracts` vs merge base with `master` (`3cfa29bae` product commit; kanban-only commits ignored)
**Plan / brief:** task.md Requirements and `## Goal (implementer restatement)` — delete the in-repo `TimeWarp.Contracts` project, consume nuget.org `2.0.0-beta.30`, drop it from smoke/release pack lists, update skills/AGENTS.md, Nuru `4.0.0-beta.11` + audit `--fix` ride-along.
**Effort:** 3 (by-diff budget)
**Reviewer roster:** general
**Session IDs:** reviewer = Claude Opus 5.5 (ganda task work review oracle, headless print mode)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
