# Tighten ContractsMixin syntax provider incrementality

## Parent

053

## Description

Even as an `IIncrementalGenerator`, `ContractsMixinGenerator` is incremental
in API only:

- Predicate is “any `class` with **any** attribute list”
- Transform uses syntax simple-name match; no `partial` check
- `Target` holds `IReadOnlyList` (reference equality → extra re-emits)
- One generated file **per attribute** (`….ApiRoute.g.cs` + Auth + OpenData)

Tighten the pipeline so editing an unrelated attributed class does not
re-run mixin emit. Can land **without** **053-004** (still filter the three
names in syntax). **053-004** later replaces this with
`ForAttributeWithMetadataName`.

## Requirements

- Predicate: attribute simple name is `ApiRoute` / `AuthApiRequest` /
  `OpenDataQueryParameters` (or the FQN forms if 053-004 already merged).
- Require `partial` on the target class; skip records/structs if unsupported.
- Equatable `record struct Target` with `ImmutableArray` parts.
- Prefer **one file per type**, not one file per attribute (avoids
  `AllowMultiple` hint-name collisions).
- Skip emit when generated text is unchanged.

### Not in scope

- Moving attributes to a public namespace (**053-004**)
- Route parser (**053-003**)
- Changing which members are generated (**053-006**)

## Checklist

- [ ] Predicate filters the three mixin attributes
- [ ] Equatable model; one hint per type
- [ ] Generator tests still pass
- [ ] Results + How to validate

## Session

- Created: 2417213 (2026-09-09)
- Cockpit: timewarp-flow Grok `01a03d38-9611-7620-aae5-848e15dafa94`

## Notes

- File: `source/foundation/foundation-contracts-generators/contracts-mixin-generator.cs`
- If **053-004** merges first, this task shrinks to leftover equality/file
  hygiene — do not duplicate ForAttribute work.
