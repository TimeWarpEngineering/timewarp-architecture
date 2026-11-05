# Review framework

## Budget (by-diff)

- Lines changed: 97
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

# Review framework — task 477

**Date:** 2026-11-05
**Host task:** kanban/in-progress/477-remove-unused-riokmapperly-reference-from-web-application/
**Diff scope:** branch `task/477-remove-unused-riokmapperly-reference-from-web-appl` vs `master` (product files: `Directory.Packages.props`, `source/container-apps/web/projects/web-application/web-application.csproj`; the rest is kanban)
**Plan / brief:** remove the unused `Riok.Mapperly` package reference and its central version pin (task 476 follow-up)
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), headless `ganda task work`

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
