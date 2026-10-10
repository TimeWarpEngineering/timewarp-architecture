# Review framework

## Budget (by-diff)

- Lines changed: 3615
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 295

**Date:** 2026-10-10
**Host task:** kanban/to-do/295-feedback-image-paste-and-file-upload-on-details-field/
**Diff scope:** branch task/295-feedback-image-paste-and-file-upload-on-details-fi vs master (866d6e724)
**Plan / brief:** task.md Requirements; documentation/developer/guides/feedback-attachments.md
**Effort:** 3 (by-diff), roster axes: general
**Reviewer roster:** general (Claude subagent, thorough pass)
**Session IDs:** review oracle (Claude Opus 5.5, ganda task work); general reviewer subagent a183e4d2cd8444693

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
