# Get Fixie tests running at root and add dev test command

## Description

Migrate the Fixie test projects from the old `TimeWarp.Architecture/Tests/` tree into the
root `tests/` directory, add them to `timewarp-architecture.slnx`, and wire the dev-cli
`test` command to actually run them. Today `dev test` runs `dotnet test` at the repo root
against a slnx that contains no test projects, so it passes without testing anything — and
CI (`.github/workflows/workflow.yml`) calls it, giving false confidence.

This is the test-migration slice of [[053-050-019-migrate-test-projects-and-gentester-to-root-source-tree]]
plus the `dev test` wiring from [[048-create-dev-cli-and-migrate-ps1-scripts-to-nuru-runfiles]].
Coordinate with 053-050-019 (which also covers GenTester and TimeWarp.Automation) so the two
do not migrate the same projects twice — likely fold its Phase 2/3 test work into this task.

Jaribu testing may be added/substituted later (see [[adopting-jaribu-testing]]); this task
is specifically about getting the existing Fixie suites green at root. Keep the wiring
test-framework-agnostic where practical so adding Jaribu later is incremental.

## Current State (investigated 2026-06-12)

- All test projects still live under `TimeWarp.Architecture/Tests/` and are NOT in the root
  slnx. The root `tests/` dir is just the `.gitkeep` placeholder from task 057.
- Their `ProjectReference`s already point at the migrated root `source/` tree (e.g.
  `...\source\container-apps\web\web-server\web-server.csproj`), so source refs are mostly done.
- Package refs are version-less and resolve via the OLD `TimeWarp.Architecture/Directory.Packages.props`.
  The root CPM already has a "Test Packages" group (added in task 057) with matching versions.
- Test projects:
  - `Tests/Analyzers/TimeWarp.Architecture.Analyzers.Tests`
  - `Tests/Analyzers/TimeWarp.Architecture.SourceGenerator.Tests`
  - `Tests/Common/Common.Infrastructure.Tests`
  - `Tests/ContainerApps/Api/Api.Server.Integration.Tests`
  - `Tests/ContainerApps/Aspire/Aspire.csproj`
  - `Tests/ContainerApps/Web/Web.Server.Integration.Tests`
  - `Tests/ContainerApps/Web/Web.Spa.Integration.Tests`
  - `Tests/EndToEnd.Playwright.Tests`
  - `Tests/Libraries/TimeWarp.Automation.Tests`
  - `Tests/TimeWarp.Testing/Testing.Common.csproj` (shared infra)

### Known blockers found while probing

- `TimeWarp.Architecture.Analyzers.Tests` fails to compile: `PartialClassDeclarationAnalyzer`
  not found (CS0246, 9 sites in `PartialClassDeclarationAnalyzer_Tests.cs`). The analyzer was
  likely renamed/removed during migration — reconcile against the current
  `source/analyzers/timewarp-architecture-analyzers` API.
- `MSB3277`: Microsoft.CodeAnalysis.CSharp version conflict (5.0.0 vs 5.3.0) in the analyzer
  tests — align the analyzer-testing package versions in the root CPM.
- Root `Directory.Build.props` is strict (warnings-as-errors, `AnalysisMode=All`,
  `AnalysisLevel=latest-all`, BannedSymbols). Once tests live under root they inherit it;
  expect analyzer/style failures the old props didn't enforce. Decide whether `tests/` needs
  its own `Directory.Build.props` to relax some rules for test code.

## Checklist

### Migrate
- [ ] Move test projects to `tests/` with kebab-case naming (mirror the source-tree layout)
- [ ] Move shared `Testing.Common` to `tests/common/timewarp-testing/`
- [ ] Fix relative `ProjectReference` paths after the move
- [ ] Add all test projects to `timewarp-architecture.slnx`
- [ ] Remove version-less reliance on the old CPM; confirm they resolve against root CPM
- [ ] Decide on a `tests/Directory.Build.props` chain (relax analyzers for test code?)

### Fix build blockers
- [ ] Resolve `PartialClassDeclarationAnalyzer` CS0246 in analyzer tests
- [ ] Resolve MSB3277 CodeAnalysis.CSharp version conflict
- [ ] Get every migrated project building under the strict root props

### Wire dev test
- [ ] Update `tools/dev-cli/endpoints/test-command.cs` to run the Fixie suites
      (was: generic `dotnet test` template). Use `dotnet test` on the slnx or per-project
      `dotnet fixie` per the framework docs.
- [ ] Decide handling of E2E Playwright tests (separate command/filter; needs browsers)
- [ ] Decide handling of integration tests that need the Aspire host running
- [ ] `dev test` runs the real suites and reports actual pass/fail
- [ ] `dev self-install` and confirm `./bin/dev test` works
- [ ] Verify CI workflow.yml now exercises real tests

## Notes

- `dev test` currently green-but-meaningless; fixing this is the point of the task.
- Old `.ps1` equivalent is `RunTests.ps1` (and `dotnet fixie Tests/[Project]`).
- Integration tests may require the Aspire AppHost; E2E uses Playwright (`dotnet test`, not
  Fixie). These may warrant separate dev-cli commands or tag filters rather than one `test`.
- After migration, the old `TimeWarp.Architecture/Tests/` tree should be removed (coordinate
  with 053-050-019 / the broader migration cleanup).

## Plan & re-verification (2026-06-24)

Re-checked the blockers (notes above were 06-12; much has migrated since):
- ✅ `PartialClassDeclarationAnalyzer` CS0246 — RESOLVED (analyzer now at
  `source/analyzers/timewarp-architecture-analyzers/partial-class-declaration-analyzer.cs`).
- ✅ MSB3277 CodeAnalysis.CSharp 5.0 vs 5.3 — RESOLVED (root CPM unifies `Microsoft.CodeAnalysis*` at 5.3.0).
- ⚠️ Strict root props remain the real risk → add `tests/Directory.Build.props` to relax analyzers for test code.
- 🔴 NEW: the `Aspire` test project references the OLD `Source/ContainerApps/Aspire/Aspire.AppHost` path (gone; now `source/container-apps/aspire/aspire-app-host`) — dangling ref, must fix.
- ✅ Most other test ProjectReferences already point at root `source/`.

Migration sliced (lowest risk first):
1. `Testing.Common` → `tests/common/timewarp-testing/` (shared infra; others depend on it).
2. Unit/analyzer slice (no infra): Common.Infrastructure.Tests, Analyzers.Tests, SourceGenerator.Tests
   → proves `tests/Directory.Build.props`, root-CPM resolution, slnx wiring, and the `dev test` Fixie command.
3. Integration slice (fix the Aspire dangling ref; decide how they get a running host).
4. E2E Playwright → separate `dev` command/filter (needs browsers).
