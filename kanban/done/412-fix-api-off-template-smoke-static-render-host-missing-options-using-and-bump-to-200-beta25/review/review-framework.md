# Review framework

## Budget (by-diff)

- Lines changed: 73
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-24
**Host task:** kanban/to-do/412-fix-api-off-template-smoke-static-render-host-missing-options-using-and-bump-to-200-beta25/
**Diff scope:** branch `task/412-fix-api-off-template-smoke-static-render-host-miss` vs `master` (commits fab70bda9, ec14ddbea; product files only)
**Plan / brief:** file-level `using Microsoft.Extensions.Options;` in `static-render-host.cs` so the `--api false` variant compiles; bump `<Version>` and task-124 platform pins to `2.0.0-beta.25` in one commit.
**Effort:** 1 (general only)
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle session not reported by host

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
