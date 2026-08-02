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

## Notes

### Implementation plan (Phase 2)

**Goal:** zero Fixie packages in the monorepo; every `tests/**/*.csproj` is Jaribu MTP.

#### A. Mechanical suite migration (11 projects)

For each project below, apply the same shape as `web-server-integration-tests` / `timewarp-testing-tests`:

1. **csproj:** drop `Fixie`, `Fixie.TestAdapter`, `TimeWarp.Fixie`; add `TimeWarp.Jaribu.TestingPlatform` +
   `Shouldly` (keep other packages); set `TestingPlatformDotnetTestSupport=true`,
   `IsTestProject=true`, `IsPackable=false`, NoWarn CA1707/CA1849/IDE0021/IDE0058 as needed.
2. **global.json:** SDK pin `10.0.301` + `"test": { "runner": "Microsoft.Testing.Platform" }`.
3. **global-usings:** drop `TimeWarp.Fixie`; add `TimeWarp.Jaribu` + `static TimeWarp.Jaribu.TestRunner`.
4. **Delete** `infrastructure/the-testing-convention.cs` / `testing-convention.cs`.
5. **Tests:** every public test class gets `[ModuleInitializer] Register() => RegisterTests<T>()`;
   every discovered test method becomes `public static` (`async Task` or `Task` returning
   `Task.CompletedTask` for sync bodies). Keep Shouldly assertions and namespaces.

| Project | ~files | Notes |
|---------|--------|-------|
| foundation-domain-tests | 3 | host-free; jaribu runfile duplicate stays as standalone exemplar |
| foundation-application-tests | 1 | |
| foundation-contracts-tests | 2 | |
| foundation-infrastructure-tests | 2 | |
| web-contracts-tests | 4 | host-free serialization |
| web-domain-tests | 2 | |
| web-infrastructure-tests | 4 | may use test infra from timewarp-testing — no TimeWarpTestingConvention |
| timewarp-identity-tests | 15 | largest; still host-free unit style |
| agent-identity-cli-tests | 3 | |
| timewarp-architecture-analyzers-tests | 11 | Rename `FixieVerifier` → `RoslynTestVerifier` (IVerifier only; no Fixie types) |
| timewarp-architecture-sourcegenerator-tests | 10 | same verifier pattern if present |

#### B. Shared library cleanup

- Delete `tests/common/timewarp-testing/testing-convention/testing-convention.cs` (TimeWarpTestingConvention).
- Remove Fixie package refs from `timewarp-testing.csproj`; drop `global using TimeWarp.Fixie`.
- Grep-clean any remaining `SpaTestApplication` / convention types.

#### C. CPM + `dev test`

- Remove `Fixie`, `Fixie.TestAdapter`, `TimeWarp.Fixie` from `Directory.Packages.props`.
- `test-command.cs`: once all projects are MTP (project-local global.json), **remove the Fixie
  branch** — always run `dotnet test -c Release` with cwd = project directory. Update Design
  region: dual-framework era ended (task 145-007). Keep sequential project loop (fixed ports).
- Optionally: after zero Fixie, root *could* set MTP runner later — **do not** in this task
  (co-located runfiles / other tooling may still care); keep per-project global.json.

#### D. Docs / comments sweep

Update live guidance (not historical kanban/analysis folders):

- `AGENTS.md` — remove “remaining Fixie is migration debt” wording where debt is gone.
- `documentation/test-structure.md`, `documentation/developer/conceptual/testing/integration-testing.md`,
  `documentation/developer/standards/file-naming.md` — present tense Jaribu only; Fixie only as history.
- `skills/tw-web-api-contracts/SKILL.md` (and examples if needed) — no “don't add Fixie” debt language.
- `tests/Directory.Build.props` comments — Jaribu-only rationale for CA1707/CA1822/RCS1102.
- Comment-only Fixie mentions in already-Jaribu csprojs (global.json “would break Fixie”).
- Leave `skills/*/analysis/**` and `kanban/**` historical.

#### E. Gates

- `dev build` 0/0
- Each migrated project: `dotnet test` from project dir (MTP)
- `dev test` (full sequential)
- `dev template-smoke` (or CI equivalent tier) — zero Fixie in generated output
- `ganda repo audit` clean

#### Out of scope

- Co-located Jaribu JARIBU_MULTI aggregator (task 136)
- Un-quarantine SPA weather (058)
- Publishing a “TimeWarp.Fixie is dead” NuGet notice beyond repo cleanup

## Session

- Started: 145-007 orchestration
- 2026-08-02: Migrated analyzer suite projects to Jaribu MTP
  - `tests/analyzers/timewarp-architecture-analyzers-tests` — 12 classes, **102** tests pass
  - `tests/analyzers/timewarp-architecture-sourcegenerator-tests` — 11 classes, **59** tests pass
  - Rename: `fixie-verifier.cs` / `FixieVerifier` → `roslyn-test-verifier.cs` / `RoslynTestVerifier`
  - Deleted both `testing-convention.cs`; Fixie package refs replaced with `TimeWarp.Jaribu.TestingPlatform`
  - Verified: `dotnet build -c Release && dotnet test -c Release` in each project dir
