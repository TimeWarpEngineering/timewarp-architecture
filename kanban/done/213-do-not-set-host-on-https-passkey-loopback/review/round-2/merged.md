# Round 2 — merged findings
**Date:** 2026-09-13
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/platform/identity-host/http-request-host-accessor-server.cs
- Description: Re-verified. `GetRequestHost()` honors `X-TimeWarp-Circuit-Host` only when `Request.Host.Host` is loopback (`localhost` case-insensitive, or an `IPAddress.IsLoopback` address with optional IPv6 brackets stripped). Public/non-loopback Host + client-supplied circuit-host header → Request.Host wins. Loopback Host (`localhost` / `127.0.0.1`) + non-empty header → header wins; empty header falls back to Request.Host. `CopyCircuitHost` still leaves `Headers.Host` unset. Direct-to-Kestrel localhost with a spoofed header remains intended loopback trust. Multi-value header `ToString()` comma-join does not create a public-path bypass.
- Suggestion: n/a
- Source: general
- Disposition notes: Fixed in `cd1f5b56`. Covered by `Ignore_Circuit_Host_Header_When_Request_Host_Is_Not_Loopback`, `Return_Circuit_Host_Header_When_Request_Host_Is_Loopback_Ip`, and `Selection_Stays_On_Request_Host_Given_Spoofed_CircuitHost_On_NonLoopback`.

## Duplicates / conflicts

- None. Prior round-1 M1 carried forward; no new IDs.
