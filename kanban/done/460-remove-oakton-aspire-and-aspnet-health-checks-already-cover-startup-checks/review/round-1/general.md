# Round 1 — general
**Date:** 2026-11-01
**Scope reviewed:** branch vs merge-base `e126c27cd`, product paths only (17 files, 366 lines with kanban)

## Summary

The change removes the Oakton package pin and both `PackageReference`s, the `global using Oakton;` /
`Oakton.Environment` lines, `SampleEnvironmentCheck`, `PostgresDbEnvironmentCheck`, and both
`ConfigureEnvironmentChecks` registrations. Both hosts now `await RunAsync()` and return `0`.
Web-server's catch still returns `1`. `AddDbContextCheck<PostgresDbContext>` stays in
`PostgresDbModule`. The `NU1608` suppression is narrowed to AutoFixture.AutoFakeItEasy with its
reason, and the web-server.csproj Oakton `PrivateAssets` workaround and comment are gone. Risk is low.

## Verification

- `git grep -i -E "oakton|environment-?check|EnvironmentCheck|check-env|RunOaktonCommands|CheckEnvironment" -- ':!kanban'`
  returns nothing, so no template config, Dockerfile, deploy file, or skill still names the removed types or commands.
- No caller invokes `Api.Server.Program.Main` or `Web.Server.Program.Main` directly and depends on
  its return code. Integration tests compose through `ConfigureServices` / `HostGraphFactory`.
- `argumentArray` is still consumed by `WebApplication.CreateBuilder` in both hosts, so command-line
  configuration still binds. Oakton's default verb with no arguments was `run` without `--check`,
  so the environment checks did not run at start before this change either. Runtime behavior is unchanged.
- The `tw-deploy` Neon reference now says `/api/health` runs the `PostgresDbContext` check. That
  holds: web-server maps `IngressPaths.WebServerHealthPath` with `MapHealthChecks` and no predicate
  (`source/container-apps/web/projects/web-server/program.cs:613`), so untagged checks run. The old
  text said a startup check ran once per process start, which was not true under the default verb.
  The new text is more accurate.
- The `#if` template-flag regions in `postgres-db-module-server.cs` and both `program.cs` files are intact.
  `ConfigureInfrastructure` keeps `AddHealthChecks()`, and the Results explain why.
- Purpose/Design regions in the touched files describe the health checks, not Oakton.
- The `markdown-content-negotiation-tests.cs` mode stays 100755 (commit 800bdf9df), so the runfile
  stays executable.
- The goal restatement matches the task requirements, so there is no misread.

I did not re-run gates in this round. The implementer reported build 0/0, test 23/23 including the
integration lane, publish phase, template-smoke `--matrix`, lint-skills, and audit (see task.md Results).

## Issues

None.
