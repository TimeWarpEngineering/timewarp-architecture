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

- [x] Remaining suites migrated; pins/conventions removed
- [x] dev test Fixie branch resolved; docs swept
- [x] dev build 0/0; host-free suites green; template-smoke; audit clean; kanban committed

- **In-scope cleanup (routed from 145-006 round-2, 2026-08-02):** delete the now-orphaned `TimeWarpTestingConvention` class (tests/common/timewarp-testing/testing-convention/) — zero consumers repo-wide since SpaTestConvention was deleted. **Done.**

## Notes

### Implementation plan (Phase 2)

See commit history — mechanical Jaribu MTP conversion of 11 suite projects + shared library cleanup.

### Wall-clock / suite counts (migration verification)

| Suite | Result |
|-------|--------|
| foundation-domain-tests | 37 pass |
| foundation-application-tests | 13 pass |
| foundation-contracts-tests | 13 pass |
| foundation-infrastructure-tests | 11 pass |
| web-contracts-tests | 38 pass |
| web-domain-tests | 26 pass |
| web-infrastructure-tests | 39 pass |
| timewarp-identity-tests | 169 pass |
| agent-identity-cli-tests | 11 pass |
| analyzers-tests | 102 pass |
| sourcegenerator-tests | 59 pass |

## Session

- Implementation + review: 145-007 Fixie retirement

## Results

### Summary

**Zero Fixie** in the monorepo: all former Fixie suite-shaped projects are Jaribu MTP with
project-local `global.json`; `TimeWarpTestingConvention` and `[NotTest]` removed; CPM pins for
`Fixie` / `Fixie.TestAdapter` / `TimeWarp.Fixie` deleted; `dev test` always uses bare
`dotnet test -c Release` with cwd = project directory (MTP-only, task 145-007).

### Verification

| Gate | Result |
|------|--------|
| `dev build` | 0/0 |
| `ganda repo audit` | PASS (after cleaning stale smoke artifacts that still listed Fixie PackageReferences) |
| `dotnet run tools/dev-cli/dev.cs -- template-smoke` | **SUCCEEDED** (SmokeDefault + SmokeNoApi; zero Fixie packages in generated apps) |
| Host-free suites (spot / migration) | All green (see table above) |
| Package refs | No Fixie PackageReference/PackageVersion remain |

### Review

Effort 1, **clean** — `review/`

- Round 1 general: 0 bugs; fixed M1–M4 (stale comments, overview label, gate evidence)
- Paths: `review/review-framework.md`, `review/round-1/{general,merged}.md`, `review/disposition.md`

### Follow-ups (out of scope)

- Full sequential `dev test` wall-clock (145-008); optional `bin/dev` self-install so AOT matches
  current harness expectations without `dotnet run tools/dev-cli/dev.cs`
- Principal-store shared abstract suite could be reshaped to static helpers (wrappers remain)
