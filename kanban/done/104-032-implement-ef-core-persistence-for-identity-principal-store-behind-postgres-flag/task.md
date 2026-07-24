# Implement EF Core persistence for identity principal store behind postgres flag

## Description

Identity is currently in-memory only: `IPrincipalStore` → singleton `InMemoryPrincipalStore`
(`web-infrastructure-module.cs`), no EF Core anywhere in timewarp-identity. Every web-server
restart wipes all principals, passkey credentials, and agent keys — discovered concretely during
task 112's public share: the first external passkey registration (colleague via
arch.timewarp.work, 2026-07-21) evaporated on the next `dev run` restart; their authenticator
still holds the client half, so sign-in fails as "unknown credential" until re-registration.

Goal: EF Core-backed `IPrincipalStore` (and the other identity stores that must survive
restarts) behind the template's `postgres` flag, with in-memory remaining the no-flag default so
the template still runs with zero infrastructure.

## Requirements

- EF persistence lives where the postgres flag already gates EF Core; the timewarp-identity
  library stays persistence-free (store interfaces only) — implementation goes in the
  infrastructure layer alongside the existing EF wiring.
- Same store semantics the in-memory implementation pins down: atomic handle-uniqueness
  (InMemoryPrincipalStore.HandleIndex under WriteLock has a documented contract in
  complete-passkey-registration-handler), optimistic concurrency surfacing as
  `ConcurrencyConflictException`, and byte[]-keyed credential lookups.
- Inventory which stores need durability vs deliberately ephemeral: `IPrincipalStore` (durable),
  `IAgentTokenStore` (short-lived tokens — decide), `IWebAuthnChallengeStore` (ephemeral by
  design — stays in-memory).
- Aggregate mapping respects the golden aggregate pattern (106) and private `Invariants`
  validators (TWA0011/0012) — no EF-driven shape leaks into the domain model.
- Migrations story consistent with the template's existing postgres flag content.
- Existing 168 identity unit tests keep running against in-memory; add integration coverage for
  the EF store (store-contract test suite runnable against both implementations would be ideal —
  one suite, two fixtures).
- Registration: flag-gated DI so `postgres` template output persists and no-flag output keeps
  the singleton in-memory store.

## Checklist

- [x] **Durability inventory** — principal/credentials durable; agent tokens + both challenge
      stores ephemeral (in-memory); decisions in Notes + store Design region
- [x] **InternalsVisibleTo** — `web-infrastructure` + `web-infrastructure-tests` can call `Snapshot`
- [x] **EF mapping** — `identity.principals` + `identity.credentials`; TypedIds; bytea handle/
      material with field access; unique `(Type, Handle)`; Version `.IsConcurrencyToken()`
- [x] **`EfPrincipalStore`** — full `IPrincipalStore` parity (snapshot-on-get, CAS Version,
      first-credential conditional tier, handle uniqueness, immutability checks)
- [x] **DbContext** — DbSets; configs via `ApplyConfigurationsFromAssembly`
- [x] **DI** — in-memory default; PostgresDbModule swaps scoped EF store only when connection present
- [x] **Template** — `!postgres` excludes EF store file; smoke both flag states
- [x] **Store-contract dual-fixture** — shared cases; in-memory + EF (Testcontainers CI fail-closed)
- [x] **Model-only tests** — schema/TypedId/concurrency/unique index without Docker
- [x] **Docs** — HowToAddYourAggregate + ADR-0009 store-CAS note; module Design regions
- [x] **Non-goals** — no IAggregateRoot conversion; no token/challenge EF; no migrations mandate;
      no library EF deps
- [x] `dev build` 0/0 and relevant tests green

## Notes

- Origin: task 112 public share made restart-wipes externally visible; blocks meaningful use of
  [[104-010-implement-credit-ledger-keyed-by-principalid]] and
  [[104-013-wire-payment-settle-to-funded-trust-tier-and-credit-balance]] (balances keyed by
  PrincipalId must survive restarts before anyone pays into them).
- Related: [[104-005]] (credential list/revoke UX assumes credentials persist),
  [[104-014-agent-end-to-end-path-register-key-then-pay-then-call-with-quota-token]].
- Unblocked by [[113-golden-persistence-implementation-postgres-first-aspire-wired-with-actor-model-evaluation]]
  (GoldenDbContext, Profile teaching path, dual-fixture pattern, ADR-0009).

### Implementation plan (Phase 2, 2026-07-24)

**Principle:** first product durable consumer of the 113 golden EF path; identity stays a
**port-backed store** (not direct-DbContext teaching aggregate like Profile). Library remains
EF-free.

