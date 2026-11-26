# Round 4 — general
**Date:** 2026-11-26
**Scope reviewed:** branch `task/530-ship-the-dev-cli-in-generated-apps` vs `master` (1382 lines), with the post-round-3 delta read in full: the origin/master merge in `template-smoke-runner.cs`, `AspireDeploy.ServesThroughIngress`, the yarp checks in `deploy-command.cs` and `open-command.cs`, and the `(!postgres)` exclude of `open-api-binary-content-tests.cs`. Prior M1 and M2 re-checked on that tree.

## Summary

The pack still ships `tools/dev-cli/`, `tools/Directory.Build.props`, `.timewarp/dev.jsonc`, and `.envrc`. Template-only endpoints stay `(true)` excludes or `templateRepository` regions. `dev db` is still excluded unless postgres and web are both on. `dev deploy migrate` still names web when the web tree is gone, because postgres requires web.

The merge keeps task 528's `--variant` selection and `SmokeWebOnly` beside the generated CLI checks. `dev deploy` still refuses without web before `DeployRunner`. It refuses a missing yarp only when `ServesThroughIngress` is true (compose, kubernetes, and the default). aca and aca-neon skip that check, which matches task 527: those targets publish web-server and smoke its own paths. `dev open` uses the same ingress test. Unknown target names are false, so preflight reports them. The new `(!postgres)` exclude sits with the other feedback excludes; the test file references `TimeWarp.Architecture.Features.Feedback`, which that flag removes, and `(!web)` already drops `tests/container-apps/web/**`.

The Goal restatement still matches the original task. Its acceptance line says open and deploy refuse naming the missing feature and does not mention the compose/kubernetes limit. That line is behind the merge fix. The original rule is that a flag-off command is excluded or refuses cleanly, and a command that still has work is not dead. aca and aca-neon still deploy and open without yarp. That is not a misread.

The merge-pass record covers build, the ingress and feature tests, the five variants whose generated `dev build` finished in the timed-out matrix, and the later `--variant` runs of SmokeNoPostgres and SmokeWebOnly. Gates were not re-run.

No new issues.

## Issues

<!-- None. -->
