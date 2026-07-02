# Hoist canonical JsonSerializerOptions into foundation-contracts

## Parent
085-analyzer-and-source-gen-opportunities-to-remove-inference-collected-candidates

## Description

The client's canonical serializer options (CamelCase) exist in three places by convention:
web-spa `program.cs` DI config, `web-spa-integration-tests`, and
`web-contracts-tests/contract-serialization.cs` (whose Design region documents this candidate).
Client and tests can silently diverge today. Refactor, not analyzer: one static declaration in
foundation-contracts (e.g. `ContractSerializationDefaults.Options`), referenced by all three —
and by `MockWebApiService` if it serializes.

## Checklist

- [x] `ContractSerializationDefaults` in foundation-contracts/types: shared `Options` instance
      (STJ freezes on first use; no seam participant mutates) + `Apply` for the DI
      `Configure<JsonSerializerOptions>` pattern.
- [x] web-spa `program.cs` → `Configure<JsonSerializerOptions>(ContractSerializationDefaults.Apply)`.
- [x] Both test projects converted; local declarations deleted.
- [x] The sweep found **nine** declaration sites, not three: web-spa program.cs, **four copies in
      timewarp-testing** (test-server-application + api/web/yarp app variants), web-spa-integration
      ×3 (Aspire test app DI, weather serialization test, the Person smoke test — which was using
      *default* options and not exercising the seam at all), api-server-integration convention, and
      web-contracts-tests. All now reference the canonical authority.
- [x] Full build 0/0; suites green: contracts 7, web-server integration 22, api-server integration
      6, analyzers 26, sourcegen 16.

## Results

**Latent mismatch found and fixed:** `ApiServerTestConvention` registered
`new JsonSerializerOptions()` — **default/PascalCase** — as the DI singleton for the api test
client, disagreeing with the camelCase seam everywhere else (masked by case-insensitive
deserialization paths). Now canonical; api integration tests pass under the corrected options.

## Notes

- Foundation package bump not required beyond the running beta train (ships with next publish).
- Server side deliberately untouched: ASP.NET Core's Web defaults are camelCase and
  framework-managed; the canonical type documents that the seam matches them.
