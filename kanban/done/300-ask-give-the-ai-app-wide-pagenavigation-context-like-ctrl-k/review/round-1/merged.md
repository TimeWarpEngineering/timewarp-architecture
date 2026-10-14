# Round 1 — merged findings
**Date:** 2026-10-15
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 4 | 0 |
| nit | 0 | 3 | 1 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/application/navigation-destinations-tests.cs:137
- Description: The four-principal tests only check that the surfaces agree; a pass-everything policy filter would still pass.
- Suggestion: Pin each principal's expected set.
- Source: general
- Disposition notes: Anonymous = `["/"]`, PlainUser = the five literal urls, Developer has `/` and no `/Settings` or `/Admin*`, Administrator = every `PageRegistry.All` url.

### M2 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/application/navigation-destinations-tests.cs:70
- Description: The guard is not recursive and misses `XPage.GetPageUrl()` / `nameof(XPage)` lists.
- Suggestion: `SearchOption.AllDirectories`, plus member-read and nameof patterns.
- Source: general
- Disposition notes: Both done (`PageNameOf`, `PageMember` regexes). There are no current offenders.

### M3 — Severity: suggestion — Status: fixed
- File: source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs:290
- Description: `NavGroup` (and `Description`) accepted only a literal, so a const silently fell to `""`; the Design comment cited a test that didn't exist.
- Suggestion: Read via `semanticModel.GetConstantValue`, as `NavOrder` does; fix the comment.
- Source: general
- Disposition notes: Both now read with `GetConstantValue`, and the comments are corrected. New generator test `Should_Read_NavGroup_And_Description_From_Constants`.

### M4 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/application/navigation-destinations-tests.cs:96
- Description: A `NavOrder` that splits a group would render a duplicate menu category, and nothing caught it.
- Suggestion: Assert each `NavGroup` is one contiguous run.
- Source: general
- Disposition notes: The contiguity check is added to `Order_The_Registry_By_NavOrder_And_Name_Catalog_Actions`.

### M5 — Severity: nit — Status: fixed
- File: source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs:29
- Description: A stale comment said "Sorted by route".
- Source: general
- Disposition notes: It now says "Sorted by NavOrder, then route (ordinal)".

### M6 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent/page-agent-scope.cs:205
- Description: `TokenAccepts` failed open for unknown constraints.
- Source: general
- Disposition notes: Only `""`/`string` accept anything; an unknown constraint now never matches (fail closed). The Design region is updated.

### M7 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/application/agent/page-agent-scope.cs:265
- Description: The off-page `navigateTo` for an action ignores whether the principal may open that page, which conflicts with "never navigate outside the site map".
- Source: general
- Disposition notes: The page choice predates this task. `navigate` refuses the url safely, so this cannot cause a wrong navigation. Fixing it means threading the principal's destinations through `PageAgentScope` and the tool set, which is outside this task's scope. It applies only to a principal holding an action permission without the page's read policy. Recorded in task Notes. Decided by: review oracle.

### M8 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/NavMenu.razor:24
- Description: Destinations were recomputed on every parent render.
- Source: general
- Disposition notes: NavMenu now caches the `AuthenticationState` task reference and recomputes only when it is replaced.

## Duplicates / conflicts

- None (single reviewer).
