# Review framework

## Budget (by-diff)

- Lines changed: 278
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope — task 331

**Date:** 2026-11-02
**Host task:** kanban/in-progress/331-feedback-an-outside-agent-reviews-feedback-through-the-api/
**Diff scope:** branch `task/331-feedback-an-outside-agent-reviews-feedback-through` vs merge-base with `master` (commit d6517bc46, plus kanban commits)
**Plan / brief:** `## Goal (implementer restatement)` and `## Description` in task.md. The OpenAPI document carries the agent token steps, lifetime, no refresh, reissue rule, ceremony signing details, and the list-feedback processed marker. Document test plus four-scope reviewer loop test. One line in tw-deploy for the in-memory token store.
**Effort:** 2 (by-diff budget), general roster axis
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle under ganda task work (headless print mode)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
