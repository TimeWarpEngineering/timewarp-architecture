# Take every ganda nuget outdated update

## Goal (implementer restatement)

Bring every package `ganda nuget outdated` still reports up to the version it names, in
`Directory.Packages.props` and any other pin file that holds the same id. Re-check the feed
first and take a newer target when one has appeared. Leave `Microsoft.OpenApi` on 2.x only
if a restore against 3.x fails with NU1608 because FastEndpoints.OpenApi or
Microsoft.AspNetCore.OpenApi still caps it below 3.0.0; in that case record the versions
checked and add a dated note on task 257. Do not take FastEndpoints 9 betas, do not add
`VersionOverride`, and do not downgrade anything else to make the graph resolve. After the
Playwright pin moves, install the matching browsers before that lane. Fix product code,
tests, and analyzers that break, without suppressing warnings. Because these pins ship in
the template, bump `<Version>` and the platform package pins in the same commit so
`dev check-version` passes. Finish only when `ganda repo audit`, `dev build` (0 warnings,
0 errors), `dev test`, `dev template-smoke`, and `dev check-version` are green, and a final
`ganda nuget outdated` shows only rows that Results explains. A comment that states a
current pin must name the version that pin has. Historical re-check lines stay as written.

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

- [x] Re-run `ganda nuget outdated` and confirm the list
- [x] Bump all pins (EF Core / Npgsql / DataProtection rc.3 together)
- [x] Microsoft.OpenApi 3.x attempted; result recorded (and task 257 updated if still gated)
- [x] Breaking changes fixed
- [x] Version bumped per `dev check-version`
- [x] Gates green; final `ganda nuget outdated` recorded in Results
- [x] Pin comments name the RC3 and 10.1.0 versions they describe (review M1)

## Session

- Created: 8d5a7906-5253-4dc5-b6d4-729819f6f6b0 (2026-10-30)
- Implementation: grok task-work (2026-10-30)
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a18b29-c08b-74e0-afa6-97ea293ae17f, max-turns 500, Done — 2026-10-30T05:34:06Z
- Review oracle round 1: claude-opus-5-5 (anthropic), headless ganda task work, effort 2, roster general, turn cap 120 — 2026-10-30
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-30T05:39:36Z
- Fix pass 2: grok task-work (2026-10-30). Rewrote the present-tense EF/Npgsql and ServiceModel sentences so they match the pins, and trimmed the task-456 OpenApi re-check to the NU1608 result.

## Review

- Round 1: M1 open at review time. Fix pass 2 rewrote the pin comment. Round 2 has not run.

## Notes

- Related: task 257 (Microsoft.OpenApi 3.x, gated on FastEndpoints 9).

## Results

Re-ran `ganda nuget outdated` on 2026-10-30. The same 22 packages were outdated, at the versions in the description. No newer target had appeared.

Took 21 of them. `Microsoft.OpenApi` stays `2.13.0`.

Tried `Microsoft.OpenApi` 3.11.0. `dotnet restore` of `source/foundation/foundation-server/foundation-server.csproj` failed with NU1608 (warning as error): Microsoft.AspNetCore.OpenApi 10.0.11 requires Microsoft.OpenApi (>= 2.7.5 && < 3.0.0) but 3.11.0 was resolved.

Versions checked 2026-10-30:

- FastEndpoints and FastEndpoints.OpenApi latest stable are still 8.3.0. That OpenApi package depends on Microsoft.AspNetCore.OpenApi 10.0.11, range [2.7.5, 3.0.0).
- FastEndpoints.OpenApi 8.4.0-beta.26 is net10.0 only and depends on Microsoft.AspNetCore.OpenApi 10.0.13, range [2.12.2, 3.0.0).
- FastEndpoints.OpenApi 9.0.0-beta.6 net11.0 depends on Microsoft.AspNetCore.OpenApi 11.0.0-rc.2.26504.105, not RC3. This task did not take that beta.
- Microsoft.AspNetCore.OpenApi 11.0.0-rc.3.26522.148 declares Microsoft.OpenApi [3.10.2, 4.0.0).
- Latest Microsoft.OpenApi 2.x is still 2.13.0. Latest overall is 3.11.0.

