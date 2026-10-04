# Round 1 — merged findings
**Date:** 2026-10-04
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 2 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: source/foundation/foundation-domain/enumeration/enumeration.cs:278
- Description: a permanently-null T-typed public static field keeps `MemberCache<T>` from caching,
  so each lookup re-reflects. The results are still correct.
- Suggestion: cache after the type initializer has run.
- Source: general
- Disposition notes: wontfix (orchestrator). A null "member" field has no legitimate use in the
  Bogard pattern, and it costs performance only, never correctness. Not caching a partial snapshot
  is the deliberate fail-safe recorded in the Design region. Forcing the cctor from inside a lookup
  that the cctor itself may have triggered adds re-entrancy complexity for no real consumer.

### M2 — Severity: nit — Status: wontfix
- File: source/foundation/foundation-domain/enumeration/enumeration.cs:146
- Description: cross-subclass CompareTo can return 0 while Equals is false.
- Suggestion: document, or order/throw across subclasses.
- Source: general
- Disposition notes: wontfix (orchestrator). The behavior is already documented in the Design region
  ("Comparison is by Value only"), and it predates this change. Sorting a mixed-subclass
  collection has no current use: `CorsPolicy` is the only subclass. Throwing across types would be
  a new breaking behavior with no requirement behind it.

## Duplicates / conflicts

- None (single reviewer).
