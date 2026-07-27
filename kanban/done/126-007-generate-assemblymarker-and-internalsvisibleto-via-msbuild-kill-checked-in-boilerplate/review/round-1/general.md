# Round 1 — general
**Date:** 2026-07-27
**Scope reviewed:** commit 6b1d8c3c

## Summary

Task 126-007 largely lands as intended: root `Directory.Build.targets` generates
`AssemblyMarker.g.cs` with auto-generated header + `.g.cs` (TWA0004 skipped via
`GeneratedCodeAnalysisFlags.None`), `%3b` semicolon escaping is correct, namespace maps cover
all 26 former marker assemblies (17 container-apps + 5 foundation + 2 analyzers + 2 libraries),
aspire / convention-analyzers / foundation-contracts-generators opt out, no checked-in
`assembly-marker.cs` / IVT `.cs` remain under `source/`, template packs root
`Directory.Build.targets` (and maps via `source/**`), web-contracts IVT uses real assembly names
(`Web.Spa`, `web-spa-integration-Tests`, `web-server`, `web-server-integration-tests`), and
program/test SPA consumers plus external `TimeWarp.State*.AssemblyMarker` sites are correct.
AGENTS.md and ADR-0002 match the new mechanism.

One SPA consumer was missed and is a real break (or silent wrong-assembly bind).

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/web-spa/components/Routes.razor:4-5
- Description: SPA normalization to `IAssemblyMarker` updated `program.cs`, web-server
  `program.cs`, and `AspireSpaTestApplication.cs`, but `Routes.razor` still uses bare
  `typeof(AssemblyMarker)`. The old `Web.Spa.AssemblyMarker` class is gone; `_Imports.razor`
  imports `TimeWarp.Architecture.Web.Spa` (which now only has `IAssemblyMarker`), while
  `global-usings.cs` has `global using TimeWarp.State`, and the package exposes
  `TimeWarp.State.AssemblyMarker`. Bare `AssemblyMarker` therefore resolves to the **State
  package** (or fails to compile if resolution differs). Either way, Blazor
  `Router.AppAssembly` / `AdditionalAssemblies` no longer correctly identify the SPA assembly —
  this is the primary SPA marker consumer for routing.
- Suggestion: Change both sites to `typeof(IAssemblyMarker)` (or
  `typeof(Web.Spa.IAssemblyMarker)` / fully qualified) so AppAssembly is the generated SPA
  marker. Re-grep for bare `AssemblyMarker` under web-spa excluding
  `TimeWarp.State*.AssemblyMarker`.
- Status: open

### Issue 2 — Severity: suggestion
- File: Directory.Build.targets:23-54
- Description: `Compile` is added only inside `GenerateAssemblyMarker`, which has
  `Inputs`/`Outputs` and can be skipped as up-to-date. When the target is skipped, MSBuild does
  not re-apply that `ItemGroup`, so a later `CoreCompile` that re-runs (e.g. after editing a
  source file) may not include `AssemblyMarker.g.cs`. That can yield missing `IAssemblyMarker`
  on incremental rebuilds even though the generated file still exists on disk. The task claimed
  incremental-build-safe; this pattern is fragile unless empirically verified.
- Suggestion: Split generation vs inclusion — keep write under Inputs/Outputs, and always add
  `Compile`/`FileWrites` in a companion target without Outputs skip (or
  `Condition="Exists('$(TwAssemblyMarkerFile)')"` always-run include before CoreCompile),
  matching safer generated-source patterns.
- Status: open
