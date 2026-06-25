---
name: mock-response-factory
description: >
  Add GetMockResponseFactory to API contracts and register factories in the SPA mock API service.
when-to-use: >
  GetMockResponseFactory, MockResponseFactory, MockCopicApiService, MockWebApiService,
  MockFactories, mock mode, IMockResponseFactory, SPA development without backend
---

# Mock Response Factory

Enables Blazor SPA development without a live backend. Every API contract exposes a
factory; the SPA mock service dispatches requests to it.

## Detection — find patterns in the current repo

```bash
rg -l 'GetMockResponseFactory' --glob '**/*.cs' | head -5
rg -l 'Mock.*ApiService' --glob '**/*.cs'
rg 'delegate.*MockResponseFactory' --glob '**/*.cs'
```

Read an existing contract + its registration before adding a new one.

## Step 1 — Implement on the contract

Add to the operation's `public static partial class`:

```csharp
public static MockResponseFactory<Response> GetMockResponseFactory()
{
  return _ => new Response(/* realistic sample data */);
}
```

Rules:

- Delegate type: `MockResponseFactory<TResponse>` — locate in the repo's shared contracts project
- Return **plausible** data, not empty shells or `default`
- `ListResponse<T>`: several varied items, set `totalCount` correctly
- Commands with empty `Response`: return `new Response()` or the repo's established pattern
- Stream/file endpoints: small in-memory stream, or follow a sibling endpoint's approach

### ListResponse example

```csharp
public static MockResponseFactory<Response> GetMockResponseFactory()
{
  AnnouncementDto[] items =
  [
    new() { Text = "Maintenance tonight", Link = "/announcements/1" },
    new() { Text = "New feature available", Link = "/announcements/2" },
  ];
  return _ => new Response(totalCount: items.Length, items);
}
```

### Command example

```csharp
public static MockResponseFactory<Response> GetMockResponseFactory()
{
  return _ => new Response { SecurityRoleId = 1, B2CGroupId = Guid.NewGuid() };
}
```

## Step 2 — Register in the SPA mock service

Mirror the registration pattern already in the repo. Common variants:

**Dictionary on mock service:**

```csharp
{ typeof(GetProfile.Query), GetProfile.GetMockResponseFactory() },
{ typeof(CreateSecurityRole.Command), CreateSecurityRole.GetMockResponseFactory() },
```

**Wrapper class per endpoint** (`MockFactories/` folder):

```csharp
internal sealed class GetProfileMockFactory : IMockResponseFactory
{
  public object Create(IApiRequest request) =>
    GetProfile.GetMockResponseFactory()((GetProfile.Query)request);
}
```

Then register the wrapper class in the mock service's factory dictionary.

## Step 3 — Verify

- SPA runs in mock mode (no backend required)
- Feature that calls the endpoint renders with sample data
- Command mocks return a response the client handler expects

## Checklist

- [ ] `GetMockResponseFactory()` on the contract partial class
- [ ] Sample data is realistic enough for UI development
- [ ] Registered in mock API service (dictionary or wrapper — match siblings)
- [ ] SPA feature verified in mock mode

## Related skills

- `web-api-contracts` — full contract workflow; mock factory is step 10 of new endpoints