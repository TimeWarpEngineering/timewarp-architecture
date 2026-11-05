# Review framework

## Budget (by-diff)

- Lines changed: 292
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-11-05
**Host task:** kanban/in-progress/464-research-generate-front-end-ux-pieces-from-contracts-counterpart-to-461/
**Diff scope:** branch `task/464-research-generate-front-end-ux-pieces-from-contrac` vs `master`
(merge base). Kitchen only: `proposal.md` (new), `task.md` (moved from to-do and edited), and
`contracts-frontend-example.md` (superseded banner). No product paths changed.
**Plan / brief:** research proposal only. Options A–D as code documents for SPA pieces from the
Roles contracts, with an analyzer-first answer per piece, ending at Steve's question.
**Effort:** 2
**Reviewer roster:** general (review oracle, inline; falsifiable claims re-run against the tree)
**Session IDs:** review oracle claude-opus-5-5 (ganda task work, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
