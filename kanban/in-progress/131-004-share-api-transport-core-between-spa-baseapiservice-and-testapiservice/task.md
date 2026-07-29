# Share API transport core between SPA BaseApiService and TestApiService

## Parent

131

## Description

Extract the shared HTTP transport core mirrored between SPA `BaseApiService` and test
`TestApiService` (task 131 F-015 extract half). Interim SPA catch/verb alignment was done
under 131; this task removes the remaining dual-maintenance mirror.

## Requirements

- Shared transport core (HttpClient + seam `JsonSerializerOptions` + token-acquisition
  delegate) both sides compose — home likely foundation client seam types already shared
  (`IApiService`, `IApiRequest`, `SharedProblemDetails`, `ContractSerializationDefaults`).
- Tests must not take a ProjectReference on the SPA stack (flag-combination constraint).
- Keep verb matrix consistent (`NotSupportedException` for unsupported verbs).
- Do not reintroduce swallow-all exception catch on problem deserialization.

## Verification concerns to honor (claude-verification, 131 review audits)

1. **Home must be WASM-safe** — foundation client layer, NOT foundation-server.
2. **Token acquisition** async per-request delegate.
3. **Verbatim extraction** except Head/Options once + Stream path reconcile.
4. **Stream/FileResponse** one behavior.
5. **Design regions** move with code.
6. **EndpointEmitModel** equatability comment rider.

## Checklist

- [ ] Choose home assembly / package for shared core (WASM-safe client layer — concern 1)
- [ ] Extract and compose from BaseApiService + TestApiService (verbatim rule — concern 3)
- [ ] Async per-request token seam works for SPA `IAccessTokenProvider` (concern 2)
- [ ] Head/Options decided once; dead arms removed or verbs supported (concern 3)
- [ ] Stream/FileResponse path reconciled to one behavior (concern 4)
- [ ] Design regions rewritten on both composers + core (concern 5)
- [ ] `EndpointEmitModel` equatability comment fixed (concern 6 rider)
- [ ] SPA + integration tests green
- [ ] `dev build` 0/0

## Notes

### Implementation plan (2026-07-29)

**Home:** `source/foundation/foundation-contracts/services/http-api-service.cs`  
`public sealed class HttpApiService : IApiService`  
Ctor: `(HttpClient, JsonSerializerOptions, Func<CancellationToken, Task<string?>>? acquireBearerTokenAsync = null)`

**Decisions:**
- Head/Options → `NotSupportedException` (no real HEAD client)
- Stream path: drop SPA-only `EnsureSuccessStatusCode` inside success branch
- SPA adapts `IAccessTokenProvider` → acquire Func; TestApiService pins header, passes null Func
- Drop BaseAuthApiService double token apply
- Unit tests in foundation-contracts-tests with HttpMessageHandler double
- Rider: honest EndpointEmitModel Design on ImmutableArray equality

**Steps:** Add HttpApiService + tests → compose BaseApiService → compose TestApiService → rider → build/test

## Session

- Created: 2026-07-28 — from task 131 disposition
- Plan: 2026-07-29 — tw-orchestrate-task Phase 2/3
