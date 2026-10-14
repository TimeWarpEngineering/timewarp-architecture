# Review framework

## Budget (by-diff)

- Lines changed: 1040
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 301

**Date:** 2026-10-14
**Host task:** kanban/to-do/301-ctrl-k-show-the-ask-ai-action-in-the-command-list/
**Diff scope:** branch task/301-ctrl-k-show-the-ask-ai-action-in-the-command-list vs master (round 1: 43f043ebf; round 2: fix delta 43f043ebf..5e1dd6dba)
**Plan / brief:** task.md Requirements + 2026-10-14 Amendment (derive Ctrl-K commands from every ActionSet, explicit opt-out, runtime display override, footer Ask removed)
**Effort:** 3 (by-diff budget)
**Reviewer roster:** general
**Session IDs:** review oracle (claude-opus-5-5, session not reported); round-1 general reviewer subagent a3652ee317e4615aa; fix-loop implementer subagent abca9dd35b58fe673

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
