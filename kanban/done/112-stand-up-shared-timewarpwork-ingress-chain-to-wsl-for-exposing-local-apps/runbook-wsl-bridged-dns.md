# Runbook: WSL2 bridged networking — DHCP/DNS ownership (TWE-001)

Resolved 2026-07-21. Symptom: after reboot (which applied the staged bridged cutover),
Claude CLI failed with `✻ Unable to connect to API (ENOTIMP)`. ENOTIMP is a c-ares **DNS**
error, not a connect error — `/etc/resolv.conf` had no `nameserver` lines at all.

## Causal chain

1. Reboot applied `.wslconfig` bridged mode. Default WSL (NAT) injects IP + resolv.conf
   from Windows; bridged mode makes the guest responsible for its own networking — but the
   stock Ubuntu WSL image ships **no in-guest network manager** (`/etc/netplan/` empty,
   systemd-networkd disabled, cloud-init disabled) because NAT mode never needed one.
2. With `dhcp=true` in `.wslconfig`, WSL's init ran its **own bundled dhcpcd** (v10.0.8 —
   distro ships 10.3.0; visible as `class-id="dhcpcd-10.0.8:..."` on the RouterOS lease,
   invisible to in-guest `ps`/journal). It acquired the address fire-and-forget: no daemon
   left running → no renewals, and no DNS landed in resolv.conf.
3. Even with the distro dhcpcd run manually, lease DNS never reached resolv.conf:
   `/usr/sbin/resolvconf` is Ubuntu's **systemd-resolved shim** (`→ resolvectl`). dhcpcd's
   `20-resolv.conf` hook delegates to it, resolved applied the servers to its own link
   state — but `/etc/resolv.conf` pointed at `/mnt/wsl/resolv.conf` (tmpfs, wiped every
   shutdown), not resolved's stub. Nameservers evaporated → bare template → ENOTIMP.
4. Router note: goldensea-gw's DHCP network originally had no explicit `dns-server`;
   RouterOS then advertises **itself** as DNS. Option 6 was likely never absent — the shim
   was the in-guest culprit. Explicit `dns-server=1.1.1.1,8.8.8.8` was set anyway
   (bypasses router resolver quirks, e.g. NOTIMP on HTTPS/type-65 queries).

## Final configuration (single owner per box)

**goldensea-gw (RouterOS)**

```
/ip dhcp-server network set [find] dns-server=1.1.1.1,8.8.8.8
```

Static leases: WSL `172.16.67.13` = MAC `02:15:5D:8B:4B:AD`; Windows `172.16.67.11` =
MAC `9C:6B:00:14:0B:9A` (client-id cleared — see gotcha below).

**Windows `C:\Users\steve\.wslconfig`** — WSL keeps hands off guest DHCP:

```
[wsl2]
networkingMode=bridged
vmSwitch=WSLBridge
macAddress=02:15:5D:8B:4B:AD
dhcp=false
```

**WSL `/etc/netplan/99-bridged.yaml`** (chmod 600) — networkd owns address, renewals, DNS:

```yaml
network:
  version: 2
  ethernets:
    eth0:
      dhcp4: true
      accept-ra: false   # segment has no IPv6 RAs; kills v6 noise
```

**WSL `/etc/wsl.conf`** — WSL must not regenerate resolv.conf:

```
[network]
generateResolvConf = false
```

**WSL resolv.conf** — points at resolved's stub (not the tmpfs file):

```
/etc/resolv.conf -> /run/systemd/resolve/stub-resolv.conf
```

Flow: netplan → systemd-networkd (DHCPv4 + renewals) → option 6 → systemd-resolved →
stub resolv.conf. Change `dns-server` on the router and it propagates on renewal with no
hand edits anywhere.

**Reverted during debugging (do not reapply):** `dpkg-divert` of `/usr/sbin/resolvconf`
(removed), hand-written resolv.conf, `nohook resolv.conf` in dhcpcd.conf, proposed
dhcpcd systemd unit / `[boot] command` (never needed — networkd is the owner).

## Gotcha: RouterOS matches client-id before MAC

