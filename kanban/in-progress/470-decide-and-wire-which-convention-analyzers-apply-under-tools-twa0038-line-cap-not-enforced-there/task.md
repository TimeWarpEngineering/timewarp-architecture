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

## Requirements

- Record the decision in a Design region (the `tools/Directory.Build.props` or the analyzer).
- Package content files are exempt without a per-file pragma.
- `dev build` stays 0 warnings. dev-cli tests pass. `dev` AOT self-install still builds.
- The `ganda repo audit` `source-file-line-cap` check runs as Warning with reason in
  `.editorconfig` (469-001); revisit once ganda offers an exclude key.

## Checklist

- [ ] Steve picks the rule set for tools/
- [ ] Wire analyzers with the agreed exemptions
- [ ] Gates green

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-04)
