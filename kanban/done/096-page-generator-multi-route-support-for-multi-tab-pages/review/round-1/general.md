# Round 1 — general
**Date:** 2026-10-03
**Scope reviewed:** `git diff master...HEAD` (92953267, f04cbc13): page-source-generator.cs, diagnostic-descriptors.cs, AnalyzerReleases.Unshipped.md, page-attribute.md, tw-blazor-layout SKILL.md, AGENTS.md, page-source-generator-tests.cs. Ran `dotnet test -c Release -- --filter-class PageSourceGenerator` in tests/analyzers/timewarp-architecture-sourcegenerator-tests: 23/23 passed.

## Summary
The implementation matches the spec and the cockpit note. The primary route alone drives GetPageUrl, IStaticRoute, RouteTemplate, the registry row and TWE009. Aliases are emitted as `[Route]` only. The model is incrementally safe: `ImmutableArray` fields are compared by `SequenceEqual`, diagnostics are location-free records, and the cached-step test passes. I found no correctness bugs in the supported route shapes, only a few edge-case gaps in duplicate and type-agreement detection.

## Issues
### Issue 1 — Severity: suggestion
- File: source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs:68
- Description: `RouteParam()` only matches `{name}` and `{name:type}`. Catch-all (`{*path}`), optional (`{id?}`, `{id:int?}`) and parameterized-constraint (`{id:min(1)}`) tokens do not match, so they are treated as literal segments. Two routes `/a/{*x}` and `/a/{*y}` (or `/a/{id?}` and `/a/{id?}` with different case/names) therefore get different ShapeKeys and TWE010 misses them. The "token names ignored" claim in the Design region, AGENTS.md and docs only holds for the matched forms. The fix to the untyped-token regex is correct, and this gap predates the task, but multi-route makes it more visible.
- Suggestion: Either normalize these token forms in ShapeKey (a `{*}` or `{?}` marker), or state in the Design region that TWE010 covers only `{name}` and `{name:type}` tokens.
- Status: open

### Issue 2 — Severity: nit
- File: source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs:312
- Description: Token type agreement (TWE011) is checked only against the primary route. Two aliases that each introduce the same alias-only token with different types (`/a/{x:int}` and `/b/{x:guid}`) pass: the `[Parameter]` dedupe keeps the first type and the second route silently binds with a mismatched property type.
- Suggestion: Also compare an alias against the already-collected `parameters` list (not just `primary.Parameters`) in `ParseRoute`.
- Status: open

### Issue 3 — Severity: nit
- File: source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs:107
- Description: When a page has `Errors`, `NavigableDiagnostic` is dropped (`PageModel.Failed` sets it to null and the output returns early). A page with both a TWE010/011 error and an invalid `Navigable` shows TWE009 only after the first error is fixed. This is harmless but does not match the "report everything" expectation.
- Suggestion: Optionally compute the navigable diagnostic before the error early-return, or accept it and say so in the Design region.
- Status: open

### Issue 4 — Severity: nit
- File: source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs:529
- Description: Switching the emitted `PageAttribute` to `AllowMultiple = false` makes a stacked `[Page]` also produce compiler error CS0579 next to TWE011, so the user sees two errors. This is intentional per the Design region, but the AGENTS.md/docs wording ("TWE011 teaching message") could mention it. Existing in-repo `[Page]` usages are single, so there is no regression.
- Suggestion: None required.
- Status: open

## Verified without findings
- Primary-only semantics: `hasParameters` uses `page.Signature` (primary), `Parameters` includes alias-only tokens for `[Parameter]`. Registry filters `Errors.IsEmpty && Navigable`.
- Alias token inheritance (`{ClientId}` after `{ClientId:string}`, `{OrderId}` after `{OrderId:Guid}` emitting `:guid`) and the typed-disagreement TWE011 path are correct.
- Duplicate detection: case-insensitive, token-name-free, trailing-slash trimmed. `{id:int}` vs `{id}` correctly distinct. A hand-written `[Route]` repeating a page route gives TWE010, and distinct hand-written aliases stay legal.
- Policy (TWE005) is handled once per page and still fail-closed. Purpose and Design regions are reconciled with the code. The descriptor SSOT, Unshipped.md and AGENTS.md agree.
