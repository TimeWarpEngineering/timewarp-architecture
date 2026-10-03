# Round 1 — merged findings
**Date:** 2026-10-03
**Sources:** general, tests

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 3 | 0 |
| nit | 0 | 2 | 2 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs:69
- Description: TWE010 shape normalization only covers `{name}` / `{name:type}`; catch-all, optional and parameterized-constraint tokens compare as literal text, so "token names ignored" overstated.
- Source: general (Issue 1)
- Disposition notes: Design region now states the scope explicitly. Those token forms are not `[Page]` route grammar (the generator emits no `[Parameter]` for them; pre-existing), so extending normalization is out of scope.

### M2 — Severity: nit — Status: fixed
- File: page-source-generator.cs (ParseRoute call for additional routes)
- Description: Token type agreement (TWE011) is checked only against the primary, so two aliases can give one alias-only token different types.
- Source: general (Issue 2)
- Disposition notes: ParseRoute now checks against every earlier route's tokens (`knownParameters`); message, descriptor description, Design region, page-attribute.md and the tw-blazor-layout skill are updated. Test `/p`,`/a/{x:int}`,`/b/{x:Guid}` → one TWE011.

### M3 — Severity: nit — Status: wontfix
- File: page-source-generator.cs:107
- Description: TWE009 is suppressed while TWE010/011/005 errors exist.
- Source: general (Issue 3)
- Disposition notes: Fail-closed (no page surface) is the pre-existing contract for every error path, and TWE009 is reported once the blocking error is fixed. Decided by: review oracle.

### M4 — Severity: nit — Status: wontfix
- File: page-source-generator.cs:529
- Description: A stacked `[Page]` also produces CS0579 (AllowMultiple = false) alongside TWE011.
- Source: general (Issue 4)
- Disposition notes: Intentional and documented in the Design region; the reviewer proposed no change. Decided by: review oracle.

### M5 — Severity: suggestion — Status: fixed
- File: tests/analyzers/timewarp-architecture-sourcegenerator-tests/page-source-generator-tests.cs
- Description: TWE010 test did not pin route text or the report count; constraint-type-distinct shape untested.
- Source: tests (Issue 1)
- Disposition notes: Exact count (3) plus the route text in each message; new test for `/a/{x:int}` + `/a/{y:guid}` (not TWE010). Also fixed the test helper's doubled diagnostics (`.Distinct()`), which the count assertion exposed.

### M6 — Severity: suggestion — Status: fixed
- File: page-source-generator-tests.cs
- Description: Untested: typed alias-only token, TWE005 on a multi-route page (also named RouteTemplate + aliases, and non-literal primary + aliases).
- Source: tests (Issue 2)
- Disposition notes: Tests added for the typed alias-only token (Guid `[Parameter]`, `{OrderId:guid}` route, parameterless GetPageUrl) and for TWE005 reported exactly once on a 3-route page. Named RouteTemplate and non-literal primary use the unchanged pre-096 primary path, so they are not re-tested.

### M7 — Severity: nit — Status: fixed
- File: page-source-generator-tests.cs (cache test)
- Description: No test that editing an alias invalidates the cached output.
- Source: tests (Issue 3)
- Disposition notes: The cache test now edits a multi-route alias. It asserts the new route is emitted, the old one is gone, and at least one step is not Cached/Unchanged.

## Duplicates / conflicts

- None; general and tests findings were disjoint.
