# Round 3 — merged findings
**Date:** 2026-11-26
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: skills/tw-deploy/SKILL.md:294
- Description: Round 1: "Rules for new code" said that without the yarp flag the published output exposes nothing. The fix scopes that sentence to Compose and Kubernetes and states that on `aca`/`aca-neon` web-server stays the one external app (`PublishAsContainerAppsEdge`). The passkey bullet names `web-server.<env domain>` when the yarp flag is off. Round 3 re-read the full branch and the sentence is still scoped that way.
- Suggestion: none remaining.
- Source: general
- Disposition notes: verified in round 2 against commit `575761ebb`. Still fixed in round 3.

### M2 — Severity: bug — Status: fixed
- File: skills/tw-deploy/references/azure-container-apps-neon.md:217
- Description: Round 1: operator pages gave only the ingress smoke and `dev open` procedure. `SKILL.md`, `azure-container-apps.md`, and `azure-container-apps-neon.md` state both shapes. Ingress: `GET /web-server/alive` and `GET /api/identity/sign-in-methods` on the ingress FQDN. No yarp flag: `GET /alive` and `GET /api/identity/sign-in-methods` on the web-server FQDN. `dev open` prints that public app's URL. The paths match `DeployPipeline.SmokePathsFor`. Round 3 confirmed the same text.
- Suggestion: none remaining.
- Source: general
- Disposition notes: verified in round 2 against commit `575761ebb`. Still fixed in round 3.

## Duplicates / conflicts

- Single reviewer. No new findings. Prior IDs M1 and M2 carried forward as fixed.
