# Review framework

## Budget (by-diff)

- Lines changed: 1690
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Framework

**Date:** 2026-10-01
**Host task:** kanban/to-do/260-every-spa-user-interaction-is-a-timewarpstate-action-audit-convert-enforce/
**Diff scope:** branch task/260-every-spa-user-interaction-is-a-timewarpstate-acti vs master (727dc24f)
**Plan / brief:** task.md — convert every non-exempt web-spa interaction to a TimeWarp.State action
(SignInState, CredentialsState.LinkMicrosoft365 + `[CatalogAction]`), analyzer proposal as Open Question, tw-blazor skill rule.
**Effort:** 3 (by-diff budget); roster axis general only
**Reviewer roster:** general (claude subagent)
**Session IDs:** review oracle (claude, headless ganda task work); general reviewer subagent a3065c0aa4fc23504; fix subagent a6822b8680c258318

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
