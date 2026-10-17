# Round 1 — general
**Date:** 2026-10-17
**Scope reviewed:** commit `ee13467a9` (workflow step, AGENTS.md note, golden, generator determinism,
`dev regenerate-generated`, generator and dev-cli tests)

## Summary

The change makes `IngressRoutePrefixGenerator` byte-stable and checks in its output as
`goldens/ingress/WebServerApiRoutePrefixes.g.cs`. It also adds `dev regenerate-generated`, which builds
yarp and the AppHost and writes the golden only when both emits agree, plus a `fast`-job CI step that
runs the command and fails on any diff or untracked file under `goldens/ingress`. The server-offer
family is correctly treated as having nothing to regenerate: task 282 removed it, and the only
`Offerable` text left in `source/` is a historical comment in `diagnostic-descriptors.cs`. Risk is low.

Verified against the repo:

- Both compiler emits, `artifacts/generated/{yarp,aspire-app-host}/…/WebServerApiRoutePrefixes.g.cs`,
  are byte-identical to the golden (`cmp`). The golden starts with `//` (no BOM) and ends with a single LF.
- `IngressRoutePrefixGenerator_` passed 15/15, and `GeneratedIngressPrefixes_` passed 4/4 (Release,
  run by the reviewer).
- `ganda repo audit` passes every check, including kebab-path and region annotations.
- `ProjectEmitDirectory` hard-codes `artifacts/generated/<project>`. That matches
  `CompilerGeneratedFilesOutputPath = $(ArtifactsDirectory)generated/$(MSBuildProjectName)`, and
  `ArtifactsDirectory` is `$(RepositoryRoot)artifacts/`.
- In repo mode, both hosts load the analyzer through a `ProjectReference`, so CI regenerates with the
  changed generator source and not a published package.
- `goldens/` is not packed: the template csproj uses explicit Content globs. It does not leak into
  generated apps.
- The case-collapse logic sets values on an `OrdinalIgnoreCase` dictionary and emits `.Values`, so the
  ordinal-least spelling wins regardless of reference order. The test with swapped reference order
  pins this.
- The `regenerate-generated` failure paths set `Environment.ExitCode`, the same as `build-command.cs`
  and other dev commands.
- Restatement vs task: the restatement matches the task. The offer family was already gone when the
  task was filed (the task cites 280–282, and 282 is the removal), and the restatement says so
  explicitly. Not a misread.

## Issues

### Issue 1 — Severity: nit
- File: .github/workflows/workflow.yml:250
- Description: When the golden drifts, `git diff --exit-code -- goldens/ingress` fails the step with a
  raw diff and no remedy line. The untracked branch does echo a message. Someone reading the CI log has
  to know to look in AGENTS.md for "re-run `dev regenerate-generated` and commit".
- Suggestion: Wrap the diff in `if ! git diff --exit-code …; then echo "Run dev regenerate-generated and commit goldens/ingress (see AGENTS.md)"; exit 1; fi`.
- Status: open
