# Disposition — task 300

**Date:** 2026-10-15
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

Reviewed at effort 3 with a general reviewer over 5df7913d2. No bugs were found. Round 1 raised
4 suggestions and 4 nits: test-strength gaps, holes in the guard, the generator dropping a
const `NavGroup`, fail-open route constraints, and NavMenu recomputing on every parent render.
Fix commit 273115a53 resolved all of them except M7. Round 2 verified those fixes and raised 2
test-only nits (M9, M10), which were fixed afterwards. Gates after the fixes: `dev build` 0
warnings and 0 errors, `NavigationDestinations_` 6/6, source-generator suite 121/121, web-spa
Unit lane 169/169.

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M7 | nit | The off-page `navigateTo` choice ignores whether the principal may open that page. This predates task 300, and `navigate` refuses such a url, so it can never cause a wrong navigation. Fixing it means threading destinations through `PageAgentScope` and the tool set, which is outside this task's scope. Recorded in task Notes. | review oracle |

## Escalations

- None.
