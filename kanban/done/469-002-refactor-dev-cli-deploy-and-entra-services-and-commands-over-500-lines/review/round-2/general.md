# Round 2 — general

**Date:** 2026-11-04
**Scope reviewed:** branch `task/469-002-refactor-dev-cli-deploy-and-entra-services-and-com` vs merge-base `f95c82ea0` (HEAD `9797af8d2`), with the fix-pass delta `28a8e0a52` re-checked against round 1. The eight over-limit dev-cli files, `services/deploy/`, `services/entra/`, `services/template-smoke/`, the runners, the three endpoints, and the dev-cli test renames.

## Summary

Round 1 raised no findings. This round re-checked those claims after the fix pass that replaced the five `TemplateSmokeHarness` partial files with six composed types. The eight files are gone. Every dev-cli `.cs` file is at most 500 lines and the new files each hold one top-level type. Endpoints stay thin. Moved method bodies, exit paths, and secret handling match the merge-base after the type renames. No new defect.

Checked against the task and the implementer restatement (no misread: same eight files, one type per file, 500-line cap, thin endpoints, unchanged verbs, Amuru wrappers; the restatement only adds `dev build` and `ganda repo audit`, which tighten the original gates):

- Line count: no `tools/dev-cli` `.cs` file is over 500. The longest are untouched `services/aspire-run.cs` (496) and lightly edited `endpoints/template-publish-smoke-command.cs` (494).
- One top-level type per new file. The seven multi-type files still in dev-cli (`db-update-command.cs`, `workflow-command.cs`, `aspire-deploy-preflight.cs`, `aspire-publish-targets.cs`, `deploy-pipeline.cs`, `deploy-smoke.cs`, `jaribu-aggregator-summary-gate.cs`) are outside the eight and were not given new types. `aspire-deploy-preflight.cs` is a callee retarget (122 insertions, 122 deletions).
- `partial` is one file per type, and each of those eleven types contains `[GeneratedRegex]`. `TemplateSmokeHarness` is a sealed 42-line composition root, not a partial. It builds one `TemplateSmokeArchitectureSuffixes` and passes that same instance to `TemplateSmokePlatformPins` and `TemplateSmokePackaging`, so the suffix cache is still shared. `TryEnsure` still prints, sets `Environment.ExitCode`, and fail-closes.
- `deploy`, `entra setup`, and `template-smoke` keep their routes and options (59, 60, and 36 lines). Handlers only forward to `DeployRunner`, `EntraSetupRunner`, and `TemplateSmokeRunner`. `dev deploy` still runs publish, deploy, migrate (`DeployMigrateCommand` with `Yes = true`), then smoke, and a non-zero `Environment.ExitCode` still stops the pipeline. `RejectSecretsOnParameters` still refuses secrets on the aspire argv.
- Entra step order is unchanged: preflight, tenant, display name, find-or-create, redirect URIs, service principal, mint, user secrets, summary. Normalized bodies of those steps match the base at 0.977–0.985; the remainder is `command` fields becoming `EntraSetupOptions` / `EntraSetupContext`. The two old `TryReadStringProperty` definitions were identical; the single `EntraAzJson` copy keeps the same success and failure conditions. Confirmation strings (`DeployConfirmationRefusal`, `DeployDeclined`) are byte-identical after the move to `AspireDeployText`.
- `RewriteCpmPins` delegates to `PlatformPinRules.RewritePins`, which is the old replace lambda moved verbatim, including the zero-pin hard-fail. Template-smoke matrix selection, pack/install order, and `Harness.*.*` call sites in `template-publish-smoke-command.cs` match the old command after the property split.
- Process calls in the split trees stay `Shell.Builder`, `DotNet.*`, and `EntraCli`. No `Process.Start`, empty catch, `#pragma`, `SuppressMessage`, or `NotImplemented`. The `BannedSymbols.txt` hit is a scan-set string, not a suppression. Every file under `services/deploy/`, `services/entra/`, and `services/template-smoke/` has Purpose and Design regions. `Directory.Build.props` already compiles `services/**/*.cs`. Test diffs are type renames plus the `services/deploy/*.cs` and `services/entra/*.cs` compile globs.
- `skills/tw-deploy/SKILL.md` points at `services/deploy/deploy-operate.cs`, which exists. Live `skills/` and `documentation/` do not still cite the deleted paths.

## Prior round

Round 1 raised no `M#` ids. Nothing to re-open. The fix pass is the only product delta since that round, and it is covered above.

## Issues

<!-- None. -->
