# Review framework

## Budget (by-diff)

- Lines changed: 97
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope (round 1)

**Date:** 2026-11-22
**Host task:** kanban/in-progress/494-move-the-ef-core-family-and-npgsql-provider-from-11-0-0-rc-3-to-11-0-0-ga-when-npgsql-entity-framework-core-postgre-sql-11-0-0-ships/
**Diff scope:** branch `task/494-move-the-ef-core-family-and-npgsql-provider-from` vs `master` (product files: `Directory.Packages.props`, `.config/dotnet-tools.json`)
**Plan / brief:** task.md `## Description` and `## Goal (implementer restatement)`
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), 2026-11-22

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks
- Prior rounds are immutable; new work goes in `round-(N+1)/`
