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

### M1 — Severity: bug — Status: open
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
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/foundation/foundation-contracts-generators/contracts-generator.cs:282, :308; source/analyzers/timewarp-architecture-analyzers/models/endpoint-metadata.cs:278, :336
- Description: The two `[Operation]` readers disagree on blank values: `IsNullOrEmpty` in
  the contracts generator, `IsNullOrWhiteSpace` in the endpoint model. Both hand-code the
  attribute FQN, and the tests use a stub attribute, so a rename of the real type goes
  undetected.
- Suggestion: use one `IsNullOrWhiteSpace` rule in both readers. Add a test that ties the
  FQN constant to the real `TimeWarp.Contracts.OperationAttribute` type, or compile the
  generator test against the real assembly.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
