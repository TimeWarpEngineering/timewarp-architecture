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

## Session

- Created: 2416398 (2026-09-09)
- Cockpit: timewarp-flow Grok `01a03d38-9611-7620-aae5-848e15dafa94`
- Implementer: Grok session `01a08eb0-1178-74e1-b52b-610bbffa131c` (2026-09-11)

## Notes

- File: `source/foundation/foundation-contracts-generators/contracts-mixin-generator.cs`
- Why today: attributes are generated `internal` in consumer RootNamespace
  so FastEndpoint matches `ApiRouteAttribute` by simple name (Moxy leftover).
- Sibling: **053-003** (parser — land before crunchit copies mixins).

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

**Files:** `contracts-mixin-generator.cs`, `hosted-route-discovery.cs`,
`endpoint-metadata.cs`, `endpoint-auth-posture-analyzer.cs`, generator + analyzer tests,
`skills/tw-web-api-contracts/SKILL.md`, how-to, release notes (2.0.0-beta.17).

**Not in scope (unchanged):** route parser (053-003), SyntaxProvider tightening (053-005),
emitting fewer members (053-006), FastEndpoint referenced-assembly walk (006-001).

**Tests:** `ContractsMixinGenerator_Tests` 9 passed; sourcegenerator suite 66 passed;
analyzer suite 157 passed; `dotnet run tools/dev-cli/dev.cs -- build` 0/0.

### How to validate

**Smoke**

```bash
cd tests/analyzers/timewarp-architecture-sourcegenerator-tests
dotnet test -c Release -- --filter-class ContractsMixinGenerator
```

**Expect**

- 9 passed (0 failed), including:
  - `Should_Emit_Public_Marker_Attributes_In_Foundation_Namespace` — generated source
    contains `namespace TimeWarp.Foundation.Features;` and `public sealed class ApiRouteAttribute`
  - `Should_Ignore_Consumer_RootNamespace_For_Attribute_Emit` — `RootNamespace=SmokeDefault`
    still emits attributes in `TimeWarp.Foundation.Features`, not `SmokeDefault`
  - `Should_Ignore_Same_Simple_Name_In_Another_Namespace` — no `GetRoute` for a foreign
    `Other.Lib.ApiRouteAttribute`
  - `Should_Generate_Route_Members_With_Type_Mapping` / `Should_Generate_Interface_Mixins` —
    `GetRoute`, `UserId`, `Top` / `ReturnTotalCount` still emitted

**Automated gate**

```bash
cd tests/analyzers/timewarp-architecture-sourcegenerator-tests && dotnet test -c Release
# expect: 66 passed

cd tests/analyzers/timewarp-architecture-analyzers-tests && dotnet test -c Release
# expect: 157 passed

dotnet run tools/dev-cli/dev.cs -- build
# expect: Build succeeded. 0 Warning(s) 0 Error(s)
```

**Not in scope:** `dev template-smoke` (full generated-app pack/install) is not required to
prove attribute FQN; the `SmokeDefault` RootNamespace generator test plus 0/0 solution
build cover dual-mode compile. Route `{LocationId}` parser is 053-003 (already landed).
