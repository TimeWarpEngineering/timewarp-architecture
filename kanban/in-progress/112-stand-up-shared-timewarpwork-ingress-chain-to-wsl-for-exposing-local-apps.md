# Stand up shared `*.timewarp.work` ingress chain to WSL for exposing local apps

## Description

Shared personal-infra ingress that multiple projects ride on — not specific to this repo, but
tracked here because this repo is the first consumer (share running dev instances with outside
people for review).

Actual topology (hostnames are the routers' real identities):

```
*.timewarp.work (wildcard DNS, pending)
  → 49.0.91.107 (static public IP; also published as shop.timewarp.ws)
    → AIS DMZ 192.168.68.2
      → timewarp-gw (shop RouterOS; 443 currently owned by web server 10.10.1.80 → SNI split needed)
        → WireGuard wg1 (10.66.2.4/30: timewarp-gw .5 ↔ goldensea-gw .6)
          → goldensea-gw (LAN bridge 172.16.67.0/24; own uplink = AIS DMZ 192.168.67.2 on ether1)
            → WSL2 Ubuntu bridged @ 172.16.67.13 (pending wsl restart)
              → Caddy :443 (live) → localhost:63620 Aspire YARP ingress http (pinned; https local = 63610)
```

The timewarp.work path is **HTTPS-only** (port 80 stays with the existing shop server) and certs
MUST come via DNS-01.

Goal: one stable entry — a reverse proxy in WSL listening on fixed 443 — that terminates TLS for
`*.timewarp.work` and routes by Host header to whichever local app owns that name. Today the
backends are `dev run` Aspire instances (ingress now pinned at 63610); later a kind cluster's
ingress ([[070-001-expose-local-kind-deploy-publicly-via-nametimewarpwork-through-mikrotik]])
plugs into the same chain as just another backend.

## Checklist

- [ ] DNS: **Cloudflare** hosts timewarp.work (confirmed 2026-07-20). Add wildcard A record:
      Name `*`, IPv4 49.0.91.107, **Proxy status: DNS only (grey cloud)** — Proxied mode would
      put Cloudflare's TLS termination in the path, breaking the SNI split and advertising ECH
      (which encrypts the SNI the splitter needs). Safe to add before the SNI split is live
      (unknown hostnames just land on the shop server).
- [ ] timewarp-gw (shop), Phase 1 — TEMP direct forward: 10.10.1.80 is currently DOWN
      (maintenance; ping timeout 2026-07-20), so shop 443 is dead anyway. Add TEMP dst-nat
      `dst-address=192.168.68.2 dst-port=443` → 10.66.2.6 `place-before=0` (disabled until the
      WSL side is ready) + srcnat masquerade out wg1 → 10.66.2.6:443 (needed in this phase: plain
      dst-nat has no fresh-connection proxy, asymmetric return applies). While enabled,
      shop.timewarp.ws visitors get a cert mismatch from our proxy instead of a timeout.
- [ ] timewarp-gw (shop), Phase 2 — SNI passthrough split when servers return: decision A vs B
      deferred until 10.10.1.80 is back and identifiable. Option A: nginx `stream`+`ssl_preread`
      on 10.10.1.80 itself (existing sites rebind internally, e.g. :8443). Option B: tiny
      HAProxy/sniproxy VM on the shop VM host; NAT rules 0/1 repoint 443 to it, default backend
      10.10.1.80, `*.timewarp.work` → 10.66.2.6:443. Either way the splitter originates a fresh
      TCP connection into the tunnel, so the Phase-1 masquerade can be retired if goldensea-gw
      routes back to 10.10.1.0/24. Replaces the TEMP rule.
- [x] goldensea-gw: static DHCP lease 172.16.67.13 for WSL MAC 02:15:5D:8B:4B:AD; dst-nat 80+443
      `in-interface=wg1` → 172.16.67.13 added **disabled**; forward filter verified (masqueraded
      tunnel src hits the rfc1918 accept). Side fix: desktop TWE-001 static lease .11 repointed to
      its current NIC MAC 9C:6B:00:14:0B:9A.
- [ ] goldensea-gw: enable the 443 dst-nat rule when the chain is ready; drop (or keep disabled)
      the port-80 rule — dead weight under the HTTPS-only design.
- [x] Windows → WSL2 bridged networking, staging: external vSwitch `WSLBridge` on "Ethernet 2"
      (Realtek 2.5GbE, `-AllowManagementOS $true`); `C:\Users\steve\.wslconfig` written
      (networkingMode=bridged, vmSwitch=WSLBridge, macAddress=02:15:5D:8B:4B:AD, dhcp, ipv6).
      Basis: Windows 10.0.26200 + WSL 2.7.10; bridged un-deprecated in WSL 2.5.6.
- [ ] Windows → WSL2 bridged networking, cutover: `wsl --shutdown` at a convenient time (kills all
      WSL sessions + Docker Desktop backend), then verify eth0 = 172.16.67.13 via goldensea-gw
      DHCP, and confirm Docker Desktop + kind still work under bridged mode. Fallback if bridged
      misbehaves: revert `.wslconfig` to NAT + `netsh interface portproxy` 443 →
      `connectaddress=127.0.0.1` (localhost relay; IP-stable, no scripts).
- [x] Reverse proxy in WSL on fixed 443: **Caddy v2.11.4** (caddyserver.com build with
      `dns.providers.cloudflare`) installed as systemd service `caddy.service` on TWE-001 WSL —
      `/usr/local/bin/caddy`, `/etc/caddy/Caddyfile`, token in root-only `/etc/caddy/caddy.env`
      (`{env.CLOUDFLARE_API_TOKEN}` in config; token never in files under git or chat). Unmapped
      hostnames 404 (verified locally).
- [x] TLS: wildcard `*.timewarp.work` cert **obtained** from Let's Encrypt production via DNS-01
      + Cloudflare token, 2026-07-20 ("certificate obtained successfully"). Auto-renews via Caddy.
- [x] Per-project name→backend mapping: lives in `/etc/caddy/Caddyfile` — one `@name host` matcher
      + `handle` block per app inside the single wildcard site; `arch.timewarp.work` claimed for
      this repo → `https://localhost:63610` (backend dev-cert verify skipped). Reload:
      `sudo systemctl reload caddy`.
- [x] Dynamic-port problem for `dev run` backends: solved by pinning both YARP ingress endpoints —
      AppHost `Ingress:Port` (https 63610) + `Ingress:HttpPort` (http 63620) in
      appsettings.Development.json; Caddy targets the http endpoint localhost:63620, YARP fans out
      internally. Matches the standalone yarp project's launchSettings by design (alternative
      ingress modes, never co-run). First pin attempt (`abd6b0dd`, `WithHostPort`) only caught the
      http endpoint — https stayed random; fixed with per-endpoint `WithEndpoint` pins.
