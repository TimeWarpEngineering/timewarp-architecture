# Review framework

## Budget (by-diff)

- Lines changed: 562
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Framework — task 440

**Date:** 2026-10-29
**Host task:** kanban/in-progress/440-fix-delete-roles-roleid-required-requestbody/
**Diff scope:** branch `task/440-fix-delete-roles-roleid-required-requestbody` vs `6c53cf167` (merge of PR #599), paths `source/` and `tests/`. Product commit: `754f58640`.
**Plan / brief:** `task.md` `## Goal (implementer restatement)`. Drop the required JSON body from `DELETE /api/Roles/{roleId}` in the OpenAPI document; keep `roleId` as the path parameter and publish `userId` as a required query parameter. Implemented as an OpenAPI operation transformer in foundation-server, registered from `CommonServerModule.AddOpenApi`.
**Effort:** 2 (host budget); roster general only
**Reviewer roster:** general (claude sonnet subagent `aa4b403fdc754d4bc`, read-only)
**Orchestrator:** review oracle (claude-fable-5-1) under `ganda task work 440`
**Session IDs:** see `task.md` `## Session`

## Gates re-run by the orchestrator

- `dev build` (./bin/dev): Build succeeded, 0 warnings, 0 errors.
- `dotnet run -c Release -- --filter-class DeleteRoles_Given_` in web-server-integration-tests: 3/3 passed.
- `dev check-version`: source `2.0.0-beta.29`, NuGet latest `2.0.0-beta.28`; source is already ahead, no bump needed on this diff.
- `ganda repo audit`: passes; one `kanban-landed` warning about other tasks' claims, outside this task.
- Live spec `https://arch.timewarp.work/openapi/v1.json` (2026-10-29): DELETE Roles still carries the body there; GET Roles publishes `userId` (query, camelCase); the other DELETEs are bodyless. The transformer changes only DELETE Roles in this repo.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
