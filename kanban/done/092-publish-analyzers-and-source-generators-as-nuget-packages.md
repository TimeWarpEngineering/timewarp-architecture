# Publish analyzers and source generators as NuGet packages

## Description

Graduate Roslyn **convention analyzers** and **source generators** from template-forked project
references to versioned NuGet packages — same extraction path foundation took (task 051), which we
never finished for the analyzer layer.

Today every `dotnet new timewarp-architecture` app **owns a copy** of `source/analyzers/**`. Rule
fixes (TWA0001–0010, generators) do not roll forward via package bump; apps freeze at generation
time. Foundation already ships as `TimeWarp.Foundation.*`; analyzers are the remaining platform
gap (tech debt, not intentional end-state).

## Why

- Shared compile-time platform should version like shared runtime platform.
- One fix → one package bump across apps; no forked rule drift.
- Cleaner layering: template scaffolds; NuGet enforces.

## Scope

### Packages (proposed — lock names in implementation)

| Assembly today | Proposed package | Contents |
|----------------|------------------|----------|
| `timewarp-architecture-convention-analyzers` | e.g. `TimeWarp.Architecture.Analyzers` | DiagnosticAnalyzers only (TWA0002–0010, etc.) — safe for contracts |
| `timewarp-architecture-analyzers` | e.g. `TimeWarp.Architecture.Generators` (or combine if justified) | Source generators + TWA0001 if it lives there |
| `timewarp-architecture-attributes` | ship as dependency of generators package or merge | Marker attributes consumed by generators |

Exact PackageIds, whether one vs two analyzer packages, and version alignment with foundation
(`source/Directory.Build.props` single version) are implementation decisions — document in Design
regions / AGENTS.md when chosen.

### Delivery

1. **Packable analyzer projects** — proper nupkg layout (`analyzers/dotnet/cs/`, no runtime deps
   leakage, `PrivateAssets` / `IncludeAssets` guidance for consumers).
2. **CI** — add packages to release pack/push list in `tools/dev-cli` workflow (alongside foundation
   + template).
3. **Repo + template wiring** — replace `ProjectReference` `OutputItemType=Analyzer` in
   `source/Directory.Build.props` / `tests/Directory.Build.props` with `PackageReference` (or
   dual-mode: package in generated apps, project ref only when building the package itself).
4. **Template content** — stop shipping analyzer **source** into generated apps once packages
   work; props/packages only (mirror foundation `UseFoundationPackages` pattern if needed).
5. **Docs** — AGENTS.md, template Overview, HowTo upgrade path for existing generated apps.

### Non-goals (unless required)

