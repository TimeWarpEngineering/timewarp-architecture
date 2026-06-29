# 053-001: Convert foundation-contracts mixins to a bundled source generator

## Parent

053-replace-morrismoxy-with-standard-c-source-generators (this is the foundation-contracts slice)

## Why now

Unblocks **051 Phase 4** (template references `TimeWarp.Foundation.*` packages instead of copying
source). Moxy `.mixin` files are `AdditionalFiles` that do NOT travel with a NuGet package, so a
generated app loses the contract-authoring mixins (RouteMixin et al.). A Roslyn source generator
ships as an analyzer asset inside the package and flows to consumers automatically — solving the
package-boundary problem natively.

## Scope (this task)

Only the **3 foundation-contracts mixins** (the ones crossing the package boundary):
- `RouteMixin.mixin` → `[RouteMixin(route, HttpVerb)]`: const RouteTemplate, GetHttpVerb(),
  GetRoute()/GetRoute(params), route-param properties (type mapping guid→Guid, int, min/max→int,
  datetime→DateTime + `:yyyy-MM-dd`, alpha/required/length/regex→string).
- `IAuthApiRequestMixin.mixin` → `[IAuthApiRequestMixin]`: implements IAuthApiRequest (UserId +
  GetAuthQueryParameters()).
- `IOpenDataQueryParametersMixin.mixin` → `[IOpenDataQueryParametersMixin]`: implements
  IOpenDataQueryParameters (Top/Skip/Filter/OrderBy/ReturnTotalCount + GetOpenDataQueryParameters()).

The other ~9 mixins (Page, StateAccess, Command/Query, etc.) stay on Moxy — tracked by parent 053.

## Design

- New project `source/foundation/foundation-contracts-generators` (netstandard2.0 Roslyn
  `IIncrementalGenerator`, CodeAnalysis.CSharp 5.3.0).
- Generator emits the 3 marker attributes (internal, AllowMultiple, Class|Struct) into the
  consumer's **RootNamespace** (read from `build_property.RootNamespace`) — matching Moxy so the
  FastEndpoint generator still resolves `<RootNamespace>.RouteMixinAttribute` from referenced
  metadata. Usages detected syntactically (route string + HttpVerb member read from attribute args).
- **Bundled into `TimeWarp.Foundation.Contracts`** package (`analyzers/dotnet/cs`). In-repo
  consumers (web-contracts, api-contracts) reference the generator project as an Analyzer only when
  building against source (`UseFoundationPackages != true`); in package mode it flows from the package.
- Remove the 3 mixins' `AdditionalFiles` from foundation-contracts/web-contracts/api-contracts and
  delete the 3 `.mixin`/`.md` files. Moxy stays for the remaining mixins.

## Checklist

- [ ] Generator project + generator (3 attributes, route parsing matching Moxy ground truth)
- [ ] Bundle into TimeWarp.Foundation.Contracts package; analyzer ref for repo consumers
- [ ] Remove 3 AdditionalFiles + delete the 3 .mixin/.md files
- [ ] Tests (generated output equivalence)
- [ ] `dev build` green; generated code matches Moxy semantics
- [ ] Resume 051 Phase 4

## Notes

Moxy ground-truth output captured 2026-06 via `EmitCompilerGeneratedFiles` on web-contracts.
Attributes are `internal` (no cross-assembly conflict; FastEndpoint reads them from metadata anyway).
No datetime route params exist today, but the type mapping is implemented for parity.
