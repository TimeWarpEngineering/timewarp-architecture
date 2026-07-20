# Expose local kind deploy publicly via `<name>.timewarp.work` through MikroTik

## Description

Child of [[070-wire-aspire-publish-for-portable-deploy-compose--kubernetes]]. Depends on 070's
kind smoke-deploy checklist item: once the generated K8s manifests run on a local kind cluster,
make that deployment reachable from the public internet at `<name>.timewarp.work`.

Goal: a `dev`-driven (or minimally scripted) path that deploys the app to local kind and serves it
publicly at a chosen hostname under **timewarp.work** (owned domain), with traffic entering through
the **MikroTik** router. Easy to bring up, easy to tear down.

Requirements:

- Hostname scheme `<name>.timewarp.work` — one name per deployed app instance; picking the name
  should be a parameter, not an edit.
- HTTPS with valid certs (Let's Encrypt via cert-manager, or wildcard DNS-01 — decide below).
- No manual router clicking per deploy: the MikroTik has a **static public IP** and forwards
  80/443 once to the dev machine on the private network; all `*.timewarp.work` traffic lands on
  the same ingress and per-app routing happens there by Host header.

## Checklist

- [ ] DNS: wildcard `*.timewarp.work` A record → the MikroTik's static public IP (single record;
      new `<name>`s need no DNS work). Record where timewarp.work DNS is hosted and how records
      are managed.
- [ ] kind ingress path: install an ingress controller (ingress-nginx is the kind-documented
      default) with `extraPortMappings` for 80/443 in the kind cluster config so host ports reach
      the ingress.
- [ ] MikroTik: one-time dst-nat (port-forward) rules WAN 80/443 → the dev machine's private-LAN
      IP (kind's ingress host ports). Document the RouterOS commands (`/ip firewall nat add ...`)
      including hairpin NAT so `<name>.timewarp.work` also works from inside the LAN. Consider a
      DHCP reservation / static lease for the dev machine so the rule doesn't rot.
- [ ] TLS: cert-manager + Let's Encrypt HTTP-01 (needs port 80 reachable), or DNS-01 with a
      wildcard cert if the DNS host has API support. Pick one and wire it.
- [ ] Ingress resources: get Host-based routing onto the deployed app — either from the Aspire
      Kubernetes publisher output (does `aspire publish` emit Ingress? confirm at build time) or a
      small kustomize/manifest overlay in `devops/` that adds the Ingress with the chosen hostname.
      Coordinate with 070's artifact-output location decision.
- [ ] Wire an easy entry point: `dev` CLI endpoint (see 061 direction) or documented one-liner that
      takes `<name>`, deploys to kind, and applies the Ingress. Include teardown.
- [ ] Verify end to end from outside the LAN (phone off wifi): HTTPS loads, WebAuthn passkey
      ceremony works on the real domain (RP ID = `<name>.timewarp.work` — good real-world exercise
      of the 104 identity work beyond localhost).
- [ ] Document the whole path in `devops/README.md` alongside 070's three deploy stories.

## Notes

- Security posture: this exposes a dev box to the internet. Keep scope to demo/dogfood instances;
  note in the docs that the MikroTik rules should be disabled when not in use, or restricted by
  src-address list if only specific networks need access.
- The YARP ingress container (yarp flag) vs K8s Ingress: decide whether public traffic terminates
  at ingress-nginx and forwards to the app's YARP entry, or whether YARP is bypassed in the K8s
  topology. Coordinates with [[107-generate-yarp-ingress-route-list-from-web-contracts-apiroute-templates]].
- This is personal-infra convenience for dogfooding, not template content — nothing here should
  leak into the `dotnet new` template output beyond the generic manifests 070 already owns.

## Session

- Created: 2026-07-20
