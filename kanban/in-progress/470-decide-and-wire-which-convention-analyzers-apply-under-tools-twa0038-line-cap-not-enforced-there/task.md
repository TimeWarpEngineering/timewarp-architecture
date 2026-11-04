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

- One `tools/Directory.Build.props` wires the full convention analyzer set; both tool props import it.
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

## Results

- New `tools/Directory.Build.props` (imported by both tool props through `GetPathOfFileAbove`)
  wires the full convention analyzer set. Monorepo: ProjectReference. Generated apps
  (`UseAnalyzerPackages=true`): the platform Analyzers package, because `tools/dev-cli` ships in the
  template and `source/analyzers` does not. The decision is in that file's Design region.
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
- `file-length-analyzer.cs` Design region points at the tools/ exemption.
- `ganda repo audit`: exit 0. Remaining warnings are not from this change: `source-file-line-cap`
  (Warning by 469-001 policy: EF migrations and a done task's research script) and `kanban-landed`
  (stale claims on other tasks).

Gates run:

- `dotnet build tools/dev-cli/dev.cs`: 0 warnings, 0 errors.
- `dotnet build tools/dev-cli/dev.cs -p:UseAnalyzerPackages=true` (generated-app path): 0 warnings.
- `dotnet build tools/agent-identity-cli/agent.cs`: 0 warnings, 0 errors.
- `dotnet run tools/dev-cli/dev.cs -- self-install` (AOT): succeeded.
- `dev build`: succeeded.
- `dotnet run --project tests/tools/dev-cli-tests`: 342/342 passed.
- `dev template-smoke` (default variant): SUCCEEDED.

### How to validate

Smoke:

```bash
dotnet build tools/dev-cli/dev.cs
dotnet build tools/agent-identity-cli/agent.cs
# Exemption carries weight: add a 501-line .cs under tools/dev-cli/services/ and rebuild,
# or set the ExemptPackageContentFromAnalyzers Condition to false and rebuild.
```

Expect: both builds report 0 warnings and 0 errors. A 501-line file under
`tools/dev-cli/services/` fails the build with TWA0038. With the exemption target disabled, the build
fails with TWA0038 on the TimeWarp.Nuru.DevCli content files `release-command.cs` and
`nuget-version-service.cs`.

