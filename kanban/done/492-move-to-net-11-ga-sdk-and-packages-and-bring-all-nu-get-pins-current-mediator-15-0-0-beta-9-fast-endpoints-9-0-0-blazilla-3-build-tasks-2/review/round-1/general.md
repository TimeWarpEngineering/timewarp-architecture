# Round 1 — general
**Date:** 2026-11-16
**Scope reviewed:** branch `task/492-move-to-net-11-ga-sdk-and-packages-and-bring-all` vs merge-base `7d832296c` (SDK, `Directory.Packages.props`, CI `global-json-file`, Roslyn lift, Jaribu `TimeProvider` qualifications, Blazilla sites, githook audit refresh) and the kitchen `## Results` / `### How to validate` record.

## Summary

The branch moves the SDK and the non-EF Microsoft / TimeWarp / third-party pins onto the versions in the task table, holds the six EF pins with `dotnet-ef` because Npgsql 11.0.0-rc.3 still exact-pins EF Core rc.3, and records build, test (including Playwright), template-smoke, check-version, outdated, and audit. That gate record covers the change, so those gates were not re-run. One rewritten pin comment still states the RC3 OpenApi dependency range. The EF hold was checked against the Npgsql nuspec and matches it.

## Issues

### Issue 1 — Severity: bug
- File: Directory.Packages.props:158
- Description: The FastEndpoints comment this task required to be rewritten still says `Microsoft.AspNetCore.OpenApi` 11.0.0 allows `Microsoft.OpenApi` `[3.10.2, 4.0.0)` and that the foundation-server reference lifts a 3.10.2 floor to 3.11.0. The 11.0.0 nuspec depends on `Microsoft.OpenApi` `[3.11.0, 4.0.0)`. The same stale story is in `source/foundation/foundation-server/foundation-server.csproj:10` (lift an rc.2 floor to the RC3 train) and `:12` (lift a 3.10.2 floor). `FastEndpoints.OpenApi` 9.0.0 net11.0 depends on `Microsoft.AspNetCore.OpenApi` 11.0.0, not 11.0.0-rc.2. Checked by fetching `https://api.nuget.org/v3-flatcontainer/microsoft.aspnetcore.openapi/11.0.0/microsoft.aspnetcore.openapi.nuspec` and `https://api.nuget.org/v3-flatcontainer/fastendpoints.openapi/9.0.0/fastendpoints.openapi.nuspec` because the rewritten comment still described the RC3 range.
- Suggestion: State the GA facts in both comments. `Microsoft.AspNetCore.OpenApi` 11.0.0 depends on `Microsoft.OpenApi` `[3.11.0, 4.0.0)`, which is the pin. `FastEndpoints.OpenApi` 9.0.0 net11.0 depends on `Microsoft.AspNetCore.OpenApi` 11.0.0. Keep the direct package references; correct the sentences that describe them.
- Status: open
