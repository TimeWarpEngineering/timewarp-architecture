# Round 3 — general
**Date:** 2026-11-26
**Scope reviewed:** branch `task/527-expose-web-server-externally-on-aca-targets-when` vs merge-base `e7a75497d` (HEAD `c5b0dc04d`). Product commit `46209caed` and docs fix `575761ebb`. Re-checked M1 and M2 and the original requirements against `## Goal (implementer restatement)` and `## Results`.

## Summary

With the yarp flag off, `PublishAsContainerAppsEdge` runs in the slot `IngressResource.Add` takes. On `aca` and `aca-neon` it calls `WithExternalHttpEndpoints` and sets `WebAuthnOptions__AllowedRpIds__0` from `PublishTarget.ContainerAppPublicHost` (`web-server.<environment default domain>`, the environment output, not the app endpoint). The method returns immediately when `ContainerApps` is null, so run mode, Compose, and Kubernetes stay on the previous branches. With the yarp flag the call is not compiled and the ingress remains the only external app. `program.cs` places the call after `PostgresResources.AddTo`, the same order the ingress block uses so the migrations snapshot does not copy the RP-ID variable.

`dev deploy` smoke and `dev open` on those targets run `az containerapp list` for the one external app named `ingress` or `web-server`. Smoke uses `DeployPipeline.SmokePathsFor`: `/alive` and `/api/identity/sign-in-methods` when the name is `web-server`, otherwise the ingress paths. `/api/identity/sign-in-methods` is a web-server route, so the direct FQDN is the right host. Publish tests expect exactly one `external: true`, on ingress when that module exists and on web-server otherwise. The yarp-off passkey facts compile only in the `#else` of `#if yarp`.

The skill rules bullet, the passkey bullet, and the smoke / `dev open` pages in `SKILL.md` and both Azure references state that ACA shape. `SKILL.md` is 499 lines. The implementer restatement matches the original requirements. No misread.

Gates were not re-run. `## Results` record `./bin/dev build`, the `DeployOperate_`, `DeployPipeline_`, `AcaPublish_Given_`, and `AcaNeonPublish_Given_` filters, the reverted yarp-off define proof (both passkey facts and `./bin/dev publish aca`), the template `program.cs` probe, `./bin/dev lint-skills`, and `ganda repo audit`. The fix-pass record covers the docs delta (build, lint-skills, audit; no test file changed). That record covers this diff. `dev template-smoke --matrix` is recorded as not run; the define-removal publish and the generation probe are the substitute, and the checklist names the nightly matrix.

`DeployPipeline.FailureMessage` for smoke still names `SmokePaths` (`/web-server/alive`). The attempt lines print the paths that were requested. Prior rounds recorded this and left it. It does not fail the smoke contract. Not reopened.

`git diff master` on the AppHost, deploy, and skill files matches this branch's own delta. Those paths are unchanged on master since `e7a75497d`.

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: skills/tw-deploy/SKILL.md:294
- Description: The rules bullet limits "exposes nothing" to Compose and Kubernetes and names `PublishAsContainerAppsEdge` for `aca` and `aca-neon`. The `dev open` passkey bullet (lines 234–235) names `ingress.` or, without yarp, `web-server.<env domain>`.
- Suggestion: none remaining.
- Status: fixed

### M2 — Severity: bug — Status: fixed
- File: skills/tw-deploy/references/azure-container-apps-neon.md:217
- Description: `SKILL.md` lines 118–120 and 143, `azure-container-apps.md` lines 46–52 and 64, and `azure-container-apps-neon.md` lines 209–219 and 243–269 state both smoke shapes and say `dev open` prints the public app's URL. The paths match `DeployPipeline.SmokePathsFor`.
- Suggestion: none remaining.
- Status: fixed

## Issues

None.
