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

## Goal (implementer restatement)

Drop Oakton from both hosts. `web-server` and `api-server` already get startup coverage from Aspire's Postgres resource health check (`WaitFor`) and from `AddDbContextCheck<PostgresDbContext>` on the ASP.NET health endpoints. Oakton's `RunOaktonCommands`, the two environment checks, the package pins, and the workarounds that exist only because Oakton caps Hosting below 10 go away. No new startup-check mechanism replaces them. Both `Main` methods still return `0` after a clean shutdown and `1` when web-server's startup catch fires. The `postgres`, `api`, and `web` template flags stay as they are.

Done when the package and references are gone, both hosts call `RunAsync`, the environment-check types and registrations are deleted, the Postgres DbContext health check remains, Oakton-only suppressions and comments are gone or narrowed, skills and regions describe the health checks, `grep -ri oakton` outside `kanban/` is empty, and build, test (including the integration lane), `workflow --phase publish`, `template-smoke --matrix`, `lint-skills`, and `ganda repo audit` pass.

## Checklist

- [x] Oakton package and references removed
- [x] Both hosts run without RunOaktonCommands
- [x] Environment checks and registrations deleted; DbContext health check kept
- [x] Oakton-only workarounds removed (NU1608 NoWarn, test comment/workaround)
- [x] Regions, skills, comments updated
- [x] Gates green, including publish phase and template-smoke --matrix

## Session

- Created: 8c10267a-9869-468f-94f3-f663f4e20b06 (2026-11-01)
- Implementation: Grok 01a196af-0f3e-7da2-b42c-3cb40f007fc8 (2026-11-01)

## Notes

- `markdown-content-negotiation-tests.cs:21` mentions Oakton's Hosting ceiling when web-server is a
  direct runfile project; check whether that isolation can now be simplified.
- Resolution: the tests stay HTTP-only through `HostGraphFactory`. That isolation is the test,
  not an Oakton workaround, so they were not given a direct `web-server` project reference. The
  Oakton sentence in the Design region was removed.

## Results

Oakton is gone from both hosts. `web-server` and `api-server` call `RunAsync` and return `0` after a clean shutdown. Web-server still returns `1` when startup throws. `SampleEnvironmentCheck`, `PostgresDbEnvironmentCheck`, and `ConfigureEnvironmentChecks` are deleted. `AddDbContextCheck<PostgresDbContext>` stays, and `ConfigureInfrastructure` still calls `AddHealthChecks` because the integration host wires `ConfigureServices` without ServiceDefaults.

`NU1608` stays in `tests/Directory.Build.props`. AutoFixture.AutoFakeItEasy 4.18.1 still depends on FakeItEasy below 9 while this repo pins FakeItEasy 9.0.1. The comment no longer names Oakton. `tw-deploy` now says a request to `/api/health` runs the Postgres check; nothing probes Postgres at process start.

`git grep -i oakton -- ':!kanban'` is empty. A raw `grep -ri oakton` of this worktree also matches the folder name `task-460-remove-oakton-…` inside generated absolute paths, the `.git` gitdir pointer, and the published migration binaries. Those hits are the worktree path, not the package. Historical kitchens under `kanban/` still name Oakton and were left as history.

`ganda repo audit` exits 0 with one non-blocking advisory: `kanban-landed` warns that nine other live claims have no usable worktree (070-001, 104-024, 104-025, 104-026, 123, 141, 198, 244, 454). `kanban-landed --fix` does not remove those, so this task left them. Publish of aca-neon logs `Failed to connect to 127.0.0.1:5432` while generating the migration script and bundle, then reports the bundle generated and the pipeline succeeds. That connection message was already how this target behaves with no local Postgres.

### How to validate

**Smoke:**

```bash
git grep -i oakton -- ':!kanban'
./bin/dev build
./bin/dev test
./bin/dev workflow --phase publish
./bin/dev template-smoke --matrix
./bin/dev lint-skills
ganda repo audit
```

**Expect:** `git grep` prints nothing. Build is 0 warnings and 0 errors. `./bin/dev test` prints `23 of 23 projects passed` and `Tests completed successfully!`, including `api-server-integration-tests`, `aspire-tests`, and `web-server-integration-tests`. Publish prints `migration bundle generated successfully` and `Pipeline SUCCEEDED`. Template smoke prints `Template smoke SUCCEEDED`. Lint prints `9 skill(s) linted, 9 passed`. Audit prints `Repository passes`.

**Automated gate:** the commands above, from this worktree, on 2026-11-01. Build 0/0 (also rebuilt inside publish in 22.59s, 0/0). Test 23/23 (`api-server-integration-tests` 23.5s, `aspire-tests` 94.9s, `web-server-integration-tests` 41.4s). Publish phase succeeded (compose, kubernetes, aca, aca-neon; production-safety aspire-tests 47/47; helm lint 0 failed). Template-smoke `--matrix` succeeded (SmokeNoYarp 0/0; Jaribu MTP web 442/442, api 9/9, timewarp-testing 14/14). Lint-skills 9/9 and `eval.yaml` valid. Audit passed 34, failed 1 advisory, skipped 2.