A dated note is on task 257.

`dotnet-ef` in `.config/dotnet-tools.json` moved from `11.0.0-rc.2.26504.105` to `11.0.0-rc.3.26522.148` with the EF Core family. Npgsql.EntityFrameworkCore.PostgreSQL 11.0.0-rc.3 pins Microsoft.EntityFrameworkCore exactly at `11.0.0-rc.3.26522.148`.

No product C# changes. The new pins compiled as they are. Playwright Chromium 156.0.8078.4 (driver 1.64.1, revision 1248) was installed before the Playwright lane.

`dev check-version` reported source `2.0.0-beta.29` already published. Bumped `<Version>` in `source/Directory.Build.props` and `timewarp-templates/Directory.Build.props`, and the eleven platform pins in `Directory.Packages.props`, to `2.0.0-beta.30`. The re-check says source is one prerelease ahead of NuGet `2.0.0-beta.29`.

`ganda repo audit` exited 0. The advisory `kanban-landed` warning is eight other claims with no worktree (070-001, 104-024, 104-025, 104-026, 123, 141, 198, 244). `--fix` would land those kitchens, so it was left alone.

Final `ganda nuget outdated` (exit 0): one outdated package, Microsoft.OpenApi 2.13.0 -> 3.11.0 (major), for the NU1608 reason above. A first attempt aborted on an HttpClient timeout after package 57; the retry completed all 132.

### Fix pass 2 (review M1)

The comment above `Microsoft.OpenApi` still said, in the present tense, that the EF family and Npgsql stay on the RC2 train and that System.ServiceModel.Primitives stays 10.0.652802. A task-456 paragraph after it described RC3 and 10.1.0 and repeated the task-420 OpenApi re-check. The present-tense sentences now say the EF family, dotnet-ef, and Npgsql are on RC3 as of task 456 and move together, because Npgsql 11.0.0-rc.3 pins Microsoft.EntityFrameworkCore at 11.0.0-rc.3.26522.148, and that System.ServiceModel.Primitives is 10.1.0 with no 11.x. The task-456 OpenApi note is only the 2026-10-30 NU1608 restore of 3.11.0. The earlier "Re-checked task" lines are unchanged. No package version changed in this pass.

### How to validate

Smoke: In `Directory.Packages.props`, read the comment above the `Microsoft.OpenApi` pin. `rg -n "stay on the RC2 train|stays 10.0.652802|no Npgsql RC3" Directory.Packages.props`. `ganda repo audit`. The pin bump's earlier gates remain `ganda nuget outdated`, `./bin/dev build`, `./bin/dev test`, `./bin/dev template-smoke`, and `./bin/dev check-version`.

Expect: The comment says the EF family, dotnet-ef, and Npgsql are on RC3 (`11.0.0-rc.3.26522.148` and `11.0.0-rc.3`) and move together, and that System.ServiceModel.Primitives is 10.1.0. The task-456 OpenApi note is the 2026-10-30 NU1608 result for 3.11.0. `rg` prints no matches. `ganda repo audit` exits 0. From the pin bump, which this pass did not re-run: `ganda nuget outdated` lists only Microsoft.OpenApi 2.13.0 -> 3.11.0; `./bin/dev build` is 0 warnings and 0 errors; `./bin/dev test` passes 23 of 23 projects, including web-spa-playwright-tests 33/33; `./bin/dev template-smoke` packs TimeWarp.Architecture 2.0.0-beta.30 with SmokeDefault at 0 warnings; `./bin/dev check-version` reports source 2.0.0-beta.30 one prerelease ahead of NuGet 2.0.0-beta.29.

### Review

Round 1 ran on 2026-10-30 at effort 2, with a general reviewer only. One finding was open: M1, a bug, at `Directory.Packages.props:166`. The pin comment block still said the EF/Npgsql family was on RC2 and System.ServiceModel.Primitives stayed 10.0.652802. Counts: bug 1 open; suggestion 0; nit 0. The reviewer re-ran `./bin/dev build` (0 warnings, 0 errors) and `./bin/dev check-version` (pass). Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`. Fix pass 2 rewrote that comment. Round 2 has not dispositioned M1.
