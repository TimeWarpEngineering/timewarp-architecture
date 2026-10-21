# Round 2 — merged findings
**Date:** 2026-10-22
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 0 | 2 | 0 |

## Resolved prior

### M1 — Severity: nit — Status: fixed
- File: tools/dev-cli/services/deploy-operate.cs:781
- Disposition notes: `ad1a2f30e`. The line is `No browser opener worked; open {url}` and the test pins it.

### M2 — Severity: nit — Status: fixed
- File: tools/dev-cli/endpoints/deploy-command.cs:400, :419
- Disposition notes: `ad1a2f30e`. `finally` prints `TemporaryForwardStopped` directly, and the
  success branch uses a non-null `CompleteMessage` local.

## Issues

### M3 — Severity: suggestion — Status: open
- File: tools/dev-cli/services/deploy-pipeline.cs:117 (also `endpoints/deploy-command.cs:427-434`,
  `tests/tools/dev-cli-tests/deploy-pipeline-tests.cs:219`)
- Description: After M2, nothing in production passes `temporaryPortForward: true`. Every
  `AnnounceAsync` call passes `false`. The `true` branch of `FollowUpAfterSmoke`, `SmokeFollowUp.Hint`,
  and the `AnnounceAsync` parameter are reached only from the
  `KubernetesWithoutAnExternalAddress_Should_HintDevOpenAndNotOpen` test. That test covers a decision
  the command never consults. The Design region (`deploy-pipeline.cs:16-17`), the XML docs, and the
  task Results still say the decision makes that choice.
- Suggestion: Remove the parameter that is always false. Drop `temporaryPortForward` from
  `FollowUpAfterSmoke` and `AnnounceAsync`, and drop `Hint` from `SmokeFollowUp`. Have the kubernetes
  test check `TemporaryForwardStopped` directly. Then fix the Design region, the XML docs, and Results.
  The alternative is to route the temporary-forward success branch back through `FollowUpAfterSmoke`.
- Source: general (Issue 1)
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).

## Verification re-run (reviewer)

- `dotnet test -c Release` in `tests/tools/dev-cli-tests`: 315 passed, 0 failed.
- `dotnet build tools/dev-cli/dev.cs --nologo -v q`: exit 0.
