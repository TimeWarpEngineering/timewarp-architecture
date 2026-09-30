# Page registry: generate an enumerable list of [Page] destinations

## Description

Child of 239 (Ctrl-K palette). The palette must index navigation destinations from one source,
not a second hand-copied route list. Today `PageSourceGenerator`
(`source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs`) emits
per-class members only (`[Route]`, `INavigableComponent`/`IStaticRoute`, `GetPageUrl`, `Policy`);
`Title`/`NavIcon` are hand-written static members required by `INavigableComponent`; NavMenu is
hand markup (`TimeWarpNavLink TPage=…`). Nothing aggregates pages.

## Requirements

- Extend the `[Page]` generator to emit one per-assembly registry (e.g. `PageRegistry.All`) of
  entries with: route template, `GetPageUrl` for static routes, `Title`, `NavIcon`, `Policy`,
  and a navigation/palette opt-in. Opt-in shape: a `[Page]` property (e.g.
  `Navigable = true` / `InPalette = true`) or a small separate attribute — decide and record in
  the generator Design region. Parameterized routes (no `IStaticRoute`) are excluded from the
  registry in v1 (they need arguments).
- Reflection-free (generated list referencing the static members); AOT/trim safe.
- Optional but preferred: NavMenu renders its product entries from the registry so the menu and
  the palette cannot drift. If NavMenu keeps hand markup (e.g. for category grouping), add a
  build-time check or test that every NavMenu `TimeWarpNavLink` target is in the registry.
- Generator tests (analyzer test project): registry contents, opt-in, parameterized-route exclusion,
  policy carried. SPA test: registry lists the expected demo pages.
- Update `tw-blazor-layout` / the analyzer Design region if the NavMenu rule changes.
- Gates: `dev build` 0/0 (generator change ⇒ full rebuild), `dev test`, `dev template-smoke`.
- **Do not start an AppHost** (`dev run`, `aspire run`, `dotnet run` of aspire-app-host) — task worktrees share the master user-secrets id. Record the manual browser check as not performed.

## Checklist

- [x] Registry generated with title/icon/policy/url and opt-in
- [x] Parameterized routes excluded
- [x] NavMenu from registry, or a drift check
- [x] Tests
- [x] Gates; no AppHost

## Notes

- Parent 239. Consumed by 239-003.
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

### Cockpit note (2026-09-30): compiler-server memory

Two walks of this task died with VBCSCompiler at ~31 GB. The uncommitted diff in this worktree
(25 files, not yet committed) was reviewed. It does NOT capture `Compilation`, `SemanticModel` or
`ISymbol`, and it has no `CompilationProvider` combine. It is not a whole-compilation leak.
It does keep a caching defect that already existed and that the new `pages.Collect()` makes wider:

- `PageModel` is a `readonly struct` with an `IReadOnlyList<(string, string)>` field and two
  `Diagnostic?` fields. Default struct equality compares the list by reference, so no model is
  ever equal to the previous one, and every generator step re-runs on every edit.
- A `Diagnostic` holds a `Location`, and the `Location` holds a `SyntaxTree`. That keeps old trees
  alive in the incremental cache. The `Collect()` registry output now keeps every page's model
  (and its trees) in one cached array.

Fix as part of this task (boyscout welcome):

1. Make `PageModel` value-equatable: a `record` with an equatable array for the parameters, or
   an explicit `IEquatable<PageModel>`.
2. Replace the two `Diagnostic?` fields with a location-free diagnostic info (descriptor id,
   file path, `TextSpan`/`LinePositionSpan`, message args). Build the `Diagnostic` inside
   `RegisterSourceOutput`.
3. Add a generator test that asserts the steps are cached: run twice with
   `trackIncrementalGeneratorSteps: true` after an unrelated edit, and expect `Cached`/`Unchanged`.

Memory discipline for the worker: run builds and tests one after another, never in parallel.
Run `dotnet build-server shutdown` after the gate runs and before finishing. Do not start an
AppHost.

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-30)
- Implement oracle (ganda task work, 2026-09-30): generator + registry + drift constraint + tests.
- Implement oracle resume (2026-09-30): cockpit-note caching fixes (value-equatable `PageModel`,
  location-free `PageDiagnostic`, cache test); gates run serially; build server shut down.

