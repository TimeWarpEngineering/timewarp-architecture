# Timewarp-gw Phase 2 SNI passthrough split for 443 when shop servers return

## Description

Extracted from task 112 (the one remaining checkbox) so 112 could close on its verified
scope. Personal infra, not repo code — tracked here because this repo consumes the chain.

Context: the `*.timewarp.work` public chain (112, done) currently enters via timewarp-gw's
**Phase 1 TEMP rule**: dst-nat rule 0 `dst-address=192.168.68.2 dst-port=443` → 10.66.2.6
(WireGuard wg1 → goldensea-gw → WSL Caddy), sitting above the generic HTTPS forward to the
shop web server 10.10.1.80 (offline for maintenance) + srcnat masquerade out wg1 for the
asymmetric return. Consequence while TEMP is live: `shop.timewarp.ws` visitors get a cert
mismatch from our proxy instead of the shop site.

Goal: replace TEMP with an SNI passthrough split so 443 serves BOTH `*.timewarp.work`
(→ tunnel) and the shop sites (→ 10.10.1.80), routed by SNI. Cloudflare stays grey-cloud
(Proxied mode would terminate TLS + advertise ECH, hiding the SNI the splitter needs — see
112's checklist).

**Blocked on:** shop site returning from maintenance. Verified unreachable 2026-07-24
(http://shop.timewarp.ws:80 and the full arch.timewarp.work chain both time out — the whole
shop entry point is down, which also means the public share chain is dark until the site is
back; Phase 1 config is intact, nothing to fix on our side).

## Decision to make first (with Steve, once 10.10.1.80 is back and identifiable)

- **Option A:** nginx `stream` + `ssl_preread` on 10.10.1.80 itself; existing sites rebind
  internally (e.g. :8443). No new host; touches the shop server's config.
- **Option B:** tiny HAProxy/sniproxy VM on the shop VM host; NAT rules 0/1 repoint 443 to
  it; default backend 10.10.1.80, `*.timewarp.work` → 10.66.2.6:443. New host; shop server
  untouched.

Either way the splitter originates a fresh TCP connection into the tunnel, so Phase 1's
srcnat masquerade can be retired if goldensea-gw routes back to 10.10.1.0/24.

## Checklist

- [ ] Confirm shop site back online and 10.10.1.80 identifiable (OS, what runs the sites)
- [ ] Decide A vs B with Steve
- [ ] Implement splitter; repoint timewarp-gw NAT; remove the Phase 1 TEMP rule + masquerade
      (if return-routing allows)
- [ ] Verify: `https://shop.timewarp.ws` serves the shop cert AND `https://arch.timewarp.work`
      serves ours, simultaneously, from off-LAN

## Notes

Origin: task 112 checklist (see `kanban/done/112-*/task.md` and its runbooks
`runbook-public-path.md` / `runbook-wsl-bridged-dns.md` for the full chain). Router work is
Steve-driven (RouterOS commands run manually); agent guides and verifies.
