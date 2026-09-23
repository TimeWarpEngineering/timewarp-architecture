# Round 1 — merged findings
**Date:** 2026-09-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

None raised. The general reviewer (round-1/general.md) verified by reading code and call sites:
`TryClaim` CAS loop race-safety and retained claim slot on a dropped write; the advisory
`ConcurrencyConflictException`-dropped / other-exceptions-propagate rule against
`RevokeCredential.Handler`'s retry loop, tested in both orderings at both seams; web and api bearer
handlers both call `RecordCoalescedAsync` after liveness with the recorder registered as a singleton
that takes `IPrincipalStore` per call; `AgentTokenGrant.CredentialId` threaded through
Issue/Validate/in-memory entry with no stale call sites; entity configuration, migration
`20260923110932_AddCredentialLastUsedAt`, and model snapshot agree (nullable timestamptz /
`DateTimeOffset?`); Purpose/Design regions reconciled (104-028 "zero Update* calls" notes
superseded); no template-flag regions touched; Jaribu + Shouldly only; kebab-case; no public setter
on `LastUsedAt`; SPA `RelativeTime` boundaries and future instants covered, absolute fallback uses
`CultureInfo.InvariantCulture`; tests cover every bullet of the task's Tests requirement.

Review-oracle spot-checks, independent of the reviewer: the ceremony handlers make no other
`UpdateCredentialAsync` call before `RecordAsync`, so the ceremony stamp cannot lose to an earlier
write from its own handler; the only `#if` in touched runfiles is the standard `#if !JARIBU_MULTI`
preamble; `dotnet run source/container-apps/web/features/identity/credential-last-used-tests.cs`
reports 4/4; `dotnet test -c Release -- --filter-class CredentialUsageRecorder` in
`tests/libraries/timewarp-identity-tests` reports 9/9.

## Duplicates / conflicts

None (single reviewer).

## Noted, not a finding

`RecordAsync` sets the coalescing slot before the store write, so a non-conflict store failure on a
ceremony write suppresses the coalesced per-request write for one interval. The ceremony call itself
already fails outright on that exception and the Design region documents the shared window; no change
requested.