## Results

- **Opt-in:** `[Page("/route", Policy = …, Navigable = true)]` — a `[Page]` property (default
  false), not a separate attribute; literal `true`/`false` only. Recorded in the
  `PageSourceGenerator` Design region.
- **Registry:** the generator emits one per-assembly `PageRegistry.All`
  (`IReadOnlyList<PageRegistryEntry>`, root namespace) with `PageType`, `RouteTemplate`, `Url`
  (`GetPageUrl()`), `Title`, `NavIcon`, `Policy` — a generated array of static member reads
  (reflection-free, AOT/trim safe), sorted by route. Emitted only when the assembly has a `[Page]`.
- **Parameterized routes:** never in the registry; `Navigable = true` on one (or a non-literal
  value) is new error **TWE009** (descriptor SSOT, AnalyzerReleases.Unshipped, AGENTS.md table).
- **NavMenu drift:** NavMenu keeps hand markup (categories, AuthorizeView groups, `#if (api)` /
  `#if (grpc)` regions). Opt-in pages also get `INavigationDestination`
  (`web-spa/components/interfaces/i-navigation-destination.cs`) and `TimeWarpNavLink` constrains
  `TPage` to it — a NavMenu link to an unregistered page is a compile error (verified: adding
  `<TimeWarpNavLink TPage=LogoutPage />` → CS0311; reverted).
- **Opted in (16):** every NavMenu target plus Profile (profile-menu destination). Excluded:
  Login, Logout, ChooseMicrosoft365, TodoItems, RoleNew, and all parameterized pages.
- **Docs:** `tw-blazor-layout` gains "Navigation destinations come from one registry" + a
  reference-implementation bullet; NavMenu comment updated.
- **Tests:** 6 new generator tests (`page-source-generator-tests.cs`: contents/opt-in/order,
  policy carried, TWE009 + exclusion, parameterized exclusion, non-literal TWE009, no registry
  without pages) and SPA suite `features/application/page-registry-tests.cs` (expected demo pages
  present, parameterized/ceremony pages absent, entries well formed).
- **Incremental caching (cockpit note):** `PageModel` is now a value-equatable `sealed record`
  (`Parameters` as `ImmutableArray`, compared by sequence). The two `Diagnostic?` fields became
  location-free `PageDiagnostic` values (descriptor, path, `TextSpan`, `LinePositionSpan`, args).
  The `Diagnostic` is built in `RegisterSourceOutput`, so no `SyntaxTree` is pinned in the cache.
  New test `Should_Cache_Page_And_Registry_Outputs_When_Unrelated_Tree_Is_Added` asserts that every
  tracked output is `Cached`/`Unchanged` after an unrelated edit and that TWE009 is still reported.
  It was checked by forcing `Equals` to return false, which made the test fail.
- **Gates (serial):** `dev build` 0/0 (full rebuild; `PageRegistry.g.cs` has 16 entries);
  `dev test` exit 0, 21 suites, 0 failed; `dev template-smoke` SUCCEEDED; `ganda repo audit` passes.
- **Manual browser check: not performed** (no AppHost started, per task rule).

### How to validate

**Smoke:**
```bash
cd tests/analyzers/timewarp-architecture-sourcegenerator-tests && dotnet test -c Release -- --filter-class PageSourceGenerator
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class All_Should_
dotnet run tools/dev-cli/dev.cs -- build
```

**Expect:** generator suite 15/15 passed (includes the incremental-cache test); SPA `All_Should_` 3/3 passed; `dev build` 0 warnings /
0 errors, with
`artifacts/generated/web-spa/timewarp-architecture-analyzers/TimeWarp.Architecture.Analyzers.PageSourceGenerator/PageRegistry.g.cs`
listing 16 entries (Home `/` first). Adding `<TimeWarpNavLink TPage=LogoutPage />` to
`NavMenu.razor` fails the web-spa build with CS0311 (INavigationDestination).

