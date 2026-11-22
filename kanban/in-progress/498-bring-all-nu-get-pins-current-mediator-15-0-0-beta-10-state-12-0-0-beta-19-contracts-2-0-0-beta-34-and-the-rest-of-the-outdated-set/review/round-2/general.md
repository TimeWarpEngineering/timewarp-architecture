# Round 2 — general
**Date:** 2026-11-22
**Scope reviewed:** fix delta `6a3524906..2df38b0bc` (commit `2df38b0bc`), product files only, against round 1 M1 and M2 and against `## Goal (implementer restatement)`.

## Summary

The fix pass does what round 1 asked. The release job installs the pinned Aspire CLI with the same script as the ci-group and template-smoke jobs, and probe mode skips it. `dev publish`, `dev deploy`, and `dev deprovision` pass a 17.0 floor into the existing CLI guard. `dev db nuke` stays on 13.6. The tw-deploy sentences and the Design comments match those floors.

The fix-pass paragraph in the implementer restatement matches the task and the round 1 findings. No misread. Recorded gates for the fix (self-install, build 0/0, dev-cli-tests 343/343, lint-skills, audit) cover the edit. No new findings.

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: .github/workflows/workflow.yml:435
- Description: The release job now has an Install Aspire CLI step before Run CI Pipeline. The script is the ci-group script (`$version` from the `<AspireVersion>` sed read, then `dotnet tool install --global Aspire.Cli --version $version`). The job `if` is `github.event_name != 'workflow_dispatch' || inputs.mode != 'probe'`, the same skip as Run CI Pipeline, so probe mode does not install and a published release and a merge dispatch do. Workflow defaults set `shell: pwsh`. `AspireVersionSources.WorkflowCliInstallError` accepts that install line. ci-group (line 255) and template-smoke (line 564) were already installed. skill-lint and the ci aggregator do not build the AppHost.
- Suggestion: none. The round 1 suggestion is in the tree.
- Status: fixed

### M2 — Severity: bug — Status: fixed
- File: tools/dev-cli/services/aspire-publish.cs:39
- Description: `AspirePublish.MinimumCliVersion` is `new(17, 0)` and both `PublishAndCheckAsync` and `PublishFilesAsync` pass it to `AspireCli.ValidateVersionAsync` with `enforceMinimum: true`. `AspireDeploy.MinimumCliVersion` is `new(17, 0)`. `PreflightScope.Deploy` and `PreflightScope.Deprovision` set `RequireAspire`, and `AspireDeployPreflight.RunAsync` passes that constant into the same guard. `dev deploy migrate` still skips the aspire preflight and is checked only when it publishes, through `PublishFilesAsync`. `DbNuke.MinimumCliVersion` remains `new(13, 6)` and still warns on a lag. Design comments in `aspire-publish.cs`, `aspire-deploy-preflight.cs`, `deploy-runner.cs`, and `deprovision-command.cs`, and tw-deploy lines 88 and 203, say 17.0.
- Suggestion: none. The round 1 suggestion is in the tree.
- Status: fixed

## Issues

None.
