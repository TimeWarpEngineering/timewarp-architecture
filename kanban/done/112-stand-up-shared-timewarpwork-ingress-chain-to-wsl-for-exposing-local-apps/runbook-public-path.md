# Runbook: `*.timewarp.work` public ingress chain — operations

Live since 2026-07-21. Companion to [runbook-wsl-bridged-dns.md](runbook-wsl-bridged-dns.md)
(WSL bridged networking layer). Task: 112.

## The chain (verified end-to-end)

```
visitor → *.timewarp.work (Cloudflare DNS, grey cloud) → 49.0.91.107
  → AIS DMZ 192.168.68.2 → timewarp-gw dstnat rule "TEMP timewarp.work all-443 -> goldensea"
    → wg1 tunnel → 10.66.2.6 (goldensea-gw) → dstnat "timewarp.work https" → 172.16.67.13
      → Caddy :443 (wildcard LE cert, DNS-01/Cloudflare) → host-header route → backend
```

Backend for `arch.timewarp.work`: Aspire YARP ingress `localhost:63620` (http, pinned;
https 63610 is the local/dashboard entry). Requires `dev run` to be up.

## Kill switches (public exposure OFF)

Either rule alone severs the public path; timewarp-gw is the outermost:

```routeros
# timewarp-gw — stop ALL 443 entering the tunnel (also restores nothing for shop; see note)
/ip firewall nat disable [find comment~"TEMP timewarp.work"]

# goldensea-gw — stop tunnel traffic reaching the WSL proxy
/ip firewall nat disable [find comment="timewarp.work https"]
```

Re-enable with the same commands, `enable` for `disable`.

Caddy itself: `sudo systemctl stop caddy` (WSL). Unmapped hostnames always return 404 — an
enabled chain with no Caddyfile mapping exposes nothing.

## TEMP rule caveat (Phase 1)

timewarp-gw currently forwards **all** public 443 into the tunnel because the shop web server
(10.10.1.80) is down for maintenance. While the TEMP rule is enabled, `shop.timewarp.ws`
visitors get our wildcard cert (mismatch error) instead of a timeout. **When the shop servers
return**: replace the TEMP rule with the SNI passthrough split (task 112 Phase-2 checklist item —
nginx ssl_preread on 10.10.1.80 or a small proxy VM; RouterOS cannot split by SNI in NAT).

## Claiming a name (new app / project)

1. Edit `/etc/caddy/Caddyfile` (WSL): add inside the `*.timewarp.work` block, above the
   fallback `handle`:

   ```caddy
   @myname host myname.timewarp.work
   handle @myname {
       reverse_proxy localhost:PORT
   }
   ```

2. `sudo systemctl reload caddy` — no cert work needed (wildcard covers every name), no DNS
   work (wildcard A record), no router work (host-header routing happens in Caddy).
3. Backend port must be **pinned** (this repo: `Ingress:Port`/`Ingress:HttpPort` in the
   AppHost's appsettings.Development.json → 63610/63620).

## Security posture

- Only names mapped in the Caddyfile serve anything; everything else is a hard 404.
- Exposure is for demo/dogfood instances — disable the timewarp-gw rule when nothing needs to
  be public (one command, above).
- Visitor source IPs currently arrive NAT'd (shop masquerade into the tunnel + goldensea
  blanket rfc1918 srcnat) — the app sees router IPs, not real clients. Fine for demos;
  rate limiting by client IP (104-015) needs the PROXY-protocol/policy-routing upgrade first.
- Cloudflare records must stay **grey cloud** (DNS only): Proxied mode inserts Cloudflare TLS
  termination and advertises ECH, both of which break the Phase-2 SNI split.
- The Cloudflare API token lives only in root-owned `/etc/caddy/caddy.env` (0600).

## Health checks

```bash
# from WSL — each hop inward
curl -s  -o /dev/null -w '%{http_code}\n' http://localhost:63620/                    # Aspire ingress (needs dev run)
curl -sk -o /dev/null -w '%{http_code}\n' --resolve x.timewarp.work:443:172.16.67.13 https://x.timewarp.work/   # Caddy: expect 404
curl -s  -o /dev/null -w '%{http_code}\n' --resolve arch.timewarp.work:443:49.0.91.107 https://arch.timewarp.work/  # full public loop: expect 200
systemctl status caddy --no-pager
journalctl -u caddy --since "1 day ago" | grep -i "certificate"                       # renewals
```

DNS note: a hostname queried before the wildcard existed can sit as negative-cached NXDOMAIN in
a resolver for ~30 min (bit us at launch — including 1.1.1.1). Test with a fresh name; purge at
https://one.one.one.one/purge-cache/ if needed.

## Probe caveat: WSL cannot test its own public chain

`curl https://arch.timewarp.work` from INSIDE WSL always times out (000), even when the
chain is fully working for external visitors: self-originated traffic tromboning out AIS →
shop → wg1 → back to this box does not survive the NAT hairpin. Do not diagnose the public
chain from WSL. Valid checks:

- Local half: `curl --resolve arch.timewarp.work:443:127.0.0.1 https://arch.timewarp.work`
  → 200 proves Caddy + Aspire ingress (requires `dev run`).
- External half: probe from a phone on mobile data / any off-LAN vantage, or ask Steve.
- `shop.timewarp.ws:80` timing out only tells you the shop WEB SERVER (10.10.1.80) is down,
  not the router — the timewarp-gw TEMP rule can be alive regardless (2026-07-24 lesson:
  misread this as "whole entry point dark" while externals worked fine).
