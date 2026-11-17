# Review framework

## Budget (by-diff)

- Lines changed: 276
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Rounds

- **Round 1** (2026-11-04). Diff: branch HEAD 8edf0ea9a vs merge-base b16e2ce15; the product
  change is 15e93cb37. Effort 3, roster general (two read-only passes, AppHost and Web.Server).
  Artifacts are in round-1/.
- **Round 2** (2026-11-04). Diff: the fix delta `8edf0ea9a..406fad697`, comments and whitespace in
  14 product files. Effort 2 (by-diff, 276 lines), roster general, a single pass by the review
  oracle (claude-opus-5-5, headless ganda task work). Artifacts: round-2/general.md and
  round-2/merged.md.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