- [ ] Per-project name→backend mapping: decide where the proxy config lives and the convention for
      claiming a `<name>` (one per project/app instance).
- [ ] Verify end to end from outside the LAN: HTTPS loads on a real device off-wifi; WebAuthn
      passkey ceremony works with RP ID `<name>.timewarp.work` (real-domain exercise of the 104
      identity work beyond localhost).
- [ ] Security notes in the runbook: only intentionally mapped names resolve to anything (proxy
      returns 404/close otherwise); how to disable the public path (timewarp-gw SNI route /
      goldensea-gw dst-nat) when nothing should be public.

## Notes

- **Decision log**
  - Mirrored WSL networking rejected: shared IP forces Windows/WSL port arbitration (two sshds
    can't both own 22). Bridged chosen; portproxy→127.0.0.1 is the recorded fallback.
  - `tls-host` in RouterOS NAT confirmed impossible (2026-07-20, "bad parameter"): NAT picks the
    destination at the TCP SYN, before the ClientHello carrying SNI exists — hence the userspace
    SNI passthrough proxy at timewarp-gw. ECH caveat: the split needs plaintext ClientHello;
    don't publish ECH configs for these names.
  - Asymmetric return path (inbound via wg1, default route via goldensea-gw's AIS uplink): solved
    structurally by the SNI proxy making a fresh connection into the tunnel. Client source IP is
    therefore the proxy's, not the visitor's — acceptable for MVP; upgrade path for real client
    IPs is PROXY protocol from the SNI proxy or policy routing, revisit when rate limiting needs
    it (cf. 104-015).
- The WSL proxy forwards to the Aspire ingress's endpoint locally; outside users get real TLS at
  the proxy instead of the localhost dev cert. Check the app tolerates being served under a
  different host/scheme (forwarded headers) — Blazor WASM + FastEndpoints should be fine, but
  WebAuthn RP ID and any absolute-URL generation must see the public host.
- Escape hatch if the chain fights back: Cloudflare Tunnel (or Tailscale Funnel) from inside WSL
  bypasses both routers and Windows entirely — plan B only; the static-IP + MikroTik path is
  preferred.
- Consumers: this repo's `dev run` review shares (first), other projects' apps, and later the kind
  deploy in [[070-001-expose-local-kind-deploy-publicly-via-nametimewarpwork-through-mikrotik]]
  (its ingress-nginx host ports become just another backend of this chain, or take over 443 on
  the WSL side for kind-hosted names).

## Session

- Created: 2026-07-20
- Implementation (network staging): 2026-07-20 — goldensea-gw done, WSL bridged staged,
  ingress port pinned; pending: DNS host answer, 10.10.1.80 identification, wsl restart.
