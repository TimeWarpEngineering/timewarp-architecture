# Expose local kind deploy publicly via `<name>.timewarp.work`

## Description

Child of [[070-wire-aspire-publish-for-portable-deploy-compose-kubernetes]]. Depends on 070's
kind smoke-deploy checklist item AND on the shared ingress chain in
[[112-stand-up-shared-timewarpwork-ingress-chain-to-wsl-for-exposing-local-apps]] (DNS, the two
MikroTik hops, Windows→WSL inbound, and TLS all live there — not here).

Goal: once the generated K8s manifests run on a local kind cluster, plug that cluster into the
shared `*.timewarp.work` chain so a deployed app is publicly served at a chosen hostname. Picking
the `<name>` should be a parameter, not an edit. Easy to bring up, easy to tear down.

## Checklist

- [ ] kind ingress path: install an ingress controller (ingress-nginx is the kind-documented
      default) with `extraPortMappings` in the kind cluster config so its ports are reachable on
      the WSL host, and register it as a backend of the 112 reverse proxy (or let it take over
      80/443 for kind-hosted names — follow whatever 112 decided).
- [ ] Ingress resources: get Host-based routing onto the deployed app — either from the Aspire
      Kubernetes publisher output (does `aspire publish` emit Ingress? confirm at build time) or a
      small kustomize/manifest overlay in `devops/` that adds the Ingress with the chosen
      hostname. Coordinate with 070's artifact-output location decision.
- [ ] Wire an easy entry point: `dev` CLI endpoint (see 061 direction) or documented one-liner
      that takes `<name>`, deploys to kind, and applies the Ingress. Include teardown.
- [ ] Verify end to end from outside the LAN: HTTPS loads, WebAuthn passkey ceremony works on the
      real domain (RP ID = `<name>.timewarp.work`).
- [ ] Document the kind-exposure path in `devops/README.md` alongside 070's three deploy stories.

## Notes

- The YARP ingress container (yarp flag) vs K8s Ingress: decide whether public traffic terminates
  at ingress-nginx and forwards to the app's YARP entry, or whether YARP is bypassed in the K8s
  topology. Coordinates with [[107-generate-yarp-ingress-route-list-from-web-contracts-apiroute-templates]].
- This is personal-infra convenience for dogfooding, not template content — nothing here should
  leak into the `dotnet new` template output beyond the generic manifests 070 already owns.

## Session

- Created: 2026-07-20
