# Review framework

## Budget (by-diff)

- Lines changed: 689
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Round 2

**Date:** 2026-11-01
**Host task:** kanban/in-progress/257-take-microsoftopenapi-3x-when-aspnetcoreopenapi-allows-it/
**Diff scope:** delta since round 1 (round-1 base 25c77b924): product commit e06c648f7 and kanban commit 52efa0805 on `task/257-take-microsoftopenapi-3x-when-aspnetcoreopenapi-al`
**Plan / brief:** round-1 `merged.md` M1 and M2, task.md Requirements and `## Goal (implementer restatement)`
**Effort:** 2 (by-diff budget above); roster general
**Reviewer roster:** general
**Session IDs:** reviewer-claude (claude-opus-5-5), headless ganda task-work review-2 oracle, session not reported

## Round 3

**Date:** 2026-11-01
**Host task:** kanban/in-progress/257-take-microsoftopenapi-3x-when-aspnetcoreopenapi-allows-it/
**Diff scope:** delta since round 2 (round-2 base 52efa0805): product commit 925b61eb3 and kanban commit de1c67301
**Plan / brief:** round-2 `merged.md` M3 and M4, task.md `## Goal (implementer restatement)`
**Effort:** 1 (by-diff budget above); roster general
**Reviewer roster:** general
**Session IDs:** reviewer-claude (claude-opus-5-5), headless ganda task-work review-3 oracle, session not reported

## Round 4

**Date:** 2026-11-01
**Host task:** kanban/in-progress/257-take-microsoftopenapi-3x-when-aspnetcoreopenapi-allows-it/
**Diff scope:** delta since round 3 (land 8a120e309): product commit 15fef86b0 (web-server.csproj, Oakton `PrivateAssets=all`) and kanban commits 6ebb7e6ee, 3e9d65272, 8c54c230f
**Plan / brief:** PR #621 publish red at `web-migrations-generate-migration-bundle`; task.md `## Goal (implementer restatement)` CI-fix paragraph
**Effort:** 2 (by-diff budget above); roster general
**Reviewer roster:** general
**Session IDs:** reviewer-claude (claude-opus-5-5), headless ganda task-work review oracle, session not reported
