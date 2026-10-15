# Round 1 — general
**Date:** 2026-10-15
**Scope reviewed:** framework scope (test-command.cs, test-parallelism.cs, host-graph-factory.cs,
in-proc-test-ports.cs, postgres-test-availability.cs, dev-cli-tests and timewarp-testing-tests)

## Summary

The dispatcher change is small and correct: the AppHost flag gates only AppHost suites, is cleared
on completion, and cannot deadlock (with nothing running, the flag is false and every base is free).
`BootsAppHost` is derived and guarded by three monorepo tests. Default `-j` is clamped to the
8-slot ring, so the smaller ring cannot throw. Risk is in the two test-infrastructure hardenings.

## Issues

### Issue 1 — Severity: bug
- File: tests/common/timewarp-testing/host-graph-factory.cs:196
- Description: The new IPv6 check runs whenever `Socket.OSSupportsIPv6` is true, but a Linux host or
  container with `disable_ipv6=1` can still create IPv6 sockets and cannot bind `::1`
  (EADDRNOTAVAIL). Every in-proc host graph would then fail with a false "port already in use".
  Devcontainers for generated apps hit this.
- Suggestion: Treat `SocketError.AddressNotAvailable` on the IPv6 probe as "no IPv6 loopback" and skip it;
  keep every other SocketException as a collision.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/common/timewarp-testing/postgres-test-availability.cs:161
- Description: `WaitForHostPortAsync` calls `ConnectAsync` with no cancellation, so one attempt can
  block for the OS SYN timeout and the 30 s deadline is not bounded. A final failed attempt also
  leaves the started container undisposed (Ryuk eventually reaps it).
- Suggestion: Bound each connect with a CancellationToken and treat cancellation like a refused
  connect. Dispose the container on the final attempt before rethrowing.
- Status: open

### Issue 3 — Severity: nit
- File: tools/dev-cli/services/test-parallelism.cs:570
- Description: The `Aspire.Hosting.Testing` regex also matches a commented-out PackageReference element.
- Suggestion: None needed: false positives only serialize a suite, which is safe.
- Status: open
