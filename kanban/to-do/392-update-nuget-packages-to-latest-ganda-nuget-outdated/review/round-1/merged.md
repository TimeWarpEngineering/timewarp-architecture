# Round 1 — merged findings
**Date:** 2026-10-20
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: Directory.Packages.props:135
- Description: The task 201 history paragraph says "Stay on 2.12.2 until …"; the pin is now 2.13.0.
- Suggestion: Change it to "Stay on 2.x until …".
- Source: general
- Disposition notes: wontfix (reviewer, 2026-10-20). The sentence sits inside the dated task 201
  entry of a re-check log. Line 130 ("pins the latest 2.x") and the task 392 entry ("Pin moved to
  2.13.0") give the live pin, and the `Version` attribute is authoritative. This comment alone
  does not justify a fix round. Change it the next time the block is touched (task 257).

## Duplicates / conflicts

- None (single reviewer).
