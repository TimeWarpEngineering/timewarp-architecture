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

- [ ] Design multi-route API (stacked `[Page]` vs `[Page(routes: ...)]`)
- [ ] Implement generator emission for N routes
- [ ] Tests in architecture generators package
- [ ] Document in page-attribute / INavigablePage guidance

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
