# Round 1 — general
**Date:** 2026-11-04
**Scope reviewed:** branch vs master: `file-length-analyzer.cs`, `file-length-analyzer-tests.cs`,
`AnalyzerReleases.Unshipped.md`, version bump (`source/Directory.Build.props`,
`timewarp-templates/Directory.Build.props`, `Directory.Packages.props`), and
`skills/tw-feature-placement/references/co-located-jaribu-runfiles.md`.

## Summary

The change adds TWA0038, a syntax-tree analyzer in the convention analyzers that fails the build
when a C# file is longer than 500 lines. The limit is set in one constant, with an
`.editorconfig` override. Generated code is exempted structurally: `GeneratedCodeAnalysisFlags.None`,
the existing migrations `generated_code = true` section, and `obj/` and `goldens/` path segments.
The analyzer-vs-audit decision is in the Design region. Risk is low.

What I checked myself:

- `dotnet run --project tests/analyzers/timewarp-architecture-analyzers-tests`: 269 total, 0 failed.
- The only tracked `.cs` files over 500 lines are the migration Designer (521) and model snapshot
  (523), both covered by the `generated_code` section and by Designer naming, and the kanban
  research script (784). `kanban/` has no `Directory.Build.props`, and the root props do not
  reference the analyzer, so the kanban exemption claim holds.
- 469-002 through 469-006 are all in `done/`, so landing the check enabled matches the task.
- The version bump matches the policy: master is still on beta.30, and the pins moved in the same commit.

`dev build` and `template-smoke` were not re-run in this review. The analyzer tests and the
offender scan were.

## Issues

### Issue 1 — Severity: suggestion
- File: source/analyzers/timewarp-architecture-convention-analyzers/file-length-analyzer.cs:6
- Description: `tools/` (dev CLI, agent-identity CLI) does not load the convention analyzers, so
  TWA0038 does not guard it. The task asks for a check on tracked `.cs` files. `tools/` already has
  files near the limit: `tools/dev-cli/services/aspire-run.cs` (496) and
  `tools/dev-cli/endpoints/template-publish-smoke-command.cs` (494). The implementer found and
  recorded the gap in Results as a follow-up but did not file a task, so it lives only in prose.
- Suggestion: File a kanban task to wire convention analyzers (or just TWA0038) into `tools/`, and
  decide how to exempt NuGet content files under `~/.nuget/packages` and the TWA0008 template-token
  hits in `services/template-smoke/`.
- Status: open

### Issue 2 — Severity: nit
- File: source/analyzers/timewarp-architecture-convention-analyzers/file-length-analyzer.cs:29
- Description: The Design region and Results both say the line count "matches `wc -l`". That is
  true only when the file ends with a line break. For `"a\nb"`, `wc -l` prints 1 and `CountLines`
  returns 2, as the test `Given_Trailing_Line_Break_Is_Not_Counted` asserts. The behavior is
  reasonable (it counts lines the way an editor does); only the wording is off.
- Suggestion: Reword to "counts lines as an editor shows them; a trailing line break does not open
  a counted line".
- Status: open

### Issue 3 — Severity: nit
- File: source/analyzers/timewarp-architecture-convention-analyzers/file-length-analyzer.cs:19
- Description: Typo in the Design region: "Roslyn treats an auto-generated comment headers".
- Suggestion: "Roslyn treats an auto-generated comment header, …".
- Status: open
