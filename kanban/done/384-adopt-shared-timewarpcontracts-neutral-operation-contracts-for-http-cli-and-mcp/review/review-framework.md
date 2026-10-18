# Review framework

## Budget (by-diff)

- Lines changed: 430
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Round 2

**Date:** 2026-10-18
**Diff scope:** fix delta `328f1acf9..04788f1d4` (round-1 base to HEAD; commits 6e2164773, 04788f1d4)
**Effort:** 2 (by-diff budget above); roster general
**Session IDs:** review-2 oracle claude-opus-5-5 (headless, session id not exposed)
