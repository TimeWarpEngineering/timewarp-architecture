# Round 2 — general
**Date:** 2026-11-04
**Scope reviewed:** fix delta `c83a4bf12..82383c9f0`, with the call sites of the moved wiring.

## Summary

The fix pass moved the convention-analyzer wiring into one gated block in the root
`Directory.Build.props`. source/, tests/, and tools/ each opt in with `EnableConventionAnalyzers`. The wiring
works. `dotnet msbuild -getItem` shows the analyzer ProjectReference and the template.json AdditionalFiles on
api-application and dev-cli-tests. The analyzer project gets no self-reference. timewarp-templates does not
opt in. M2, M3, and M4 are fixed. The M1 fix replaced one wrong sentence with another. Round 1 caused this:
its M1 finding wrongly said tools/dev-cli ships in generated apps. Two Design comments and a boy-scout scan
entry now rest on that wrong claim.

Gates re-run by this reviewer: `dotnet build tools/dev-cli/dev.cs` 0 warnings, 0 errors.
`dotnet build tools/agent-identity-cli/agent.cs` 0 warnings, 0 errors. `dev build` (bin/dev) 0 warnings,
0 errors.

## Issues

### Issue 1 — Severity: suggestion (re-opens round-1 M1)
- File: .editorconfig:407; tools/Directory.Build.props:30-31; tools/dev-cli/services/template-smoke/source-name-literal-scan-sets.cs:45
- Description: tools/ is not template content. `timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj`
  (Content items, lines 27–50) packs source/, tests/, msbuild/, .template.config/, skills/, some kanban stubs,
  and named root files. It does not pack tools/. The default template-smoke output
  (`artifacts/template-smoke/work/SmokeDefault/`) has no `tools/` directory. Round-1 M1 read only the
  template.json excludes and missed this. The fix pass then wrote three things on that wrong premise:
  - `.editorconfig:407`: "tools/dev-cli ships in generated apps". The section ships, because `.editorconfig`
    ships, but it matches no file in a generated app.
  - `tools/Directory.Build.props:30-31` Design region: "tools/dev-cli ships in generated apps and
    source/analyzers does not, so the wiring's package form is the one a generated app uses." Under tools/,
    the package form runs only when someone passes `-p:UseAnalyzerPackages=true` by hand.
  - `source-name-literal-scan-sets.cs:45` adds `tools/Directory.Build.props` to a list whose Design region
    says "Roots under the monorepo that ship into generated apps (or root wiring they inherit)". The file is
    neither. An unsafe-literal hit there would fail template-smoke for content that never ships.
  The same claim is in task.md Results (lines 109 and 113).
- Suggestion: State the fact in both comments. tools/ is monorepo-only, because the template pack project
  does not include it. The `.editorconfig` section ships as inert text. In tools/, the package form of the
  wiring is reached only with an explicit `-p:UseAnalyzerPackages=true`. Remove the scan-set entry and correct
  the two task.md Results lines.
- Status: open

## Re-verified prior findings

- M1: re-opened as Issue 1 above.
- M2: fixed. Both Design regions now state the real reach: the bulk key silences TWA* and IDE rules, and an
  id-level CA severity still wins.
- M3: fixed. The package-only IDE ids are out of dev-cli NoWarn. IDE0005 stays, and the comment gives the
  reason (no XML doc file, task 364). The dev-cli build has 0 warnings.
- M4: fixed. There is one gated block in the root props. MSBuild evaluates properties before items, so the
  child opt-in reaches the item conditions. `-getItem` confirms it.
