# Review framework

## Budget (by-diff)

- Lines changed: 162
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-30
**Host task:** kanban/in-progress/457-take-timewarpnuru-400-beta8/
**Diff scope:** branch `task/457-take-timewarpnuru-400-beta8` vs `master` (product paths: `Directory.Packages.props`, `tools/dev-cli/Directory.Build.props`)
**Plan / brief:** task.md `## Description`, `## Requirements`, `## Goal (implementer restatement)`
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), this ganda task work review node

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit. Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
