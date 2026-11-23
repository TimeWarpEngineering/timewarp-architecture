# Round 1 — merged findings
**Date:** 2026-11-22
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: .github/workflows/workflow.yml:432
- Description: The release job (a published release, and `workflow_dispatch` with mode `merge`) runs `dev workflow`, whose pipeline is clean → build → pack → push → template-publish-smoke. `dev build` builds `timewarp-architecture.slnx`, which includes `aspire-app-host.csproj`. That job has no Install Aspire CLI step. ci-group (line 255) and template-smoke (line 547) install the pin; this job does not. Probe mode skips the build, and nightly uses the template-smoke job, so those two paths are fine. The skill line that says CI installs the pinned CLI in every job that builds the AppHost (`skills/tw-deploy/SKILL.md:86`) is not true of this job. Targeted read, not a build: `Aspire.Hosting.AppHost.props` in the local NuGet cache for `aspire.hosting.apphost` 17.0.0 defaults `AspireUseCliBundle` to true for a `.csproj`, and `Aspire.Hosting.AppHost.targets` target `ResolveAspireCliBundlePaths` emits ASPIRE009 when DCP and the dashboard stay unresolved. Reason: confirm the AppHost build needs the CLI bundle after `AspireUseCliBundle=false` was removed, which is why the release job cannot rely on a bare runner.
- Suggestion: Add the same Install Aspire CLI step the ci-group job uses, before Run CI Pipeline, with that step's `if` so probe mode still skips it.
- Source: general
- Disposition notes: fixed. `workflow.yml` release job has the same Install Aspire CLI step as the ci groups (reads `<AspireVersion>`), before Run CI Pipeline, with `if: github.event_name != 'workflow_dispatch' || inputs.mode != 'probe'`. `AspireVersionSources` accepts it (dev-cli-tests 343/343).

### M2 — Severity: bug — Status: fixed
- File: tools/dev-cli/services/aspire-publish.cs:38
- Description: `dev publish` and `dev deploy` still hard-refuse only a CLI below 13.6 (`AspirePublish.MinimumCliVersion` and `AspireDeploy.MinimumCliVersion` in `tools/dev-cli/services/deploy/aspire-deploy.cs:34`). A CLI from 13.6 up to but not including 17 warns because it is older than `<AspireVersion>` and then continues. Both verbs build this AppHost (`aspire publish` / `aspire deploy`). The task requires the Aspire CLI on dev machines to be 17, and `dev run` already refuses below 17.0. `skills/tw-deploy/SKILL.md:88` and the preflight sentence at line 203 document the 13.6 floor. `dev db nuke` stays on 13.6; it runs `aspire stop` and does not build the AppHost.
- Suggestion: Raise the publish and deploy hard minimum to 17.0, and update the tw-deploy sentences and the Design comments that still say 13.6 for those verbs (`aspire-publish.cs`, `aspire-deploy-preflight.cs`, `deploy-runner.cs`).
- Source: general
- Disposition notes: fixed. `AspirePublish.MinimumCliVersion` and `AspireDeploy.MinimumCliVersion` are 17.0. The Design comments in `aspire-publish.cs`, `aspire-deploy-preflight.cs`, `deploy-runner.cs`, and `deprovision-command.cs`, and tw-deploy SKILL.md lines 88 and 203, say 17.0. `dev db nuke` keeps 13.6.

## Duplicates / conflicts

- No overlap. One reviewer.
