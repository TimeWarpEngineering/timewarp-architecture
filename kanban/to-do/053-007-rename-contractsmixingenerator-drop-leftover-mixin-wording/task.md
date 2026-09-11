# Rename ContractsMixinGenerator drop leftover mixin wording

## Parent

053

## Description

**053-002** already renamed the **attributes** (`[RouteMixin]` → `[ApiRoute]`,
etc.). **192** dropped `Page.mixin` from Blazor skills. The Roslyn type and
files still say mixin:

- `ContractsMixinGenerator`
- `contracts-mixin-generator.cs`
- `contracts-mixin-generator-tests.cs` / `ContractsMixinGenerator_Tests`
- helpers like `MixinHintNames`, `CreateMixinProvider`
- comments, agent-context regions, skill/analysis leftovers

Crunchit should copy the cleaned name, not `ContractsMixinGenerator`.

**053-002** left the door open (“mixin is a fair word for members mixed into
a partial”). Decision for this child: **drop it.** Intent naming, no
mechanism marker — same convention as the attributes.

## Requirements

Rename, do not change emit behavior (parser **053-003**, FQN **053-004**,
incrementality **053-005**, slimmer members **053-006** stay as-is).

**Canonical names:**

| Today | After |
|-------|--------|
| `ContractsMixinGenerator` | `ContractsGenerator` |
| `contracts-mixin-generator.cs` | `contracts-generator.cs` |
| `ContractsMixinGenerator_Tests` | `ContractsGenerator_Tests` |
| `contracts-mixin-generator-tests.cs` | `contracts-generator-tests.cs` |
| `ContractsMixinAttributes.g.cs` (hint) | `ContractsGeneratorAttributes.g.cs` (or keep if tests pin it — update tests) |
| `MixinHintNames` / `CreateMixinProvider` | drop Mixin from identifiers |

Sweep:

- foundation-contracts-generators project
- analyzer/sourcegenerator tests
- skills (`tw-web-api-contracts` + analysis docs that still say RouteMixin /
  ContractsMixinGenerator)
- `documentation/` / how-to that names the generator type
- `#region` Purpose/Design comments

Do **not** rename `[ApiRoute]` / `[AuthApiRequest]` / `[OpenDataQueryParameters]`
again. Do **not** rename `IAuthApiRequest`.

Bump Foundation.Contracts if the public generator type is part of the
package surface (same bar as 053-002/053-003).

### Not in scope

- FastEndpoint assembly scan (**006-001**)
- Behavior changes to generated members

## Checklist

- [ ] Type + files + tests renamed to `ContractsGenerator`
- [ ] Identifier/comment/skill/docs sweep (no leftover Mixin on this generator)
- [ ] Package bump if the public type ships
- [ ] Generator tests still pass
- [ ] Results + How to validate

## Session

- Created: 1063987 (2026-09-11)
- Cockpit: timewarp-flow Grok `01a03d38-9611-7620-aae5-848e15dafa94`.
  Do not implement in cockpit.

## Notes

- Follows **053-002** (attributes) and **192** (Page.mixin).
- Target name `ContractsGenerator` matches `foundation-contracts-generators`.
