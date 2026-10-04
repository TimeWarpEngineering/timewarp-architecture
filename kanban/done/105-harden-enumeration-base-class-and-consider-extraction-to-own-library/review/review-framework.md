# Review framework

## Budget (by-diff)

- Lines changed: 1272
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 105

**Date:** 2026-10-04
**Host task:** kanban/to-do/105-harden-enumeration-base-class-and-consider-extraction-to-own-library/
**Diff scope:** branch `task/105-harden-enumeration-base-class-and-consider-extract` vs `origin/master` (commits 3fc9a638, 0953c8cc)
**Plan / brief:** harden `Enumeration` (cache, IEquatable/==, IComparable<T>, FromString ambiguity, STJ converter, TWA0028 member-shape analyzer); extraction left as an open question for Steve
**Effort:** 3 (by-diff budget); roster axes: general
**Reviewer roster:** general
**Session IDs:** review oracle (Claude Opus 5.5, ganda task work headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
