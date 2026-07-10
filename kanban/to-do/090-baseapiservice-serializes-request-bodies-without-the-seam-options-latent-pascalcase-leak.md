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

- [ ] Pass options in PrepareContent
- [ ] Seam-casing test
- [ ] dev build 0/0, dev test green

## Session

- Created: 2026-07-11 (found during 071 TestApiService work)
