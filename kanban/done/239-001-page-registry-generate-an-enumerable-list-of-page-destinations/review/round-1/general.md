# Round 1 — general
**Date:** 2026-09-30
**Scope reviewed:** branch vs 64676d8b

## Summary

The generator change is correct as far as I can tell. `PageModel` equality covers every field. `Parameters` is compared with `SequenceEqual`, and `GetHashCode` only uses fields that `Equals` also compares. Cached models hold no `SyntaxTree`, `ISymbol` or `Location`: `PageDiagnostic` stores only the path, span and args, and the `Diagnostic` is built in `RegisterSourceOutput`.

Other checks that passed:
- Registry output is ordered deterministically (ordinal by route, then by hint name).
- `Policy` is never null, because it defaults to `Policies.Anonymous`.
- TWE009 is fail-closed. The page surface is still emitted; only registry membership is refused.
- The descriptor SSOT, the AnalyzerReleases.Unshipped entry and the AGENTS.md table all match.
- The `#if (api)` / `#if (grpc)` regions in NavMenu are intact.
- The Purpose/Design regions of the generator, the descriptors, the new interface and the SPA test were updated to match the change.
- The tests cover the happy path, both exclusion paths, both TWE009 forms, the no-pages case and incremental caching.

The only finding is a nit about how strong the drift guarantee is.

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/web/projects/web-spa/components/interfaces/i-navigation-destination.cs:18
- Description: `INavigationDestination` is a public marker interface that anyone can implement. Its Design region and the NavMenu comment say a NavMenu link "is a registry entry at compile time". That is only true if the generator is the only thing that adds the interface. A page written as `partial class FooPage : INavigationDestination` without `Navigable = true` compiles into `TimeWarpNavLink` but is not listed in `PageRegistry`, so the menu and the palette can drift again.
- Suggestion: Choose one of these:
  - Tone down the claim in the Design region: the constraint guarantees the opt-in, provided no one implements the marker by hand.
  - Enforce it, for example with a registry test that checks every type in the web-spa assembly that implements `INavigationDestination` appears in `PageRegistry.All`. The existing `page-registry-tests.cs` is a natural home for this.
- Status: fixed
