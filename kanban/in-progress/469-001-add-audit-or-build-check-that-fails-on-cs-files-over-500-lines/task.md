# Add audit or build check that fails on .cs files over 500 lines

## Parent

469

## Description

Add a check that fails when a tracked `.cs` file exceeds 500 lines. Decide whether it lives in
`ganda repo audit` (repo-wide, runs in tw-pr gates) or as an analyzer or MSBuild target in this
repo (runs in `dev build`, ships to generated apps). Prefer the one that also protects generated
apps, since this repo is the template. Record the decision in the check's Design region.

## Requirements

- Threshold 500 lines, configurable in one place.
- Exempt generated code: EF migrations under `platform/postgres/migrations/`, `*.Designer.cs`,
  `*.g.cs`, `goldens/`, and `obj/`. Decide whether `kanban/**` research scripts are exempt.
- Clear message naming the file and its line count.
- Enabled only after 469-002 through 469-006 merge. Until then the check may land disabled or
  as a warning, but `dev build` must stay 0 warnings, so a disabled check is the safer landing.

## Depends on

- 469-002
- 469-003
- 469-004
- 469-005
- 469-006

## Goal (implementer restatement)

Make the build fail when a C# source file is longer than 500 lines, so the refactor done in
469-002 through 469-006 cannot regress. The check must also reach generated apps, because this
repo is the template.

Acceptance:

- One place sets the 500-line limit.
- Generated code is exempt: EF migrations under `platform/postgres/migrations/`, `*.Designer.cs`,
  `*.g.cs`, `goldens/`, and `obj/`. `kanban/**` research scripts are exempt, with the reason
  recorded.
- The message names the file and its line count.
- Tests cover the threshold boundary, each exemption, and the message text.
- The check lands enabled, and `dev build` stays at 0 warnings and 0 errors on master content.
- The audit-vs-analyzer decision is recorded in the check's Design region.

## Checklist

- [x] Choose audit vs analyzer and record why
- [x] Implement with exemptions
- [x] Tests for threshold, exemption, and message
- [x] Enable once master has no offenders

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-03)
- Implemented under ganda task work, implement oracle (2026-11-04).
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 11819ef8-9d7b-4f28-aa8c-6f30dae640a0, max-turns 500, Done — 2026-11-04T02:56:18Z

## Notes

- The check is per file. It cannot see class size, so it accepts the Jaribu `partial` exception
  recorded in the parent (469) Requirements without any special casing.
- As of 2026-11-04, after 469-002 through 469-006 merged, the only tracked `.cs` files over 500
  lines are the two EF migration files under `platform/postgres/migrations/` and
  `kanban/done/238-.../research/run-rank-experiment.cs`. The exemption list must cover those, so
  the check can land enabled.

## Results

TWA0038 (`FileLengthAnalyzer`) is in the convention analyzers. It fails a build when a C# file
is longer than 500 lines. It lands enabled as a Warning, and the build treats warnings as errors.

- Analyzer, not a ganda audit check. The convention analyzers run in every `dev build` here and,
  through the platform Analyzers package, in every generated app. Ganda is private and does not
  ship. The reasoning is in the Design region of
  `source/analyzers/timewarp-architecture-convention-analyzers/file-length-analyzer.cs`.
- The limit is set in one place: `FileLengthAnalyzer.DefaultMaximumLines = 500`. A generated app can
  override it with `dotnet_diagnostic.TWA0038.max_lines = <n>` under `[*.cs]` in `.editorconfig`.
  This repo does not set that key.
- How each exemption works:
  - `GeneratedCodeAnalysisFlags.None` skips files Roslyn treats as generated: an auto-generated
    header, `*.g.cs`, `*.Designer.cs`, and source-generator or SDK obj/ output.
  - EF migrations, including the hand-named `<timestamp>_<Name>.cs` files, are skipped because
    the existing `.editorconfig` section from 147-007 sets `generated_code = true` on them.
  - Any path under an `obj/` or `goldens/` segment is skipped explicitly.
  - `kanban/**` is exempt because no project there loads the analyzers. A done task's research
    script is a record, not product code.
