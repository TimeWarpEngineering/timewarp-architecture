# Review framework — task 214

**Date:** 2026-09-13
**Host task:** kanban/in-progress/214-replace-directory-structure-severity-override-with-exclude-documentation/
**Diff scope:** branch `task/214-replace-directory-structure-severity-override-with-exclude-documentation` vs `origin/master` (product commit `dfc5b4ca`; kanban 210-005 disposition note `def68bf7`; kitchen `task.md` excluded from product review)
**Plan / brief:** Replace `[ganda.audit] directory-structure.severity = warning` with `directory-structure.exclude = documentation` so the check is error again except the retired `documentation/` tree. No other `[ganda.audit]` keys. No product code. Close 210-005 M5 with one Escalations line. Gate: `ganda repo audit` PASS on `directory-structure` with `Expected directory structure is present (excluded: documentation/)`, no blocking failures; installed ganda at/after PR #157.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok review oracle 01a099e0-9d9f-74a1-8a2c-4e387affb2d9 (2026-09-13)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
