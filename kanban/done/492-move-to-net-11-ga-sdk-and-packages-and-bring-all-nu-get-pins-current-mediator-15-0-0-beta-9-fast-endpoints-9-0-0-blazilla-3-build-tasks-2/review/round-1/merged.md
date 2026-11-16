# Round 1 — merged findings
**Date:** 2026-11-16
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: Directory.Packages.props:158
- Description: The FastEndpoints comment this task required to be rewritten still says `Microsoft.AspNetCore.OpenApi` 11.0.0 allows `Microsoft.OpenApi` `[3.10.2, 4.0.0)` and that the foundation-server reference lifts a 3.10.2 floor to 3.11.0. The 11.0.0 nuspec depends on `Microsoft.OpenApi` `[3.11.0, 4.0.0)`. The same stale story is in `source/foundation/foundation-server/foundation-server.csproj:10` (lift an rc.2 floor to the RC3 train) and `:12` (lift a 3.10.2 floor). `FastEndpoints.OpenApi` 9.0.0 net11.0 depends on `Microsoft.AspNetCore.OpenApi` 11.0.0, not 11.0.0-rc.2. Checked by fetching `https://api.nuget.org/v3-flatcontainer/microsoft.aspnetcore.openapi/11.0.0/microsoft.aspnetcore.openapi.nuspec` and `https://api.nuget.org/v3-flatcontainer/fastendpoints.openapi/9.0.0/fastendpoints.openapi.nuspec` because the rewritten comment still described the RC3 range.
- Suggestion: State the GA facts in both comments. `Microsoft.AspNetCore.OpenApi` 11.0.0 depends on `Microsoft.OpenApi` `[3.11.0, 4.0.0)`, which is the pin. `FastEndpoints.OpenApi` 9.0.0 net11.0 depends on `Microsoft.AspNetCore.OpenApi` 11.0.0. Keep the direct package references; correct the sentences that describe them.
- Source: general
- Disposition notes: Fixed in the round-1 fix pass (implementer-claude, 2026-11-16). `Directory.Packages.props` FastEndpoints comment and both `foundation-server.csproj` reference comments now state the GA nuspec facts (FastEndpoints.OpenApi 9.0.0 net11.0 → Microsoft.AspNetCore.OpenApi 11.0.0 → Microsoft.OpenApi [3.11.0, 4.0.0)); direct references kept.

## Duplicates / conflicts

- Single reviewer; nothing to collapse.
