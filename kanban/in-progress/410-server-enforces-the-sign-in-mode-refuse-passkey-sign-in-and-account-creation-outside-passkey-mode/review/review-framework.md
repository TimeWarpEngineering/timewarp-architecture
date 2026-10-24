# Review framework

## Budget (by-diff)

- Lines changed: 727
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Round 2

- **Date:** 2026-10-25
- **Diff scope:** fix delta `1745d1b84..689d4f2af` (round-1 base.sha to branch HEAD)
- **Brief:** round-1 `merged.md` M1–M6, and the round-2 bullets of task.md `## Goal (implementer restatement)`
- **Reviewer roster:** general (review oracle, claude-opus-5-5); session not reported by host

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
