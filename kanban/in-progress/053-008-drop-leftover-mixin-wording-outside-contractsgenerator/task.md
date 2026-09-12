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

- [x] `"GeneratedMixins"` fallback renamed; tests updated
- [x] Known comment/doc sites cleaned
- [x] Todo DTO fossil rephrased
- [x] Extra live sites found in the same pass cleaned
- [x] Results + How to validate

## Session

- Created: 99163 (2026-09-11)
- Cockpit: timewarp-flow Grok `01a03d38-9611-7620-aae5-848e15dafa94`
- Implementer: Grok session `01a0944c-9016-7ad2-bf24-c2820d0370ed` (2026-09-12)

## Notes

- Trigger: Muse review of 053-007 leftovers.
- Related: **053-002** (attributes), **192** (Page.mixin skills), **053-007**
  (ContractsGenerator).
- Same-pass extra: StateAccess generator comment still said “matching the Moxy
  mixin after sourceName substitution” — rephrased to former Moxy template.
- Left as history (not present-tense mechanism): how-to
  `how-to-write-bff-api-contracts.md` pre-rename `[RouteMixin]` names;
  `documentation/developer/reference/api-endpoint-source-generator.md`
  “legacy `[RouteMixin]` name”. Did not rewrite `skills/*/analysis/*` or
  `kanban/done/`.
- Tests never pinned `"GeneratedMixins"` (they always pass `RootNamespace`);
  only the StateAccess test comment was updated.

## Results

Fallback namespace for Page and StateAccess generators is `"Generated"` when
`RootNamespace` is missing. Stale comments now name attributes,
FluentValidation `Include`, or Moxy origin history — not mixin as the live
mechanism. The todo-item DTO fossil is endpoint-centric vs entity DTO; it
does not revive DTO-driven contract generation.

**Files changed**

- `source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs`
- `source/analyzers/timewarp-architecture-analyzers/generators/state-access-source-generator.cs`
- `source/analyzers/timewarp-architecture-analyzers/generators/fast-endpoint-source-generator.md`
- `source/foundation/foundation-contracts/base/i-auth-api-request.cs`
- `source/foundation/foundation-contracts/base/api-request-extensions.cs`
- `source/container-apps/web/features/todo-items/todo-item-dto-contracts.cs`
- `tests/analyzers/timewarp-architecture-sourcegenerator-tests/state-access-source-generator-tests.cs`

**Decisions / deviations**

- Fallback identifier is `Generated` (task example).
- Emit behavior unchanged except that fallback string.
- How-to/reference notes that name the old `[RouteMixin]` *attribute* were
  left; they are migration history, not leftover mechanism wording.

**Tests:** `cd tests/analyzers/timewarp-architecture-sourcegenerator-tests &&
dotnet test -c Release` — 76 passed, 0 failed (2026-09-12).

### How to validate

**Smoke**

```bash
rg -n 'GeneratedMixins' source tests
rg -n -i 'mixin' source/analyzers source/foundation source/container-apps/web/features/todo-items tests/analyzers
cd tests/analyzers/timewarp-architecture-sourcegenerator-tests && dotnet test -c Release
```

**Expect**

- First `rg`: no matches under `source/` or `tests/`.
- Second `rg`: no matches in those live product/test trees (historical
  `[RouteMixin]` names remain only under `documentation/` and
  `skills/*/analysis/`).
- `dotnet test`: 76 passed, 0 failed. Page/StateAccess tests still emit into
  the supplied `RootNamespace` (`TimeWarp.Architecture`).

**Automated gate**

```bash
cd tests/analyzers/timewarp-architecture-sourcegenerator-tests && dotnet test -c Release
# expect: Passed! total: 76 failed: 0
```

**Not in scope:** repo-wide `rg mixin` as a completeness gate; rewriting RFC
snapshots or closed `kanban/done/` kitchens.
