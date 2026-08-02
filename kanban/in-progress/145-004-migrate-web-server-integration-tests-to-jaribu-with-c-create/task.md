# Migrate web-server-integration-tests to Jaribu with C-create

## Description

At-scale proof (parent 145; findings §7.3). DEPENDS ON 145-002 (factory). Hybrid topology:
slice-shaped co-locate where practical; host-level remainder stays suite-shaped under Jaribu.

## Checklist

- [x] Triage table — `triage.md`
- [x] Co-located hello runfile pass standalone + aggregator (7)
- [x] Host-level remainder green under Jaribu (suite 95 pass)
- [x] Fixie plumbing removed; smoke web count 7
- [x] Before/after wall-clock; build 0/0; audit pass; review clean

## Session

- 2026-08-01: implement (worker) + verify + disposition clean

## Results

### Summary

web-server-integration-tests is Jaribu MTP with C-create `HostGraphFactory.CreateWebWithApiAsync`
per host-using class. Hello endpoint co-located. Fixie convention deleted. Suite does not dissolve
(identity/BFF host-level remainder stays).

### Wall-clock (145-008 data)

| | Before (Fixie) | After (Jaribu MTP) |
|--|----------------|---------------------|
| Pass | 97 + 1 skip | 95 + intentional RunForever skip |
| Wall | ~31s | ~24s |
| Test duration (runner) | ~6s | ~7s |

Coverage parity: 95 suite + 2 hello co-located = 97.

### Verification

| Gate | Result |
|------|--------|
| suite `dotnet test` | 95 succeeded |
| hello standalone | 2/2 |
| web-jaribu-tests | 7/7 |
| solution build | 0/0 |
| ganda repo audit | PASS |

### Review

Effort 1, **clean** — `review/`

## Results (final, round-3)

Reopened after round-2 refuted round-1 (SmokeNoApi 21x CS0117; orphaned CPM pins failing
audit). Fix (effa18a1, 095bb0f0, merged to dev): HostGraphFactory.CreateWebAsync added and
all 21 call sites branch on the api flag — census showed 100% of the suite is
web-only-meaningful (zero Graph.Api uses; BFF api HttpClient is SPA-client-side only), so
the suite now degrades to web-only under --api false exactly as the Fixie DI wiring did;
orphaned xUnit pins removed (audit 23/23); hello-tests.cs added to smoke tiers 1/2.
Migration substance from round 2 stands: method-level parity 97→95+2 with zero silent
losses, triage reasoned, wall-clock faster than Fixie (~24s vs ~31s — recorded for the
145-008 gate). Final gates green twice (fix worktree ×3 smoke runs; orchestrator re-run on
merged dev). Side artifacts: ganda 197 filed (cpm-consistency scans stale artifacts/).
Review: 3 rounds, disposition clean.
