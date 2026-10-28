# Review framework

## Budget (by-diff)

- Lines changed: 255
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Framework — task 430

**Date:** 2026-10-29
**Host task:** kanban/in-progress/430-agent-link-prompt-points-the-agent-at-the-openapi-document-and-works-on-windows-macos-and-linux/
**Diff scope:** branch `task/430-agent-link-prompt-points-the-agent-at-the-openapi` vs merge-base `19294382f` (commit `223dc9f19`; product paths `agent-link-onboarding-prompt.cs`, `AgentLinksPage.razor`, the spa prompt test, the Playwright onboarding test)
**Plan / brief:** `## Goal (implementer restatement)` and `## Requirements` in task.md
**Effort:** 2 (by-diff budget), roster axis general
**Reviewer roster:** general
**Session IDs:** review oracle claude-opus-5-5 (headless ganda task work, 2026-10-29)

### Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
