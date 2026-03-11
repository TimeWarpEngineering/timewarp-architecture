# Fix ICurrenUserService Interface Naming Typo

## Description

The interface `ICurrenUserService` in `Common.Application/Abstractions/ICurrenUserService.cs` is missing a 't' in "Current". Rename to `ICurrentUserService` and update all references across the codebase.

## Checklist

- [x] Rename interface from `ICurrenUserService` to `ICurrentUserService`
- [x] Rename file from `ICurrenUserService.cs` to `ICurrentUserService.cs`
- [x] Update all implementations of the interface
- [x] Update all usages/injections of the interface
- [x] Verify build succeeds after rename
- [x] Run tests to confirm no regressions

## Notes

- Source: Code review finding (1.1 Interface Naming Typo)
- Impact: Misspelled interface causes confusion when implementing or consuming
