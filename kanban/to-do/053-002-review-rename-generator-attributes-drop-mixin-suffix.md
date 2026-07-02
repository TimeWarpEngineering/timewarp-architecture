# 053-002: Review & rename generator attributes — drop the "Mixin" suffix

## Parent

053-replace-morrismoxy-with-standard-c-source-generators

## Summary

The attributes that used to be Morris.Moxy mixins still carry the "Mixin" suffix, which is Moxy-era
terminology now that they're plain Roslyn `IIncrementalGenerator`s. Review whether to rename them to
read as ordinary capability/trigger attributes, and rename the ones where it's worth the churn.

Current names:
- `StateAccessMixinAttribute` → `[StateAccessMixin]` (web-spa) — candidate: `[StateAccess]` /
  `[GenerateStateAccessors]` / `[StateAccessor]`.
- `RouteMixinAttribute` → `[RouteMixin]` (foundation-contracts) — candidate: `[Route]` / `[ApiRoute]`.
- `IAuthApiRequestMixinAttribute` → `[IAuthApiRequestMixin]` — candidate: `[AuthApiRequest]`.
- `IOpenDataQueryParametersMixinAttribute` → `[IOpenDataQueryParametersMixin]` — candidate:
  `[OpenDataQueryParameters]`.

## Maintainer directive (2026-07-02) — this task now gates the skill rewrite

Per the RFC Decision 8 ruling ([[contract-conventions-rfc]] in `skills/web-api-contracts/analysis/`):

- **Compatibility / package versioning is explicitly NOT a factor.** "We want the best solution, we
  don't want tech debt because of 'compatibility' or previous bad decisions." The foundation package
  is beta and the maintainer owns the downstream — decide the names on merits alone, then bump and
  publish without ceremony. (The "package API change" bullet below stays as *work to do*, not as a
  reason to hedge.)
- **The question set, in order:** (1) **Should** the source-gen attributes be renamed? (2) If so, to
  **what**? (3) Should there be a **consistent suffix/prefix/convention that signals "this attribute
  drives source generation"** — or is intent-naming with no mechanism marker (the .NET ecosystem
  norm: `[ObservableProperty]`, `[JsonSerializable]`, `[LibraryImport]`) the right call?
- **Sequencing:** answer + implement this, **then** the skill rewrite
  ([[081-rewrite-web-api-contracts-skillmd-per-rfc-resolutions]]) proceeds once, against final names.
- While in the FastEndpoint generator's name match, also fix the **hardcoded
  `"TimeWarp.Architecture.RouteMixinAttribute"`** (`endpoint-metadata.cs:31`) — contrary to the
  bullet below it is **not** `<RootNamespace>`-parameterized; it is pinned to the
  `TimeWarp.Architecture` namespace, a latent break for generated apps with a different root
  namespace (verified in the RFC mechanism note).
- Naming collision note for the `[Route]` candidate: `Microsoft.AspNetCore.Mvc.RouteAttribute`
  exists — weigh `[ApiRoute]` against it.

## Considerations (why this is a deliberate pass, not a one-off)

- **Consistency:** rename all together or none — a half-renamed set is worse than leaving "Mixin"
  everywhere.
- **`RouteMixinAttribute` is the expensive one:**
  - Read **by name** from referenced-assembly metadata by the FastEndpoint generator
    (`endpoint-metadata.cs` matches `"<RootNamespace>.RouteMixinAttribute"`) — rename must be applied
    there too.
  - Ships in the **published `TimeWarp.Foundation.Contracts`** package (the generator is bundled). A
    rename is effectively a package API change → coordinate a version bump and the generated-template
    contract usages.
  - Touches every `[RouteMixin(...)]` contract usage and the `webapi-contracts` skill
    (`timewarp-flow/master/skills/webapi-contracts`).
- **`StateAccessMixin` is cheap/contained:** web-spa-internal, no package, no cross-generator
  coupling — just the generator const + ~10 usage sites.
- Counter-argument to keep: "mixin" is a legitimate general concept (mixes members into a partial
  type) independent of Moxy. Decide whether the suffix communicates intent or just noise.

## Checklist

- [ ] Decide: rename, and to what names (keep a consistent convention across all four).
- [ ] If yes: rename `StateAccessMixin` (low-risk) + usages.
- [ ] If yes: rename the foundation attributes — update the FastEndpoint generator's name match,
      all contract usages, the webapi-contracts skill, bump the foundation package version.
- [ ] If no: note the decision and close.

## Notes

Raised after converting StateAccessMixin and the 3 foundation contract mixins to source generators —
the names outlived the tool that coined them. Deferred from that work to avoid a half-renamed tree.
