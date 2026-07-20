# Stand up shared `*.timewarp.work` ingress chain to WSL for exposing local apps

## Description

Shared personal-infra ingress that multiple projects ride on — not specific to this repo, but
tracked here because this repo is the first consumer (share running dev instances with outside
people for review).

Actual topology:

```
*.timewarp.work (wildcard DNS)
  → static IP (already published as shop.timewarp.ws)
    → timewarp-mikrotik (shop)
      → WireGuard tunnel wg1 (10.66.2.4/30: shop .5 ↔ goldensea .6)
        → goldensea-gw (LAN bridge 172.16.67.0/24)
          → WSL2 Ubuntu bridged @ 172.16.67.13  ← Aspire `dev run` (today: https://localhost:17204/, dynamic ports)
```

Discovered 2026-07-20 (goldensea-gw `/interface print`, `/ip address print`): the shop↔goldensea
link is a **WireGuard tunnel**, and goldensea's own uplink is AIS behind a private DMZ
(192.168.67.2 on ether1) — so inbound arrives via wg1 while the default route exits via AIS.
**Asymmetric-return gotcha**: the shop end must masquerade 80/443 traffic into the tunnel so
replies return via wg1; without it every connection half-opens. Trade-off: the WSL proxy sees
10.66.2.5 as client IP, not the real one — acceptable for MVP; the upgrade path (preserve client
IP) is policy routing on goldensea (mangle connection-mark on wg1 arrivals → routing-mark replies
back out wg1), noted for later (matters for rate limiting / abuse visibility, cf. 104-015).

Goal: one stable entry — a reverse proxy in WSL listening on fixed 80/443 — that terminates TLS
for `*.timewarp.work` and routes by Host header to whichever local app owns that name. Today the
backends are `dev run` Aspire instances (dynamic ports); later a kind cluster's ingress
([[070-001-expose-local-kind-deploy-publicly-via-nametimewarpwork-through-mikrotik]]) plugs into
the same chain as just another backend.

## Checklist

- [ ] DNS: wildcard `*.timewarp.work` A record → the static IP (or CNAME → `shop.timewarp.ws`
      since that name already tracks it). Record where timewarp.work DNS is hosted.
- [ ] timewarp-mikrotik: discovered 2026-07-20 — 80/443 are ALREADY forwarded to an existing web
      server at 10.10.1.80 (public IP 49.0.91.107 → AIS DMZ → 192.168.68.2 → dst-nat). Plan:
      **TLS SNI split** — dst-nat rule `tls-host=*.timewarp.work` placed before the generic 443
      forward sends only timewarp.work traffic to 10.66.2.6 (goldensea via wg1) + srcnat
      masquerade out wg1 for the return path. Consequences: port 80 stays with the old server →
      the timewarp.work path is **HTTPS-only** and certs MUST use DNS-01 (HTTP-01 challenges
      would hit 10.10.1.80). Alternative (rejected for now): vhost on 10.10.1.80 proxying to the
      tunnel — touches the production shop server. ECH caveat: SNI matching needs plaintext
      ClientHello; don't publish ECH configs for these names.
- [ ] golden-sea-mikrotik: dst-nat 80/443 → the WSL instance's own LAN IP (bridged mode below;
      give its pinned MAC a static DHCP lease so the rule doesn't rot). Windows box is not in the
      traffic path.
- [ ] Windows → WSL2 inbound: verified 2026-07-20 — Windows build **10.0.26200**, WSL **2.7.10**,
      current mode stock **nat** (`wslinfo --networking-mode`, eth0 172.30.x.x/20, no `.wslconfig`
      present). Plan: **bridged networking** (un-deprecated in WSL 2.5.6 — "Bring back bridged
      networking mode"; we're well past that). **Progress 2026-07-20**: external vSwitch
      `WSLBridge` created on "Ethernet 2" (Realtek 2.5GbE) with `-AllowManagementOS $true`;
      `.wslconfig` staged at `C:\Users\steve\.wslconfig` (bridged, vmSwitch=WSLBridge,
      macAddress=02:15:5D:8B:4B:AD, dhcp, ipv6). Awaiting `wsl --shutdown` at a convenient time
      (kills all WSL sessions + Docker Desktop backend). WSL gets its own IP on the golden-sea LAN:
      `.wslconfig` `networkingMode=bridged` + `vmSwitch=<external Hyper-V vSwitch>` + pinned
      `macAddress=` so a static DHCP lease from golden-sea-mikrotik sticks. Router then dst-nats
      80/443 straight to the WSL IP — Windows is out of the traffic path entirely, and SSH to
      Windows vs WSL stays two distinct IPs. Prereq: create the external vSwitch (needs Hyper-V
      management tools). At implementation time confirm Docker Desktop + kind behave under
      bridged mode. Fallback if bridged misbehaves: stay NAT and
      `netsh interface portproxy` 80/443 → `connectaddress=127.0.0.1` (the WSL localhost relay
      delivers it; IP-stable across reboots, no scripts). Mirrored mode rejected: shared IP means
      Windows/WSL port arbitration (e.g. two sshds can't both own 22).
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
