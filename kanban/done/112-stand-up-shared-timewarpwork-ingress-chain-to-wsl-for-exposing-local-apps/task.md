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

- [x] DNS: **Cloudflare** hosts timewarp.work. Wildcard A `*.timewarp.work` → 49.0.91.107 added
      **DNS only (grey cloud)** (2026-07-21; apex A record too). Grey cloud is load-bearing:
      Proxied mode would put Cloudflare's TLS termination in the path, breaking the SNI split and
      advertising ECH (which encrypts the SNI the splitter needs).
- [x] timewarp-gw (shop), Phase 1 — TEMP direct forward LIVE 2026-07-21: dst-nat rule 0
      `dst-address=192.168.68.2 dst-port=443` → 10.66.2.6 sits above the generic HTTPS forward
      (10.10.1.80, currently down for maintenance) + srcnat masquerade out wg1 → 10.66.2.6:443
      for the asymmetric return. While TEMP is enabled, shop.timewarp.ws visitors get a cert
      mismatch from our proxy instead of a timeout. Remove when Phase 2 lands.
- [x] timewarp-gw (shop), Phase 2 — EXTRACTED to task 123 (2026-07-24) so 112 closes on its
      verified scope. Blocked on the shop site returning from maintenance — verified still fully
      offline 2026-07-24 (shop.timewarp.ws:80 AND the arch.timewarp.work chain both time out; the
      shared public entry point is dark until the site is back, Phase-1 config intact). Full A/B
      decision context and the TEMP-rule retirement steps live in
      [123](../../to-do/123-timewarp-gw-phase-2-sni-passthrough-split-for-443-when-shop-servers-return.md).
- [x] goldensea-gw: static DHCP lease 172.16.67.13 for WSL MAC 02:15:5D:8B:4B:AD; dst-nat 80+443
      `in-interface=wg1` → 172.16.67.13 added **disabled**; forward filter verified (masqueraded
      tunnel src hits the rfc1918 accept). Side fix: desktop TWE-001 static lease .11 repointed to
      its current NIC MAC 9C:6B:00:14:0B:9A.
- [x] goldensea-gw: 443 dst-nat rule ENABLED 2026-07-21 (port-80 rule left disabled — dead weight
      under the HTTPS-only design). Note: goldensea's blanket `srcnat src-address-list=rfc1918 →
      192.168.67.2` also rewrites tunnel-arriving sources; conntrack un-NATs replies so the path
      works — double NAT, revisit only if client-IP preservation lands.
- [x] Full public path VERIFIED 2026-07-21: `https://arch.timewarp.work` via 49.0.91.107 → HTTP
      200, valid TLS, 153 ms (WSL out via AIS → shop → wg1 → goldensea → Caddy → Aspire ingress).
- [x] Windows → WSL2 bridged networking, staging: external vSwitch `WSLBridge` on "Ethernet 2"
      (Realtek 2.5GbE, `-AllowManagementOS $true`); `C:\Users\steve\.wslconfig` written
      (networkingMode=bridged, vmSwitch=WSLBridge, macAddress=02:15:5D:8B:4B:AD; `dhcp=false`
      after cutover — guest netplan/networkd owns DHCP+DNS, see runbook).
      Basis: Windows 10.0.26200 + WSL 2.7.10; bridged un-deprecated in WSL 2.5.6.
- [x] Windows → WSL2 bridged networking, cutover DONE 2026-07-21: eth0 = 172.16.67.13 via
      goldensea-gw DHCP, Caddy auto-started, Docker Desktop works under bridged mode (kind still
      unverified — 070-001's problem). The cutover surfaced a DHCP/DNS ownership mess (WSL's
      bundled fire-and-forget dhcpcd, resolvconf shim vs resolved stub, RouterOS client-id-beats-
      MAC lease matching) — full causal chain, final config (dhcp=false + netplan/networkd owns
      the guest network), and post-reboot verification commands in
      [runbook-wsl-bridged-dns.md](runbook-wsl-bridged-dns.md). Fallback if bridged ever
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
- [x] Verify end to end from outside the LAN: **confirmed 2026-07-21** — phone on mobile data
      loads `https://arch.timewarp.work` (initial ERR_NAME_NOT_RESOLVED was negative-cached
      NXDOMAIN at resolvers that saw queries before the wildcard record existed — 1.1.1.1
      included; aged out within ~30 min).
- [x] WebAuthn passkey ceremony from an off-LAN device with RP ID `arch.timewarp.work`:
      **VERIFIED 2026-07-21 — colleague in Texas registered a passkey on desktop** through the
      full public chain. First attempt failed as predicted (static `WebAuthnOptions.RpId=
      localhost` vs public hostname). The old env/user-secret `WebAuthnOptions:RpId=
      arch.timewarp.work` workaround (which broke localhost passkeys while set) is RETIRED:
      [[104-031-select-webauthn-rp-id-from-request-host-against-allowlist]] landed per-request
      RP-ID selection, so `RpId` no longer exists. To serve passkeys on a personal share host now,
      add the hostname to the allowlist via a user secret
      (`WebAuthnOptions:AllowedRpIds:0=arch.timewarp.work`) — it APPENDS to the built-in
      `localhost` default (both hosts work simultaneously; no localhost breakage). Known non-infra
      residual: Steve's Android phone gets `NotAllowedError` on both register and sign-in —
      client-side authenticator-provider issue (passkey provider selection / screen lock / Play
      services), page + API load fine; not a chain or server problem.
- [x] Security notes + operations runbook: [runbook-public-path.md](runbook-public-path.md) —
      chain diagram, kill switches (either router severs the path; unmapped names always 404),
      TEMP-rule caveat, name-claiming procedure (Caddyfile block + reload, nothing else), NAT'd
      client-IP caveat, grey-cloud requirement, health checks.

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
