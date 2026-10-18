# Round 1 — merged findings
**Date:** 2026-10-18
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/foundation/timewarp-contracts/timewarp-contracts.csproj:12 (release path tools/dev-cli/endpoints/workflow-command.cs:58, :256-260)
- Description: The separate `TimeWarp.Contracts` `<Version>1.0.0-beta.1` has no gate.
  Release repacks the package every run and pushes with `--skip-duplicate`, and
  `dev check-version` checks it at the repo version `2.0.0-beta.20`. A content change
  without a version bump then ships silently as the stale nuget `1.0.0-beta.1`, under a new
  `TimeWarp.Foundation.Contracts` that needs the new API.
- Suggestion: add a dev-cli check that fails when the package's own version is already
  published and `source/foundation/timewarp-contracts/` changed since that release. Run it
  before push in release and on the PR / `tw-pr` path. Make the gate output state the
  package's real version, and document the bump rule.
- Source: general
- Disposition notes: HUMAN DECISION (Steven, 2026-10-18): do NOT add a new dev-cli check. Remove the separate `<Version>1.0.0-beta.1` from timewarp-contracts.csproj so `TimeWarp.Contracts` takes the repo version from `source/Directory.Build.props` like every other foundation package; the existing `dev check-version` then covers it. Update the task Versioning section, the package readme and the csproj comment to match (the package versions with the repo, no independent 1.0.0-beta.1 line; 384-001's "promote to 1.0.0" becomes "leave prerelease with the repo"). Prefer removing code over adding it. Fixed accordingly: `<Version>` removed; check-version lists TimeWarp.Contracts at 2.0.0-beta.20.

### M2 — Severity: suggestion — Status: fixed
- File: source/foundation/foundation-contracts-generators/contracts-generator.cs:282, :308; source/analyzers/timewarp-architecture-analyzers/models/endpoint-metadata.cs:278, :336
- Description: The two `[Operation]` readers disagree on blank values: `IsNullOrEmpty` in
  the contracts generator, `IsNullOrWhiteSpace` in the endpoint model. Both hand-code the
  attribute FQN, and the tests use a stub attribute, so a rename of the real type goes
  undetected.
- Suggestion: use one `IsNullOrWhiteSpace` rule in both readers. Add a test that ties the
  FQN constant to the real `TimeWarp.Contracts.OperationAttribute` type, or compile the
  generator test against the real assembly.
- Source: general
- Disposition notes: Fixed. Both readers use `IsNullOrWhiteSpace` for name and description (a whitespace name supplies no operation). Generator and endpoint-metadata tests reference the real `timewarp-contracts` project, and `operation-attribute-name-tests.cs` pins the FQN.

## Duplicates / conflicts

- None (single reviewer).
