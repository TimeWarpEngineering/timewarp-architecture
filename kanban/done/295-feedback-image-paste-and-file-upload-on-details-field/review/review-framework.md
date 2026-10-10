# Review framework

## Budget (by-diff)

- Lines changed: 5653
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 295

**Date:** 2026-10-10
**Host task:** kanban/done/295-feedback-image-paste-and-file-upload-on-details-field/
**Diff scope:** rounds 1–3: branch task/295-feedback-image-paste-and-file-upload-on-details-fi vs master (866d6e724). Round 4: CI-fix delta 5a37e7725..35b95c73b (action-catalog test, feedback attachment Playwright test). Round 5: upload-415 fix delta bf4ec7c8d..741a56e20 (endpoint accepts metadata, `Accepts_` HTTP test, upload failure message, guide). Round 6: paste-binding fix delta 8b29b0356..7d884d8a3 (`feedback-paste.ts`, `feedback-paste-js-module.cs` Design region, guide).
**Plan / brief:** task.md Requirements; documentation/developer/guides/feedback-attachments.md; task Notes (CI failures on PR #458)
**Effort:** 3 (by-diff), roster axes: general
**Reviewer roster:** general (rounds 1–3: Claude subagent; rounds 4–6: review oracle direct pass)
**Session IDs:** review oracle (Claude Opus 5.5, ganda task work); round 1–3 general reviewer subagent a183e4d2cd8444693; rounds 4–6 review oracle (Claude Opus 5.5, ganda task work, session not reported)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
