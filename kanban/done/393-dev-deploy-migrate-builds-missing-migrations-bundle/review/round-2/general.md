# Round 2 — general
**Date:** 2026-10-21
**Scope reviewed:** fix delta `80af5fa25..bf734a6a1` (commit bf734a6a1 "fix: run dev deploy from the tested step list"): `tools/dev-cli/endpoints/deploy-command.cs`, `tools/dev-cli/services/deploy-pipeline.cs`, `tools/dev-cli/services/neon-connection.cs`, `source/container-apps/aspire/projects/aspire-app-host/program.cs`, `tests/tools/dev-cli-tests/{deploy-pipeline,deploy-operate,neon-connection}-tests.cs`, `skills/tw-deploy/SKILL.md`, `skills/tw-deploy/references/azure-container-apps-neon.md`, task.md.

## Summary

The fix pass closes all three round-1 findings. The handler now runs `DeployPipeline.RunAsync(hasDatabase, RunStepAsync, ct)`. The executed order therefore comes from `Steps(hasDatabase)`, and the new `Run_Given_` tests drive that runner with fake delegates. They cover the full order, the missing migrate step, and stop-on-first-failure. `StopMessage` is deleted and nothing references it. The AppHost uses the public `GetOutput("AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN")`. That output name is in the 13.6.2 `Aspire.Hosting.Azure.AppContainers.dll`, the version pinned in `Directory.Packages.props`. The agreement test now asserts the public path and no `ContainerAppDomain`. The region table holds a nearby set per AWS region. The first entry is the one the warning names, and new tests cover a quiet nearby pair and a far pair. The skill text matches.

The restated Goal was updated for the fix and still matches the task. There is no new misread.

Verified in this round: `dev-cli-tests` Release run, 293 passed, 0 failed. AppHost `dotnet build -c Release` gave 0 warnings and 0 errors. The live Azure deploy was not repeated. The fix changes how the default-domain expression is read, not the Bicep output it reads.

## Issues

None. The step delegates keep the earlier failure semantics. `Stop` sets `Environment.ExitCode`, and the outer guard only fills an unset exit code. Migrate still checks `Environment.ExitCode`, as before the fix.

## Prior findings

| ID | Round-1 severity | Round-2 status | Evidence |
|----|------------------|----------------|----------|
| M1 | bug | fixed | deploy-command.cs runs `DeployPipeline.RunAsync`; deploy-pipeline.cs `RunAsync`; deploy-pipeline-tests.cs `Run_Given_` (3 tests); `StopMessage` removed |
| M2 | suggestion | fixed | program.cs `containerApps.GetOutput("AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN")`; deploy-operate-tests.cs asserts it and `ShouldNotContain("ContainerAppDomain")` |
| M3 | nit | fixed | neon-connection.cs `NearbyAzureRegions` sets + `IsNearbyLocation`; neon-connection-tests.cs `NearbyPair_Should_BeSilent`, `DistantPair_Should_SayFarAndNameThePreferredRegion` |
