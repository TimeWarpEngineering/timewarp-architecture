# Review framework

## Budget (by-diff)

- Lines changed: 149
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

**Round 2 (2026-10-30):** effort 2 by diff, roster `general`. Same product scope, checked against
the requirements retargeted to beta.9 (task.md, commit `9e44b197e`).

**Round 3 (2026-10-30):** effort 2 by diff, roster `general`, turn cap 120. Scope is the fix-pass
delta `9e44b197e..018ef652d`, which adds `tools/dev-cli/Directory.Build.targets` and
`tools/dev-cli/services/repo-config-service.cs`.

**Round 4 (2026-10-31):** effort 1 by diff, roster `general`, turn cap 80. Scope is the delta
since round 3, `018ef652d..6143384cd`. It contains kanban changes only: the operator decision and
the retarget to beta.10.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit. Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