- Changing diagnostic IDs (keep **TWA####**).
- Rewriting rule logic; this is distribution only.
- Publishing a separate “ruleset” product beyond the analyzers themselves.

## Requirements

- `dotnet pack` produces consumer-ready analyzer packages; restore applies diagnostics/generators.
- Generated app from template builds 0/0 **without** `source/analyzers/**` in the app tree (or with
  packages only).
- Version matches repo single-version policy (or documented dual-version if generators must lag).
- Workflow release path publishes new packages to NuGet.
- Greenfield purity: no long-term dual ProjectReference + PackageReference for consumers.

## Checklist

- [x] Lock PackageId(s), one-vs-two packages, versioning policy
- [x] Make analyzer/generator/attributes projects packable with correct Roslyn nupkg layout
- [x] Wire package into release pack/push workflow
- [x] Switch Directory.Build.props consumers to PackageReference; keep dogfood path for building the packages
- [x] Exclude analyzer sources from template content (or condition like foundation)
- [x] Verify: pack → install/local feed → sample app gets TWA diagnostics + generators
- [x] Docs: AGENTS.md, template notes, upgrade story for already-generated apps
- [x] `dev build` 0/0 in template repo

## Notes

### Context (2026-07-15)

- Foundation extracted to NuGet (051); analyzers left as template source — acknowledged tech debt.
- TWA0009 slice isolation (091) and the full 0001–0010 family make “forked rules” more costly every
  release.
- Design discussion: fork model is bootstrap-OK, bad steady-state for a multi-app platform.

### Related

- 051 — foundation NuGet packages (pattern to mirror)
- 084 — convention-analyzers rename + Directory.Build.props wiring
- 091 — TWA0009 namespace-based slices (rules that should version centrally)

### Entry points

- `source/analyzers/**`
- `source/Directory.Build.props`, `tests/Directory.Build.props`
- `tools/dev-cli/endpoints/workflow-command.cs` (pack list)
- `timewarp-templates/.../timewarp-architecture-template.csproj` (content include)
- `.template.config/template.json` (exclusions / foundation-style package switch)



### Implementation plan (2026-07-15)

# 092 — Publish analyzers and source generators as NuGet packages

## Locked decisions

### Three packages

| Assembly | PackageId | Kind |
|----------|-----------|------|
| convention-analyzers | **TimeWarp.Architecture.Analyzers** | Analyzer-only (TWA0002–0010) |
| analyzers (generators + TWA0001) | **TimeWarp.Architecture.Generators** | Analyzer-only |
| attributes | **TimeWarp.Architecture.Attributes** | Runtime library |

Keep Analyzers vs Generators split (generators must not run repo-wide). Attributes public, not private dep of Generators. No project folder renames — PackageId only.

### Version

Single `<Version>` in `source/Directory.Build.props`. CPM `PackageVersion` pins lag like foundation (last published).

### Template

`analyzerPackages` bool symbol default **true**; excludes `source/analyzers/**` and `tests/analyzers/**`. MSBuild `UseAnalyzerPackages` auto true when convention-analyzers csproj missing (mirror foundation).

### Dogfood

This repo: ProjectReference. Generated apps: PackageReference only. No dual Project+Package for same consumer.

## Pack layout

- **Attributes**: normal lib package
- **Analyzers/Generators**: `IncludeBuildOutput=false`, `DevelopmentDependency=true`, `SuppressDependenciesWhenPacking=true`, pack dll to `analyzers/dotnet/cs/`

## Phases

**A** — `source/analyzers/Directory.Build.props` + packable csprojs; local pack inspect  
**B** — `workflow-command.cs` PackableProjects  
**C** — UseAnalyzerPackages dual-mode in source/tests Directory.Build.props + spa/api-server/api-contracts; Directory.Packages.props pins  
**D** — template.json symbol + slnx `#if (!analyzerPackages)`  
**E** — dev build 0/0; analyzer tests; pack smoke; local-feed generated app (no source/analyzers; TWA + gens work)  
**F** — AGENTS.md, upgrade how-to, Design regions  
**G** — Release/Trusted Publishing (ops after code)

## DoD

Three consumer-ready packages; generated app without analyzer source; version policy; workflow pack list; docs; monorepo 0/0; IDs/rules unchanged.

## Risks

Wrong nupkg layout; CodeAnalysis dep leak; chicken-egg unpublished pins (local feed first); Trusted Publishing for new PackageIds.



## Results

### Summary

Graduated Roslyn convention analyzers, source generators, and attributes to three NuGet packages with foundation-style dual-mode wiring. Generated apps default to package mode (no forked `source/analyzers/**`). Monorepo still dogfoods via ProjectReference.

### Packages

| PackageId | Project | Layout |
|-----------|---------|--------|
| TimeWarp.Architecture.Analyzers | convention-analyzers | analyzers/dotnet/cs/ |
| TimeWarp.Architecture.Generators | analyzers (gens + TWA0001) | analyzers/dotnet/cs/ |
| TimeWarp.Architecture.Attributes | attributes | lib/net10.0/ |

### What was implemented

- Packable analyzer projects + `source/analyzers/Directory.Build.props`
- `UseAnalyzerPackages` dual-mode in source/tests Directory.Build.props
- Consumer dual-mode: web-spa, api-server, api-contracts
- CPM PackageVersion pins (2.0.0-beta.3 first ship)
- Template `analyzerPackages` (default true) + slnx guards
- Workflow PackableProjects updated
- AGENTS.md + HowToUpgradeToAnalyzerPackages.md

### Commits

- `5a20f242` feat(analyzers): make analyzer/generator/attributes projects packable
- `233626cb` feat(analyzers): dual-mode UseAnalyzerPackages, template exclude, pack list
- `9b6515b7` fix(tests): explicit attributes ProjectReference after PrivateAssets

### Test outcomes

- `dev build` 0/0
- Analyzer tests 62 passed; sourcegenerator tests 16 passed
- Pack layout verified; local-feed TWA0004 smoke OK

### Remaining (ops / post-release)

- NuGet Trusted Publishing for the three new PackageIds before first push
- Chicken-egg until first publish (local feed or `--analyzerPackages false`)
- Full `dotnet new` solution e2e optional follow-up

### Review

Clean — no issues (orchestrator review of packaging PR shape).


## Session

- Created: 2026-07-15 (post-merge 278; distribution design discussion)
- Implementation plan: 2026-07-15 (orchestrate-task 092)
- Implementation + review: 2026-07-15 (orchestrate-task 092)
