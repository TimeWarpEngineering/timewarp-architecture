# Review framework

## Budget (by-diff)

- Lines changed: 537
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Framework — task 466

**Date:** 2026-11-02
**Host task:** kanban/in-progress/466-agents-request-only-scopes-an-admin-approved-through-roles-or-the-principal/
**Diff scope:** branch `task/466-agents-request-only-scopes-an-admin-approved-throu` vs merge-base with `master` (a012755c0, f9a137c39)
**Plan / brief:** task.md Description + `## Goal (implementer restatement)`. Issuance refuses unapproved agent scopes with 403. Role and principal agent-scope stores, admin contracts, and UI.
**Effort:** 3 (budget by diff). Roster axes: general.
**Reviewer roster:** general (review oracle, Claude Opus 5.5)
**Session IDs:** review oracle round 1, Claude Code headless (ganda task work review node)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
