# Review framework

## Budget (by-diff)

- Lines changed: 126
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

# Review framework — task 390

**Date:** 2026-10-19
**Host task:** kanban/in-progress/390-remove-tye-references-from-package-tags-and-docs/
**Diff scope:** branch `task/390-remove-tye-references-from-package-tags-and-docs` vs `master` (ahead 3, behind 0)
**Plan / brief:** Drop `Tye` (and stale tags) from the `TimeWarp.Architecture` `<PackageTags>`, sweep for Tye outside kanban history, bump `<Version>` and platform pins when source equals the published NuGet version.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), headless print mode, session id not exposed

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
