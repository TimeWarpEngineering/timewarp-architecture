# Round 2 — general
**Date:** 2026-10-22
**Scope reviewed:** fix delta `a129b32a9..7bca85a38` (round-1 base.sha to HEAD): `ad1a2f30e` (product, 34 lines) and `7bca85a38` (kitchen)

## Summary

The fix pass closes both round-1 nits. `NoBrowserMessage` no longer names `--no-browser`, and the test
pins the exact line. The temporary kubernetes forward prints `TemporaryForwardStopped` straight from
`finally`, and the success branch uses a non-null `CompleteMessage` local. The output is the same as
before. The M2 change has a side effect: no production caller passes `temporaryPortForward: true`
to `DeployPipeline.FollowUpAfterSmoke` anymore. That branch, `SmokeFollowUp.Hint`, and the
`AnnounceAsync` parameter are now reached only from a test.

## Prior findings

- M1: fixed. `tools/dev-cli/services/deploy-operate.cs:781` is `No browser opener worked; open {url}`.
  `deploy-operate-tests.cs:765` checks the whole string. The line is still shown in the two cases it
  covers: no launcher on PATH (`ChooseBrowserOpen`) and every launcher failed (`DeployBrowser.OpenAsync`).
- M2: fixed. `tools/dev-cli/endpoints/deploy-command.cs:400` and `:419`. The nullable local and the
  `??` are gone.

## Issues

### Issue 1 — Severity: suggestion
- File: tools/dev-cli/services/deploy-pipeline.cs:117 (also `endpoints/deploy-command.cs:427-434`,
  `tests/tools/dev-cli-tests/deploy-pipeline-tests.cs:219`)
- Description: Since M2, the temporary-forward path in `deploy-command.cs` no longer calls
  `FollowUpAfterSmoke`. The three `AnnounceAsync` calls (lines 278, 322, 370) all pass
  `temporaryPortForward: false`. So the `temporaryPortForward: true` branch of `FollowUpAfterSmoke`,
  `SmokeFollowUp.Hint`, and the `AnnounceAsync` `temporaryPortForward` parameter are never reached in
  production. The test `KubernetesWithoutAnExternalAddress_Should_HintDevOpenAndNotOpen` covers a
  decision the command never consults. The kubernetes "no browser, print the hint" behavior comes from
  inline code that no test reaches. Several texts still say the decision makes that choice: the Design
  region at `deploy-pipeline.cs:16-17`, the XML docs on `FollowUpAfterSmoke` and `AnnounceAsync`, and
  the task `## Results` and How-to-validate text.
- Suggestion: Pick one source of truth. Either (a) remove `temporaryPortForward` from
  `FollowUpAfterSmoke` and `AnnounceAsync`, remove `Hint` from `SmokeFollowUp`, and rewrite the
  kubernetes test to check `TemporaryForwardStopped` directly, then fix the Design region, the XML
  docs, and Results so they say the temporary forward never opens because it never calls the opener;
  or (b) have the temporary-forward success branch call `FollowUpAfterSmoke(..., temporaryPortForward: true, …)`
  again, keep a non-null local, and keep `finally` printing `TemporaryForwardStopped`. Option (a) is
  smaller and removes the parameter that is always false.
- Status: open

## Verification re-run (reviewer)

- `dotnet test -c Release` in `tests/tools/dev-cli-tests`: 315 passed, 0 failed.
- `dotnet build tools/dev-cli/dev.cs --nologo -v q`: exit 0.
- No live deploy (no cluster or subscription in this walk).

## Goal restatement check

The `## Goal (implementer restatement)` matches the task Description and Checklist, including the
kubernetes decision Steve made on 2026-10-22. No misread.
