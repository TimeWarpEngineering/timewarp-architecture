# Page generator multi-route support for multi-tab pages

## Description

Architecture **Page generator** currently emits a single `[Route]` from one `[Page(...)]`
attribute. Multi-tab pages need multiple absolute routes on the same component.

**Crunchit workaround (epic 033):**

```csharp
[Page("/clients")]                          // generator emits this route
[Route("/clients/revenue")]                 // manual extra route
[Route("/clients/me-close")]
public partial class ClientsPage : ...
```

Same pattern: `DashboardPage` (`[Page("/dashboard")]` + `[Route("/")]`),
`ClientDetailPage` (`[Page("/clients/{ClientId:string}")]` + `[Route("/clients/{ClientId}/revenue")]`).

## Requirements

- Allow multiple `[Page]` attributes **or** multi-route parameters on one page class.
- Generator emits one `[Route]` per declared path; `GetPageUrl` / nav helpers remain well-defined
  (primary route vs additional routes — document semantics).
- Preserve Policy/const-ref behavior from task **094**.

## Checklist

- [x] Design multi-route API (stacked `[Page]` vs `[Page(routes: ...)]`)
- [x] Implement generator emission for N routes
- [x] Tests in architecture generators package
- [x] Document in page-attribute / INavigablePage guidance

### Cockpit note (2026-10-03): this spec predates PageRegistry

Since this task was written, `PageSourceGenerator` gained (task 239-001):

- `Navigable = true`;
- the generated `PageRegistry.All` and `INavigationDestination`;
- **TWE009**, which says a navigable page needs a static route;
- a value-equatable, cached incremental model (`PageModel` record plus location-free
  `PageDiagnostic`), which must not regress.

The multi-route design must define:

- **Primary vs additional routes.** `GetPageUrl` / `RouteTemplate` / `PageRegistry` use exactly
  one **primary** route. Additional routes are `[Route]`-only aliases and never get registry rows.
  Document which declaration is primary.
- **TWE009 with multiple routes.** `Navigable` judges the primary route. An additional
  parameterized route must not trip TWE009, and must not be silently dropped from emission.
- **Policy / TWE005** (const-ref Policy, old architecture task 094) applies once per page, not
  per route.
- **Crunchit parity.** The three workarounds in the Description compile and behave identically
  under the new API:
  - Clients: `/clients` plus revenue and me-close;
  - Dashboard: `/dashboard` plus `/`;
  - ClientDetail: a parameterized primary plus a parameterized additional route.
- **Diagnostics.** Duplicate route strings across one page, and conflicting declarations, are
  fail-closed diagnostics with new TWE ids registered in the descriptor SSOT and the AGENTS.md
  generator table.
- **Tests.** Extend `page-source-generator-tests.cs`, including the incremental-cached test (all
  outputs `Cached`/`Unchanged` after an unrelated edit).

Choose the API shape (stacked `[Page]` versus a routes parameter) and record the reason in the
generator's Design region. The skill text goes in `tw-blazor-layout` (navigation destinations)
or wherever the `[Page]` guidance lives. Do not start an AppHost.

## Notes

- **Severity:** Medium — product workaround is clean and already used.
- **Owner:** TimeWarp.Architecture.Generators.
- **Consumer:** Crunchit Clients / Client detail / Dashboard (033-002…005).
- **Catalogued:** Crunchit 033-007.

## Results

**API shape: one `[Page]` with params aliases** — `[Page("/primary", "/alias", …, Policy = …, Navigable = …)]`
(`PageAttribute(string RouteTemplate, params string[] AdditionalRoutes)`, `AllowMultiple = false`).
Stacked `[Page]` was rejected: each copy could carry its own Policy/Navigable, needing an ownership
rule; one attribute keeps route + policy + opt-in together. Reason recorded in the
`PageSourceGenerator` Design region.

- **Primary vs additional:** the first argument is primary — it alone drives `GetPageUrl`,
  `IStaticRoute`, `RouteTemplate`, the `PageRegistry` row, and the TWE009 judgment. Aliases are
  emitted as `[Route]` only (never registry rows), so a parameterized alias on a static navigable
  page is legal and still emitted.
- **Policy / TWE005:** once per page.
- **Alias tokens:** an untyped alias token inherits the primary's type (`{ClientId}` after
  `{ClientId:string}`; `{OrderId}` after `{OrderId:Guid}` emits `:guid`); alias-only tokens get a
  `[Parameter]` prop. Fixed the route-token regex, which mis-split untyped `{Name}` tokens.
- **New diagnostics** (SSOT `diagnostic-descriptors.cs`, `AnalyzerReleases.Unshipped.md`,
  AGENTS.md table): **TWE010** — two routes of one page are the same Blazor route (case-insensitive,
  token-name-free), including a hand-written `[Route]` repeating a `[Page]` route. **TWE011** —
  stacked `[Page]`, non-literal alias, or alias token typed differently from the primary. Both
  fail-closed (no page surface emitted). Distinct hand-written `[Route]` aliases (the pre-096
  Crunchit workaround) still compile.
- **Crunchit parity:** the Clients, Dashboard, and ClientDetail shapes are covered by tests.
- **Docs:** `skills/tw-blazor-layout/SKILL.md` (navigation section), `web-spa/mixins/page-attribute.md`.
- **Gates:** `page-source-generator-tests` + whole sourcegenerator suite 99/99 passing; `dev build` 0 warnings / 0 errors.

### How to validate

**Smoke:**

```bash
cd tests/analyzers/timewarp-architecture-sourcegenerator-tests
dotnet test -c Release -- --filter-class PageSourceGenerator
cd ../../.. && dotnet run tools/dev-cli/dev.cs -- build
```

**Expect:** all PageSourceGenerator tests pass. That includes the three Crunchit parity tests, the
TWE009 primary-only behavior, TWE010/TWE011, and the incremental test, where every output is
`Cached`/`Unchanged` with multi-route and TWE010 pages present. The full build reports 0 warnings and
0 errors, and the existing web-spa `[Page]` usages are unchanged.
