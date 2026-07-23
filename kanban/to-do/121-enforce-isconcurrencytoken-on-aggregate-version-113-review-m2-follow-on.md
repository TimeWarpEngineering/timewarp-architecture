# Enforce IsConcurrencyToken on aggregate Version (113 review M2 follow-on)

## Description

GoldenDbContext's concurrency story is a **two-party contract** (ADR-0009): the golden
SaveChanges hook increments `Version` on Modified aggregate roots, but the optimistic WHERE
clause only exists if the host's entity configuration also maps
`.Property(x => x.Version).IsConcurrencyToken()` (ProfileEntityTypeConfiguration does).
Forget the mapping and the increment is silent bookkeeping — lost updates ship with green
builds and green tests. This is exactly the agreement-by-memory class the repo's standing
directive says to make build-breaking (see AGENTS.md "Prefer analyzers/source generators
over convention-by-memory").

113 review finding M2 was dispositioned wontfix-in-113 with this follow-on promised; this
task is that follow-on.

## Options to decide (with Steve) before building

1. **Runtime model check in GoldenDbContext** (likely simplest): on model finalization
   (`OnModelCreating` end or a model-finalizing convention), walk entity types implementing
   `IAggregateRoot` and throw if `Version` is not a concurrency token. Fails at startup and
   in every integration test that touches the context — loud, no new analyzer plumbing,
   works identically in package mode. Con: not compile-time.
2. **Roslyn analyzer (TWA00xx)**: flag an `IEntityTypeConfiguration<T>` for an
   `IAggregateRoot` whose Configure lacks an `IsConcurrencyToken()` call on Version.
   Compile-time, but syntactic matching is brittle (helper methods, base configurations)
   and it can't see conventions applied elsewhere.
3. **Both**: analyzer as fast feedback, model check as ground truth.

Recommendation to discuss: option 1 first (ground truth, cheap); add the analyzer only if
the runtime failure proves too late in practice.

## Checklist

- [ ] Decide enforcement point with Steve (runtime model check vs analyzer vs both)
- [ ] Implement; failure message must name the entity and show the exact mapping line to add
- [ ] Test: aggregate root mapped WITHOUT IsConcurrencyToken → loud failure; WITH → passes
- [ ] Update ADR-0009 + HowToAddYourAggregate to say the contract is now enforced, not remembered
- [ ] `dev build` 0/0, `dev test`, `dev template-smoke` green

## Notes

Origin: task 113 review round 1, finding M2 (accepted-exceptions disposition, follow-on
promised but not filed at the time — filed during the independent round-2 review).
GoldenDbContext: `source/foundation/foundation-infrastructure/persistence/golden-db-context.cs`
(namespace TimeWarp.Foundation.Persistence — ships in the TimeWarp.Foundation.Infrastructure
package, so a runtime check lands in generated apps with no template edit).
Exemplar mapping: `source/container-apps/web/features/profile/profile-entity-type-configuration-infrastructure.cs`.
