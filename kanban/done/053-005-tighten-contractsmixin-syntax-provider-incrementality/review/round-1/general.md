# Round 1 — general
**Date:** 2026-09-11
**Scope reviewed:** branch `task/053-005-tighten-contractsmixin-syntax-provider-incremental` vs `origin/master` (product commit 9de9b74e plus Results 40ff0c05)

## Summary

The change keeps the three `ForAttributeWithMetadataName` pipelines from 053-004, adds a partial-class predicate (records/structs skipped), merges collected targets into one equatable `record struct Target` per type, and emits a single `{fqn}.g.cs`. Content `Equals`/`GetHashCode` via `SequenceEqual` correctly avoids `ImmutableArray` reference-equality re-emits; focused tests (15) pass, including trivia/unrelated `Modified` checks. Overall risk is low for the common single-attribute case. The AllowMultiple path only fixed hint-name collisions — it still concatenates full route bodies and produces uncompilable duplicate members.

## Issues

### Issue 1 — Severity: bug
- File: source/foundation/foundation-contracts-generators/contracts-mixin-generator.cs:340
- Description: One-file-per-type merge avoids `{hint}.ApiRoute.g.cs` collisions, but `Transform` still adds one `Part` per `[ApiRoute]` (`contracts-mixin-generator.cs:189-199`) and `Wrap` appends every `part.Body` (`:340-341`). Two `[ApiRoute]` attributes on one class therefore emit duplicate `RouteTemplate`, `GetHttpVerb`, `GetRoute`, and parameter properties. Reproduced for the new `Should_Emit_One_Hint_When_AllowMultiple_ApiRoute` fixture: `Test.Features.Ccc.Dual.Command.g.cs` fails with CS0102/CS0111 (and CS0229). The test (`contracts-mixin-generator-tests.cs:276-300`) only asserts a single hint name and that both route strings appear — it does not compile the generated source, so the failure is masked. Pre-053-005 also broke on AllowMultiple (duplicate hint names); this change swaps that for silent invalid C# while advertising success.
- Suggestion: Either (a) reject/diagnostic when more than one part of the same `Kind` lands on a type, (b) emit only the first route part and document that AllowMultiple is unsupported for generation, or (c) design non-colliding multi-route members — and extend the AllowMultiple test to compile the generated trees (or assert no duplicate member names) so uncompilable emit fails the suite.
- Status: open
