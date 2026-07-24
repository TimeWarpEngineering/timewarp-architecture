# Round 2 — independent post-hoc review (orchestrator)

**Date:** 2026-07-24
**Scope:** Grok's full 104-032 implementation (7 commits on dev: EfPrincipalStore, identity
entity configs, DI swap, dual-fixture contract suite, docs). Round 1 was the build agent's
self-review (clean); this round is the independent verification pass with empirical re-runs.

## Verified green (re-run, not taken from Results)

- `dev build` — 0 warnings / 0 errors
- `dev template-smoke` — **SUCCEEDED, both matrices** (SmokeDefault + SmokeNoPostgres).
  Grok's verification omitted this gate again, but this time the template seam was done
  right: `ef-principal-store.cs` was added to the `(!postgres)` exclude, and the
  postgres-db-context edits kept the 115 `<Using>` pattern intact.
- identity-tests — 169/169; web-infrastructure-tests — 39/39 (31 EF contract cases against
  live Postgres via Testcontainers + identity model-mapping + Profile suites; Docker present,
  so the live path actually ran)
- web-server-integration-tests — 97 passed / 1 skipped (skip-mode DI: in-memory store, all
  identity ceremony endpoints) — not in Grok's verification list
- aspire-tests — 7/7 — not in Grok's verification list, and the one that matters most here:
  full Aspire boot **with** postgres means the scoped `EfPrincipalStore` registration is
  actually resolved through real DI on the identity endpoints. This empirically closes the
  singleton→scoped lifetime risk introduced by the swap (in-memory was a singleton; a
  singleton consumer would have blown up here).

## Analysis checks (independent)

- **Test coverage in the refactor:** all 31 test-method names from the two deleted in-memory
  suites exist in the new shared contract suite — nothing was dropped in the consolidation.
- **DI ordering:** `WebInfrastructureModule` (program.cs:154) registers the in-memory default
  before `PostgresDbModule` (program.cs:157) conditionally replaces it — correct, and the
  ordering dependency is documented on both sides. No root-provider or singleton consumer of
  `IPrincipalStore` exists in product code (the two `GetRequiredService<IPrincipalStore>`
  sites are in integration tests that run skip-mode).
- **Store semantics:** EfPrincipalStore matches the in-memory CAS contract (snapshot-on-get,
  Next-on-update, caller instance not advanced, conditional tier-bump on first credential,
  Type/Handle immutability, CreatedAt ordering). Round 1's claims check out line-by-line.

## Findings (observations, none blocking)

1. **Process (repeat):** verification again omitted `dev template-smoke` plus the
   web-server-integration and aspire suites. Green this time, but the 113 round-2 lesson
   stands: template-content diffs are not done until smoke runs; DI-lifetime changes are not
   done until a postgres-connected host boots.
2. **Rode-along dependency bump:** Fixie 4.1.0 → 4.2.0 landed inside 113-004's commit
   (b70b0616) while `.config/dotnet-tools.json` still pins fixie.console 4.1.0
   (`rollForward: false`). Currently harmless (all Fixie suites run), but tool/package drift
   deserves its own commit next time. (Side note discovered here: aspire-tests is xunit, not
   Fixie — `dotnet test`, not `dotnet fixie`, is its runner. Pre-existing, from 117.)
3. **`IsUniqueViolation` breadth:** matches any exception text containing "unique"
   (case-insensitive) to avoid an Npgsql type dependency. A non-unique DbUpdateException
   whose message happens to contain "unique" would be mistranslated to the duplicate-handle
   error. Low likelihood; acceptable as-is.
4. **Fabricated actual version on true races:** `TranslateConcurrency` reports
   `expected + 1` as the actual version (real value unknown without a reload) — documented
   in-line; in-memory reports the true actual. Divergence only surfaces in a genuine
   write-race window, where the caller retries anyway.

## Verdict

Clean disposition upheld. Parity, template seam, DI swap, and test architecture are all
sound, and every claimed number reproduced exactly. 104-032 stays done.
