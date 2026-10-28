# Review framework

## Budget (by-diff)

- Lines changed: 523
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Review framework — task 428

**Date:** 2026-10-28
**Host task:** kanban/in-progress/428-improve-agent-links-page-onboarding/
**Diff scope:** branch `task/428-improve-agent-links-page-onboarding` vs merge-base `5510b6096` (round 1 at HEAD `f029de790`)
**Plan / brief:** `task.md` `## Goal (implementer restatement)` and Requirements 1–4: onboarding copy, account id with copy, openssl + curl agent prompt, AOT client skip decision, Playwright coverage
**Effort:** 2 (by-diff budget); roster axes: general
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), ganda task work headless

### Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