- Line count matches `wc -l`.
- The diagnostic anchors on the first token, so `#pragma warning disable TWA0038` at the top of a
  file suppresses it.
- Message: `File 'x.cs' has N lines, over the 500-line limit; extract a responsibility into its own type`.
- Tests: `tests/analyzers/timewarp-architecture-analyzers-tests/file-length-analyzer-tests.cs`
  covers 10 scenarios:
  - 500 lines is clean and 501 is flagged.
  - The exact message text.
  - An auto-generated header.
  - `*.g.cs` and `*.Designer.cs`.
  - A migrations `generated_code` section.
  - obj/ and goldens/ paths.
  - The `.editorconfig` override.
  - `#pragma` suppression.
  - Line counting.
- Release: the Analyzers package changed, so the platform version moved from 2.0.0-beta.30 to
  2.0.0-beta.31. That covers `source/Directory.Build.props`, `timewarp-templates/Directory.Build.props`,
  and the platform pins in `Directory.Packages.props`, all in one commit. The TimeWarp.Contracts pin
  stays on its own line.
- Skill: `tw-feature-placement/references/co-located-jaribu-runfiles.md` now names TWA0038 next to
  the existing 500-line runfile rule.

Gates run (all foreground, in this worktree):

- Analyzer tests: `dotnet run --project tests/analyzers/timewarp-architecture-analyzers-tests`
  passed, 269 total and 0 failed.
- `./bin/dev build`: 0 warnings, 0 errors. Re-run after the version bump.
- A probe padded `source/foundation/foundation-domain/entities/base/entity-version.cs` to 544
  lines. The build of `foundation-domain.csproj` then failed with
  `error TWA0038: File 'entity-version.cs' has 544 lines, over the 500-line limit; ...`.
  The file was restored afterwards.
- `./bin/dev template-smoke`: SUCCEEDED, with the locally packed Analyzers package that includes
  TWA0038.
- `./bin/dev check-version`: the source version is new and safe to release.
- `./bin/dev lint-skills`: 9 of 9 passed.
- `ganda repo audit`: passes. The one advisory warning is kanban-landed, about live claims of
  other tasks that have no worktree. It is not on this id and not fixable here.

Follow-up: `tools/` (the dev CLI and the agent-identity CLI) does not load the convention
analyzers. TWA0038 reaches only the dev-cli files that `tests/tools/dev-cli-tests` compile-includes.
A trial `tools/Directory.Build.props` that wired the analyzers found two problems:

- TWA0008 template-token hits in `services/template-smoke/`.
- TWA0038 hits on TimeWarp.Nuru.DevCli NuGet content files under `~/.nuget/packages`
  (609 and 704 lines).

Covering `tools/` needs its own decision: which rules apply there, and how to exempt package
content files. The trial was backed out, and that decision is left to a separate task.

### How to validate

Smoke:

```bash
dotnet run --project tests/analyzers/timewarp-architecture-analyzers-tests
./bin/dev build
f=source/foundation/foundation-domain/entities/base/entity-version.cs
cp "$f" /tmp/ev.cs && seq 1 520 | sed 's|^|// filler |' >> "$f"
dotnet build source/foundation/foundation-domain/foundation-domain.csproj; cp /tmp/ev.cs "$f"
```

Expect:

- The analyzer test run reports failed: 0, and its list includes the `Should_Limit_File_Length` cases.
- `dev build` reports 0 warnings and 0 errors. The EF migration Designer and snapshot files over
  500 lines are not flagged.
- The padded probe build fails with
  `error TWA0038: File 'entity-version.cs' has 544 lines, over the 500-line limit; extract a responsibility into its own type`.
- After the restore, `git status` is clean for that file.
