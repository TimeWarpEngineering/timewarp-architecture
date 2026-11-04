# Round 2 — merged findings
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
- File: .editorconfig:407; tools/Directory.Build.props:30-31; tools/dev-cli/services/template-smoke/source-name-literal-scan-sets.cs:45
- Description: tools/ does not ship in generated apps. The template pack project (`timewarp-architecture-template.csproj`
  Content items) has no tools/ include, and the SmokeDefault output has no tools/ directory. Round-1 M1
  wrongly said tools/dev-cli ships, and the fix pass carried that claim into the `.editorconfig` comment, the
  tools/Directory.Build.props Design region, a source-name scan-set entry, and task.md Results lines 109 and 113.
- Suggestion: Say that tools/ is monorepo-only, because the template pack does not include it. Say that the
  package form of the wiring is reached under tools/ only with an explicit `-p:UseAnalyzerPackages=true`.
  Remove the `tools/Directory.Build.props` scan-set entry. Correct the Results lines.
- Source: general (round 2)
- Disposition notes: fixed in the round-3 fix pass. Confirmed the pack project's Content items include
  source/, tests/, msbuild/, .template.config/, skills/, and root files, and no tools/ path. `.editorconfig:407`
  now says tools/ is monorepo-only and the section matches nothing in a generated app (the root
  .editorconfig itself ships). The `tools/Directory.Build.props` Design region says the same and that the
  package form is reached under tools/ only with `-p:UseAnalyzerPackages=true`. The `tools/Directory.Build.props`
  scan-set entry is removed, so `source-name-literal-scan-sets.cs` matches master. Task Results corrected.

### M2 — Severity: suggestion — Status: fixed
- Re-verified in round 2: both Design regions state the bulk-key reach and the id-level CA bypass.

### M3 — Severity: suggestion — Status: fixed
- Re-verified in round 2: the dev-cli NoWarn has the package-only IDE ids removed, IDE0005 keeps its reason,
  and the build has 0 warnings.

### M4 — Severity: suggestion — Status: fixed
- Re-verified in round 2: the root props has the one gated wiring block. `-getItem` shows the
  ProjectReference and template.json on api-application and dev-cli-tests, no self-reference on the
  analyzer, and no opt-in for timewarp-templates.

## Duplicates / conflicts

- None. Single reviewer. M1 keeps its round-1 ID with a corrected description.
