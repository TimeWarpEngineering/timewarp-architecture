# Review framework

## Budget (by-diff)

- Lines changed: 867
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 349

**Date:** 2026-10-13
**Host task:** kanban/to-do/349-redesign-chatassistant-panel-to-look-polished-cloudflare-level/
**Diff scope:** commit 4228d4886 (the task branch's only commit beyond the 348 merge)
**Plan / brief:** task.md Requirements and Results (Ask panel visual redesign)
**Effort:** 3 (by-diff budget); roster axes: general
**Reviewer roster:** general (Claude subagent, read-only); orchestrator re-review in round 2
**Session IDs:** review oracle claude-opus-5-5 (headless, id not reported); general reviewer subagent a5decffb365815db4

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
