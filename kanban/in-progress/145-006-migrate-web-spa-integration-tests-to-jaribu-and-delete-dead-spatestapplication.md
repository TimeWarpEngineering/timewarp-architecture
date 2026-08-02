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

- [ ] Suite green under Jaribu; fakes/composition semantics unchanged
- [ ] Dead path + registration deleted; direct consumer migrated
- [ ] Partial-graph evaluated with verdict documented
- [ ] Before/after wall-clock recorded; dev build 0/0; full dev test; kanban committed
