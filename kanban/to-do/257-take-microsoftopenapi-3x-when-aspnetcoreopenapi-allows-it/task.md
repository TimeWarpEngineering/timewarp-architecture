# Take Microsoft.OpenApi 3.x when AspNetCore.OpenApi allows it

## Description

`Directory.Packages.props` pins `Microsoft.OpenApi` 2.12.2 (NU1903 lift over the 2.0.0 that
FastEndpoints.OpenApi / Microsoft.AspNetCore.OpenApi pull). `ganda nuget outdated` reports
3.10.2, but every Microsoft.AspNetCore.OpenApi 10.0.x nuspec (re-checked at 10.0.12, task 249)
declares `Microsoft.OpenApi [2.12.0, 3.0.0)`, so a 3.x pin raises NU1608 (warning-as-error).
Deferred from tasks 201 and 249.

**Gate:** start only when the Microsoft.AspNetCore.OpenApi version this repo pins (via the ASP.NET
Core train, .NET 11 expected) declares a Microsoft.OpenApi 3.x range, and FastEndpoints.OpenApi
on the pinned train accepts it.

## Requirements

- Confirm the gate from the nuspecs (AspNetCore.OpenApi + FastEndpoints.OpenApi); if still
  capped below 3.0.0, record the date/versions checked and leave this task in to-do.
- Bump the pin in `Directory.Packages.props`, update its comment, and fix any product code that
  touches the OpenApi object model (Scalar / FastEndpoints document transformers).
- No `VersionOverride`; do not downgrade anything else to satisfy the graph.
- Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`; OpenAPI document
  and Scalar UI still served by web-server and api-server.

## Checklist

- [ ] Gate confirmed from nuspecs
- [ ] Pin bumped + comment reconciled
- [ ] OpenApi object-model call sites fixed
- [ ] Gates green

## Session

- Created: 943834 (2026-09-29), follow-up from task 249

## Notes

- Task 249 Results record the 10.0.12 nuspec check.
- Re-checked task 392 (2026-10-20): FastEndpoints.OpenApi 8.3.0 (latest stable) still pins
  Microsoft.AspNetCore.OpenApi 10.0.11, range [2.7.5, 3.0.0); pin moved to 2.13.0.
  FastEndpoints.OpenApi 9.0.0-beta.2 has a net11.0 group on Microsoft.AspNetCore.OpenApi
  11.0.0-rc.2.26504.105, range [3.10.0, 4.0.0). The gate opens once this repo takes
  FastEndpoints 9 (currently beta).
- Re-checked task 456 (2026-10-30): Microsoft.OpenApi 3.11.0 restore of foundation-server raised
  NU1608. Microsoft.AspNetCore.OpenApi 10.0.11 (the package FastEndpoints.OpenApi 8.3.0, latest
  stable, still depends on) requires Microsoft.OpenApi (>= 2.7.5 && < 3.0.0). FastEndpoints.OpenApi
  8.4.0-beta.26 is net10.0 only and depends on Microsoft.AspNetCore.OpenApi 10.0.13
  ([2.12.2, 3.0.0)). 9.0.0-beta.6's net11.0 group still depends on Microsoft.AspNetCore.OpenApi
  11.0.0-rc.2.26504.105, not 11.0.0-rc.3.26522.148 (range [3.10.2, 4.0.0)). Latest 2.x remains
  2.13.0. This task did not take FastEndpoints 9 betas.
