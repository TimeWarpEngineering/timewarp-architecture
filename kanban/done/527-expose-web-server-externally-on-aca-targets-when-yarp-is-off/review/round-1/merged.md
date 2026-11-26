# Round 1 — merged findings
**Date:** 2026-11-26
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: skills/tw-deploy/SKILL.md:291
- Description: "Rules for new code" still says that without the yarp flag the published output exposes nothing and the operator adds their own edge. That sentence is absolute. It contradicts the topology bullet at lines 418–420 and `WebServerResource.PublishAsContainerAppsEdge`, which make web-server the external container app on `aca` and `aca-neon`. An agent following the rules list would remove the exposure this task adds. The same skill's passkey bullet (line 231) still says the deploy adds only the public ingress host. `references/passkey-hosts.md` already states the web-server host.
- Suggestion: Qualify the rule. Compose and Kubernetes still publish no host port and no Ingress. On `aca` and `aca-neon`, web-server is the one external app and its `web-server.<environment domain>` host is the passkey RP ID. Point the passkey bullet at that host when the yarp flag is off.
- Source: general
- Disposition notes: fixed in round-1 fix pass. Rules bullet scoped to Compose/Kubernetes; aca/aca-neon keep web-server external; passkey bullet names the web-server host.

### M2 — Severity: bug — Status: open
- File: skills/tw-deploy/references/azure-container-apps-neon.md:217
- Description: The first consumer of this task is a web-only `aca-neon` deploy (`task.md` description, task 491). `dev deploy` smoke in code uses `DeployPipeline.WebServerSmokePaths` (`/alive` and `/api/identity/sign-in-methods`) when the public app is web-server, because `/web-server/alive` is an ingress rewrite (`IngressPaths.WebServerAlivePath`) and web-server maps `HealthEndpointPaths.Alive` (`/alive`). The operator pages still give only the ingress procedure. `SKILL.md` line 118 says smoke is `GET /web-server/alive`. Lines 104–105 and 141 say `dev deploy` opens the ingress. The Neon guide says `dev deploy` requests `GET /web-server/alive` (line 217), then the Smoke section curls `https://<ingress-fqdn>/web-server/alive` (lines 245–258) and says `dev open` prints `https://<ingress-fqdn>` (line 258). `references/azure-container-apps.md` line 59 still comments `dev open --target aca` as the ingress container app's URL. Those curls 404 on the public web-server app.
- Suggestion: In the skill and both Azure references, state the two smoke shapes. Ingress public app: `GET /web-server/alive` and `GET /api/identity/sign-in-methods` on `https://<ingress-fqdn>`. No yarp flag: the same identity path and `GET /alive` on `https://<web-server-fqdn>`. Say `dev open` prints that public app's URL.
- Source: general
- Disposition notes: fixed in round-1 fix pass. SKILL.md and both Azure references state both smoke shapes and that `dev open` prints the public app's URL.

## Duplicates / conflicts

- Single reviewer. No overlaps to collapse.
