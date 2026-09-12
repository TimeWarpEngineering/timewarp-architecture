# Drop leftover mixin wording outside ContractsGenerator

## Parent

053

## Description

**053-007** renamed `ContractsMixinGenerator` → `ContractsGenerator`. Muse
listed leftovers **outside** that file. None are live Morris.Moxy (package
gone). Clean the ones we found; do not hunt the whole repo or rewrite
historical RFC/kanban snapshots.

## Requirements

Known sites (verified on master after 053-007):

1. **Fallback namespace identifier** (can appear in generated code if
   `RootNamespace` is missing) — rename `"GeneratedMixins"`:
   - `source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs:38`
   - `source/analyzers/timewarp-architecture-analyzers/generators/state-access-source-generator.cs:21`
   Use a non-mixin fallback (e.g. `Generated`). Update tests if they pin the
   string.

2. **Wrong / stale comments** — say attribute / FluentValidation / origin
   history, not "mixin":
   - `source/foundation/foundation-contracts/base/i-auth-api-request.cs:10`
     (“validators Include it via mixins” — that is FluentValidation `Include`)
   - `source/foundation/foundation-contracts/base/api-request-extensions.cs:7`
   - `source/analyzers/timewarp-architecture-analyzers/generators/fast-endpoint-source-generator.md:89`
     (“`[AuthApiRequest]` mixin” → attribute)
   - Purpose comments on Page + StateAccess generators (Moxy origin is fine
     as *history*; drop present-tense “mixin” as the mechanism name)
   - `tests/analyzers/timewarp-architecture-sourcegenerator-tests/state-access-source-generator-tests.cs:7`

3. **Demo fossil** (found; clean it):
   - `source/container-apps/web/features/todo-items/todo-item-dto-contracts.cs`
     Purpose/Design + `TODO: Revist the Mixins` — rephrase to endpoint-centric
     vs entity DTO; do not revive mixin-from-DTO generation.

If you **find** another live product/skill/how-to site in the same pass
(same class as above: current generator, foundation, how-to — not
`kanban/done/` and not `skills/*/analysis/` RFC snapshots), clean it too.

### Do not

- Rewrite `skills/tw-web-api-contracts/analysis/*` RFC/composer ballots
- Rewrite closed `kanban/done/` kitchens or their `review/`
- Rename `[ApiRoute]` / `[StateAccess]` / `[Page]`
- Change Page/StateAccess emit behavior beyond the fallback namespace string
- Repo-wide `rg mixin` as a completeness gate

## Checklist

- [ ] `"GeneratedMixins"` fallback renamed; tests updated
- [ ] Known comment/doc sites cleaned
- [ ] Todo DTO fossil rephrased
- [ ] Extra live sites found in the same pass cleaned
- [ ] Results + How to validate

## Session

- Created: 99163 (2026-09-11)
- Cockpit: timewarp-flow Grok `01a03d38-9611-7620-aae5-848e15dafa94`

## Notes

- Trigger: Muse review of 053-007 leftovers.
- Related: **053-002** (attributes), **192** (Page.mixin skills), **053-007**
  (ContractsGenerator).
