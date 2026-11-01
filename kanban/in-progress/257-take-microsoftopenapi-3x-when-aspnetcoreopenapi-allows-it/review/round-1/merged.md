# Round 1 — merged findings
**Date:** 2026-11-01
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: kanban/in-progress/257-take-microsoftopenapi-3x-when-aspnetcoreopenapi-allows-it/task.md:61
- Description: Implementation is uncommitted, `## Results` is missing, and the checklist is
  unchecked. The required gates (full `dev test` integration lane, `dev template-smoke`,
  `dev regenerate-generated`, `ganda repo audit`, `ganda nuget outdated`) and the OpenAPI/Scalar
  evidence for web-server and api-server were not recorded. The implementer returned Done while the
  full test run was still backgrounded.
- Suggestion: Finish every gate in the foreground and commit the product diff on the task branch.
  Write Results with gate outcomes and the OpenAPI/Scalar evidence, and tick the checklist.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: Directory.Packages.props:139; source/foundation/foundation-server/foundation-server.csproj:12
- Description: The pin comments still give lifting a transitive Microsoft.OpenApi 2.0.0
  (GHSA-v5pm-xwqc-g5wc) as the reason for the direct reference. After the move, the only dependent
  is Microsoft.AspNetCore.OpenApi 11 RC3 with `[3.10.2, 4.0.0)`, so the direct reference only lifts
  3.10.2 to 3.11.0. This fails the "comment says what is true after this change" requirement.
- Suggestion: Reword both comments to the 3.10.2 → 3.11.0 lift and drop the 2.0.0 / GHSA clause.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None.
