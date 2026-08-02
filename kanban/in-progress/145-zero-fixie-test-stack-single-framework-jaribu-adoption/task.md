# Zero-Fixie test stack: single-framework Jaribu adoption

## Description

Epic (parent). **Decision locked (Steve, 2026-07-31, task 143):** single-framework Jaribu is
the north star — zero Fixie AND zero xUnit — with the **C-create + A** lifetime model
(per-class-owned host graphs from an explicit ordered factory in timewarp-testing; C-share +
Jaribu MTP session hooks deferred until aggregator cost data demands), **hybrid migration
topology** (product-slice tests co-locate; topology/cross-service suites stay suite-shaped and
SHRINK as slice tests move out), and the **two-lane Aspire role** (in-proc hand-rolled host
lane for mediator/DI-substitution tests; closed-box Aspire.Hosting.Testing lane for
topology/process-isolation tests).

Decision record + full evidence: `kanban/done/143-research-aspire-and-jaribu-assembly-fixture-strategy-for-zero-fixie/findings.md`
(headline: TimeWarp.Fixie's assembly sharing is per-class in reality — decompile-verified).

## Children (dependency order)

- 145-001 docs first (AGENTS.md/skills state the north star)
- 145-002 C-create HostGraphFactory (unblocks 004)
- 145-003 aspire-tests xUnit→Jaribu (smallest; kills third framework)
- 145-004 web-server-integration-tests →Jaribu+C-create (largest; proves model)
- 145-005 api-server-integration-tests two-lane consolidation
- 145-006 web-spa migration + dead-path deletion
- 145-007 retire TimeWarp.Fixie (last)
- 145-008 (backlog, data-gated) Jaribu MTP session hooks
- 145-009 (backlog, decision) runtime-config-gated mock auth

## Checklist

- [ ] All non-backlog children done
- [ ] Backlog children resolved (done or archived-with-reason)

## Session

- Created: fe3c947a-a536-495b-88dd-794216a1fa8e (2026-07-31, from task 143 decision)
