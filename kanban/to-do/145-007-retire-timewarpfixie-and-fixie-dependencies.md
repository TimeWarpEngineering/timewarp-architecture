# Retire TimeWarp.Fixie and Fixie dependencies

## Description

Final child (parent 145): when 145-003..006 land and the remaining small Fixie suites
(web-contracts-tests, web-domain/infrastructure-tests, foundation-*, identity, analyzers,
agent-identity-cli) are migrated or explicitly re-scoped, remove Fixie from the repo.
NOTE: those small suites are mostly host-free — migrating them is mechanical Jaribu class
conversion; fold them into this task as its first requirement.

## Requirements

1. Migrate remaining host-free Fixie suites to Jaribu (mechanical: conventions → plain
   classes + [ModuleInitializer]; Shouldly already used).
2. Remove Fixie / Fixie.TestAdapter / TimeWarp.Fixie CPM pins + all TestingConvention files;
   remove xunit pins if 145-003 left any.
3. dev test glob semantics re-checked (all projects now MTP — the Fixie invocation branch in
   test-command.cs becomes dead; remove or keep documented, implementer's call with note).
4. AGENTS.md/docs sweep: no remaining Fixie references except historical kanban/analysis.
5. Template output check: generated apps get zero Fixie (template-smoke green).

## Checklist

- [ ] Remaining suites migrated; pins/conventions removed
- [ ] dev test Fixie branch resolved; docs swept
- [ ] dev build 0/0; full dev test; template-smoke ×3; audit clean; kanban committed

- **In-scope cleanup (routed from 145-006 round-2, 2026-08-02):** delete the now-orphaned `TimeWarpTestingConvention` class (tests/common/timewarp-testing/testing-convention/) — zero consumers repo-wide since SpaTestConvention was deleted.
