# Round 3 — merged findings
**Date:** 2026-10-22
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 2 | 0 |

## Resolved prior

### M1 — Severity: nit — Status: fixed
- File: tools/dev-cli/services/deploy-operate.cs:781
- Disposition notes: `ad1a2f30e` (verified round 2).

### M2 — Severity: nit — Status: fixed
- File: tools/dev-cli/endpoints/deploy-command.cs:400, :419
- Disposition notes: `ad1a2f30e` (verified round 2).

### M3 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/services/deploy-pipeline.cs:118
- Disposition notes: `070dbbc59`. `temporaryPortForward` is gone from `FollowUpAfterSmoke` and `AnnounceAsync`.
  `SmokeFollowUp` has no `Hint`. The kubernetes test asserts `TemporaryForwardStopped`. The Design region and XML docs
  say the temporary forward does not call the follow-up.

## Issues

None new.

## Duplicates / conflicts

- None (single reviewer).
