# Fix ganda repo audit failures

## Description

`ganda repo audit` reports 5 failing checks (11 pass, 1 skip). Bring the repo into
compliance so the audit runs clean. Re-run `ganda repo audit` after each fix to verify.

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

- [ ] Reference TimeWarp.Build.Tasks in Directory.Build.props
- [ ] Add required banned API rule
- [ ] Resolve all 55 missing PackageVersion entries (add to CPM or retire)
- [ ] Create documentation/, tests/, skills/, kanban/backlog/
- [ ] Add .github/workflows/workflow.yml
- [ ] `ganda repo audit` passes with no blocking failures

## Notes

Audit run on 2026-06-11 with ganda against the dev worktree. Related existing tasks:
053-050-019 (test project migration), 056 (nullable reference types).