#### Durability inventory

| Store | Survive restart? | Decision |
|-------|------------------|----------|
| `IPrincipalStore` (Principal + Credential incl. agent keys) | Yes | **EF behind postgres** |
| `IAgentTokenStore` (~15 min bearer grants) | No | **In-memory** (Redis later if multi-replica) |
| `IWebAuthnChallengeStore` / `IAgentKeyChallengeStore` | No | **In-memory** |

#### Version authority (soft-gate silence = accept)

**Store-CAS + Snapshot** — do **not** make Principal/Credential `IAggregateRoot` this task.
Store owns `EntityVersion.Next` / `ConcurrencyConflictException`. Host maps
`.IsConcurrencyToken()` as DB race belt. Avoids double-bump with GoldenDbContext.

#### Mapping

- Schema `identity`; tables `principals`, `credentials` (independent entities, not OwnsMany)
- Unique `(Type, Handle)` for atomic handle uniqueness
- Field access for byte[] Handle/PublicMaterial
- InternalsVisibleTo web-infrastructure (+ tests) for `Snapshot`

#### Code homes

- Configs: `features/identity/*-entity-type-configuration-infrastructure.cs`
- Store: `web-infrastructure/persistence/ef-principal-store.cs` (+ template `!postgres` exclude)
- DI: WebInfrastructureModule always InMemory; PostgresDbModule swaps scoped EfPrincipalStore
  when connection present (skip-mode)

#### Dual-fixture tests

- Shared abstract contract (refactor existing in-memory tests)
- In-memory fixture in timewarp-identity-tests
- EF fixture in web-infrastructure-tests (Testcontainers; CI fail-closed like Profile)

#### Out of scope

IAggregateRoot conversion; token/challenge EF; migrations mandate; Orleans/outbox; library EF;
IPrincipalStore API changes.

#### Soft-gates (proceed unless vetoed)

1. Store-CAS not Golden-owned Version
2. Tokens stay in-memory
3. InternalsVisibleTo not public rehydrate API
4. Schema name `identity`

## Results

**Completed 2026-07-24** — EF-backed `IPrincipalStore` behind postgres; first product durable
consumer of the 113 golden path. Library stays EF-free; tokens/challenges remain in-memory.

### What was implemented
- `EfPrincipalStore` with full in-memory parity (snapshot-on-get, store-CAS Version,
  first-credential tier bump, handle uniqueness, immutability checks)
- EF mapping: schema `identity`, tables `principals`/`credentials`, TypedIds, bytea field access,
  unique `(Type, Handle)`, Version `.IsConcurrencyToken()`
- DI: always in-memory default; PostgresDbModule swaps scoped EF store when connection present
- Template `!postgres` excludes `ef-principal-store.cs`
- Dual-fixture store-contract suite + model-only mapping tests
- Docs: HowToAddYourAggregate, ADR-0009 store-CAS note, Design regions

### Key decisions
- Version authority = **store-CAS + Snapshot** (not IAggregateRoot / Golden-owned Version)
- Agent tokens + challenge stores stay ephemeral in-memory
- InternalsVisibleTo for Snapshot (no public rehydrate API)

### Files (primary)
- `web-infrastructure/persistence/ef-principal-store.cs`
- `features/identity/{principal,credential}-entity-type-configuration-infrastructure.cs`
- `postgres-db-context.cs`, `postgres-db-module.cs`, `web-infrastructure-module.cs`
- `timewarp-identity.csproj` InternalsVisibleTo
- Dual-fixture tests under `timewarp-identity-tests` + `web-infrastructure-tests`
- HowToAddYourAggregate + ADR-0009

### Tests
- `dev build` 0/0
- `timewarp-identity-tests` — **169 passed**
- `web-infrastructure-tests` — **39 passed** (EF contract on Testcontainers)

### Phase 4b review
- Effort 1 (general); 1 round under `review/`
- Final counts: **0 open** (no findings)
- Disposition: **clean** (`review/disposition.md`)
- Paths: `review/review-framework.md`, `review/round-1/{general,merged}.md`, `review/disposition.md`

### Commits
- `f6d80f3f` feat(identity): persist principals via EF behind postgres flag
- `f10e3b81` test(identity): dual-fixture IPrincipalStore contract suite
- `3ff78687` docs: record identity store-CAS path and dual-fixture pattern

## Session

- Created: 2026-07-21 (spun out of 112 share observations)
- Plan: 2026-07-24 (orchestrator + plan agent; unblocked by 113)
- Implementation + review disposition: 2026-07-24
