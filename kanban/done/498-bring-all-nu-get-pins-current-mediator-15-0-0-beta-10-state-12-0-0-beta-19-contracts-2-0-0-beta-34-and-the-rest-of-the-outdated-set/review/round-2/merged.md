# Round 2 — merged findings
**Date:** 2026-11-22
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: .github/workflows/workflow.yml:435
- Description: The release job installs the pinned Aspire CLI before Run CI Pipeline, with the same script as the ci-group job. Probe mode skips the step. A published release and a merge dispatch install it. The workflow default shell is pwsh.
- Suggestion: none. The round 1 suggestion is in the tree.
- Source: general
- Disposition notes: confirmed fixed in round 2 by reading `2df38b0bc`. No re-open.

### M2 — Severity: bug — Status: fixed
- File: tools/dev-cli/services/aspire-publish.cs:39
- Description: `AspirePublish.MinimumCliVersion` and `AspireDeploy.MinimumCliVersion` are 17.0 and are the floors `dev publish`, `dev deploy`, and `dev deprovision` enforce. `dev db nuke` stays on 13.6. The tw-deploy skill and the Design comments say the same.
- Suggestion: none. The round 1 suggestion is in the tree.
- Source: general
- Disposition notes: confirmed fixed in round 2 by reading the constants and the call sites in `2df38b0bc`. No re-open.

## Duplicates / conflicts

- No new findings. M1 and M2 carried forward from round 1. One reviewer.
