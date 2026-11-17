# Round 1 — general
**Date:** 2026-11-04
**Scope reviewed:** branch vs master: `tools/Directory.Build.props`, `.editorconfig` (template-smoke section), `file-length-analyzer.cs` Design comment; surrounding `tools/dev-cli/Directory.Build.props`, `tools/agent-identity-cli/Directory.Build.props`, `source/Directory.Build.props`, `tests/Directory.Build.props`, `.template.config/template.json`.

## Summary

The change does what the task asks. `tools/Directory.Build.props` loads the convention analyzers into both tool runfiles (ProjectReference in the monorepo, the Analyzers package when `UseAnalyzerPackages=true`). TWA0008 is off only for `tools/dev-cli/services/template-smoke/`. A build-time global analyzer config exempts NuGet package content files by absolute path, with no per-file pragma. The decision is recorded in a Design region. The goal restatement matches the task and Steve's decision, so there is no misread.

Re-verified: `dotnet build tools/dev-cli/dev.cs --no-incremental` reports 0 warnings and 0 errors, and the compiler command line carries both the convention analyzer DLL and `/analyzerconfig:.../package-content-analyzers.globalconfig`. That file lists the 27 TimeWarp.Nuru.DevCli content files. `dotnet build tools/agent-identity-cli/agent.cs` succeeds. The dev-cli tests pass 342/342. `ganda repo audit` exits 0; its two warnings are the pre-existing `kanban-landed` and `source-file-line-cap` (Warning by 469-001 policy).

The findings are about accuracy and leftover duplication. Two comments state something false or overstated. One existing suppression list is now redundant. The analyzer wiring now exists in a third copy.

## Issues

### Issue 1 — Severity: suggestion
- File: .editorconfig:407
- Description: The new comment says "tools/ is monorepo-only (template.json excludes it)." That is false. `.template.config/template.json` excludes only `tools/agent-identity-cli/**`. `tools/dev-cli/**`, including `services/template-smoke/`, ships in generated apps. The comment also contradicts the new `tools/Directory.Build.props` Design region, which says "tools/dev-cli ships in generated apps." The root `.editorconfig` ships too, so agents in generated apps will read the wrong claim.
- Suggestion: Drop the "monorepo-only" sentence, or replace it with an accurate one: tools/dev-cli ships, and the section applies in generated apps as well.
- Status: open

### Issue 2 — Severity: suggestion
- File: tools/Directory.Build.props:15 (Design region), source/analyzers/timewarp-architecture-convention-analyzers/file-length-analyzer.cs:29
- Description: Both comments say the exemption turns analyzer diagnostics off for every package content file. It does not cover every rule. A per-path `dotnet_analyzer_diagnostic.severity = none` loses to an id-level `dotnet_diagnostic.<id>.severity` entry. The SDK's `analysislevel_11_all_warnaserror.globalconfig`, which is also passed to csc, sets those for CA rules. Probe: building with a global `-p:NoWarn` that leaves out the root CA1812 suppression and the dev-cli IDE suppressions gives `error CA1812` on `content/any/endpoints/check-version-command.cs`, `release-command.cs`, and `self-install-command.cs`. TWA* and IDE* do not fire, because no id-level global-config severity is set for them. So the exemption works for the convention rules, which is this task's goal. The comments overstate it, and a future id-level global setting for a TWA rule would silently bypass it.
- Suggestion: State the actual reach in both comments: the exemption silences rules that have no id-level severity in a global config (the TWA* rules, IDE*), and the CA rules set by AnalysisLevel still need NoWarn. Optionally, also emit `dotnet_diagnostic.TWA0038.severity = none` in each section, or one line per TWA id, so the convention rules are covered by id.
- Status: open

### Issue 3 — Severity: suggestion
- File: tools/dev-cli/Directory.Build.props:9-13
- Description: IDE0005, IDE0022, IDE0046, IDE0055, IDE0058, IDE0065, IDE0066, IDE0078, IDE0160, and IDE0290 are in `NoWarn` only because, as the comment there says, they "fire in TimeWarp.Nuru.DevCli content files" and the repo `.editorconfig` cannot reach those files. The new path exemption now covers them. The same probe (IDE ids not suppressed) shows no IDE diagnostic on content files or on `tools/dev-cli` sources. `NoWarn` is project-wide, so these entries also hide the IDE rules on the repo's own dev-cli code, and that is exactly the code this task wants guarded. The comment is now stale.
- Suggestion: Remove those IDE ids from `tools/dev-cli/Directory.Build.props` `NoWarn` (keep IDE0211 and the CA/RCS ids), and update the comment to point at the tools/ package-content exemption. Re-run `dotnet build tools/dev-cli/dev.cs` and `dev self-install` to confirm 0 warnings.
- Status: open

### Issue 4 — Severity: suggestion
- File: tools/Directory.Build.props:29-45
- Description: The convention-analyzer wiring exists in three places: the ProjectReference/PackageReference split, the two `CompilerVisibleProperty` items, and the `template.json` `AdditionalFiles` item. They are in `source/Directory.Build.props`, `tests/Directory.Build.props`, and now `tools/Directory.Build.props`, and the copies must agree. A future analyzer that needs a new compiler-visible property has to be added in all three. AGENTS.md hard rule: "When two things must agree, generate one from the other or add a build check."
- Suggestion: Move the shared ItemGroups into one `msbuild/convention-analyzers.props` (or similar) and import it from the three props. Keep the per-tree guards, such as the convention-analyzers project self-reference exclusion, at the import site or as a condition on a property.
- Status: open
