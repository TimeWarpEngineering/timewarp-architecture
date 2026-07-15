# Add NullApiService to Foundation.Contracts for mock-first SPA fallback

## Description

Ship a **null-object** `IApiService` in **TimeWarp.Foundation.Contracts** for contract-first /
mock-first SPAs that have **no real BFF** yet.

Today the template always has a host to fall back to:

- `MockWebApiService` → contract factories, else **real** `WebServerApiService` (HTTP)
- Older `MockApiService` → missing mock **throws** `NotImplementedException`

Greenfield WASM-only products (e.g. Crunchit after 033-001) need an inner service for
`MockWebApiService` when there is no HttpClient BFF: return a **501 SharedProblemDetails**
so callers keep pattern-matching the `OneOf` instead of catching exceptions.

### Provenance

Crunchit `source/web-spa/services/api/null-api-service.cs` (generic — only Foundation types):

```csharp
public sealed class NullApiService : IApiService
{
  public Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>(
    IApiRequest request, CancellationToken cancellationToken) where TResponse : class
  {
    SharedProblemDetails problem = new()
    {
      Title = "No API backend",
      Status = (int)HttpStatusCode.NotImplemented,
      Detail = $"No mock factory and no real BFF for {request.GetType().FullName} ({request.GetHttpVerb()} {request.GetRoute()})."
    };
    return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>(problem);
  }
}
```

## Checklist

- [ ] Add `NullApiService` (or `NoBackendApiService`) implementing `IApiService` in
      `foundation-contracts` next to `i-api-service.cs`
- [ ] Default **501** + stable Title/Detail; optional ctor/options for Title, Status, Detail format
- [ ] XML + Purpose/Design regions (generic language — no product names)
- [ ] Unit/contract test: returns problem arm with type/route/verb in Detail; does not throw
- [ ] Document usage: `MockWebApiService(new NullApiService(), …)` for mock-only DI
- [ ] Optional: template note when MOCK_WEB_API without a host is a supported mode
- [ ] Pack/publish Foundation.Contracts; bump consumer CPM (Crunchit can delete local copy)

## Notes

### Why Foundation, not template SPA

Depends only on `IApiService`, `IApiRequest`, `SharedProblemDetails`, `FileResponse`, `OneOf` —
zero Blazor/HttpClient/domain. Any product that is mock-first before a BFF exists needs this.

### Design choices

| Concern | Decision |
|---------|----------|
| Missing mock behavior | **ProblemDetails (501)** not throw — matches OneOf contract |
| Configurability | Default strings OK; allow override of Title/Status/message template |
| DI | **Do not** auto-register — product `program.cs` chooses mock vs real |
| Naming | Prefer `NullApiService` (null object); alias `NoBackendApiService` if clearer |
| Host-present apps | Unchanged: keep HTTP fallback; null service is for **no transport** only |

### Non-goals

- Replacing architecture’s throw-on-missing-mock for host-present demos
- Product-specific error copy
- Full offline HTTP client simulation

### Related

- Crunchit 033-001 scaffold (local copy until package ships)
- `MockWebApiService` / `GeneratedMockResponseFactories` (Architecture Generators)
- `IApiService` Design region: “mock implementations stand in for real servers”
