# Review framework — task 292

**Date:** 2026-10-10
**Host task:** kanban/in-progress/292-ask-ai-follow-up-to-271-cloudflare-style-side-panel-edit-modes-and-webmcp-parity/
**Diff scope:** branch `task/292-ask-ai-follow-up-to-271-cloudflare-style-side-pane` vs `master` (commit ebfaecdfb; 65 files, 2116+/127-)
**Plan / brief:** `design.md` + task.md Requirements — docked Ask AI side panel, edit modes (Ask before editing default / per-conversation Automatically edit), per-conversation SPA credential, `@` resource references, WebMCP parity test.
**Budget.ByDiff:** 2243 lines changed → effort 3; roster axes: general; turn cap 200
**Effort:** 3
**Reviewer roster:** general, tests, security (three parallel read-only reviewers)
**Session IDs:** ganda task-work review oracle (Claude Opus 5.5), 2026-10-10

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
