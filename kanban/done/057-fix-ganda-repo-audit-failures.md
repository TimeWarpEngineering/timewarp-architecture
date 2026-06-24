# Fix ganda repo audit failures

## Description

`ganda repo audit` reports 5 failing checks (11 pass, 1 skip). Bring the repo into
compliance so the audit runs clean.

## Related

- 048 (in progress) scaffolded the same convention files the audit fixers touch
  (Directory.Build.props, BannedSymbols.txt, directory skeleton) via
  `ganda repo enforce-dev-cli --fix`. Coordinate so the two do not produce
  conflicting edits.
- 053-050-019 (test project migration) owns most of the cpm-consistency fallout.

## Plan

1. Run `ganda repo audit --fix` first. Four of the five failures are auto-fixable
   (assembly-metadata, banned-symbols, directory-structure, workflow-file). Review
   the generated changes before committing.
2. Resolve cpm-consistency manually — it is the only non-fixable check and needs
   per-package decisions (see below).
3. Re-run `ganda repo audit` to confirm no blocking failures remain.

## Requirements

All 5 failing checks below pass and the audit reports no blocking failures.

### 1. assembly-metadata (Error)

`TimeWarp.Build.Tasks` is not referenced in `Directory.Build.props`.

### 2. banned-symbols (Error)

Required banned API rule is missing. `banned-api-analyzers` passes (configuration files
exist), so only the required rule entry needs to be added to `BannedSymbols.txt`.

### 3. cpm-consistency (Error)

55 missing `PackageVersion` entries — packages referenced by projects but absent from
`Directory.Packages.props`. Unique packages (mostly test dependencies):

- Aspire.Hosting.Testing
- AutoFixture / AutoFixture.AutoFakeItEasy
- Boxed.AspNetCore / Boxed.DotnetNewTest
- coverlet.collector
- FakeItEasy
- Fixie / Fixie.TestAdapter / TimeWarp.Fixie
- FluentAssertions
- GlobalUsingsAnalyzer (from BuildProps)
- Microsoft.AspNetCore.Mvc.Testing
- Microsoft.CodeAnalysis.CSharp.Analyzer.Testing / Microsoft.CodeAnalysis.CSharp.Workspaces
- Microsoft.NET.Test.Sdk
- Microsoft.Playwright
- Shouldly
- TimeWarp.SourceGenerators
- xunit / xunit.runner.visualstudio

Most originate from test projects that have not been migrated to the root source tree
yet — coordinate with task 053-050-019 (migrate test projects) rather than duplicating
effort. Decide per package: add a `PackageVersion` to `Directory.Packages.props` or
retire the reference with the old project tree.

### 4. directory-structure (Error)

Missing 4 expected directories:

- `documentation/`
- `tests/`
- `skills/`
- `kanban/backlog/`

`tests/` overlaps with task 053-050-019. For the others, create the directory with its
expected content or initial placeholder per the ganda convention.

### 5. workflow-file (Error)

`.github/workflows/workflow.yml` is missing. Add the expected CI workflow file.

## Checklist

- [x] Run `ganda repo audit --fix` and review/commit the generated changes
      (covers assembly-metadata, banned-symbols, directory-structure, workflow-file)
- [x] Resolve all 55 missing PackageVersion entries (add to CPM or retire)
- [x] `ganda repo audit` passes with no blocking failures

## Notes

Audit run on 2026-06-11 with ganda against the dev worktree. Related existing tasks:
053-050-019 (test project migration), 056 (nullable reference types).

## Implementation Notes

Completed 2026-06-11. Final audit: 16 passed, 0 failed, 1 skipped.

- `ganda repo audit --fix` handled the four fixable checks: TimeWarp.Build.Tasks
  reference (props reformatted by hand afterward — the fixer appends unformatted
  one-liners), ProcessStartInfo banned-symbol rule, directory skeleton with .gitkeep
  files, and the CI workflow.yml. TimeWarp.Build.Tasks only injects CommitHash and
  CommitDate assembly metadata; it does not alter build behavior.
- cpm-consistency: added a "Test Packages" ItemGroup to Directory.Packages.props with
  the 21 unique missing packages. Versions sourced from
  TimeWarp.Architecture/Directory.Packages.props; family-versioned packages aligned
  with existing root pins (Mvc.Testing 10.0.9, Aspire.Hosting.Testing 13.4.3,
  CodeAnalysis.CSharp.Workspaces 5.3.0).
- Added global.json pinning SDK 10.0.301 (rollForward latestFeature). The machine now
  has the .NET 11.0.100-preview.5 SDK installed alongside 10.x, and with
  AnalysisLevel=latest-all plus warnings-as-errors the preview SDK's newer analyzers
  produced 423 style errors. Pinning keeps the repo deterministic; remove the pin
  deliberately when moving to .NET 11.
