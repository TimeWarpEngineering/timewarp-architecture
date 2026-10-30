# Round 1 — merged findings
**Date:** 2026-10-30
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: Directory.Packages.props:166
- Description: The pin comment block still states the pre-456 versions as current. Lines 166–173 say
  EF Core, dotnet-ef, and Npgsql "stay on the RC2 train", that "nuget.org has no Npgsql RC3", that
  Npgsql pins EF Core "exactly at 11.0.0-rc.2.26504.105", and that System.ServiceModel.Primitives
  "stays 10.0.652802". The pins are now RC3 and 10.1.0. The task-456 paragraph was appended, and it
  contradicts the earlier text instead of replacing it. This fails the task requirement "Update any
  pin comments so they match the new versions."
- Suggestion: Rewrite those present-tense sentences to describe the RC3 / 10.1.0 state and why the EF
  family and Npgsql move together. Trim the task-456 OpenApi re-check to what is new (the NU1608
  restore result and the date). Leave the historical re-check lines alone.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
