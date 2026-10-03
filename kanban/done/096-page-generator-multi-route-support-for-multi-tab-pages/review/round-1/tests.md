# Round 1 — tests
**Date:** 2026-10-03
**Scope reviewed:** `git diff master...HEAD` test file `tests/analyzers/timewarp-architecture-sourcegenerator-tests/page-source-generator-tests.cs` against `source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs` (multi-route `[Page]`, TWE010/TWE011).

## Summary
Coverage of the new behavior is good: primary-only registry rows, alias `[Route]` emission, all three Crunchit shapes, TWE009 primary-only judgment (both directions), TWE010 (case, token-name, repeated alias, hand-written `[Route]` duplicate plus the legal distinct alias), TWE011 (stacked, non-literal, token-type conflict), Policy emitted once, and the incremental cache test extended with multi-route and TWE010 pages. All assertions use Shouldly in Jaribu style. `dotnet test -c Release` in the sourcegenerator-tests project: 99 total, 99 passed, 0 failed, 0 skipped. Only minor gaps remain.

## Issues
### Issue 1 — Severity: suggestion
- File: tests/analyzers/timewarp-architecture-sourcegenerator-tests/page-source-generator-tests.cs:400
- Description: TWE010 test checks only that each page name appears in some TWE010 message; it does not assert how many TWE010 reports (so `/a`,`/b`,`/b` could report extra or the case page could be reported on the wrong route). Also no test that routes differing only by constraint type (`{id:int}` vs `{id:guid}`) are NOT TWE010 duplicates (or are, whichever the ShapeKey intends).
- Suggestion: Assert the offending route text appears in the message (e.g. `"/Clients"`, `"/b"`), and add a case pinning the constraint-differs-shape decision.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/analyzers/timewarp-architecture-sourcegenerator-tests/page-source-generator-tests.cs:307
- Description: Untested branches: (a) an alias-only token (token absent from the primary) producing a `[Parameter]` property, including a typed alias-only token; (b) named `RouteTemplate = "..."` combined with aliases; (c) TWE005 (bad Policy) on a multi-route page still reported once and fail-closed; (d) a non-literal primary combined with aliases. Test at line 369 covers only an untyped alias-only token (`ClientId`).
- Suggestion: Add small tests for (a) typed alias-only token and (c) at minimum.
- Status: open

### Issue 3 — Severity: nit
- File: tests/analyzers/timewarp-architecture-sourcegenerator-tests/page-source-generator-tests.cs:475
- Description: The cache test only adds an unrelated tree. It does not show that editing a multi-route page's alias list invalidates (Modified) output, which would guard against `AdditionalRouteAttributes` being dropped from model equality (the model's Equals/hash includes it, but nothing tests it).
- Suggestion: Add a second run that changes an alias string and assert the page output reason is not Cached/Unchanged and the new route is emitted.
- Status: open
