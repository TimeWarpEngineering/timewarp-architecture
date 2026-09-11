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

- [x] Predicate filters the three mixin attributes
- [x] Equatable model; one hint per type
- [x] Generator tests still pass
- [x] Results + How to validate
- [x] Review round 1 M1: first-wins same-Kind parts; compile AllowMultiple emit
- [x] Review disposition (clean, 2 rounds)

## Session

- Created: 2417213 (2026-09-09)
- Cockpit: timewarp-flow Grok `01a03d38-9611-7620-aae5-848e15dafa94`
- Implementer: Grok session `01a08f88-dc4f-73d3-9909-be1827b7d1a6` (2026-09-11)
- Review oracle: Grok session `01a08f94-8716-7961-a22f-87afc085beae` (2026-09-11)

## Notes

- File: `source/foundation/foundation-contracts-generators/contracts-mixin-generator.cs`
- If **053-004** merges first, this task shrinks to leftover equality/file
  hygiene — do not duplicate ForAttribute work.
- **053-004 already merged** (PR #337). This id did not re-do
  `ForAttributeWithMetadataName`; leftover work is the partial predicate,
  equatable `Target`, one hint per type, and skip-unchanged emit.
- Review kitchen: `review/review-framework.md`, `review/round-1/`,
  `review/round-2/`, `review/disposition.md`.

## Results

**053-004 already landed** `ForAttributeWithMetadataName` on the three public
`TimeWarp.Foundation.Features` metadata names, so this id did not duplicate
that discovery. Remaining incrementality:

- Predicate requires a **partial class** (`ClassDeclarationSyntax` +
  `partial`). Records and structs are skipped (unsupported).
- `Target` / `Part` are `readonly record struct`s. `Target` stores
  `ImmutableArray` containers/parts and implements content `Equals` /
  `GetHashCode` via `SequenceEqual` — `ImmutableArray.Equals` is backing-array
  reference equality and was re-emitting on every transform.
- The three attribute pipelines are `Collect`ed, merged by hint, and emitted
  as **one `{fqn}.g.cs` per type** (combined base list + bodies). Avoids
  `AllowMultiple` collisions on `{fqn}.{Kind}.g.cs`.
- Extra same-kind attributes are **first-wins**: after the first successful
  `Part` in `Transform`, further attributes of that metadata name are ignored
  so the one file per type stays compilable (no duplicate `RouteTemplate` /
  `GetHttpVerb` / `GetRoute`). Multi-route member APIs stay in **053-006**.
- `RegisterSourceOutput` skips when `Target` is unchanged (trivia-only edits
  and unrelated attributed classes do not `Modified` mixin outputs).

**Files:** `contracts-mixin-generator.cs`, `contracts-mixin-generator-tests.cs`.

**Not in scope (unchanged):** public attribute namespace (053-004), route
parser (053-003), which members are generated (053-006).

**Tests:** `ContractsMixinGenerator_Tests` 15 passed (9 existing + 6 new:
one hint per type, AllowMultiple first-wins + compile, skip non-partial, skip
record/struct, unrelated attributed class, trivia-only). Generator project
0 Warning(s) 0 Error(s).

### How to validate

**Smoke**

```bash
cd tests/analyzers/timewarp-architecture-sourcegenerator-tests
dotnet test -c Release -- --filter-class ContractsMixinGenerator
dotnet test -c Release -- --filter-method Should_Emit_One_Hint_Per_Type
dotnet test -c Release -- --filter-method Should_Not_Modify_Output_When_Unrelated_Attributed_Class_Is_Added
```

**Expect**

- Mixin class: 15 passed (0 failed), including:
  - `Should_Emit_One_Hint_Per_Type` — hints are
    `Test.Features.Admin.Roles.GetRole.Query.g.cs` and
    `…GetRoles.Query.g.cs` (no `.ApiRoute.` / `.AuthApiRequest.` /
    `.OpenDataQueryParameters.` suffix); GetRoles source contains both
    `IAuthApiRequest` and `IOpenDataQueryParameters`
  - `Should_Emit_One_Hint_When_AllowMultiple_ApiRoute` — single
    `Test.Features.Ccc.Dual.Command.g.cs`; first route (`api/a/{Id}`, Get)
    only; second route absent; generated trees compile with no errors
  - `Should_Skip_Non_Partial_Class` / `Should_Skip_Records_And_Structs` —
    no `GetRoute` / `ItemId` members
  - `Should_Not_Modify_Output_When_Unrelated_Attributed_Class_Is_Added` and
    `Should_Not_Modify_Output_When_Only_Trivia_Changes_On_A_Mixin_Class` —
    tracked source outputs have no `IncrementalStepRunReason.Modified`
  - Existing 053-003/053-004 cases still pass (`GetRoute`, `UserId`, OData
    members, foundation-namespace attributes, foreign `Other.Lib.ApiRoute`
    ignored)

**Automated gate**

```bash
cd tests/analyzers/timewarp-architecture-sourcegenerator-tests && dotnet test -c Release
# expect: 74 passed (68 prior + 6 incrementality/hygiene tests)

dotnet build source/foundation/foundation-contracts-generators/foundation-contracts-generators.csproj -c Release
# expect: Build succeeded. 0 Warning(s) 0 Error(s)

dotnet run tools/dev-cli/dev.cs -- build
# expect: Build succeeded. 0 Warning(s) 0 Error(s)
```

**Not in scope:** `dev template-smoke`; emitting fewer mixin members (053-006).

**Review disposition:** clean (0 open). Effort 1, general only. 2 rounds.

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

- M1 (bug, fixed): one-file-per-type merge concatenated extra `[ApiRoute]` bodies (CS0102/CS0111). `Transform` first-wins same-kind parts; AllowMultiple test compiles generated trees.
- Paths: `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`. No wontfix; no escalation.
