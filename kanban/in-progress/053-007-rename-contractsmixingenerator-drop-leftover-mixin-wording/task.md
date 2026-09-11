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

- [x] Type + files + tests renamed to `ContractsGenerator`
- [x] Identifier/comment/skill/docs sweep (no leftover Mixin on this generator)
- [x] Package bump if the public type ships
- [x] Generator tests still pass
- [x] Results + How to validate

## Session

- Created: 1063987 (2026-09-11)
- Cockpit: timewarp-flow Grok `01a03d38-9611-7620-aae5-848e15dafa94`.
  Do not implement in cockpit.
- Implementer: Grok session `01a09167-0252-7fa0-bf29-caca8b3aba6c` (2026-09-11)

## Notes

- Follows **053-002** (attributes) and **192** (Page.mixin).
- Target name `ContractsGenerator` matches `foundation-contracts-generators`.

## Results

Renamed the bundled contracts generator off leftover mixin wording. Emit
behavior is unchanged (parser **053-003**, FQN **053-004**, incrementality
**053-005**, slimmer members **053-006**).

**Canonical names**

| Before | After |
|--------|--------|
| `ContractsMixinGenerator` | `ContractsGenerator` |
| `contracts-mixin-generator.cs` | `contracts-generator.cs` |
| `ContractsMixinGenerator_Tests` | `ContractsGenerator_Tests` |
| `contracts-mixin-generator-tests.cs` | `contracts-generator-tests.cs` |
| `ContractsMixinAttributes.g.cs` | `ContractsGeneratorAttributes.g.cs` |
| `CreateMixinProvider` | `CreateAttributeProvider` |
| `MixinHintNames` | `HintNames` |

**Files**

- `source/foundation/foundation-contracts-generators/contracts-generator.cs`
- `source/foundation/foundation-contracts-generators/foundation-contracts-generators.csproj`
- `source/foundation/foundation-contracts/foundation-contracts.csproj`
- `source/container-apps/api/projects/api-contracts/api-contracts.csproj`
- `source/analyzers/shared/hosted-route-discovery.cs`
- `source/analyzers/timewarp-architecture-convention-analyzers/endpoint-auth-posture-analyzer.cs`
- `tests/analyzers/timewarp-architecture-sourcegenerator-tests/contracts-generator-tests.cs`
- `tests/analyzers/timewarp-architecture-analyzers-tests/endpoint-auth-posture-analyzer-tests.cs`
- `skills/tw-web-api-contracts/SKILL.md` + `analysis/`
- `documentation/developer/how-to-guides/web-api-contracts/how-to-write-bff-api-contracts.md`
- `documentation/release-notes.md` (2.0.0-beta.17 note)

**Decisions**

- No extra version bump. Latest published tag is `v2.0.0-beta.16`; repo
  `<Version>` is already `2.0.0-beta.17` (same bar as **053-003**). The
  generator type is not consumer-facing API (053-002 left it internal to
  the analyzer asset); attributes stay `[ApiRoute]` /
  `[AuthApiRequest]` / `[OpenDataQueryParameters]`.
- Historical `kanban/done/` task records were left as snapshots.
- Analysis RFC/reviews keep pre-rename `[RouteMixin]` ballots; generator
  type/file paths were updated and a 053-007 note was added.
- Crunchit should copy `ContractsGenerator`, not `ContractsMixinGenerator`.

**Tests**

- `ContractsGenerator_Tests`: 17 passed.
- Sourcegenerator suite: 76 passed.
- `Should_Enforce_Auth_Posture`: 9 passed.

### How to validate

**Smoke**

```bash
cd tests/analyzers/timewarp-architecture-sourcegenerator-tests
dotnet test -c Release -- --filter-class ContractsGenerator
```

From the repo root:

```bash
rg -n 'ContractsMixinGenerator|contracts-mixin-generator|MixinHintNames|CreateMixinProvider|ContractsMixinAttributes' \
  --glob '!kanban/done/**' --glob '!kanban/in-progress/053-007*/**'
dotnet build source/container-apps/web/projects/web-contracts/web-contracts.csproj -c Release \
  -p:EmitCompilerGeneratedFiles=true -p:CompilerGeneratedFilesOutputPath=obj/generated --no-incremental
```

Inspect:

- `obj/generated/foundation-contracts-generators/TimeWarp.Foundation.Contracts.Generators.ContractsGenerator/ContractsGeneratorAttributes.g.cs`
- `obj/generated/foundation-contracts-generators/TimeWarp.Foundation.Contracts.Generators.ContractsGenerator/TimeWarp.Architecture.Features.Admin.Roles.GetRoles.Query.g.cs`

**Expect**

- Filter-class `ContractsGenerator`: 17 passed, 0 failed.
- Live-tree grep: no leftover generator type/file/helper names outside this
  task kitchen and historical `kanban/done/` records.
- Attributes hint is `ContractsGeneratorAttributes.g.cs` (not
  `ContractsMixinAttributes.g.cs`).
- GetRoles generated members still include `GetRoute() => RouteTemplate;`,
  `GetAuthQueryParameters()`, `GetOpenDataQueryParameters()` — emit
  behavior unchanged.

**Automated gate**

```bash
cd tests/analyzers/timewarp-architecture-sourcegenerator-tests && dotnet test -c Release
# expect: 76 passed
```

**Not in scope:** nuget.org publish of 2.0.0-beta.17; Crunchit copy of the
type (consumer follow-up). FastEndpoint assembly scan (**006-001**).
