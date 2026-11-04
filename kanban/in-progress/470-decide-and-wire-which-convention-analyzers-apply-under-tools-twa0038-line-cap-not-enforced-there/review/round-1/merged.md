# Round 1 — merged findings
**Date:** 2026-11-04
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 4 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: .editorconfig:407
- Description: The comment says "tools/ is monorepo-only (template.json excludes it)." That is false. template.json excludes only `tools/agent-identity-cli/**`, so tools/dev-cli (including template-smoke) ships. The comment also contradicts the tools/Directory.Build.props Design region.
- Suggestion: Remove or correct the sentence. Generated apps get this section too.
- Source: general
- Disposition notes: Fixed in the fix pass: the comment now says tools/dev-cli ships in generated apps, so the section ships too.

### M2 — Severity: suggestion — Status: fixed
- File: tools/Directory.Build.props:15; source/analyzers/timewarp-architecture-convention-analyzers/file-length-analyzer.cs:29
- Description: The comments say the package-content exemption turns all analyzer diagnostics off. Id-level severities from the AnalysisLevel global config still win: CA1812 fires on three TimeWarp.Nuru.DevCli content files when NoWarn is not covering it. The TWA* and IDE* rules are silenced as intended.
- Suggestion: State the real reach in both comments. Optionally, also emit id-level `none` entries for the TWA ids, so a future global TWA severity cannot bypass the exemption.
- Source: general
- Disposition notes: Fixed in the fix pass: the tools/Directory.Build.props Design region and the file-length-analyzer.cs Design region now state the real reach. The bulk key silences TWA* and IDE rules. An id-level severity wins, so CA ids that AnalysisLevel latest-all sets still run (CA1812 does, and root NoWarn covers it). The optional id-level TWA entries were not added: a hand list of TWA ids would have to agree with the analyzer set (AGENTS.md hard rule), and no config sets a TWA id severity for package paths today. The Design region names that bypass.

### M3 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/Directory.Build.props:9-13
- Description: The IDE0005, 0022, 0046, 0055, 0058, 0065, 0066, 0078, 0160, and 0290 NoWarn entries exist only for package content. The path exemption now covers them, and a probe shows none fire with the entries removed. Project-wide NoWarn also hides those rules on the repo's own dev-cli code, and the comment is stale.
- Suggestion: Drop those IDE ids from NoWarn (keep IDE0211 and the CA/RCS ids), update the comment, and re-run the dev-cli build and `dev self-install`.
- Source: general
- Disposition notes: Fixed in the fix pass: dropped IDE0022/0046/0055/0058/0065/0066/0078/0160/0290 from dev-cli NoWarn and updated the comment. IDE0005 stays in NoWarn: with it removed, the build fails with EnableGenerateDocumentationFile, because the dev CLI sets GenerateDocumentationFile false (task 364, AOT publish MSB3030). The comment says so. The dev-cli build is 0 warnings, and `dev self-install` succeeds.

### M4 — Severity: suggestion — Status: fixed
- File: tools/Directory.Build.props:29-45
- Description: The convention-analyzer wiring (reference split, CompilerVisibleProperty items, template.json AdditionalFiles) is now copied in source/, tests/, and tools/ Directory.Build.props, and the three copies must agree (AGENTS.md hard rule).
- Suggestion: Put the shared wiring in one msbuild props file and import it from all three.
- Source: general
- Disposition notes: Fixed in the fix pass: the wiring (ProjectReference/PackageReference split, CompilerVisibleProperty items, template.json AdditionalFiles) is now one block in the root Directory.Build.props, gated on EnableConventionAnalyzers. source/, tests/, and tools/ Directory.Build.props each set that property to true. An imported msbuild/*.props was tried first and rejected: ganda cpm-consistency finds PackageReference only in Directory.Build.props and *.csproj, so it reported the Analyzers PackageVersion as orphaned (audit Error).

## Duplicates / conflicts

- None. Single reviewer.
