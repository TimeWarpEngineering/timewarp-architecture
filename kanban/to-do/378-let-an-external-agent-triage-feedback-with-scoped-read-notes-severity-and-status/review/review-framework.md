# Review framework

## Budget (by-diff)

- Lines changed: 1768
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Framework — task 378

**Date:** 2026-10-17
**Host task:** kanban/to-do/378-let-an-external-agent-triage-feedback-with-scoped-read-notes-severity-and-status/
**Diff scope:** branch `task/378-let-an-external-agent-triage-feedback-with-scoped` vs `master` (commit d1b316dc7, 45 files)
**Plan / brief:** task.md `## Goal (implementer restatement)` and `## Requirements`: four non-admin `feedback.*` permissions on agent scopes and Administrator, filer-invisible monotonic severity, `SetFeedbackSeverity` endpoint, `ListFeedback` severity filter, admin UI.
**Effort:** 3 (by-diff budget), single `general` reviewer axis
**Reviewer roster:** general
**Session IDs:** review oracle claude (claude-opus-5-5), session not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
