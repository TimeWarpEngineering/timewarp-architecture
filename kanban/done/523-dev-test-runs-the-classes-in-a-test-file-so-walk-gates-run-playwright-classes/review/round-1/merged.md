# Round 1 — merged findings
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
- File: tools/dev-cli/services/test-file-selection.cs:150
- Description: `ClassFilters` emits `Namespace.Class` (or `+Class` when the declaration is indented deeper than the file's least-indented class) and `test-command.cs` passes that string to `dotnet test -- --filter-class`. TimeWarp.Jaribu.TestingPlatform defines that option as "Run only test classes whose FullName contains this substring (ordinal ignore-case)" (`jaribu-command-line-options.cs`, ArgumentArity.ExactlyOne). Every substring of a shorter FullName is also a substring of a longer FullName that starts with it, so the shorter class cannot be selected alone. The design comment at `test-file-selection.cs:25` says test class names in this repo do not collide that way. They do. `tests/foundation/foundation-domain-tests/entity-tests.cs` declares `Equality` in namespace `TimeWarp.Architecture.Foundation.Domain.Tests` (line 19). `enumeration-tests.cs` in the same project and the same namespace declares `Equality_Operators` (line 310). The filter `TimeWarp.Architecture.Foundation.Domain.Tests.Equality` is contained in `TimeWarp.Architecture.Foundation.Domain.Tests.Equality_Operators`, so `dev test` on `entity-tests.cs` also runs `Equality_Operators`. The requirement is that a file under any `tests/` project runs filtered to the test classes that file declares. The mapping tests never put a prefix pair in one project, so they stay green. The recorded dark-theme run (4 methods) does not cover this pair.
- Suggestion: Before starting `dotnet test`, derive the same filter string for every other class the owning project registers. If the chosen filter is an ordinal-ignore-case substring of a different class's FullName, fail the file with an error that names the file, the class, and the other class. Do not start the run. Jaribu's contains-match cannot isolate the shorter name. Pin the case with a mapping test: same project, same namespace, classes `Equality` and `Equality_Operators`, and the `Equality` file returns that error.
- Source: general
- Disposition notes: fixed in the round-2 fix pass. `TestFileSelection.Select` checks each class filter against every other registered FullName in the project and fails the file naming the file, the class, and the other class. Pinned by `ClassWhoseFilterIsInAnotherClassName_Should_FailNamingFileClassAndOtherClass`.

## Duplicates / conflicts

- One reviewer. No overlap.