TWE-001's static `.11` lease kept losing to a dynamic `.197`: the lease carried
`client-id="1:b4:2e:99:a0:45:69"` from the desktop's **previous NIC**. RouterOS matches
client-id in preference to MAC, so the repointed MAC never matched — even on DISCOVER.

Fix: `/ip dhcp-server lease set [find where address=172.16.67.11] client-id=""`, then on
Windows release/renew. **After any NIC swap, clear `client-id` on the static lease, don't
just repoint the MAC.** Also: removing a bound dynamic lease router-side is not enough
while the client still holds it — it re-REQUESTs and RouterOS re-ACKs; release client-side
first. (`active-client-id` in lease detail is runtime info, harmless; only the configured
`client-id=` field constrains matching.)

## Verification (run after any reboot / network change)

```bash
# WSL
networkctl status eth0        # Address: 172.16.67.13 (DHCPv4 via 172.16.67.1) — networkd owns it
resolvectl status eth0        # DNS Servers: 1.1.1.1 8.8.8.8 (from lease)
ps aux | grep [d]hcpcd        # must be EMPTY (dhcp=false working)
getent ahosts api.anthropic.com
curl -s -o /dev/null -w '%{http_code} via %{remote_ip}\n' https://api.anthropic.com  # 4xx = DNS+TCP+TLS OK
```

```
# Windows
ipconfig | findstr 172.16.67  # 172.16.67.11 (vEthernet WSLBridge)
```

```
# goldensea-gw
/ip dhcp-server lease print where mac-address=02:15:5D:8B:4B:AD   # .13 bound
/ip dhcp-server lease print where mac-address=9C:6B:00:14:0B:9A   # .11 bound, no client-id
```

Notes: `getent hosts` shows only one address family (prefers AAAA) — use `ahosts` or curl
for a real check. Windows management vNIC (`vEthernet (WSLBridge)`, `-AllowManagementOS`)
is itself a DHCP client on this segment; the physical NIC shows "media disconnected" by
design. Watch for Wi-Fi auto-connecting when the wired path blips → dual default gateways
(192.168.67.1 AIS vs 172.16.67.1 bridge — deceptively similar numbering).

## Consequence: no localhost relay (Windows ↔ WSL are separate hosts now)

CORRECTION (2026-07-22): the relay is not entirely gone under bridged mode — `wslrelay.exe`
still relays SELECTED ports (observed: the Aspire dashboard port 17204 works from a Windows
browser; trigger appears tied to the aspire CLI/dashboard flow, mechanism not fully pinned).
Arbitrary WSL-loopback ports (e.g. the ingress 63610/63620, dcp-bound to 127.0.0.1) get NO
relay and are unreachable from Windows localhost — and a netsh portproxy cannot reach
loopback-only binds either; only wslrelay-style vsock plumbing (or ssh -L) can.
WSL services that bind localhost (Aspire dashboard :17204, service endpoints 636xx) are
unreachable from Windows even via `172.16.67.13:<port>` — they don't listen on the LAN interface.

Resolution: Caddy (which binds 0.0.0.0:443) is the front door for anything Windows needs:

- App: `https://arch.timewarp.work` (+ Windows hosts entry `172.16.67.13 arch.timewarp.work`
  for the LAN-direct path instead of the out-and-back-through-the-internet trombone).
- Dashboard: `https://dashboard.timewarp.work` route in the Caddyfile, `remote_ip
  172.16.67.0/24` guarded (public visitors arrive masqueraded as router IPs → 404). Swap the
  host into the per-run `/login?t=...` link from `dev run` output.
- Anything else that must be reached from Windows: add a Caddy handle block, or access it from
  inside WSL. Do NOT rebind template launchSettings to 0.0.0.0 — that changes the shipped
  template's security posture for a personal-infra convenience.

## Still open (from the ingress checklist)

- Docker Desktop + kind verification under bridged mode.
- Then: wildcard DNS record, timewarp-gw Phase 1, enable goldensea-gw 443 dst-nat,
  end-to-end test from off-LAN.
