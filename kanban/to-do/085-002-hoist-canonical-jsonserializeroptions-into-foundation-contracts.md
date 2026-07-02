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

- [ ] Add the canonical options to foundation-contracts (with Purpose/Design regions explaining
      it is THE seam definition).
- [ ] web-spa `program.cs` `Configure<JsonSerializerOptions>` uses it.
- [ ] Both test projects use it; delete their local declarations.
- [ ] Grep for other `new JsonSerializerOptions` under source/ that mean "the contract seam".
- [ ] Full build + all fast test suites green.

## Notes

- Foundation package bump not required beyond the running beta train (ships with next publish).
