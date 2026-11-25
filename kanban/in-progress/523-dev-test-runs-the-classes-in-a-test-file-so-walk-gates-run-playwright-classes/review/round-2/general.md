# Round 2 — general
**Date:** 2026-11-26
**Scope reviewed:** Fix delta `1378d535f` since round 1 (review M1). Product files: `tools/dev-cli/services/test-file-selection.cs`, `tests/tools/dev-cli-tests/test-file-selection-tests.cs`. The unchanged `tools/dev-cli/endpoints/test-command.cs` error path was read to confirm a selection error returns before build or `dotnet test`. The real pair is `tests/foundation/foundation-domain-tests/entity-tests.cs` (`Equality`) and `enumeration-tests.cs` (`Equality_Operators`).

## Summary

`TestFileSelection.Select` now keeps each registered class's FullName for the owning project. Before any run, a file whose `--filter-class` value is an ordinal ignore-case substring of another registered FullName fails with one error per collision. The error names the file, the class FullName, the filter, and the other class. `TestFilesAsync` already returns that error before `BuildAsync` or `dotnet test`. Filters for a class that does not collide are unchanged: `Namespace.Class` at the file's least indent, `+Class` when the declaration is indented deeper. The two new mapping tests pin `Equality` / `Equality_Operators`, and the longer name still maps to one filter. The fix-pass record covers `./bin/dev build`, 13/13 mapping tests (the file declares 13 tests), the live `entity-tests.cs` refusal, the live `enumeration-tests.cs` run (14 class runs), and `ganda repo audit`. The goal restatement matches the task, including the M1 refusal paragraph. No new defect in this delta.

## Issues
