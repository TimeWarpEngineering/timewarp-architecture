# Round 5 — general
**Date:** 2026-11-26
**Scope reviewed:** branch `task/530-ship-the-dev-cli-in-generated-apps` vs `master` (1483 lines). The post-round-4 product delta was read in full: `e504f120e` rewords the Design comment and the generated-app release-mode refusal in `tools/dev-cli/endpoints/workflow-command.cs`. Prior M1 and M2 were re-read on the current tree (`app-features.cs`, `deploy-command.cs`, `deploy-migrate-command.cs`, `open-command.cs`, `aspire-deploy.cs`).

## Summary

The pack still ships `tools/dev-cli/`, `tools/Directory.Build.props`, `.timewarp/dev.jsonc`, and `.envrc`. Template-only endpoints stay `(true)` excludes or `templateRepository` regions. `dev db` is still excluded unless postgres and web are both on. `dev deploy migrate` still names web when the web tree is gone, because postgres requires web. `dev deploy` still refuses without web before `DeployRunner`, and refuses a missing yarp only when `ServesThroughIngress` is true.

The new delta does not invoke deploy. `NeverAutomated_Given_` matches `\bdev(\.cs)?\s+(--\s+)?(deploy|deprovision|open)\b` and `\baspire\s+(deploy|destroy)\b` on each line of `.github/**/*.yml` and `workflow-command.cs`. The old refusal text `` `dev deploy` `` matched that pattern. The replacement says an operator deploys the app with this CLI's deploy command, sets exit code 1, and returns. `workflow-template-repository.cs` (the template repo's release path: pack, push, template-publish-smoke) has no deploy, deprovision, open, or aspire deploy/destroy call. The guard still scans the file. Rewording the false positive is not a silenced or weakened check.

The Goal restatement still matches the original task. The release-mode sentence is the template-only NuGet path, not a flag-off refusal. The Goal line that `open` and `deploy` name the missing feature stays behind the compose/kubernetes limit from the merge, which round 4 already recorded. That is not a misread.

The CI fix record covers build, the 2/2 NeverAutomated test, and audit. Gates were not re-run.

No new issues.

## Issues

<!-- None. -->
