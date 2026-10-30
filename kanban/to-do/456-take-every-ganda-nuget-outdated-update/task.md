# Take every ganda nuget outdated update

## Description

Take every update that `ganda nuget outdated` reports for this repo. Bump the pins in
`Directory.Packages.props` (and `msbuild/timewarp-platform-packages.props` or any other pin
file that holds them), and fix any code that breaks.

Reported 2026-10-30 (22 outdated: 1 major, 8 minor, 13 patch):

| Package | Current | Target |
|---------|---------|--------|
| libphonenumber-csharp | 9.0.41 | 9.0.42 |
| Grpc.AspNetCore | 2.84.0 | 2.85.0 |
| Grpc.AspNetCore.Server.Reflection | 2.84.0 | 2.85.0 |
| Grpc.AspNetCore.Web | 2.84.0 | 2.85.0 |
| Grpc.Net.Client | 2.84.0 | 2.85.0 |
| Grpc.Net.Client.Web | 2.84.0 | 2.85.0 |
| System.ServiceModel.Primitives | 10.0.652802 | 10.1.0 |
| Microsoft.AspNetCore.DataProtection.EntityFrameworkCore | 11.0.0-rc.2.26504.105 | 11.0.0-rc.3.26522.148 |
| Microsoft.EntityFrameworkCore | 11.0.0-rc.2.26504.105 | 11.0.0-rc.3.26522.148 |
| Microsoft.EntityFrameworkCore.Design | 11.0.0-rc.2.26504.105 | 11.0.0-rc.3.26522.148 |
| Microsoft.EntityFrameworkCore.InMemory | 11.0.0-rc.2.26504.105 | 11.0.0-rc.3.26522.148 |
| Microsoft.EntityFrameworkCore.Tools | 11.0.0-rc.2.26504.105 | 11.0.0-rc.3.26522.148 |
| Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | 11.0.0-rc.2.26504.105 | 11.0.0-rc.3.26522.148 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 11.0.0-rc.2 | 11.0.0-rc.3 |
| Microsoft.OpenApi | 2.13.0 | 3.11.0 (see Requirements) |
| Scalar.AspNetCore | 2.18.2 | 2.18.6 |
| Microsoft.Playwright | 1.63.0 | 1.64.1 |
| Markdig | 1.4.2 | 1.5.1 |
| OpenTelemetry.Instrumentation.AspNetCore | 1.20.0 | 1.20.1 |
| OpenTelemetry.Instrumentation.Http | 1.20.0 | 1.20.1 |
| OpenTelemetry.Instrumentation.Runtime | 1.20.0 | 1.20.1 |
| System.Security.Cryptography.Pkcs | 10.0.13 | 10.0.14 |

Re-run `ganda nuget outdated` at the start. If newer versions have appeared, take those instead.

## Requirements

- Bump every package listed above. Update any pin comments so they match the new versions.
- **Microsoft.OpenApi 3.x is task 257's gate.** Try the 3.x pin. If restore raises NU1608
  because FastEndpoints.OpenApi / Microsoft.AspNetCore.OpenApi still cap it below 3.0.0, leave
  the pin where it is, record the versions checked in Results, and add a dated line to task
  257's Notes. Do not take FastEndpoints 9 betas in this task.
- No `VersionOverride`. Do not downgrade anything to make the graph resolve.
- After the Microsoft.Playwright bump, install the matching browsers before running the
  Playwright lane.
- Fix any breaking changes in product code, tests, or analyzers. Do not suppress warnings.
- These pins ship in the template, so run `dev check-version` and bump `<Version>` (and the
  platform package pins) in the same commit, per `tw-pr`.
- Gates: `ganda repo audit`, `dev build` (0 warnings, 0 errors), `dev test`,
  `dev template-smoke`, `dev check-version`. Re-run `ganda nuget outdated` at the end; the only
  rows allowed to remain are ones recorded in Results with a reason.

## Checklist

- [ ] Re-run `ganda nuget outdated` and confirm the list
- [ ] Bump all pins (EF Core / Npgsql / DataProtection rc.3 together)
- [ ] Microsoft.OpenApi 3.x attempted; result recorded (and task 257 updated if still gated)
- [ ] Breaking changes fixed
- [ ] Version bumped per `dev check-version`
- [ ] Gates green; final `ganda nuget outdated` recorded in Results

## Session

- Created: 8d5a7906-5253-4dc5-b6d4-729819f6f6b0 (2026-10-30)

## Notes

- Related: task 257 (Microsoft.OpenApi 3.x, gated on FastEndpoints 9).

## Results
