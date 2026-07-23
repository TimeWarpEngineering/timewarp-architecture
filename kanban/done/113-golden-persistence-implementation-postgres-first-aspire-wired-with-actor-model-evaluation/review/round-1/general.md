# Round 1 — general
**Date:** 2026-07-23
**Scope reviewed:** remaining 113 golden path (GoldenDbContext, Profile EF, docs)

## Summary

The remaining golden path (113-003/004/005) is coherent and correctly implemented end-to-end. `GoldenDbContext` closes the child→root SaveChanges gap for the OwnsMany + `WithOwner()` / no CLR back-nav shape, fail-closed missing Version, and store-side Version increment; Profile hosts the two-party concurrency half (`.IsConcurrencyToken()` + access-mode pin) with mapping and live Postgres coverage; ADR-0009 + HowToAddYourAggregate match the code and settled soft-gate decisions. Soft-skip of live Postgres tests without Docker is an acceptable residual (model-mapping always runs; GHA ubuntu runners have Docker; task notes document the skip). Foundation packaging expands EF onto `TimeWarp.Foundation.Infrastructure` by design (decision 5); helpers stay private and Npgsql stays host-only.

## Issues

### Issue 1 — Severity: suggestion
- File: tests/container-apps/web/web-infrastructure-tests/profile-postgres-persistence-tests.cs:90-101
- Description: Live round-trip / concurrency tests soft-skip (print + return success) when neither a connection string nor Docker/Testcontainers is available. That is fine for local agent machines, but if CI ever runs without a working Docker daemon the suite still greens while concurrency is never exercised. Model-mapping tests prove `.IsConcurrencyToken()` is configured; they do not prove `DbUpdateConcurrencyException` at the store.
- Suggestion: When `CI` (or an equivalent gate env) is set and availability resolution fails, fail the test instead of soft-skipping. Keep soft-skip for interactive local runs. Optionally narrow the catch filter (drop broad `InvalidOperationException` / `NotSupportedException`) so real Testcontainers misconfiguration is not reclassified as "no Docker."
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/features/profile/profile-entity-type-configuration-infrastructure.cs:53-55 (contract); source/foundation/foundation-infrastructure/persistence/golden-db-context.cs:23-26 (hook half)
- Description: The two-party Version contract is documented well (ADR, how-to, Design regions) and proven for Profile, but new aggregates still rely on memory for the host half (`.IsConcurrencyToken()` on `Version`). AGENTS.md prefers analyzers when two things must agree; ADR-0009 already lists silent LWW without the host half as a negative consequence. Nothing in the build fails a mapped `IAggregateRoot` that omits the concurrency token.
- Suggestion: Follow-on analyzer (or model convention in `GoldenDbContext.OnModelCreating` that auto-applies `IsConcurrencyToken` for every mapped `IAggregateRoot.Version`) so the host half cannot drift. Auto-apply is stronger (closes the two-party gap entirely); an analyzer is less behavior-changing for intentional non-token Version uses.
- Status: open

### Issue 3 — Severity: nit
- File: tests/foundation/foundation-infrastructure-tests/golden-db-context-tests.cs:29-48
- Description: Child→root coverage is excellent for the critical OwnsMany + no CLR back-nav rewrite path (Version + invariants). Design also claims deleted children still resolve to a live root so Version moves; that path is untested. Non-ownership FK and reference-navigation fallbacks are similarly untested (acceptable for Profile teaching, which has no children).
- Suggestion: Optional harness cases for (a) delete owned child → root Version++, (b) add owned child after initial save → root Version++. Not blocking for 113 closeout.
- Status: open
