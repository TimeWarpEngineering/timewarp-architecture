# Round 2 — general
**Date:** 2026-10-22
**Scope reviewed:** fix delta `598e03177..13dd8cef6` (`source/foundation/timewarp-contracts/`, `skills/tw-web-api-contracts/`, `tests/analyzers/timewarp-architecture-sourcegenerator-tests/`, `task.md`). Round 1 findings M1–M4 were checked again against that delta.

## Summary

The fix commit changes only docs and tests. The destructive hint now uses the MCP meaning in the XML docs, the Design region, the package readme, `SKILL.md`, and `neutral-operations.md`: a delete or an overwrite, as opposed to an update that only adds data. Those docs also say that MCP treats an omitted hint as true and reads it only for a tool that is not read-only. The readme example is now `notes.append`. The blank-name generator rule is in both author-facing sections. Three new tests cover a whitespace name with hints in each reader and an explicit `[Destructive(true)]`. The checklist no longer claims the release was cut. M1–M4 are fixed.

One small leftover from M1: two test fixtures still contradict the destructive meaning the docs now give (Issue 1).

Gates I re-ran on `13dd8cef6`:
- `dev build`: 0 warnings, 0 errors.
- Foundation contracts tests: 47 of 47 passed.
- Source generator tests: 149 of 149 passed.
- `ganda repo audit`: passed.
- `dev lint-skills`: not run, because `vally` is not on PATH in this session. The implementer reports that it passed with vally 0.19.0.

## Prior findings

- M1: fixed. `destructive-attribute.cs:22` summary, the remarks at :31-34, `operation-descriptor.cs:43-44`, the readme hint table and example, and `neutral-operations.md:80-84` all use the MCP meaning.
- M2: fixed. `readme.md:31-33` and `neutral-operations.md:72-75` state the non-blank `[Operation]` name rule.
- M3: fixed. `Should_Omit_Hints_When_The_Operation_Name_Is_Whitespace` and `Should_Copy_An_Explicit_True_Destructive_Hint` were added to `contracts-generator-tests.cs`, and `Should_Drop_Hints_When_The_Operation_Name_Is_Whitespace` to `endpoint-operation-metadata-tests.cs`.
- M4: fixed. The checklist item is unticked and reworded to the `dev release` step after merge.

## Issues

### Issue 1 — Severity: nit
- File: tests/foundation/foundation-contracts-tests/operation-catalog-tool-hint-tests.cs:23 (also tests/analyzers/timewarp-architecture-sourcegenerator-tests/contracts-generator-tests.cs:953)
- Description: The fixtures still model the hint in ways the M1 docs now rule out. `Stated_Hints_Should_Appear_Beside_The_Name` pairs `rename_symbol` ("Rename a symbol across the solution", which overwrites) with `new DestructiveAttribute(false)`. M1 replaced exactly this example in the readme. The new `Should_Copy_An_Explicit_True_Destructive_Hint` puts `[Destructive(true)]` on a nested class named `Query` with `HttpVerb.Get`, but `readme.md` and `neutral-operations.md` now say `destructiveHint` does not apply to an `IQuery`. The assertions pass, because neither reader checks the kind. A reader who copies the fixtures gets the wrong pattern.
- Suggestion: In the catalog test, use an additive operation for the `Destructive(false)` case (for example `notes.append`, matching the readme), or keep `rename_symbol` and state `[Destructive]`. In the generator test, name the class `Command` and use a non-GET verb (for example `HttpVerb.Delete` on `PurgeCache`).
- Status: open
