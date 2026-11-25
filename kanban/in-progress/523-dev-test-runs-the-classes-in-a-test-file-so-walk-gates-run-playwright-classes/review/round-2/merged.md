# Round 2 — merged findings
**Date:** 2026-11-26
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tools/dev-cli/services/test-file-selection.cs:186
- Description: Round 1: Jaribu `--filter-class` is an ordinal ignore-case contains match, so `TimeWarp.Architecture.Foundation.Domain.Tests.Equality` also selects `…Equality_Operators`. Round 2 re-checked the fix delta. `Collisions` compares each chosen filter with every other registered FullName in the project. A hit adds an error naming the file, the class, and the other class, and the file adds no run. `test-command.cs` returns selection errors before build or `dotnet test`. `ClassWhoseFilterIsInAnotherClassName_Should_FailNamingFileClassAndOtherClass` pins the pair; `LongerClassName_Should_StillRunAlone` pins the longer name. The recorded live `entity-tests.cs` refusal matches that message. The same-namespace class list in `foundation-domain-tests` has this one prefix pair.
- Suggestion: (round 1, done) Refuse the file before `dotnet test` and pin the pair with a mapping test.
- Source: general
- Disposition notes: Fixed in `1378d535f`. Round 2 re-verified by reading the delta, the command error path, the mapping tests, and the recorded fix-pass gates. No reopen.

## Duplicates / conflicts

- One reviewer. No new findings. M1 carried forward as fixed.
