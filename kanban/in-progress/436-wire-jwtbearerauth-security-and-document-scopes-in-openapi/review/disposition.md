# Disposition — task 436

**Date:** 2026-10-29
**Outcome:** clean
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 raised M1, a bug: foundation held a hand copy of the policy-to-scope map and scheme name
with no drift check. It also raised M2, a suggestion: the 403 description was overwritten and the
`WWW-Authenticate` header was marked required. The fix pass, `da2cb14ac`, moved the map to the
hosts. Web inverts `AgentScopePermissionSeed.DefaultGrants`, and api reuses its `AddPolicy`
constants. A seed-walk test was added. Bearer-failure text is now appended, covers handler-level
rejections, and the header is optional. Round 2 re-verified both findings as fixed and found nothing
new. Tests: `OpenApiBearerSecurity_` 13/13 and `OpenApiDocument_` 4/4 passed. api-server built with
0 warnings.

## Exception log (if accepted-exceptions)

None.

## Escalations

None.
