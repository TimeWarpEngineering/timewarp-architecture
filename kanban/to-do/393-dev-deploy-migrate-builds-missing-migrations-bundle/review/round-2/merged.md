# Round 2 — merged findings
**Date:** 2026-10-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tools/dev-cli/endpoints/deploy-command.cs; tools/dev-cli/services/deploy-pipeline.cs; tests/tools/dev-cli-tests/deploy-pipeline-tests.cs
- Description: The step-order tests locked `Steps()` and a test-only `StopMessage()`, not the order the handler ran.
- Source: general (round 1)
- Disposition notes: Fixed in bf734a6a1. The handler runs `DeployPipeline.RunAsync` over `Steps(hasDatabase)`. The `Run_Given_` tests use fake delegates to cover the order, the missing migrate step, and stop-on-first-failure. `StopMessage` was removed.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs
- Description: The AppHost read the internal `ContainerAppDomain` getter by reflection.
- Source: general (round 1)
- Disposition notes: Fixed in bf734a6a1. The AppHost uses the public `GetOutput("AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN")`. The name is confirmed in the Aspire.Hosting.Azure.AppContainers 13.6.2 assembly. The agreement test was updated.

### M3 — Severity: nit — Status: fixed
- File: tools/dev-cli/services/neon-connection.cs
- Description: The region warning called nearby pairs such as us-east-1/eastus2 "far".
- Source: general (round 1)
- Disposition notes: Fixed in bf734a6a1. Each AWS region has a set of nearby Azure regions, and the first is the one the warning names. Tests cover a quiet nearby pair and a far pair. The skill text was updated.

## Duplicates / conflicts

- None (single reviewer, no new findings this round).
