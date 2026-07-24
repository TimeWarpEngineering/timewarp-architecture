# Round 1 — general
**Date:** 2026-07-24
**Scope reviewed:** 104-032 EF IPrincipalStore + dual-fixture

## Summary

Phase 4b meets the accepted plan leans. `EfPrincipalStore` is a faithful port of
`InMemoryPrincipalStore` store-CAS semantics (snapshot-on-get, `EntityVersion.Next` on successful
Update*, caller instance not advanced, conditional first-credential Provisional→Keyed bump,
handle uniqueness via pre-check + unique `(Type, Handle)`, Type/Handle immutability on update,
list ordered by `CreatedAt`). Principal/Credential stay non-`IAggregateRoot`, so
`GoldenDbContext` does not auto-increment Version — no double authority with the golden hook.
Mapping still pairs `.IsConcurrencyToken()` as the DB race belt; `DbUpdateConcurrencyException`
is translated to `ConcurrencyConflictException`.

DI is correct: `WebInfrastructureModule` always registers singleton in-memory; `PostgresDbModule`
runs after it, skip-mode returns early with no connection string, and only when connected does
`RemoveAll<IPrincipalStore>()` + scoped `EfPrincipalStore`. No singleton was found capturing
`IPrincipalStore` (handlers/auth scheme resolve per request). Tokens/challenge stores remain
in-memory. Library stays EF-free; `InternalsVisibleTo` for `web-infrastructure` /
`web-infrastructure-tests` is the soft-gated Snapshot surface (no public rehydrate API).

Template `!postgres` excludes `ef-principal-store.cs` with the other postgres host files; identity
entity configs remain (same pattern as Profile). Dual-fixture shared contract + in-memory fixture +
EF Testcontainers fixture (CI fail-closed) and connection-free model-mapping tests cover the
parity surface. Docs (HowToAddYourAggregate §6, ADR-0009) accurately describe store-CAS vs golden
Version and the dual-fixture pattern.

## Issues

_None._
