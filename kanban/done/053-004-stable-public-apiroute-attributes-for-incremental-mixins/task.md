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

- [x] Stable public attributes (post-init, not per-consumer RootNamespace internals)
- [x] Mixin generator uses `ForAttributeWithMetadataName`
- [x] FastEndpoint/ingress match FQN
- [x] Template / dual-mode RootNamespace still works
- [x] Results + How to validate
- [x] Implementation review disposition (`review/`)
- [x] CI skill-lint: hyphenate eval grader name (PR #337)

## Session

- Created: 2416398 (2026-09-09)
- Cockpit: timewarp-flow Grok `01a03d38-9611-7620-aae5-848e15dafa94`
- Implementer: Grok session `01a08eb0-1178-74e1-b52b-610bbffa131c` (2026-09-11)
- Review oracle: Grok session `01a08ed1-1284-71d2-aa1c-8e9a336ca9f5` (2026-09-11)
- Implementer (skill-lint grader name): Grok session `01a08ee5-3c87-7e20-930c-cf7581295227` (2026-09-11)
- Review oracle (round 3): Grok session `01a08eea-52e7-71a3-93e1-a255e912ed33` (2026-09-11)

## Notes

- File: `source/foundation/foundation-contracts-generators/contracts-mixin-generator.cs`
- Why today: attributes are generated `internal` in consumer RootNamespace
  so FastEndpoint matches `ApiRouteAttribute` by simple name (Moxy leftover).
- Sibling: **053-003** (parser — land before crunchit copies mixins).
- PR #337 skill-lint (`103155212457`) failed `invalid-grader-name`
  `invokes_web_api_contracts`. Renamed to `invokes-web-api-contracts`
  in `skills/tw-web-api-contracts/evals/eval.yaml` on this same id.
  Local `vally lint skills` and `vally lint --eval-spec` exit 0.

## Results

Public `[ApiRoute]` / `[AuthApiRequest]` / `[OpenDataQueryParameters]` now live in
`TimeWarp.Foundation.Features` via `RegisterPostInitializationOutput` (same namespace as
`IAuthApiRequest` / `HttpVerb`). `ContractsMixinGenerator` discovers applications with
`ForAttributeWithMetadataName` on those metadata names. FastEndpoint, ingress, and TWA0006
coverage match `ApiRouteAttribute` by FQN (namespace + name string, not
`GetTypeByMetadataName` + `SymbolEqualityComparer`, because each contracts assembly still
has its own generated copy). TWA0014's `[AuthApiRequest]` path uses the same FQN helper.

Task 115 dual-mode: `TimeWarp.Foundation.*` is not rewritten by template `sourceName`;
contracts already `global using TimeWarp.Foundation.Features`. Generator tests pass with
`RootNamespace=SmokeDefault`. Existing mixins still emit `GetRoute` / `UserId` / OData
members. A foreign `Other.Lib.ApiRouteAttribute` is ignored.

PR #337 skill-lint failed because `skills/tw-web-api-contracts/evals/eval.yaml` used
grader name `invokes_web_api_contracts`. Vally requires lowercase hyphens. Renamed to
`invokes-web-api-contracts` on this id. Local `vally lint skills` 8/8; eval-spec lint
exits 0 (scoring-defaults-applied warning only).

**Files:** `contracts-mixin-generator.cs`, `hosted-route-discovery.cs`,
`endpoint-metadata.cs`, `endpoint-auth-posture-analyzer.cs`, generator + analyzer tests
(including FastEndpoint/ingress FQN foreign-attribute cases),
`ingress-route-prefix-generator.cs` Design region, `skills/tw-web-api-contracts/SKILL.md`,
`skills/tw-web-api-contracts/evals/eval.yaml`, how-to, release notes (2.0.0-beta.17).

**Not in scope (unchanged):** route parser (053-003), SyntaxProvider tightening (053-005),
emitting fewer members (053-006), FastEndpoint referenced-assembly walk (006-001).

**Tests:** `ContractsMixinGenerator_Tests` 9 passed; sourcegenerator suite 68 passed
(66 plus two FQN foreign-attribute cases); analyzer suite 157 passed;
`dotnet run tools/dev-cli/dev.cs -- build` 0/0; `vally lint skills` 8 passed.

### How to validate

**Smoke**

```bash
vally lint skills
vally lint --eval-spec skills/tw-web-api-contracts/evals/eval.yaml

cd tests/analyzers/timewarp-architecture-sourcegenerator-tests
dotnet test -c Release -- --filter-class ContractsMixinGenerator
dotnet test -c Release -- --filter-method Should_Ignore_Foreign_ApiRouteAttribute_Same_Simple_Name
```

**Expect**

- `vally lint skills`: 8 skill(s) linted, 8 passed (exit 0)
- `vally lint --eval-spec …/eval.yaml`: exit 0; no `invalid-grader-name`;
  optional warning `scoring-defaults-applied` is OK
- Mixin class: 9 passed (0 failed), including:
  - `Should_Emit_Public_Marker_Attributes_In_Foundation_Namespace` — generated source
    contains `namespace TimeWarp.Foundation.Features;` and `public sealed class ApiRouteAttribute`
  - `Should_Ignore_Consumer_RootNamespace_For_Attribute_Emit` — `RootNamespace=SmokeDefault`
    still emits attributes in `TimeWarp.Foundation.Features`, not `SmokeDefault`
  - `Should_Ignore_Same_Simple_Name_In_Another_Namespace` — no `GetRoute` for a foreign
    `Other.Lib.ApiRouteAttribute`
  - `Should_Generate_Route_Members_With_Type_Mapping` / `Should_Generate_Interface_Mixins` —
    `GetRoute`, `UserId`, `Top` / `ReturnTotalCount` still emitted
- Foreign-attribute method filter: 2 passed — FastEndpoint reports TWE007 `missing ApiRoute`
  and emits nothing; ingress does not emit `api/collided` (empty `All`)

**Automated gate**

```bash
vally lint skills
vally lint --eval-spec skills/tw-web-api-contracts/evals/eval.yaml
# expect: exit 0 (8 skills passed; no invalid-grader-name)

cd tests/analyzers/timewarp-architecture-sourcegenerator-tests && dotnet test -c Release
# expect: 68 passed (66 plus two FQN foreign-attribute cases)

cd tests/analyzers/timewarp-architecture-analyzers-tests && dotnet test -c Release
# expect: 157 passed

dotnet run tools/dev-cli/dev.cs -- build
# expect: Build succeeded. 0 Warning(s) 0 Error(s)
```

**Not in scope:** `dev template-smoke` (full generated-app pack/install) is not required to
prove attribute FQN; the `SmokeDefault` RootNamespace generator test plus 0/0 solution
build cover dual-mode compile. Route `{LocationId}` parser is 053-003 (already landed).

### Review disposition

- **Rounds:** 3 · **Effort:** 1 · **Roster:** general
- **Counts (final):** bug 0 / suggestion 1 fixed / nit 1 fixed (open=0, wontfix=0)
- **Disposition:** `clean` — M1 added FastEndpoint + ingress tests that a foreign
  `Other.Lib.ApiRouteAttribute` is ignored (TWE007 `missing ApiRoute`; no `api/collided`
  prefix). M2 updated the ingress Design region to FQN match. Round 2 re-verified both.
  Round 3 re-verified M1/M2 on HEAD through `d2f33a9c` and reviewed the post-disposition
  `eval.yaml` grader hyphen (`invokes-web-api-contracts`); no new findings.
- **Paths:**
  - `review/review-framework.md`
  - `review/round-1/general.md`
  - `review/round-1/merged.md`
  - `review/round-2/general.md`
  - `review/round-2/merged.md`
  - `review/round-3/general.md`
  - `review/round-3/merged.md`
  - `review/disposition.md`
