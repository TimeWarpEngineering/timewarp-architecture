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
- [ ] Document in page-mixin / INavigablePage guidance

## Notes

- **Severity:** Medium — product workaround is clean and already used.
- **Owner:** TimeWarp.Architecture.Generators.
- **Consumer:** Crunchit Clients / Client detail / Dashboard (033-002…005).
- **Catalogued:** Crunchit 033-007.
