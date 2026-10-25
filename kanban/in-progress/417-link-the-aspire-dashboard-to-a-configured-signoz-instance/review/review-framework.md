# Review framework

## Budget (by-diff)

- Lines changed: 314
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Framework — task 417

**Date:** 2026-10-26
**Host task:** kanban/in-progress/417-link-the-aspire-dashboard-to-a-configured-signoz-instance/
**Diff scope:** branch `task/417-link-the-aspire-dashboard-to-a-configured-signoz-i` vs merge-base with `master` (66f337c65 product commit; kanban commits excluded)
**Plan / brief:** task.md `## Goal (implementer restatement)` and `## Requirements` — run-mode `signoz` external-service link from `Observability:SigNoz:Url`, publish unchanged, no committed URL, Design region + `tw-deploy` note, AppHost model test.
**Effort:** 2 (by-diff budget)
**Reviewer roster:** general
**Session IDs:** review oracle (claude-opus-5-5, headless ganda task work; session id not exposed)

### Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
