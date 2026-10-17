# Round 1 — merged findings
**Date:** 2026-10-17
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: .github/workflows/workflow.yml:250
- Description: A golden drift fails the CI step with a raw `git diff` and no remedy line.
- Suggestion: Echo "run `dev regenerate-generated` and commit" before `exit 1`.
- Source: general
- Disposition notes: wontfix (decided by the review oracle). This is polish only, and the gate already
  fails correctly. The step comment, the AGENTS.md "Generated ingress prefixes" section, and the
  command's Design region all state the remedy. One nit does not justify a fix pass. Fold it into the
  next edit of this step.

## Duplicates / conflicts

- None (single reviewer).
