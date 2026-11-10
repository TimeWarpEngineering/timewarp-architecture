# Round 1 — general
**Date:** 2026-11-10
**Scope reviewed:** same as `review/review-framework.md` (branch vs `7624ca457`)

## Summary

The split is correct. `Directory.Packages.props` pins TimeWarp.SourceGenerators 1.0.0-beta.13. The three generic analyzers and their tests are deleted, and the built Architecture analyzer assembly no longer contains the TWA0004, TWA0023, or TWA0038 strings. Decompiling beta.13 shows the same ids, titles, messages, categories, defaults, `GeneratedCodeAnalysisFlags.None`, the `/obj/` and `/goldens/` exemptions, the `max_lines` key, and opt-out matching by the simple name `TypeStemIdentifierAttribute`. `.editorconfig` keys keep working. The root `TimeWarp.SourceGenerators` reference reaches every project the convention analyzers reached. No project sets `EnableConventionAnalyzers` to false, kanban research and timewarp-templates do not import the root props, and `.githooks` runs with `RunAnalyzers=false`. So the rules cover the same projects as before. The implementation matches the restatement, and the restatement matches the original task. Requirement 6 waits on timewarp-state task 153, as the task says. The two open findings are doc text that copies a version which will change.

Gates re-run in this worktree:

- `dotnet run tools/dev-cli/dev.cs -- build`: Build succeeded, 0 warnings, 0 errors. That run also dispatched a Nuru command through the new `IRequestHandler` / `ValueTask` handler shape.
- `dotnet test -c Release` in `tests/analyzers/timewarp-architecture-analyzers-tests`: 263 passed, 0 failed.
- `dotnet msbuild web-spa.csproj -getItem:Analyzer`: both `timewarp-source-generators.dll` 1.0.0-beta.13 and `timewarp-architecture-convention-analyzers.dll` load.
- nuget.org: TimeWarp.Architecture.Analyzers latest is 2.0.0-beta.31, so the 2.0.0-beta.32 pin that generated apps take is unpublished and will ship without the three rules. TimeWarp.SourceGenerators 1.0.0-beta.13 is published.
- `.github/workflows/workflow.yml` now runs every step in pwsh. Checked locally with pwsh 7.6: `rm -rf ~/.nuget/packages/*/2.0.0-smoke` expands `~` and the glob for native `rm`, and `dotnet dev-certs https --trust || true` still exits 0. The converted steps check `$LASTEXITCODE` after each native call.
- The `.editorconfig` TW0002 comment says beta.12 removed that analyzer. Confirmed: beta.13 declares TW0001, TW0007, TWA0004, TWA0023, and TWA0038 only.

## Issues

### Issue 1 — Severity: suggestion
- File: AGENTS.md:52
- Description: The diagnostics row says SourceGenerators reports the three ids "(pin 1.0.0-beta.13)", and `documentation/developer/guides/analyzer-package-split.md:17` says "This repo pins that version in `Directory.Packages.props`". Both copy the version pinned in `Directory.Packages.props:253`. The next SourceGenerators bump makes both wrong, and no check catches it. AGENTS.md hard rule: "When two things must agree, generate one from the other or add a build check."
- Suggestion: State the minimum version and point at the pin instead of repeating it. For example: "reported by `TimeWarp.SourceGenerators` (1.0.0-beta.13 and later; pin in `Directory.Packages.props`)". Make the same change to the guide sentence. The "since 1.0.0-beta.13" and "1.0.0-beta.13 and later" wording elsewhere (root `Directory.Build.props`, csproj Description, `AnalyzerReleases.Unshipped.md` headers) is a minimum version and is fine.
- Status: open

### Issue 2 — Severity: nit
- File: source/analyzers/timewarp-architecture-attributes/type-stem-identifier-attribute.cs:13
- Description: The Design region says "convention-analyzers match by simple name and must not ProjectReference this assembly". The TWA0023 analyzer that matches this attribute now lives in TimeWarp.SourceGenerators (`TypeStemIdentifierAnalyzer`, which matches `TypeStemIdentifierAttribute` by simple name). The convention-analyzers project no longer reads the attribute, so the Design region names the wrong package.
- Suggestion: Say that TimeWarp.SourceGenerators (TWA0023) matches by simple name and does not reference this assembly. Keep the rest of the Design region.
- Status: open
