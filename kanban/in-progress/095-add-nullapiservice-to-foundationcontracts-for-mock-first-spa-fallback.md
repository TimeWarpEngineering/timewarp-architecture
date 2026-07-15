# Add NullApiService to Foundation.Contracts for mock-first SPA fallback

## Description

Ship a **minimal null-object** `IApiService` in **TimeWarp.Foundation.Contracts** for
contract-first / mock-first SPAs that have **no real BFF transport** yet.

This is a **platform primitive**, not product glue. It completes the story already stated on
`IApiService`: transport-agnostic execution with an explicit `OneOf` problem arm so callers
pattern-match instead of catching exceptions.

### Gap today

| Implementation | Missing mock / no route | Assumes |
|----------------|-------------------------|---------|
| `MockWebApiService` | Fall through to **real** `IApiService` (HTTP) | A host/BFF exists |
| `MockApiService` | **`NotImplementedException`** | Loud fail; fights OneOf |

Neither is correct for **WASM-only / mock-first before a host**. Greenfield products need a
**terminal** `IApiService` that stays in the OneOf world when no factory and no HTTP client exist.

### Required shape (golden, minimal)

```csharp
// foundation-contracts — sealed, parameterless, no options bag
public sealed class NullApiService : IApiService
{
  public Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>(
    IApiRequest request, CancellationToken cancellationToken) where TResponse : class
  {
    // Always return problem arm — never throw for "not implemented"
    // Status = 501 (Not Implemented) — frozen semantic; document in Design region
    // Title/Detail fixed platform copy (no product names)
    // Detail: type full name always; verb/route best-effort (resilient if metadata incomplete)
  }
}
```

**Name:** `NullApiService` only (null-object pattern). No public alias. Design region clarifies:
terminal service when no transport is registered — not an NRE.

### Composition (document explicitly)

```text
Mock-first, no BFF:
  MockWebApiService (factories)
          └── NullApiService          // terminal — 501 problem

Mock + real host (template today):
  MockWebApiService (factories)
          └── WebServerApiService     // HTTP — unchanged
```

Product `program.cs` wires the inner service. **Do not auto-register** NullApiService in DI.

### Provenance

Crunchit `null-api-service.cs` dogfooded the shape (Foundation types only). Architecture owns the
canonical implementation; products delete local copies after package upgrade.

## Checklist

- [ ] Add `sealed class NullApiService : IApiService` in `foundation-contracts` next to
      `i-api-service.cs` (kebab path: `services/null-api-service.cs`)
- [ ] Fixed **501** + fixed Title/Detail platform copy; **no** ctor options / message templates
- [ ] Resilient Detail: always request type full name; verb/route best-effort (no throw from
      `GetRoute`/`GetHttpVerb` failures if avoidable)
- [ ] `#region Purpose` / `#region Design` + XML on public type (generic language — no product names)
- [ ] Unit test: returns problem arm (Status 501); Detail mentions type; **does not throw**
- [ ] Document the two compositions (mock-only + Null vs mock + HTTP) near `IApiService` and/or
      mock-web-api-service Design / developer how-to snippet
- [ ] Optional one-line comment under template `MOCK_WEB_API` only if accurate — **do not** change
      host-present fall-through to NullApiService
- [ ] Pack/publish via normal Foundation release; consumers bump CPM and delete local copies

## Notes

### Why Foundation.Contracts

Depends only on `IApiService`, `IApiRequest`, `SharedProblemDetails`, `FileResponse`, `OneOf` —
zero Blazor / HttpClient / domain. Any mock-first product before a BFF needs this. Not Generators
(nothing to generate). Not a separate package (one type).

### Design decisions (locked)

| Concern | Decision |
|---------|----------|
| Missing implementation | **501 `SharedProblemDetails`**, never throw for this path |
| Status code | **501** frozen (Not Implemented); document; do not invent per-product statuses |
| Configurability | **None in v1** — fixed copy; products that need custom copy write their own 3-line type |
| DI | **Explicit product registration only** — no host auto-wire |
| Naming | **`NullApiService` only** — no dual public names |
| Host-present apps | **Unchanged** — keep HTTP fall-through when a BFF exists |
| Mock factory happy path | Still required for UI; Null is the **terminal**, not a substitute for factories |

### Non-goals

- Product-specific error copy or localized messages
- Options/builder API for Title/Status/Detail templates
- Auto-registering Null when `MOCK_WEB_API` is set (would hide missing factories on host apps)
- Replacing or rewriting `MockApiService` throw path (separate tech debt / follow-up)
- Offline HTTP client simulation
- Changing `MockWebApiService` validation throw behavior

### Follow-up (not this task)

- Align or delete legacy `MockApiService` (throw on missing mock) so the template OneOf story is
  consistent — new task if/when worth it

### Related

- `source/foundation/foundation-contracts/services/i-api-service.cs` (Design: mocks stand in for servers)
- `source/container-apps/web/web-spa/services/mocks/mock-web-api-service.cs` (factory registry + fall-through)
- `source/container-apps/web/web-spa/services/mocks/mock-api-service.cs` (legacy throw path — out of scope)
- Mock factory registry generator (`MockWebApiService` host type)
- Downstream: Crunchit local `NullApiService` until Foundation.Contracts ships the type



### Implementation plan (2026-07-15)

1. Add `services/null-api-service.cs` — sealed NullApiService, fixed 501, resilient Detail
2. Extend IApiService Design: two compositions (Null vs HTTP terminal)
3. Extend MockWebApiService Design: document Null as no-transport inner
4. New `tests/foundation/foundation-contracts-tests` (Fixie + Shouldly) + slnx entry
5. Test: problem arm, Status 501, type in Detail, does not throw
6. No DI auto-register; no template MOCK_WEB_API wiring change


## Session

- Created: from Crunchit mock-first SPA gap
- Revised: 2026-07-15 — golden platform primitive: minimal NullApiService, fixed 501, no options,
  explicit composition docs, host behavior unchanged
