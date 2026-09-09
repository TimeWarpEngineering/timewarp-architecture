# Emit less per-contract mixin members

## Parent

053

## Description

Mixin emit is larger than the skill needs:

- Parameterless `GetRoute()` always interpolates
  `FormattableString.Invariant($"api/Roles")` even for static templates.
- Parameterized + parameterless overloads duplicate the template string.
- `[AuthApiRequest]` always emits **private** `GetAuthQueryParameters()` —
  only list queries that implement `IQueryStringRouteProvider` compose it
  (`GetRoles`, `ListPrincipals`). POST / GET-by-id should keep using
  **manual** `: IAuthApiRequest` + `UserId` (skill: two forms).
- Property types use bare `Guid` / `DateTime` (implicit usings), not
  `global::`.

Shrink emission. Do not change route parsing (**053-003**).

## Requirements

- Static templates: `GetRoute()` returns `RouteTemplate` (no interpolation).
- Parameterized `GetRoute(...)` only when the template has `{params}`.
- Emit `GetAuthQueryParameters` only when the same partial is (or should
  be) an `IQueryStringRouteProvider` / also has `[OpenDataQueryParameters]`.
  Do **not** force every `[AuthApiRequest]` onto query-string form.
- Use `global::System.Guid` (and peers) in generated members.
- Existing hosted contracts still compile; GetRoles still composes auth +
  OData query helpers.

### Not in scope

- Parser (**053-003**), FQN attributes (**053-004**), SyntaxProvider
  incrementality (**053-005**)

## Checklist

- [ ] Static GetRoute is a const return
- [ ] Auth query helper only on query-string contracts
- [ ] `global::` types in generated members
- [ ] Roles list + create still generate correctly
- [ ] Results + How to validate

## Session

- Created: 2417840 (2026-09-09)
- Cockpit: timewarp-flow Grok `01a03d38-9611-7620-aae5-848e15dafa94`

## Notes

- File: `source/foundation/foundation-contracts-generators/contracts-mixin-generator.cs`
- Skill: `[AuthApiRequest]` vs manual `IAuthApiRequest` are two forms;
  do not collapse them.
