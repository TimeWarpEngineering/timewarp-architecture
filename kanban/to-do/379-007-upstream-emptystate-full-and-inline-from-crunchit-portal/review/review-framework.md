# Review framework

## Budget (by-diff)

- Lines changed: 60
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Framework — task 379-007

**Date:** 2026-10-23
**Host task:** kanban/to-do/379-007-upstream-emptystate-full-and-inline-from-crunchit-portal/
**Diff scope:** branch task/379-007-upstream-emptystate-full-and-inline-from-crunchit vs master (commit 05385f757)
**Plan / brief:** task.md Checklist, Decision, and `## Goal (implementer restatement)`: native `EmptyState` with Full and Inline variants, two adopters, a style-guide card, render tests
**Effort:** 2 (by-diff budget); roster axes: general
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle session not reported by host

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
