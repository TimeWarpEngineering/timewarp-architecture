# Decide and wire which convention analyzers apply under tools/ (TWA0038 line cap not enforced there)

## Description

Task 469-001 added TWA0038 (`FileLengthAnalyzer`, 500-line cap) to the convention analyzers.
Projects under `tools/` (the dev CLI and the agent-identity CLI) do not load the convention
analyzers, so the cap reaches dev-cli sources only through the files that
`tests/tools/dev-cli-tests` compile-includes. The dev-cli sources were brought under 500 lines in
469-002, but nothing guards them.

The 469-001 worker trialed a `tools/Directory.Build.props` that wired the analyzers and backed it
out after two findings:

- TWA0008 template-token hits in `tools/dev-cli/services/template-smoke/`, which legitimately
  handles template tokens as strings.
- TWA0038 hits on TimeWarp.Nuru.DevCli NuGet content files pulled from `~/.nuget/packages`
  (609 and 704 lines), which are not this repo's code.

Follows 469 (done). The decision is which convention rules apply under `tools/` and how package
content files are exempted. Steve decides the rule set; the task then wires it.

## Goal (implementer restatement)

Make the convention analyzers (TWA*) run on the projects under `tools/` (the dev CLI and the
agent-identity CLI runfiles), so TWA0038's 500-line cap and the other rules guard those sources
directly, not only through the files `tests/tools/dev-cli-tests` compile-includes.

Acceptance:

- `tools/Directory.Build.props` turns on the full convention analyzer set; both tool props import it.
  The wiring itself is one block in the root `Directory.Build.props`, which source/, tests/, and tools/
  opt into (review M4).
- TWA0008 is off only for `tools/dev-cli/services/template-smoke/` via an `.editorconfig` section.
- NuGet package content files (`TimeWarp.Nuru.DevCli` under the NuGet package root) are exempt by
  path in the props or analyzer, with no per-file pragma.
- No other exemptions. A rule that fires on code needing a design change stops the task (Blocked).
- The decision is recorded in a Design region.
- `dev build` 0 warnings, dev-cli tests pass, `dev self-install` (AOT) builds, `ganda repo audit` clean.

## Requirements

- Record the decision in a Design region (the `tools/Directory.Build.props` or the analyzer).
- Package content files are exempt without a per-file pragma.
- `dev build` stays 0 warnings. dev-cli tests pass. `dev` AOT self-install still builds.
- The `ganda repo audit` `source-file-line-cap` check runs as Warning with reason in
  `.editorconfig` (469-001); revisit once ganda offers an exclude key.

## Checklist

- [x] Steve picks the rule set for tools/ (2026-11-04, see Notes)
- [x] Wire analyzers with the agreed exemptions
- [x] Gates green

## Review

- Round 1: Blocked — open M1, M2, M3, M4
- Fix pass for round 2: M1–M4 fixed (dispositions in `review/round-1/merged.md`)

## Notes

Decision (Steve, 2026-11-04): load the full convention analyzer set under `tools/`. TWA0008 is off
for `tools/dev-cli/services/template-smoke/` via an `.editorconfig` section (it handles template
tokens as strings). NuGet package content files (`~/.nuget/packages`, e.g. TimeWarp.Nuru.DevCli)
are exempt by path in the analyzer or props, no per-file pragma. TWA0038 is enforced. If another
rule fires on code that needs a design change to fix, stop and report; do not add exemptions
beyond these.

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-04)
- Implemented (implement oracle, 2026-11-04): tools/Directory.Build.props, .editorconfig section,
  TWA0038 Design pointer. Gates in Results.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 688b1990-79b2-4b19-a0c5-821bc2eb2bdd, max-turns 500, Done — 2026-11-04T07:47:06Z
