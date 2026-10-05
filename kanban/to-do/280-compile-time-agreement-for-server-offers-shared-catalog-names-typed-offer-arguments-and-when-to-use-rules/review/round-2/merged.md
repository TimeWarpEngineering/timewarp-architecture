# Round 2 — merged findings
**Date:** 2026-10-05
**Sources:** general (round 2)

## Resolved prior

M1–M13: fixed in 3f9df71a8, re-verified in round-2/general.md.

## Counts (this round, new findings)

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M14 — Severity: nit — Status: fixed
- File: source/analyzers/timewarp-architecture-convention-analyzers/action-offer-agreement-analyzer.cs:305
- Description: `[CatalogAction]` on a non-class declaration (a nested record) climbs to the enclosing class and checks the wrong constructor; TWA0029 accepts the name even though TimeWarp.State doesn't catalog it.
- Suggestion: Use the attribute's own declaration and treat non-class targets as not cataloged.
- Source: general
- Disposition notes: fixed in 778426f35; orchestrator re-verified diff + analyzer suite 28/28

### M15 — Severity: suggestion — Status: fixed
- File: action-offer-agreement-analyzer.cs:255
- Description: The nullability rule is keyed on the parameter's annotation, but the binder refuses null for any required parameter. It misses `string?`→`string?` and `Guid?`→`Guid?` required parameters, and a nullable property on an optional parameter can open the ordering gap.
- Suggestion: Key on required-ness (no explicit default value), and cover both Nullable<T> and annotated reference properties. Handle or document the optional case, and update the skill sentence.
- Source: general
- Disposition notes: fixed in 778426f35; orchestrator re-verified diff + analyzer suite 28/28

### M16 — Severity: nit — Status: fixed
- File: action-offer-agreement-analyzer.cs:332
- Description: Any `[JsonIgnore]` skips the property, but conditions other than `Always` still serialize it.
- Suggestion: Skip only when Condition is absent or Always.
- Source: general
- Disposition notes: fixed in 778426f35; orchestrator re-verified diff + analyzer suite 28/28
