# Stand up shared `*.timewarp.work` ingress chain to WSL for exposing local apps

## Description

Shared personal-infra ingress that multiple projects ride on — not specific to this repo, but
tracked here because this repo is the first consumer (share running dev instances with outside
people for review).

Actual topology:

```
*.timewarp.work (wildcard DNS)
  → static IP (already published as shop.timewarp.ws)
    → timewarp-mikrotik
      → golden-sea-mikrotik
        → Windows machine
          → WSL2 Ubuntu  ← Aspire `dev run` lives here (today: https://localhost:17204/, dynamic ports)
```

Goal: one stable entry — a reverse proxy in WSL listening on fixed 80/443 — that terminates TLS
for `*.timewarp.work` and routes by Host header to whichever local app owns that name. Today the
backends are `dev run` Aspire instances (dynamic ports); later a kind cluster's ingress
([[070-001-expose-local-kind-deploy-publicly-via-nametimewarpwork-through-mikrotik]]) plugs into
the same chain as just another backend.

## Checklist

- [ ] DNS: wildcard `*.timewarp.work` A record → the static IP (or CNAME → `shop.timewarp.ws`
      since that name already tracks it). Record where timewarp.work DNS is hosted.
- [ ] timewarp-mikrotik: dst-nat 80/443 → golden-sea-mikrotik. Document RouterOS commands,
      including hairpin NAT for LAN-side access.
- [ ] golden-sea-mikrotik: dst-nat 80/443 → the Windows machine's private IP (give it a static
      DHCP lease so the rule doesn't rot).
- [ ] Windows → WSL2 inbound: verified 2026-07-20 — Windows build **10.0.26200** (Win11 25H2,
      mirrored-capable), current mode is stock **nat** (`wslinfo --networking-mode`, eth0
      172.30.x.x/20, no `.wslconfig` present). Plan: switch to **mirrored networking**
      (`.wslconfig` `networkingMode=mirrored` + Hyper-V firewall allow rules for 80/443) so WSL
      shares the Windows LAN IP and golden-sea-mikrotik dst-nats straight to it — no portproxy,
      no per-boot IP chasing. At implementation time confirm Docker Desktop + kind behave under
      mirrored mode; fallback is `netsh interface portproxy` 80/443 → WSL IP with a startup
      script (NAT IP changes per boot).
- [ ] Reverse proxy in WSL on fixed 80/443 (Caddy / Traefik / YARP — pick one; Caddy is the
      least-config option for automatic TLS) terminating `*.timewarp.work` TLS and routing by
      Host header to local backend ports.
- [ ] TLS: wildcard cert via Let's Encrypt DNS-01 (needs API access at the DNS host) so every
      `<name>` is covered with no per-app issuance; HTTP-01 per-name is the fallback since port 80
      is forwarded anyway.
- [ ] Solve the dynamic-port problem for `dev run` backends: Aspire assigns dynamic ports (e.g.
      17204 today), which breaks a static proxy map. Options: pin the public-facing endpoint's
      port per project (launchSettings / `WithEndpoint(port:)`) — simplest; or a small
      registration step where `dev run` writes its current port into the proxy config and reloads.
      Pick one and wire it.
- [ ] Per-project name→backend mapping: decide where the proxy config lives and the convention for
      claiming a `<name>` (one per project/app instance).
- [ ] Verify end to end from outside the LAN: HTTPS loads on a real device off-wifi; WebAuthn
      passkey ceremony works with RP ID `<name>.timewarp.work` (real-domain exercise of the 104
      identity work beyond localhost).
- [ ] Security notes in the runbook: only intentionally mapped names resolve to anything (proxy
      returns 404/close otherwise); how to disable the timewarp-mikrotik forward when nothing
      should be public.

## Notes

- The proxy forwards to the Aspire app's **http** endpoint; outside users get real TLS at the
  proxy instead of the localhost dev cert. Check the app tolerates being served under a different
  host/scheme (forwarded headers) — Blazor WASM + FastEndpoints should be fine, but WebAuthn RP ID
  and any absolute-URL generation must see the public host.
- Escape hatch if the double-NAT + WSL hop fights back: Cloudflare Tunnel (or Tailscale Funnel)
  from inside WSL bypasses both routers and Windows entirely — worth noting as plan B even though
  the static-IP + MikroTik path is preferred.
- Consumers: this repo's `dev run` review shares (first), other projects' apps, and later the kind
  deploy in [[070-001-expose-local-kind-deploy-publicly-via-nametimewarpwork-through-mikrotik]]
  (its ingress-nginx host ports become just another backend of this chain, or take over 80/443 on
  the WSL side for kind-hosted names).

## Session

- Created: 2026-07-20
