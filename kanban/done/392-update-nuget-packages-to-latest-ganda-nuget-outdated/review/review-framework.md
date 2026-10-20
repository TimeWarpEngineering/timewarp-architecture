# Review framework

## Budget (by-diff)

- Lines changed: 158
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

# Review framework — task 392

**Date:** 2026-10-20
**Host task:** kanban/to-do/392-update-nuget-packages-to-latest-ganda-nuget-outdated/
**Diff scope:** branch `task/392-update-nuget-packages-to-latest-ganda-nuget-outdat` vs `master` (032cb1d3d; product files: `Directory.Packages.props`, `aspire-app-host.csproj`, `foundation-server.csproj`, `.github/workflows/workflow.yml`; kanban: tasks 392 and 257 notes)
**Plan / brief:** move every `ganda nuget outdated` package to latest; Aspire SDK + CLI on 13.6.2; Microsoft.OpenApi held on 2.x with evidence
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), session not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
