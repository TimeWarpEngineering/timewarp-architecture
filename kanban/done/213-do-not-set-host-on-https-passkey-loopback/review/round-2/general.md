# Round 2 — general
**Date:** 2026-09-13
**Scope reviewed:** post-fix delta `cd1f5b56` (loopback-only X-TimeWarp-Circuit-Host) plus residual M1

## Summary

M1 is fixed: `GetRequestHost()` honors `X-TimeWarp-Circuit-Host` only when `Request.Host.Host` is loopback (`localhost` case-insensitive, or an `IPAddress.IsLoopback` address with optional IPv6 brackets stripped). On a non-loopback Host the header is ignored and `Request.Host.Host` wins, covered by unit `Ignore_Circuit_Host_Header_When_Request_Host_Is_Not_Loopback` and e2e `Selection_Stays_On_Request_Host_Given_Spoofed_CircuitHost_On_NonLoopback`. `CopyCircuitHost` still leaves `Headers.Host` unset; no SSL bypass, HTTP loopback, or `UseForwardedHeaders`. Design regions on the accessor, interface, WebAuthn options, mock defaults, and `program.cs` match the loopback gate. No new defects on the fix delta.

## Issues

### M1 — Severity: bug
- File: source/container-apps/web/platform/identity-host/http-request-host-accessor-server.cs
- Description: Re-verified. Public/non-loopback Host + client-supplied circuit-host header → Request.Host wins. Loopback Host (`localhost` / `127.0.0.1`) + non-empty header → header wins; empty header falls back to Request.Host. `IsLoopbackHost` treats `Localhost` via OrdinalIgnoreCase and bracketed IPv6 via strip-then-`IPAddress.IsLoopback`. Direct-to-Kestrel localhost with a spoofed header remains intended loopback trust (same as setting Host on localhost). Multi-value header `ToString()` comma-join does not create a public-path bypass (header ignored unless Host is loopback; on loopback a joined value fails allowlist match).
- Suggestion: n/a
- Status: fixed
