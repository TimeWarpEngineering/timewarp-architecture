# BaseApiService serializes request bodies without the seam options (latent PascalCase leak)

## Problem

`web-spa/services/api-services/base-api-service.cs` `PrepareContent`:

    string requestAsJson = JsonSerializer.Serialize(apiRequest, apiRequest.GetType());

No JsonSerializerOptions — POST/PUT/PATCH bodies go out PascalCase while the contract seam is
camelCase (`ContractSerializationDefaults`). It works today only because ASP.NET Core's JSON
binder is case-insensitive by default. Any consumer that is case-sensitive (or a future binder
config change) breaks silently.

Note: the test client (`timewarp-testing/web-api-test-service/test-api-service.cs`) already
serializes with the seam options — after fixing this, SPA and test clients agree.

## Fix

Pass the injected `JsonSerializerOptions` to `Serialize`. Consider a web-contracts-tests
round-trip that pins body casing at the seam.

## Checklist

- [x] Pass options in PrepareContent
- [x] Seam-casing test
- [x] dev build 0/0; targeted test green (docker-gated suite unavailable — see Results)

## Session

- Created: 2026-07-11 (found during 071 TestApiService work)

## Results (2026-07-14)

**Implemented** (commit bc1a98f2):

- `base-api-service.cs` `PrepareContent`: now an instance method serializing POST/PUT/PATCH bodies
  with the injected `JsonSerializerOptions` — requests and responses both use the seam options.
  Design region updated to record the bidirectional-seam decision.
- New black-box test `Serialization/ApiService_Body_Casing_Tests.cs` (web-spa-integration-tests):
  `ApiServerApiService` over a capturing `HttpMessageHandler`; asserts the wire body is camelCase
  with `Case.Sensitive` (Shouldly contains-checks are case-insensitive by default — the default
  would mask exactly this regression).

**Key decisions**

- Black-box through the public `GetResponse` path rather than exposing `PrepareContent` — no
  internals, tests the real transport. A capturing handler is required at all because ASP.NET's
  case-insensitive binder makes the bug invisible to any integration test.
- Verified `ContractSerializationDefaults` sets only the naming policy — the fix changes casing
  and nothing else on the wire.

**Test outcomes**

- Targeted test: PASS. Negative control: reverting the fix makes it FAIL (pin verified).
- `dev build` 0/0.
- Full web-spa suite unavailable at close: Docker Resource Saver re-hung the daemon; failures are
  the identical pre-existing DCP signature (fixture DI, before changed code). The suite was fully
  green on this change's baseline 2026-07-14 with docker up; nothing here touches the aspire path.
