# Round 1 — merged findings
**Date:** 2026-09-11
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/foundation/foundation-contracts-generators/contracts-mixin-generator.cs:340
- Description: One-file-per-type merge avoids `{hint}.ApiRoute.g.cs` collisions, but `Transform` still adds one `Part` per `[ApiRoute]` (`contracts-mixin-generator.cs:189-199`) and `Wrap` appends every `part.Body` (`:340-341`). Two `[ApiRoute]` attributes on one class therefore emit duplicate `RouteTemplate`, `GetHttpVerb`, `GetRoute`, and parameter properties. Reproduced for the new `Should_Emit_One_Hint_When_AllowMultiple_ApiRoute` fixture: `Test.Features.Ccc.Dual.Command.g.cs` fails with CS0102/CS0111 (and CS0229). The test (`contracts-mixin-generator-tests.cs:276-300`) only asserts a single hint name and that both route strings appear — it does not compile the generated source, so the failure is masked. Pre-053-005 also broke on AllowMultiple (duplicate hint names); this change swaps that for silent invalid C# while advertising success.
- Suggestion: Keep one hint per type. Collapse same-`Kind` parts (first wins) so extra `[ApiRoute]` / `[AuthApiRequest]` / `[OpenDataQueryParameters]` do not duplicate members. Document that extra same-kind attributes are ignored for emit. Extend the AllowMultiple test to compile the generated trees (no error diagnostics) and to assert the first route’s members, not two concatenated bodies. Do not invent a multi-route member API (053-006).
- Source: general
- Disposition notes: `Transform` breaks after the first successful `Part` (`contracts-mixin-generator.cs:200-204`). Design region documents first-wins. AllowMultiple test asserts first route only, second route absent, and compiles generated trees with no errors. Focused suite 15 passed.

## Duplicates / conflicts

- Single general finding; no collapse needed.
