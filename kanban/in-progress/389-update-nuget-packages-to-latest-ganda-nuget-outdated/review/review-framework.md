# Review framework

## Budget (by-diff)

- Lines changed: 234
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Framework — task 389

**Date:** 2026-10-19
**Host task:** kanban/in-progress/389-update-nuget-packages-to-latest-ganda-nuget-outdated/
**Diff scope:** branch `task/389-update-nuget-packages-to-latest-ganda-nuget-outdat` vs `master` (3 commits; product files `.config/dotnet-tools.json`, `Directory.Packages.props`, `source/Directory.Build.props`, `timewarp-templates/Directory.Build.props`)
**Plan / brief:** task.md `## Goal (implementer restatement)` and `## Description` — take every `ganda nuget outdated` update the build and tests accept, record evidence for pins that stay, bump `<Version>` with the platform pins, prove with build / tests / check-version / template-smoke / repo audit.
**Effort:** 2 (budget by diff)
**Reviewer roster:** general
**Session IDs:** review oracle claude-opus-5-5 (session id not exposed in print mode)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
