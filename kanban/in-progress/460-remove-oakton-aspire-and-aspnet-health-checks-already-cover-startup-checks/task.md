# Remove Oakton; Aspire and ASP.NET health checks already cover startup checks

## Description

`web-server` and `api-server` end `Program` with `RunOaktonCommands(args)`, and two Oakton
environment checks are registered: `SampleEnvironmentCheck` (a placeholder) and
`PostgresDbEnvironmentCheck`. Nothing in CI, the dev-cli, deploy, or the skills runs an Oakton
command (`check-env`, `describe`, `run --check`), so the checks do not run.

The same need is already covered:

- AppHost: `AddPostgres` carries Aspire's built-in resource health check, and web-server and
  web-migrations `WaitFor(postgresDb)` (`source/container-apps/aspire/projects/aspire-app-host/program.cs`).
- App: `postgres-db-module-server.cs` registers `AddDbContextCheck<PostgresDbContext>`, served by
  ServiceDefaults `AddDefaultHealthChecks` / `MapDefaultEndpoints` and the web-server ingress
  health path.

Oakton 6.3.0 (last published 2024-11) caps `Microsoft.Extensions.Hosting` below 10 while this
repo is on 11, which has needed workarounds. Decided 2026-11-01 (Steven): remove Oakton.

## Requirements

- Remove the `Oakton` `PackageVersion` from `Directory.Packages.props` and the `PackageReference`
  from `web-server.csproj` (including the `PrivateAssets=all` added in task 257) and
  `api-server.csproj`.
- `web-server` and `api-server` `Program`: replace `RunOaktonCommands(args)` with the normal
  `RunAsync()` / `Run()`, keep the return-code behavior the callers rely on, and drop the
  `global using Oakton;` / `Oakton.Environment` lines.
- Delete `SampleEnvironmentCheck`, `PostgresDbEnvironmentCheck`, and their
  `ConfigureEnvironmentChecks` registrations. The Postgres `AddDbContextCheck` health check stays.
- Remove the workarounds that exist only for Oakton: the Oakton part of the `NU1608` `NoWarn` in
  `tests/Directory.Build.props` (remove the whole suppression if nothing else still needs it;
  keep it, narrowed and with its reason, if AutoFixture.AutoFakeItEasy still does), the Oakton
  explanation in `source/container-apps/web/platform/agent-discovery/markdown-content-negotiation-tests.cs`
  (and the workaround itself if Oakton was its only reason), and any pin comment that names
  Oakton.
- Update Purpose/Design regions, skills, and comments that describe Oakton commands or
  environment checks so they describe the health checks instead. Do not add a new startup-check
  mechanism.
- Keep `#if` template-flag regions intact (`postgres`, `api`, `web`).
- Gates: `dev build` 0 warnings and 0 errors; `dev test` including the integration lane
  (`aspire-tests`, `web-server-integration-tests`, `api-server-integration-tests`); every phase
  CI runs, including `./bin/dev workflow --phase publish` (the migration bundle step);
  `dev template-smoke` with `--matrix` (the change touches `postgres`/`api` flag regions);
  `dev lint-skills` if skills change; `ganda repo audit`. `grep -ri oakton` outside `kanban/`
  finds nothing.

## Checklist

- [ ] Oakton package and references removed
- [ ] Both hosts run without RunOaktonCommands
- [ ] Environment checks and registrations deleted; DbContext health check kept
- [ ] Oakton-only workarounds removed (NU1608 NoWarn, test comment/workaround)
- [ ] Regions, skills, comments updated
- [ ] Gates green, including publish phase and template-smoke --matrix

## Session

- Created: 8c10267a-9869-468f-94f3-f663f4e20b06 (2026-11-01)

## Notes

- `markdown-content-negotiation-tests.cs:21` mentions Oakton's Hosting ceiling when web-server is a
  direct runfile project; check whether that isolation can now be simplified.
