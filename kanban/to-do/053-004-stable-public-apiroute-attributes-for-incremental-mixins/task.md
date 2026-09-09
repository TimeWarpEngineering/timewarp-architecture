# Stable public ApiRoute attributes for incremental mixins

## Parent

053

## Description

`ContractsMixinGenerator` emits `ApiRoute` / `AuthApiRequest` /
`OpenDataQueryParameters` as **internal** types in the consumer
`RootNamespace`, then matches them by **simple name**. That blocks
`ForAttributeWithMetadataName` and can collide with any other
`ApiRouteAttribute`.

TypedId already uses `RegisterPostInitializationOutput` + a stable
namespace. Do the same for the three mixin attributes so crunchit (and
this template) get real incremental mixins.

Do **not** fix the `{LocationId}` parser here — that is **053-003**.

## Requirements

- Public attributes live in a stable foundation namespace (same idea as
  `IAuthApiRequest` / TypedId), via `RegisterPostInitializationOutput`.
- `ContractsMixinGenerator` discovers them with
  `ForAttributeWithMetadataName`, not `CreateSyntaxProvider` + simple-name
  string match.
- FastEndpoint / ingress matching uses the **FQN**, not simple name.
- Template `sourceName` rewrite and generated-app RootNamespace must still
  compile (task **115** dual-mode is the constraint — do not regress it).
- Tests: generator tests for discovery; existing contracts still emit
  `GetRoute` / `UserId` / OData members.

### Not in scope

- Route param tokenization (**053-003**)
- SyntaxProvider predicate-only tightening (**053-005**)
- Emitting fewer members (**053-006**)
- FastEndpoint referenced-assembly walk (**006-001**)

## Checklist

- [ ] Stable public attributes (post-init, not per-consumer RootNamespace internals)
- [ ] Mixin generator uses `ForAttributeWithMetadataName`
- [ ] FastEndpoint/ingress match FQN
- [ ] Template / dual-mode RootNamespace still works
- [ ] Results + How to validate

## Session

- Created: 2416398 (2026-09-09)
- Cockpit: timewarp-flow Grok `01a03d38-9611-7620-aae5-848e15dafa94`

## Notes

- File: `source/foundation/foundation-contracts-generators/contracts-mixin-generator.cs`
- Why today: attributes are generated `internal` in consumer RootNamespace
  so FastEndpoint matches `ApiRouteAttribute` by simple name (Moxy leftover).
- Sibling: **053-003** (parser — land before crunchit copies mixins).
