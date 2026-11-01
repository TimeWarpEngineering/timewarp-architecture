# Round 1 — general
**Date:** 2026-11-01
**Scope reviewed:** same as framework (uncommitted worktree diff, 8 files)

## Summary

The diff moves FastEndpoints and FastEndpoints.OpenApi to 9.0.0-beta.6, adds a direct
Microsoft.AspNetCore.OpenApi 11.0.0-rc.3.26522.148 reference on foundation-server, and pins
Microsoft.OpenApi 3.11.0. The 3.x call sites are fixed (`IOpenApiMediaType` dictionaries, a
concrete cast in `OpenApiOneOf`). Tests follow the 3.1 `examples` array, and a new api-server test
checks Scalar at `/scalar/v1`. Re-verified: all four pins are the latest on nuget.org
(FastEndpoints* 9.0.0-beta.6, Microsoft.OpenApi 3.11.0, Microsoft.AspNetCore.OpenApi
11.0.0-rc.3.26522.148, matching the other `Microsoft.AspNetCore.*` pins). foundation-server
`project.assets.json` resolves Microsoft.OpenApi 3.11.0 with no VersionOverride. `dev build`
re-run by the reviewer: 0 warnings, 0 errors. The analyzer generator has no Microsoft.OpenApi
object-model use, so it needed no change. The code change is sound. The task is not finished:
nothing is committed, Results are missing, and most of the gates were never recorded.

## Issues

### Issue 1 — Severity: bug
- File: kanban/in-progress/257-take-microsoftopenapi-3x-when-aspnetcoreopenapi-allows-it/task.md:61
- Description: The implement oracle returned Done before the Done bar was met. The 8 product
  files are uncommitted in the worktree; the task branch has only kanban commits on top of master,
  so open-pr would ship nothing. task.md has no `## Results`, and every checklist item is still
  unchecked. The implementer log ends with "The full `dev test` will run in the background.
  Template-smoke, audit, and outdated checks will follow once the test finishes." So the required
  gates were never run to completion or recorded: full `dev test` including the integration lane,
  `dev template-smoke`, `dev regenerate-generated` (no diff under `goldens/ingress`),
  `ganda repo audit`, and `ganda nuget outdated` (no remaining FastEndpoints / Microsoft.OpenApi
  update). The requirement to record OpenAPI document and Scalar evidence for web-server and
  api-server under Results is also unmet. The log says the targeted api-server and
  web-server-integration-tests runs passed, but nothing durable records it.
- Suggestion: Run every gate to completion in the foreground and commit the product changes on
  the task branch. Then write `## Results`: gate outcomes with pass counts; the test names that
  prove `/openapi/v1.json` and `/scalar/v1` return 200 on api-server
  (`OpenApiDocument_Given_.Scalar_Ui_Should_Point_At_The_Document` plus the document test) and on
  web-server (the existing ScalarPage / document tests); the regenerate-generated diff result; and
  the `ganda nuget outdated` output. Tick the checklist.
- Status: open

### Issue 2 — Severity: suggestion
- File: Directory.Packages.props:139 (also source/foundation/foundation-server/foundation-server.csproj:12)
- Description: The Requirements say to rewrite the pin comment "down to what is true after this
  change". The new comments still say the direct Microsoft.OpenApi reference lifts "the transitive
  Microsoft.OpenApi 2.0.0 package" (GHSA-v5pm-xwqc-g5wc). After this change nothing in the graph
  pulls 2.0.0. foundation-server `project.assets.json` shows Microsoft.AspNetCore.OpenApi 11 RC3
  declaring Microsoft.OpenApi `[3.10.2, 4.0.0)`, and that is the only Microsoft.OpenApi
  dependency. The direct reference now only lifts the 3.10.2 floor to the 3.11.0 pin. The GHSA
  note is history and no longer a reason.
- Suggestion: Reword both comments. For example: the direct Microsoft.OpenApi reference lifts
  Microsoft.AspNetCore.OpenApi's 3.10.2 floor to the 3.11.0 pin. Drop the 2.0.0 / GHSA clause.
- Status: open
