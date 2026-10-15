# Round 1 — merged findings
**Date:** 2026-10-15
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tests/common/timewarp-testing/host-graph-factory.cs:196
- Description: IPv6 port check gives a false "in use" error when `::1` cannot be bound (IPv6 disabled).
- Suggestion: Skip the IPv6 probe on AddressNotAvailable.
- Source: general
- Disposition notes: Fixed. The IPv6 probe passes `required: false`, and `AddressNotAvailable` is
  ignored only for that probe. Every other error still fails loudly.

### M2 — Severity: suggestion — Status: fixed
- File: tests/common/timewarp-testing/postgres-test-availability.cs:161
- Description: Unbounded ConnectAsync in the host-port probe, and the container is not disposed after the final failed attempt.
- Suggestion: Add a per-connect timeout and dispose before rethrowing.
- Source: general
- Disposition notes: Fixed. Each connect has a 5 s CancellationToken, and cancellation retries until the
  30 s deadline. A retryable failure always disposes the container and rethrows on the last attempt.

### M3 — Severity: nit — Status: wontfix
- File: tools/dev-cli/services/test-parallelism.cs:570
- Description: The regex matches a commented-out Aspire.Hosting.Testing PackageReference.
- Source: general
- Disposition notes: wontfix (orchestrator). Over-inclusion only runs a suite in the serial lane,
  and the monorepo guard test pins the exact set.

## Duplicates / conflicts

- None.
