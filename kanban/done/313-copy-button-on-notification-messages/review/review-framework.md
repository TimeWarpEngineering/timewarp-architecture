# Review framework

## Budget (by-diff)

- Lines changed: 619
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 313

**Date:** 2026-10-12
**Host task:** kanban/to-do/313-copy-button-on-notification-messages/
**Diff scope:** branch `task/313-copy-button-on-notification-messages` vs `master` (commit 89c7e3caf)
**Plan / brief:** task.md Requirements: copy button per message bar → `CopyMessage` action →
`ClipboardJsModule`; icon-swap confirmation; clipboard failure becomes a Warning and never throws.
**Effort:** 2 (budget by-diff); roster axes: general
**Reviewer roster:** general
**Session IDs:** review oracle (Claude Opus 5.5, ganda task work headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
