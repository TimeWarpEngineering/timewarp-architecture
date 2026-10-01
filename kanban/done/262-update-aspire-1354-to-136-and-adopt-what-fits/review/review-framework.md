# Review framework

## Budget (by-diff)

- Lines changed: 153
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

# Review framework — task 262

**Date:** 2026-10-01
**Host task:** kanban/to-do/262-update-aspire-1354-to-136-and-adopt-what-fits/
**Diff scope:** commit 6e2b0e7f (branch task/262-… vs master b1c71e8c), product files only
**Plan / brief:** Aspire 13.5.4 → 13.6.0 pins/SDK; Postgres `WithRepl()` in Development; skills note dashboard persistence + REPL; deterministic single-flight test gate
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle (claude-opus-5-5, headless ganda task work)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Prior rounds are immutable; new work goes in `round-(N+1)/`
