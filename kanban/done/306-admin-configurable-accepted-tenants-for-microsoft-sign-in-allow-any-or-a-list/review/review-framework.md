# Review framework

## Budget (by-diff)

- Lines changed: 140
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Round 2 scope

- **Date:** 2026-10-21
- **Diff scope:** fix delta `9896f0a67..e00a0e7dd` (8 files). M1–M3 re-verified against the post-fix tree.
- **Reviewer roster:** general (review oracle, claude-opus-5-5, headless print mode). Effort 1.
