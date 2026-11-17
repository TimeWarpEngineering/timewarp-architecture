# Round 1 — general

**Date:** 2026-11-04
**Scope reviewed:** branch `task/469-002-refactor-dev-cli-deploy-and-entra-services-and-com` vs merge-base `f95c82ea0`. The eight over-limit dev-cli files and their callers, the new `services/deploy/`, `services/entra/`, and `services/template-smoke/` files, the runners, and the dev-cli test renames.

## Summary

The change splits the eight over-limit deploy, entra, and template-smoke files into one top-level type per file, all at or under 500 lines, and moves endpoint orchestration into services. Route and option surfaces are unchanged. Compared method bodies, exit paths, and test diffs match the base after the new type names. No behavior defect turned up.

Checked against the task and the implementer restatement:

- Every dev-cli `.cs` file is 500 lines or fewer. The longest are untouched `services/aspire-run.cs` (496) and lightly edited `endpoints/template-publish-smoke-command.cs` (494). Each new file declares one top-level type. `TemplateSmokeHarness` is one partial class across responsibility files, which still meets one type per file.
- `deploy`, `entra setup`, and `template-smoke` keep their routes and options. The handlers only forward into `DeployRunner`, `EntraSetupRunner`, and `TemplateSmokeRunner`.
- `dev deploy` still runs publish, deploy, migrate, then smoke. The migrate delegate is still `DeployMigrateCommand` with `Yes = true`, and a non-zero `Environment.ExitCode` still stops the pipeline. `BuildDeployArguments` still refuses to put secrets on the aspire command line.
- `dev entra setup` keeps the same step order, dry-run skips, and exit code 1 paths. The two old `TryReadStringProperty` copies were identical; the single `EntraAzJson` copy preserves the same success and failure conditions.
- Template-smoke matrix rows, package list, `2.0.0-smoke`, removed symbols, vendored trees, and the generate/assert/restore/build order match the old command. `PlatformPinRules.TryEvaluatePlatformPins` keeps the zero-pin hard-fail.
- Process calls stay `Shell.Builder`, `DotNet.*`, and `EntraCli` wrappers. Test diffs in `aspire-deploy-tests.cs`, `deploy-operate-tests.cs`, `entra-setup-tests.cs`, and `entra-tenant-tests.cs` are type renames (equal insertions and deletions; every changed line names a moved type).
- `aspire-deploy-preflight.cs` still holds `DeployPreflight` and `AspireDeployPreflight`. Both types were already there at the merge-base. The file is 404 lines and is not one of the eight. This change only retargets callees.

## Issues

<!-- None. -->
