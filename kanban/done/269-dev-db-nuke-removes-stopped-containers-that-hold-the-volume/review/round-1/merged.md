# Round 1 — merged findings
**Date:** 2026-10-01
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/services/db-nuke.cs:89
- Description: The running-container refusal claims "Nothing was removed" after `aspire stop --volumes` has already run.
- Suggestion: Make the claim narrower.
- Source: general
- Disposition notes: Changed to "No container or remaining volume was removed". Test assertion updated.

### M2 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/endpoints/db-nuke-command.cs:217
- Description: The `docker rm` failure says "No volume was removed" after `aspire stop --volumes`.
- Suggestion: Make the claim narrower.
- Source: general
- Disposition notes: Changed to "The remaining volume(s) were not removed".

### M3 — Severity: nit — Status: fixed
- File: tools/dev-cli/endpoints/db-nuke-command.cs:168
- Description: The sweep message "Aspire adopted but does not own" is wrong when a stopped container blocked removal of an owned volume.
- Suggestion: Name what is actually left.
- Source: general
- Disposition notes: Changed to "volume(s) `aspire stop` left behind".

### M4 — Severity: nit — Status: fixed
- File: tools/dev-cli/endpoints/db-nuke-command.cs:29
- Description: Over-long line in the Design region.
- Suggestion: Rewrap.
- Source: general
- Disposition notes: Rewrapped.

## Duplicates / conflicts

- None.
