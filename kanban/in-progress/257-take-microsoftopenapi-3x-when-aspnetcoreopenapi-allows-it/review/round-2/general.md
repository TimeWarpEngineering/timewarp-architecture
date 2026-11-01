# Round 2 — general
**Date:** 2026-11-01
**Scope reviewed:** delta since round 1 (base 25c77b924): e06c648f7 (product) and 52efa0805 (task.md)

## Summary

The fix pass committed the product diff. FastEndpoints is on 9.0.0-beta.6,
Microsoft.AspNetCore.OpenApi on 11.0.0-rc.3.26522.148, and Microsoft.OpenApi on 3.11.0, with no
`VersionOverride`. It also moved the OpenApi 3 call sites to `IOpenApiMediaType`, added a Scalar
UI test, ticked the checklist, and wrote Results with the gates. The reviewer re-ran
`./bin/dev build` in this worktree: 0 warnings, 0 errors. Round-1 M1 and M2 are fixed. The pin
comment rewrite deleted more than the re-check history: it also deleted the only record of the RC
train coupling rules (M3). One test Design region still names FastEndpoints 8.3 (M4).

## Prior findings

- M1 (bug): fixed. The product diff is committed (e06c648f7). Results record build, test
  23/23 including the integration lane, template-smoke, regenerate-generated with no golden diff,
  audit, and nuget outdated, plus OpenAPI and Scalar evidence. The checklist is ticked. The build
  was re-verified here. The other gates were not re-run by this reviewer.
- M2 (suggestion): fixed. `Directory.Packages.props:139` and `foundation-server.csproj:10-12` now
  describe the 11.0.0-rc.2 → RC3 lift and the 3.10.2 → 3.11.0 lift. Neither mentions 2.0.0 or
  GHSA-v5pm-xwqc-g5wc.

## Issues

### Issue 1 — Severity: suggestion
- File: Directory.Packages.props:139
- Description: The old Microsoft.OpenApi comment also held facts about the RC train that are still
  true and are recorded nowhere else in the repo (checked with a repo grep outside `kanban/`):
  - Shared-framework Microsoft.* / System.* pins match SDK 11.0.100-rc.3.26522.148.
  - Npgsql 11.0.0-rc.3 pins Microsoft.EntityFrameworkCore exactly, so the EF family and Npgsql
    move together. Moving EF alone fails restore (NU1608/NU1107).
  - The Extensions monthly packages (Http.Resilience, ServiceDiscovery, Telemetry, AI) stay on
    10.11.0, and System.ServiceModel.Primitives stays on 10.1.0, because neither has an 11.x.

  The task asked for the comment to be cut down to what is still true after the change, with the
  task-by-task re-check history dropped. These facts are true and are not re-check history.
  Without them, the 10.x pins look like mistakes, and an agent could move EF alone.
- Suggestion: Restore those facts as a short comment next to the pins they explain: the
  EF/Npgsql coupling by the EF block, and the "no 11.x" reason by the Extensions and
  ServiceModel pins. Keep the task-by-task history out.
- Status: open

### Issue 2 — Severity: nit
- File: tests/container-apps/web/web-server-integration-tests/features/identity/fast-endpoint-auth-schemes-tests.cs:6
- Description: The Design region still says the test "isolates FastEndpoints 8.3 Policies() vs
  AuthSchemes() against ASP.NET Core 10". The suite now runs on FastEndpoints 9.0.0-beta.6 and
  ASP.NET Core 11 RC3, so the region names a stack this test no longer runs on.
- Suggestion: Drop the version numbers ("FastEndpoints Policies() vs AuthSchemes() against
  ASP.NET Core PolicyEvaluator.AuthenticateAsync"), or name the current versions.
- Status: open

## Checked, no finding

- `open-api-one-of.cs:176-182`: the `is OpenApiMediaType concrete` guard skips only non-concrete
  media types (references). FastEndpoints does not emit media-type references on problem
  responses, so behavior is unchanged.
- `open-api-request-agent-human-link-tests.cs`: `example` → `examples` array matches what the
  .NET 11 OpenAPI 3.1 stack writes. The assertion still requires one parseable non-empty Guid.
- Goal restatement vs task: consistent. No misread.
