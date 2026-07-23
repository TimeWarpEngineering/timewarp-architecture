# Dual actor spike — comparison (113-002)

Spike branch `spike/113-002-dual-actor` @ `fe0296e` (worktree torn down; branch retained).
Same golden ledger aggregate (nested private Invariants, Entity Version token, golden SaveChanges
hook, IsConcurrencyToken) driven three ways over ephemeral Postgres. All results re-verified
independently by the orchestrator.

## The demonstrations (N=50 parallel debits, one principal, seed 1000)

| Path | Conflicts | Balance | Version | Events |
|---|---|---|---|---|
| Plain EF baseline | 404–795 retries across runs | 950 ✓ | 50 ✓ | 50 ✓ |
| Akka.NET 1.5.70 actor | **0** (structural assert) | 950 ✓ | 50 ✓ | 50 ✓ |
| Orleans 10.2.2 grain | **0** (structural assert) | 950 ✓ | 50 ✓ | 50 ✓ |

Both candidates went green in ~1 hour each (day-caps untouched); hosting APIs compiled
first-try from docs for both.

## Measured per-candidate facts

| Axis | Akka.NET | Orleans |
|---|---|---|
| Glue code | 4 files / ~114 lines (incl. hand-written child-per-principal coordinator + message record) | 3 files / ~88 lines (runtime maps id→grain; interface method IS the contract) |
| New concepts | ~12 | ~9 (incl. `[GenerateSerializer]`/`[Id]`) |
| Activation | no async PreStart → lazy-load on first message | real async `OnActivateAsync` → eager load |
| Domain-error propagation | **swallowed by default**: overdraw → caller gets `AskTimeoutException`, real exception only in actor log (fixable via Status.Failure envelope — idiom tax, measured) | real `InvalidOperationException` with `Ledger.Debit` in the caller's stack, out of the box |
| .NET 10 | forward-compat via net6.0 TFM (worked cleanly) | first-class net10.0 TFM |
| Deps hygiene | transitively pulls OpenTelemetry.Api 1.10 → 4 NU1902 advisories | clean, 0 warnings |
| AOT posture | HOCON + reflection Props — not AOT-oriented | source-gen serializers — AOT-friendly (latent only: shared EF + DomainInvariantsGuard reflection cap both) |
| Aspire | no first-party integration (only community Aaron.Akka.Aspire 0.1/0.2) | `Aspire.Hosting.Orleans` first-party, mature |
| Boundary tax | local Ask passes CLR refs — none | one 8-line `[GenerateSerializer]` DTO (grain boundary serializes even locally) |
| Startup | 148–1098 ms | 56–1417 ms — warmup noise, both sub-second warm; NOT a differentiator |

## IPersistentState verdict (1h box): SKIP under EF-only

Wrapping EF in `IPersistentState` needs a custom IGrainStorage, introduces a SECOND concurrency
token (Orleans ETag duplicating the golden Version) and a state-DTO mapping layer, for zero
benefit while EF is the store. Direct `IDbContextFactory` in the grain is strictly simpler and
keeps Version the single authority. IPersistentState earns its keep only if Orleans storage IS
the store — ruled out by axis 5.

## Substrate-residue proof

ledger-substrate has zero Akka/Orleans references (grep-verified); both hosts consume only the
aggregate + IDbContextFactory + IIntegrationEventPublisher and publish the identical event.
Deleting the losing candidate removes one folder + one solution entry.

## Findings for 113 proper

1. **Seam packaging (113 decision 5)**: sealed `PostgresDbContext` forced the spike to replicate
   the ~40-line golden SaveChanges hook (calling the same foundation seams). A non-sealed base or
   a foundation-packaged SaveChanges interceptor removes the duplication for every consumer.
2. **"No actors" stays fully viable**: the baseline PROVES plain EF + retry reaches the correct
   result; in-process, single-writer ≈ a keyed semaphore. The frameworks pay for themselves via
   distribution/lifetime — untested here by design.
3. **Honest bias**: single-process only; neither Akka Cluster.Sharding nor Orleans multi-silo
   placement exercised. Distributed single-writer would need its own spike.

## Implementer lean (labeled; NOT the decision — Steve gates per axis 5)

On single-node ergonomics as measured, Orleans fit better: less glue, real activation hook,
domain errors propagate, source-gen serializers, first-party Aspire, cleaner deps. Akka's costs
here were the hand-written router, the Ask-swallows-exceptions idiom, and reflection posture.
Explicitly NOT weighed: Akka's distributed-systems maturity, untested in-proc.
