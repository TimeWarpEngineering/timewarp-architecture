# Review framework

## Budget (by-diff)

- Lines changed: 673
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 336

**Date:** 2026-10-13
**Host task:** kanban/to-do/336-admin-feedback-list-shows-the-owner-display-name-instead-of-the-raw-principal-id/
**Diff scope:** branch task/336-admin-feedback-list-shows-the-owner-display-name-i vs master (commit 67dc45c08)
**Plan / brief:** owner display name on admin feedback list + detail, gated on admin.profiles.read, one IProfileDirectory.FindManyAsync batch per page (task.md Requirements/Results)
**Effort:** 2 (by-diff budget; roster axes: general)
**Reviewer roster:** general
**Session IDs:** review oracle claude-opus-5-5 (session not reported); general reviewer subagent ae31e2d3b3e3aafcb (sonnet)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
