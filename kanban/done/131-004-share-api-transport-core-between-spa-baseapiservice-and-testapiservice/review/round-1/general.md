# Round 1 — general

**Date:** 2026-07-29  
**Scope reviewed:** `HttpApiService` extract and composers + rider:

- `source/foundation/foundation-contracts/services/http-api-service.cs`
- `source/container-apps/web/projects/web-spa/services/api-services/base-api-service.cs`
- `source/container-apps/web/projects/web-spa/services/api-services/base-auth-api-service.cs`
- `tests/common/timewarp-testing/web-api-test-service/test-api-service.cs`
- `tests/common/timewarp-testing/web-api-test-service/web-api-test-service.cs` (public surface)
- `tests/foundation/foundation-contracts-tests/http-api-service-tests.cs`
- `source/analyzers/timewarp-architecture-analyzers/models/endpoint-metadata.cs` (EndpointEmitModel Design rider)
- Project refs: `foundation-contracts.csproj`, `timewarp-testing.csproj`, `foundation-contracts-tests.csproj`

## Summary

Task 131-004 (F-015 extract half) is met. Shared HTTP transport lives once in
**foundation-contracts** as `HttpApiService`; SPA `BaseApiService` and test
`TestApiService` are thin composers; token seam is async per-request; verb matrix
throws `NotSupportedException` for unsupported verbs; problem deserialization is
filter-catch only; Stream/204 behavior is single-path; Design regions describe
composition (not “mirrors BaseApiService”); EndpointEmitModel equatability caveat
is honest; public Test/WebApi test client surface is intact. **Zero issues.**

## Goals verification (claims re-checked)

| # | Concern | Verdict | Evidence |
|---|---------|---------|----------|
| 1 | Home is foundation-contracts (WASM-safe), not foundation-server | **Met** | Path `source/foundation/foundation-contracts/services/http-api-service.cs`; package `TimeWarp.Foundation.Contracts`; deps are Http/Json/OneOf-level only (no ASP.NET host stack). No `HttpApiService` under `foundation-server`. |
| 2 | Async per-request token Func; SPA MSAL adapter; Test pins at ctor | **Met** | Ctor `Func<CancellationToken, Task<string?>>? acquireBearerTokenAsync`; `ApplyBearerTokenAsync` invokes before each send. SPA: `CreateAcquireBearerToken(IAccessTokenProvider)` → `RequestAccessToken` / `TryGetToken`. Test: sets `DefaultRequestHeaders.Authorization` when `bearerToken` non-null; passes `acquireBearerTokenAsync: null`. |
| 3 | No ProjectReference SPA from tests | **Met** | `timewarp-testing.csproj` refs web-server / api-server / yarp / identity only — no `web-spa`. Test Design states flag-combination constraint. Integration suites use `TestApiService` from timewarp-testing. |
| 4 | `NotSupportedException` Head/Options; no swallow-all catch | **Met** | Switch default: `throw new NotSupportedException($"HttpVerb: {verb} is not supported.")` (covers Head/Options). `HandleProblemResponse` filters `JsonException or InvalidOperationException` only; outer catch is `OperationCanceledException` → 499 only. Unit test asserts Head → `NotSupportedException`. |
| 5 | Stream/FileResponse without EnsureSuccess; 204 before success | **Met** | 204 branch before `IsSuccessStatusCode` (lines 66–75). Stream → `FileResponse` only inside `HandleSuccessResponse` (already success branch); no `EnsureSuccessStatusCode` in `HttpApiService`. Tests: 204 → problem; Stream TResponse → FileResponse. |
| 6 | Design regions honest; no “mirrors BaseApiService” | **Met** | Core/SPA/Test/BaseAuth Design regions describe single home + composers. Grep: no “mirrors BaseApiService” / “mirrors that transport” on the extracted types. WebApiTestService Design correctly notes past SPA reflection trap. |
| 7 | EndpointEmitModel comment fixed | **Met** | Design L26–30: record supports Collect/conflict without static state; **honest caveat** that `ImmutableArray<T>` equatability is backing-array reference equality, acceptable because conflict keys on Route/HttpVerb. |
| 8 | Public TestApiService / WebApiTestService surface intact | **Met** | `public sealed class TestApiService` with ctor `(HttpClient, JsonSerializerOptions, string? bearerToken = "dummy-token")`, `GetResponse`, public `GetHttpResponseMessage`. `public class WebApiTestService` still takes concrete `TestApiService`, exposes `ConfirmEndpointValidationError` + `GetResponse`. Call sites (test apps, identity/role tests) unchanged in shape. |

## Issues

_None._

## Non-issues (checked, not raised)

- **Composer thinness:** BaseApiService only owns named-client / MSAL Func wiring; TestApiService only owns header pin + raw-message facade. No residual verb/route/problem private methods on either side.
- **BaseAuthApiService:** no longer re-applies token in `GetResponse` (Design documents prior double `RequestAccessToken`); remains a semantic subclass only.
- **Unit coverage home:** `foundation-contracts-tests` ProjectReferences foundation-contracts only (handler double via `HttpMessageHandler`); covers success, query route, 204, problem JSON, non-JSON error, cancel→499, Head, Stream/FileResponse, bearer set/null/absent acquire.
- **Options verb:** no dedicated Options unit test; same switch arm as Head (`NotSupportedException`). Adequate for the “decided once” requirement.
- **Bearer null does not clear existing Authorization:** `ApplyBearerTokenAsync` only sets when non-null. Matches “pin on client / set when acquired” model used by tests; SPA typically retains a valid MSAL token. Not a regression introduced by extract wording vs prior DefaultRequestHeaders pattern.
- **IApiService contract:** composers still implement `GetResponse`; raw HTTP remains off the interface (TestApiService/WebApiTestService only) — intentional for flag-safe tests.
- **task.md checklist boxes:** still unchecked in the kanban note; process hygiene only, not a product defect.
