# Round 1 — merged findings
**Date:** 2026-10-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: tools/dev-cli/endpoints/deploy-command.cs:208; tools/dev-cli/services/deploy-pipeline.cs:67; tests/tools/dev-cli-tests/deploy-pipeline-tests.cs:12
- Description: The step-order and stop tests lock `DeployPipeline.Steps()` and the test-only `StopMessage()`. The handler's executed order is hand-written and does not use either one, so reordering or skipping a step stays green. The checklist item "tests cover the step order and stop-on-failure" is met in name only.
- Suggestion: Run the handler from `Steps(hasDatabase)` through a pure step-runner that takes per-step delegates and stops on the first non-zero result. Test that runner with fake delegates. Delete `StopMessage` if nothing in production calls it.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:765
- Description: The AppHost reads the internal `ContainerAppDomain` getter by reflection, but the public `GetOutput("AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN")` returns the same Bicep output. If Aspire renames the member, the reflection path skips silently.
- Suggestion: Use the public `GetOutput`. Update the `ContainerAppDomain` agreement assertion in deploy-operate-tests.cs.
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: tools/dev-cli/services/neon-connection.cs:1077
- Description: The region warning fires for any location that is not the one paired region, so nearby pairs such as us-east-1/eastus2 are called "far".
- Suggestion: Accept a small set of nearby Azure regions per AWS region, or reword the warning to "not the paired region".
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
