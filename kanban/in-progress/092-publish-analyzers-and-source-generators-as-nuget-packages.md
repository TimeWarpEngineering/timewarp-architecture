# Publish analyzers and source generators as NuGet packages

## Description

Graduate Roslyn **convention analyzers** and **source generators** from template-forked project
references to versioned NuGet packages — same extraction path foundation took (task 051), which we
never finished for the analyzer layer.

Today every `dotnet new timewarp-architecture` app **owns a copy** of `source/analyzers/**`. Rule
fixes (TWPA0001–0010, generators) do not roll forward via package bump; apps freeze at generation
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
| `timewarp-architecture-convention-analyzers` | e.g. `TimeWarp.Architecture.Analyzers` | DiagnosticAnalyzers only (TWPA0002–0010, etc.) — safe for contracts |
| `timewarp-architecture-analyzers` | e.g. `TimeWarp.Architecture.Generators` (or combine if justified) | Source generators + TWPA0001 if it lives there |
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

- Changing diagnostic IDs (keep **TWPA####**).
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

- [ ] Lock PackageId(s), one-vs-two packages, versioning policy
- [ ] Make analyzer/generator/attributes projects packable with correct Roslyn nupkg layout
- [ ] Wire package into release pack/push workflow
- [ ] Switch Directory.Build.props consumers to PackageReference; keep dogfood path for building the packages
- [ ] Exclude analyzer sources from template content (or condition like foundation)
- [ ] Verify: pack → install/local feed → sample app gets TWPA diagnostics + generators
- [ ] Docs: AGENTS.md, template notes, upgrade story for already-generated apps
- [ ] `dev build` 0/0 in template repo

## Notes

### Context (2026-07-15)

- Foundation extracted to NuGet (051); analyzers left as template source — acknowledged tech debt.
- TWPA0009 slice isolation (091) and the full 0001–0010 family make “forked rules” more costly every
  release.
- Design discussion: fork model is bootstrap-OK, bad steady-state for a multi-app platform.

### Related

- 051 — foundation NuGet packages (pattern to mirror)
- 084 — convention-analyzers rename + Directory.Build.props wiring
- 091 — TWPA0009 namespace-based slices (rules that should version centrally)

### Entry points

- `source/analyzers/**`
- `source/Directory.Build.props`, `tests/Directory.Build.props`
- `tools/dev-cli/endpoints/workflow-command.cs` (pack list)
- `timewarp-templates/.../timewarp-architecture-template.csproj` (content include)
- `.template.config/template.json` (exclusions / foundation-style package switch)

## Session

- Created: 2026-07-15 (post-merge 278; distribution design discussion)
