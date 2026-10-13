# Review framework

## Budget (by-diff)

- Lines changed: 651
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 348

**Date:** 2026-10-13
**Host task:** kanban/to-do/348-chat-panel-starter-prompts-focus-on-primary-use-cases/
**Diff scope:** branch `task/348-chat-panel-starter-prompts-focus-on-primary-use-ca` commit f5267b12e vs base fb80a7891 (14 files, +594/−57)
**Plan / brief:** replace Ask's hardcoded suggestions with a configurable 4–6 starter-prompt list (icon, title, subtitle, prompt) bound from `XAI:StarterPrompts`, carried by GetAgentChatConfiguration, rendered by `AskStarterPrompts.razor`.
**Effort:** 2 (by-diff budget)
**Reviewer roster:** general
**Session IDs:** review oracle claude-opus-5-5 (session not reported); general reviewer subagent (sonnet)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
