# Update AGENTS.md and skills for zero-Fixie north star

## Description

State the locked north star (parent 145; decision record kanban/done/143-research-aspire-and-jaribu-assembly-fixture-strategy-for-zero-fixie/findings.md §6) in the docs BEFORE migrations
start, so no agent extends Fixie/xUnit in the meantime.

## Requirements

1. AGENTS.md Stack/testing bullets: replace "host-level `tests/` suites stay Fixie +
   Shouldly" and "migrate last or never" with the decided policy — single-framework Jaribu
   target; existing Fixie suites migrate per epic 145 (suite-shrinking hybrid topology:
   slice-shaped tests co-locate, host-level remainder stays suite-shaped); aspire-tests xUnit
   is a known deviation being removed (145-003); do NOT extend Fixie or xUnit.
2. AGENTS.md: two-lane Aspire statement (in-proc lane for DI-substitution/pipeline;
   closed-box lane for topology/process isolation; fixed ports live in the in-proc lane only).
3. skills/tw-feature-placement: C-create fixture model note in the co-located preamble
   section (per-class SetupOnce creates its OWN graph via HostGraphFactory — 145-002 — and
   CleanUpOnce disposes it; never share hosts via process statics; Testcontainers-postgres
   Lazy is the documented no-dispose exception).
4. Reconcile the adopting-jaribu migration-policy wording wherever else it appears
   (documentation/developer/standards, tw-jaribu pointer note).

## Checklist

- [ ] AGENTS.md testing/stack + enforcement wording updated
- [ ] tw-feature-placement C-create note added
- [ ] Standards docs reconciled
- [ ] dev build 0/0; ganda repo audit clean; kanban committed
