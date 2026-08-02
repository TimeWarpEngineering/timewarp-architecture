# Migrate web-spa-integration-tests to Jaribu and delete dead SpaTestApplication

## Description

Heaviest suite (77.57s test / 2:13 wall for 11 tests — full Aspire graph per class) + a dead
competing host path (parent 145; kanban/done/143-research-aspire-and-jaribu-assembly-fixture-strategy-for-zero-fixie/findings.md §4). Depends on 145-003 pattern.

## Requirements

1. BaseTest chain → Jaribu: SetupOnce-owned DistributedApplication + AspireSpaTestApplication
   (SPA's own ServiceCollection composition is NOT Aspire-constrained — keep the
   IJSRuntime/IAccessTokenProvider fakes and toast-handler removal exactly as-is);
   per-test scope creation moves from BaseTest ctor to an explicit helper.
2. DELETE the dead `SpaTestApplication<TVia,TProgram>` class + its TimeWarpTestingConvention
   registration; migrate its one direct consumer (pipeline/clone-state-behavior-tests.cs).
3. Evaluate partial-graph startup (WithExplicitStart / conditional resources) to avoid
   booting grpc/postgres for SPA state tests — adopt if it works, document if not.
4. SpaTestConvention/Fixie wiring deleted; record before/after wall-clock (145-008 gate data).

## Checklist

- [x] Suite green under Jaribu; fakes/composition semantics unchanged
- [x] Dead path + registration deleted; direct consumer migrated
- [x] Partial-graph evaluated with verdict documented
- [x] Before/after wall-clock recorded; dev build 0/0; full SPA suite green; kanban committed

## Notes

### Wall-clock (145-008 gate data)

| Lane | Result | Wall |
|------|--------|------|
| Before (Fixie, Release) | 11 pass, 3 skip | **95.15s** |
| After (Jaribu MTP, first green) | 11 pass, 2 skip | **118.63s** |
| After review R1 (skip-only class no SetupOnce) | 11 pass, 2 skip | **108.64s** |

Jaribu is not faster here: clone-state now boots full Aspire (was dead SpaTestApplication→Yarp
in-proc), and every host class still pays full-graph SetupOnce. Gate data for 145-008; do not
treat wall increase as a regression of the framework switch alone.

### Partial-graph verdict: **not adopted**

AppHost `WaitFor` chains (web→postgres-db, ingress→backends) and ingress fidelity mean SPA state
tests that exercise the AspireSpaTestApplication HttpClient still need web + api + ingress (+
postgres for web). Skipping grpc alone is a small share of boot vs postgres/web. WithExplicitStart
without AppHost/model support for “SPA-only subgraph” risks WaitFor hangs. Keep full graph +
`Postgres:UseDataVolume=false` (same as SpaTestConvention). Revisit only if AppHost gains a
first-class test profile for pruned resources.

### Implementation notes

- `SpaTestScope` + `SpaIntegrationHost` replace Fixie BaseTest ctor DI.
- After Send, re-fetch state via `Store.GetState` (dispatch replaces state instances).
- Generic `Send<TRequest>(TRequest)` so Mediator resolves the concrete handler type.
- Toast ExceptionNotification handler removed on AspireSpaTestApplication (from deleted path).
- `ISpaTestApplication` kept in timewarp-testing; class SpaTestApplication deleted.

## Session

- Implementation + review: 145-006 SPA Jaribu migration (this session)

## Results

### Summary

`web-spa-integration-tests` is Jaribu MTP (project-local `global.json`, no Fixie). Host classes use
`SpaIntegrationHost` SetupOnce + `AspireSpaTestApplication` + per-test `SpaTestScope`. Dead
`SpaTestApplication<TVia,TProgram>` and `SpaTestConvention` deleted; `ISpaTestApplication` kept.
clone-state migrated to Aspire path. Partial-graph **not adopted** (see Notes). Weather fetch remains
quarantined (task 058) without paying Aspire boot.

### Wall-clock (145-008)

| | Before (Fixie) | After (Jaribu + R1) |
|--|----------------|---------------------|
| Result | 11 pass / 3 skip | 11 pass / 2 skip |
| Wall (Release) | **95.15s** | **108.64s** |

### Verification

| Gate | Result |
|------|--------|
| SPA suite MTP | 11 pass, 2 skip |
| `dev build` | 0/0 |
| SpaTestApplication class | deleted (grep clean except history/interface) |

### Review

Effort 1, **accepted-exceptions** — `review/`

- Round 1 general: 0 bugs; fixed M1–M3 (dead Send API, skip-only SetupOnce, Skip message); wontfix M4
  (ingress reachability poll — same Healthy-only gate as pre-migration; re-open if CI EOFs)
- Paths: `review/review-framework.md`, `review/round-1/{general,merged}.md`, `review/disposition.md`

