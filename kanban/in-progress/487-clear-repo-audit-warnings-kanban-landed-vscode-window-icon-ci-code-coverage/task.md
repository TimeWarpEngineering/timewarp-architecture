# Clear repo audit warnings: kanban-landed, vscode-window-icon, ci-code-coverage

## Description

`ganda repo audit` reported warnings for `kanban-landed`, `vscode-window-icon`, `ci-code-coverage`,
and `source-file-line-cap`. The first two were cleared locally outside this task (stale claims
released; ignored `.vscode/settings.json` generated). `source-file-line-cap` stays as is (EF-generated
migrations and a done-task research script; TWA0038 enforces). This task makes `ci-code-coverage`
pass by really collecting and uploading coverage in CI.

## Requirements

- `dev test` runs every test project with Microsoft Testing Platform coverage
  (`--coverage`, Cobertura) into `artifacts/coverage/<project>/coverage.cobertura.xml`.
- `Microsoft.Testing.Extensions.CodeCoverage` is a centrally pinned package referenced by test projects.
- CI uploads `artifacts/coverage` from each test matrix job.
- No `ci-code-coverage.severity = off`; no new test frameworks.

## Checklist

- [x] Read the audit check source and the Pele sibling pattern
- [x] Pin `Microsoft.Testing.Extensions.CodeCoverage` in Directory.Packages.props
- [x] Reference it from test projects (root Directory.Build.props, switched on by tests/Directory.Build.props)
- [x] Add the coverage flags to `dotnet test` in dev-cli (per-project results directory)
- [x] Update the dotnet-test-arguments test
- [x] Add the `coverage` upload step to workflow.yml
- [x] `ganda repo audit`: ci-code-coverage passes
- [x] `dev build`, `dev test --group fast` (13 Cobertura files), `dev check-version`
- [ ] `dev template-smoke`
- [ ] Commit, push, open PR

## Session

- Created: 3db28a25-2474-4f26-9c94-cf08f42e9c9a (2026-11-10)
