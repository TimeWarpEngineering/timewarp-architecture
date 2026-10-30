# Round 2 — merged findings
**Date:** 2026-10-29
**Sources:** general
**Scope:** delta `33384f091..778ebb0c5` (`source/`, `tests/`), the fix pass for round 1

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-server-integration-tests/open-api-delete-request-body-tests.cs:115
- Description: (round 1) The Design-region promises that a nullable leftover scalar publishes an optional query parameter and that a leftover object or collection keeps the body with `required: false` had no test.
- Source: general
- Disposition notes: Fixed in 778ebb0c5 and re-verified in round 2. `DeleteProbeOptionalEndpoint` (`Guid Id` route, `int? Limit`, `string? Note`) and `DeleteProbePayloadEndpoint` (`Guid Id` route, `IReadOnlyList<Guid> Items`) live in the test file and are admitted by the FastEndpoints filter only from the test namespace. `Optional_Scalars_Should_Publish_Optional_Query_Parameters` fails without the transformer because `requestBody` would remain and the `limit` query parameter would be missing. `Collection_Property_Should_Keep_An_Optional_RequestBody` fails if the transformer is absent or forces `required: true`. The orchestrator re-ran the fixture: 5/5 pass.

### M2 — Severity: nit — Status: fixed
- File: source/foundation/foundation-server/open-api-delete-request-body.cs:312
- Description: (round 1) The `SchemaFor` fallback mapped `DateOnly` to `date-time`, left `sbyte`, `ushort`, `uint`, and `ulong` as strings, and published enums as a bare string without saying so.
- Source: general
- Disposition notes: Fixed in 778ebb0c5 and re-verified in round 2. `DateOnly` is `date`, `TimeOnly` is `time`, and the integer branch now covers every signed and unsigned integral type that `IsScalar` accepts. The Design region (lines 14-16) states that the body property schema is the primary source and that the enum fallback is a plain string because the app's JSON options own the enum wire format, which is the second remedy round 1 offered.

## New findings

None. Zero issues is a valid outcome; the reviewer did not invent findings.

## Duplicates / conflicts

- Single reviewer; no duplicates. Prior IDs M1 and M2 are carried from round 1 with updated status.

## Verified, no finding

- The `Assemblies` and `Filter` change in `SetupOnce` admits only the three real endpoints plus the two probes. Every other `AddFastEndpoints` call in the tests tree sets `DisableAutoDiscovery = true` with its own assembly and filter and none includes the test assembly, so the public probe endpoints cannot leak into another fixture.
- `IsOptional` treats `int?` as optional through `Nullable.GetUnderlyingType` and `string?` through `NullabilityInfoContext` (the test project has nullable enabled), so both probe scalars publish `required: false`.
- The `IsRequired` test helper treats an omitted `required` key as false, which matches the OpenAPI writer dropping a false boolean.
- Both probes are `AllowAnonymous`; the bearer-security transformer returns early for them.
- Design regions in both files match the code. Purpose and Design regions present (TWA0004), kebab file names, Jaribu with Shouldly only, no estimates.
- The `SchemaFor` fallback is still not exercised directly, because the body-property schema is found first in every current case. Round 1 did not ask for that and it is recorded as a fact, not a finding.
- Gates re-run by the orchestrator on 778ebb0c5: `dev build` 0 warnings 0 errors; `dotnet run -c Release -- --filter-class DeleteRoles_Given_` 5/5 passed.
