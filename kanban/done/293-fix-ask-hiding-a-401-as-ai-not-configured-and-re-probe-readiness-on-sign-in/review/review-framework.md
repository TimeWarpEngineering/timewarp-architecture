# Review framework

## Budget (by-diff)

- Lines changed: 1115
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 293

**Date:** 2026-10-10
**Host task:** kanban/in-progress/293-fix-ask-hiding-a-401-as-ai-not-configured-and-re-probe-readiness-on-sign-in/
**Diff scope:** branch `task/293-fix-ask-hiding-a-401-as-ai-not-configured-and-re-p` vs `master` (merge-base)
**Plan / brief:** task.md Requirements + Results — four-state Ask readiness (Configured / NotConfigured / Unauthenticated / Error) via pure `ChatReadinessProbe`; probe re-run from `AuthenticationStateListener` on every auth change; `RelayChatClient` reports real problem text.
**Effort:** 3 (by-diff budget) — roster axes: general
**Reviewer roster:** general
**Session IDs:** ganda task-work review oracle (Claude Opus 5.5), general reviewer subagent

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
