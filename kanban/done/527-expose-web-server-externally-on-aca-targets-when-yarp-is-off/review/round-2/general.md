# Round 2 — general
**Date:** 2026-11-26
**Scope reviewed:** fix commit `575761ebb` (parent `ad36db434`). Product files: `skills/tw-deploy/SKILL.md`, `skills/tw-deploy/references/azure-container-apps.md`, `skills/tw-deploy/references/azure-container-apps-neon.md`. Prior findings M1 and M2 re-checked against that delta and against `## Results`.

## Summary

The fix pass is docs only. The "Rules for new code" bullet now limits "exposes nothing" to Compose and Kubernetes and names `PublishAsContainerAppsEdge` for `aca` and `aca-neon`. The passkey bullet names `web-server.<env domain>` when the yarp flag is off. Smoke and `dev open` in `SKILL.md` and both Azure references state the ingress shape (`GET /web-server/alive` and `GET /api/identity/sign-in-methods` on the ingress FQDN) and the yarp-off shape (`GET /alive` and the same identity path on the web-server FQDN). Those paths match `DeployPipeline.SmokePathsFor` (`/alive` and `/api/identity/sign-in-methods` for `web-server`, otherwise `SmokePaths`).

The implementer restatement still matches the original requirements. No misread.

Gates were not re-run. The fix-pass `## Results` record a passed `./bin/dev build`, `./bin/dev lint-skills` (11/11, SKILL.md at 499 lines), and `ganda repo audit` (43 passed, 1 non-blocking `vscode-window-icon` warning, 2 skipped). No test file changed. That record covers this delta.

`DeployPipeline.FailureMessage` for smoke still names `SmokePaths` (`/web-server/alive`). The fix pass recorded that and left it. It is outside this docs delta, so this round does not reopen it.

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: skills/tw-deploy/SKILL.md:294
- Description: The rules bullet now says Compose and Kubernetes expose nothing without the yarp flag, and that on `aca`/`aca-neon` web-server stays the one external app (`PublishAsContainerAppsEdge`). The `dev open` passkey bullet (line 234) names `ingress.` or, without yarp, `web-server.<env domain>`.
- Suggestion: none remaining.
- Status: fixed

### M2 — Severity: bug — Status: fixed
- File: skills/tw-deploy/references/azure-container-apps-neon.md:217
- Description: `SKILL.md` lines 104–105, 118–120, and 143, `azure-container-apps.md` lines 49–52 and 64, and `azure-container-apps-neon.md` lines 217–219 and 243–269 state both smoke shapes and say `dev open` prints the public app's URL.
- Suggestion: none remaining.
- Status: fixed

## Issues

None.