- Review round 1 (review oracle, claude claude-opus-5-5, effort 1, roster: general, turn cap 80),
  2026-11-04: 4 open suggestions (M1–M4), 0 bugs. A fix pass is needed. Review files:
  `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session a40e1e07-e36a-4ebb-8bdf-2167cb2bac15, max-turns 80, artifacts general.md, merged.md — 2026-11-04T07:53:16Z

- Fix pass (implement-fix-2 oracle, claude-opus-5-5, 2026-11-04): fixed M1–M4. Wiring moved to the
  root props behind `EnableConventionAnalyzers`. Gates re-run (see Results).

## Results

- New `tools/Directory.Build.props` (imported by both tool props through `GetPathOfFileAbove`) sets
  `EnableConventionAnalyzers=true`, so the full convention analyzer set loads under tools/. The decision
  is in that file's Design region.
- One wiring for all three trees (review M4): the root `Directory.Build.props` holds the
  ProjectReference (monorepo) / Analyzers PackageReference (generated apps, `UseAnalyzerPackages=true`)
  split, the CompilerVisibleProperty items, and the template.json AdditionalFiles. All of it is gated on
  `EnableConventionAnalyzers`. source/, tests/, and tools/ Directory.Build.props each set that property.
  MSBuild evaluates properties before items, so a child-set property gates the root items. timewarp-templates,
  kanban research runfiles, and eval fixtures do not opt in. An imported `msbuild/*.props` was tried
  first: ganda cpm-consistency scans only Directory.Build.props and *.csproj for PackageReference, so it
  reported the Analyzers PackageVersion as orphaned.
- TWA0008 off for `tools/dev-cli/services/template-smoke/**.cs` in `.editorconfig` (8 hits there,
  in `template-smoke-test-tables.cs` and `template-smoke-runner.cs`).
- Package content exemption: target `ExemptPackageContentFromAnalyzers` writes
  `obj/.../package-content-analyzers.globalconfig` with one absolute-path section per Compile item
  under `$(NuGetPackageRoot)`, each `dotnet_analyzer_diagnostic.severity = none`, and adds it to
  `EditorConfigFiles`. No per-file pragma, no path list. `generated_code = true` was tried first and
  rejected: it also disables the nullable context, so CS8669 fires on every annotation in the
  TimeWarp.Nuru.DevCli content files.
- Proof the exemption carries weight: with the target disabled, the dev CLI build fails with TWA0038
  on `release-command.cs` and `nuget-version-service.cs` (package content). With it on, the build is clean.
- No other convention rule fired on tools/ code, so no design-change stop was needed.
- `file-length-analyzer.cs` Design region points at the tools/ exemption and states its reach
  (review M2). The bulk severity key silences TWA* and IDE rules on package content. CA ids that
  AnalysisLevel sets by id still run (CA1812, covered by root NoWarn).
- `.editorconfig` template-smoke comment corrected (M1): tools/dev-cli ships in generated apps.
- dev-cli NoWarn no longer lists the package-content IDE ids (M3). IDE0005 stays, because the dev CLI
  turns off the XML doc file (task 364) and the SDK would otherwise report EnableGenerateDocumentationFile.
- Boy-scout: `tools/Directory.Build.props` added to the template-smoke source-name literal scan files
  (it ships in generated apps).
- `ganda repo audit`: exit 0. Remaining warnings are not from this change: `source-file-line-cap`
  (Warning by 469-001 policy: EF migrations and a done task's research script) and `kanban-landed`
  (stale claims on other tasks).

Gates run (fix pass, after M1–M4):

- `dotnet build tools/dev-cli/dev.cs`: 0 warnings, 0 errors. With `-p:UseAnalyzerPackages=true`: 0 warnings.
- `dotnet build tools/agent-identity-cli/agent.cs`: 0 warnings, 0 errors.
- Exemption weight: with the `ExemptPackageContentFromAnalyzers` condition set to false, the build fails
  with TWA0038 on `release-command.cs` and `nuget-version-service.cs`. Restored, the build is clean.
- `dotnet msbuild -getItem`: api-application and dev-cli-tests reference the convention analyzer; the
  analyzer project and timewarp-templates do not.
- `dotnet run tools/dev-cli/dev.cs -- self-install` (AOT): succeeded.
- `dev build`: 0 warnings, 0 errors.
- `dotnet run --project tests/tools/dev-cli-tests`: 342/342 passed.
- `dev template-smoke` (default variant): SUCCEEDED.
- `ganda repo audit`: exit 0. Only the pre-existing warnings remain (`source-file-line-cap`,
  `kanban-landed`).

Gates run (implement pass):

- `dotnet build tools/dev-cli/dev.cs`: 0 warnings, 0 errors.
- `dotnet build tools/dev-cli/dev.cs -p:UseAnalyzerPackages=true` (generated-app path): 0 warnings.
- `dotnet build tools/agent-identity-cli/agent.cs`: 0 warnings, 0 errors.
- `dotnet run tools/dev-cli/dev.cs -- self-install` (AOT): succeeded.
- `dev build`: succeeded.
- `dotnet run --project tests/tools/dev-cli-tests`: 342/342 passed.
- `dev template-smoke` (default variant): SUCCEEDED.

### Review

- Round 1 (effort 1, general): bug 0, suggestion 4 (M1–M4), nit 0. The fix pass fixed all four;
  dispositions are in `review/round-1/merged.md`. Round 2 re-reviews.
- The reviewer re-ran these gates: dev-cli and agent-identity-cli builds (0 warnings), dev-cli tests
  342/342, and `ganda repo audit` (exit 0, only the pre-existing warnings).

### How to validate

Smoke:

```bash
dotnet build tools/dev-cli/dev.cs
dotnet build tools/agent-identity-cli/agent.cs
dotnet msbuild timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj -getProperty:EnableConventionAnalyzers
# Exemption carries weight: add a 501-line .cs under tools/dev-cli/services/ and rebuild,
# or set the ExemptPackageContentFromAnalyzers Condition to false and rebuild.
```

Expect: both builds report 0 warnings and 0 errors. The msbuild query prints an empty line (the
template packer does not opt in). A 501-line file under
`tools/dev-cli/services/` fails the build with TWA0038. With the exemption target disabled, the build
fails with TWA0038 on the TimeWarp.Nuru.DevCli content files `release-command.cs` and
`nuget-version-service.cs`.

