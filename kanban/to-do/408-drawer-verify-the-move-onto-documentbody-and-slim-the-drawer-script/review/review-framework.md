# Review framework

## Budget (by-diff)

- Lines changed: 233
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Rounds

- Round 1: branch vs `8151be94b` (commit `f5eccc471`), general. See `round-1/merged.md`.
- Round 2: fix delta `7819e05b7..0f55dc6da`, general, reviewer claude-opus-5-5 (headless ganda task work, session id not exposed). See `round-2/merged.md`.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
