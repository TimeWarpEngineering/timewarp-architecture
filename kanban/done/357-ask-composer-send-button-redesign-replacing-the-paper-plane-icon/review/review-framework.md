# Review framework

## Budget (by-diff)

- Lines changed: 489
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 357

**Date:** 2026-10-14
**Host task:** kanban/to-do/357-ask-composer-send-button-redesign-replacing-the-paper-plane-icon/
**Diff scope:** commit 21ff7e050 (task branch vs master): AgentAsk.razor(.css), new AskSendButton.razor(.css), ask-send-button-tests.cs, ask-surface-playwright-tests.cs
**Plan / brief:** task.md Requirements — round arrow-up send, muted/solid states, Stop while streaming, a11y name, tests
**Effort:** 2 (by-diff budget)
**Reviewer roster:** general
**Session IDs:** review oracle claude-opus-5-5 (session not reported)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
