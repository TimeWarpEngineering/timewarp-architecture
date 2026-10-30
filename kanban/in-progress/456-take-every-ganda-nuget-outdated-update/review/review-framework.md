# Review framework

## Budget (by-diff)

- Lines changed: 288
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope — task 456

**Date:** 2026-10-30
**Host task:** kanban/in-progress/456-take-every-ganda-nuget-outdated-update/
**Diff scope:** branch `task/456-take-every-ganda-nuget-outdated-update` vs merge-base with `master`
(c8f20ef3f, c13b430b5, 73cc78bd9). Product files: `Directory.Packages.props`,
`.config/dotnet-tools.json`, `source/Directory.Build.props`, `timewarp-templates/Directory.Build.props`.
Also the task 257 Notes line. The two master commits ahead are kanban-only (`land kanban 456`).
**Plan / brief:** take every `ganda nuget outdated` update (21 of 22). Hold Microsoft.OpenApi on 2.x
because of NU1608. Bump `<Version>` and the platform pins to 2.0.0-beta.30.
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle claude-opus-5-5 (ganda task work headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
