# Review framework — task 347

**Date:** 2026-10-13
**Host task:** kanban/to-do/347-add-short-dismissible-chats-are-recorded-notice-to-the-chatassistant-panel/
**Diff scope:** `git show 1bf0af06d` (task branch vs origin/master; only commit owned by this task)
**Plan / brief:** task.md Requirements + Results — configurable, dismissible "Chats are recorded." banner above the Ask input, dismissal in local storage
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle (claude-opus-5-5, ganda task work); general reviewer subagent a08274bbceb2e36f0 (sonnet)

## Budget (by-diff)

- Lines changed: 545
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
