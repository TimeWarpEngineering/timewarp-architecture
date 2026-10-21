# Round 1 — general
**Date:** 2026-10-21
**Scope reviewed:** same as framework (branch vs master, 23 files)

## Summary

`dev deploy` now runs publish → deploy → migrate (when `platform/postgres/migrations` exists) →
smoke → URL, stopping at the first failure with a step-specific re-run verb. `dev deploy migrate`
publishes a missing artifact instead of refusing. The AppHost puts `ingress.<ACA environment domain>`
on `WebAuthnOptions:AllowedRpIds` and adds an optional `passkey-rp-host` parameter. aca-neon preflight
warns on a Neon/Azure region mismatch without printing the string. Skills lead with the one command.
Secrets stay off argv and out of output (the file publish receives only the passkey host). Re-ran
`tests/tools/dev-cli-tests` in Release: 289 passed, matching the claim. The live aca-neon proof is
recorded in Results; not re-run here. Dominant risks: the step-order test does not lock the executed
order, and the AppHost reflects into an internal Aspire getter where a public API exists.

## Issues

### Issue 1 — Severity: bug
- File: tools/dev-cli/endpoints/deploy-command.cs:208 (and tools/dev-cli/services/deploy-pipeline.cs:67, tests/tools/dev-cli-tests/deploy-pipeline-tests.cs:12)
- Description: The checklist requires "dev-cli tests cover the step order and the stop-on-failure message". The tests lock `DeployPipeline.Steps(hasDatabase)` and `DeployPipeline.StopMessage(...)`, but the handler never runs either one as its flow. `Steps()` is only printed as the `Steps:` line. The executed order is four hand-written blocks in `DeployCommand.Handler.Handle`. `StopMessage` has no production caller (grep: only deploy-pipeline-tests.cs). Reordering or dropping a step in the handler, or making migrate run when `hasDatabase` is false, leaves every test green. Two things must agree (the printed/tested order and the executed order) with no generation or check between them (AGENTS.md hard rule). That is missing required coverage: the test measures a list, not the behavior.
- Suggestion: Drive the handler from `DeployPipeline.Steps(hasDatabase)`: map each step name to its delegate (publish/deploy/migrate/smoke returning an exit code) and loop, stopping with `FailureMessage` (or `StopMessage`) on the first non-zero result. A pure helper in deploy-pipeline.cs that takes the step delegates (for example `RunAsync(IReadOnlyList<string> steps, Func<string, Task<int>> run)`) can then be tested with fake delegates for order and stop-after-first-failure. Delete `StopMessage` if it stays test-only.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:765
- Description: The AppHost reads `AzureContainerAppEnvironmentResource.ContainerAppDomain` by reflection (`BindingFlags.NonPublic`) because its getter is internal in 13.6.2. Aspire.Hosting.Azure 13.6.2 has a public way to get the same output: `AzureBicepResourceExtensions.GetOutput(IResourceBuilder<AzureBicepResource>, string)`, or `AzureBicepResource.GetOutput(name)`. The output name is `AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN`, which is the `aca_env_outputs_azure_container_apps_environment_default_domain` both new aspire tests assert. Reflection into a private member reconstructs internals instead of using the tool's API, and it ships in every generated app. If a later Aspire renames the member, the `if` skips silently. Only the aspire publish tests would catch it, not the build.
- Suggestion: Replace the reflection with `containerApps.GetOutput("AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN")` (or the resource's public `GetOutput`), so a missing output fails loudly. Update the `program.ShouldContain("ContainerAppDomain")` agreement assertion in deploy-operate-tests.cs to match.
- Status: open

### Issue 3 — Severity: nit
- File: tools/dev-cli/services/neon-connection.cs:1077
- Description: `RegionDistanceWarning` treats every Azure location other than the one paired region as "far". For example `us-east-1` with `eastus2` (both Virginia), or `eu-west-1` with `westeurope`, prints "far from Azure location … Use eastus instead". The checklist asks for a warning when the regions are far. An exact-match check produces false positives on nearby pairs. It is only a warning, so nothing breaks.
- Suggestion: Pair each AWS region with a small set of acceptable Azure regions (for example us-east-1 → eastus, eastus2), and stay quiet on any of them. Or soften the wording to "not the paired region" instead of "far from".
- Status: open
