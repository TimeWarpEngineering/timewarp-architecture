# Round 2 — merged findings
**Date:** 2026-10-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/services/aspire-run.cs:262
- Description: (round 1) The workflow agreement check passed whenever `<AspireVersion>` appeared anywhere in
  `workflow.yml`, comments included.
- Source: general
- Disposition notes: Fixed in 949eb3dd8. `#` comments are stripped. Each `Aspire.Cli` install line must
  pass `--version $var`, and `var=` must be a `sed` read of `<AspireVersion>` in `Directory.Packages.props`.
  The tests `CommentOnlyWorkflow_Should_Fail`, `VersionFromAnotherSource_Should_Fail`, and
  `InstallVariableNotTheSedRead_Should_Fail` cover it. Re-verified in round 2: 308/308 pass.

### M2 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/services/aspire-run.cs:138
- Description: (round 1) A prerelease `<AspireVersion>` did not parse and blocked every Aspire verb with a
  misleading "could not read" message.
- Source: general
- Disposition notes: Fixed in 949eb3dd8. `ParseVersionCore` is shared with `ParseCliVersion`. An unparseable
  present value is named in the refusal (`ReadRepoAspireVersionError`, `DescribeMissingRepoAspireVersion`).
  The tests `PrereleaseProperty_Should_ParseNumericCore` and `UnparseableProperty_Should_NameTheValue`
  cover it. Re-verified in round 2.

## Duplicates / conflicts

- None (single reviewer, no new findings).
