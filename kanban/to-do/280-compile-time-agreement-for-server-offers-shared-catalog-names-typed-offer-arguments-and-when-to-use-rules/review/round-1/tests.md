# Round 1 — tests
**Date:** 2026-10-05
**Scope reviewed:** branch vs master (7215eadeb), test focus

## Summary
The analyzer suite covers every branch the brief names: a passing case, SDK gate off, unknown name, derived default name not counting, property with no matching parameter, wrong type, `[JsonPropertyName]` (positive only), required parameter unbound, bad `UserInput` (optional, missing, already bound), offers from metadata, and camelCase parity with STJ. Re-ran it: 11/11 pass, which matches the claim. The SPA reflection check fails closed: zero records gives an empty set against `OfferedActionNames.All` (3 names), so it fails. The serialization test pins the Revoke key set exactly. The csproj `<Using>` uses the composed property, so it is sourceName-safe. The open items are coverage gaps, mainly the production-shaped (transitive) offer discovery path, which no automated test exercises.

## Issues

### Issue 1 — Severity: suggestion
- File: tests/analyzers/timewarp-architecture-analyzers-tests/action-offer-agreement-analyzer-tests.cs:293
- Description: `Given_Offers_In_A_Referenced_Assembly_Checks_Them` declares `ActionOfferAttribute` in the same assembly as the offer records. That only exercises the `definers.Contains(assembly)` branch of `OfferAssemblies`. Production has the attribute in TimeWarp.Architecture.Attributes and the records in web-contracts, which references it. That goes through the other branch, `assembly.Modules.Any(module => module.ReferencedAssemblySymbols.Any(definers.Contains))`, and no test covers it. If that branch regresses, the analyzer goes silent in the real SPA build and all 11 tests still pass. The only proof today is the manual negative build recorded in task.md.
- Suggestion: Add a three-project case. Project "Attributes" defines `ActionOfferAttribute`. Project "Contracts" references "Attributes" and declares the records. The SPA references both (or only Contracts). Expect TWA0029 at `WithNoLocation()` for an unknown name.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/analyzers/timewarp-architecture-convention-analyzers/action-offer-agreement-analyzer.cs:124
- Description: `if (catalogActions.IsEmpty) return;` skips every check when the SPA compilation has `CatalogActionAttribute` but no `[CatalogAction]` types. In that case every `[ActionOffer]` names an action that does not exist, yet nothing is reported. No test pins this, either as intended or as a bug, and the Design region does not mention it.
- Suggestion: Either drop the early return so every offer gets TWA0029, or keep it, record the reason in Design, and add a test that pins it.
- Status: open

### Issue 3 — Severity: suggestion
- File: tests/analyzers/timewarp-architecture-analyzers-tests/action-offer-agreement-analyzer-tests.cs:279
- Description: Several analyzer branches the Design region describes have no test:
  - a `[JsonIgnore]` property is skipped;
  - properties inherited from a base record are bound;
  - the first explicit constructor is chosen when an action has more than one;
  - a `[JsonPropertyName]` that names no parameter is reported (only the positive case exists);
  - a gate value of `false` (only an absent gate is tested);
  - two actions with the same explicit Name. Here the first entry pulled from a `ConcurrentBag` wins, so the TWA0030 target can be nondeterministic and the duplicate is never reported.
- Suggestion: Add small cases for JsonIgnore, inheritance, multiple constructors and the JsonPropertyName negative. Decide what duplicate explicit names should do (report it, or deterministic ordering) and pin that with a test.
- Status: open

### Issue 4 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/identity/credential-offers-tests.cs:74
- Description: The reflection check works out bound names as `JsonNamingPolicy.CamelCase.ConvertName(property.Name)` over every public instance property. It ignores `[JsonPropertyName]` and `[JsonIgnore]`, while both the analyzer and the real wire (`OfferedAction.Create` serializes with `ContractSerializationDefaults.Options`) honor them. No record uses these attributes today, so this is latent. The first record that does will make this test disagree with the analyzer, either failing wrongly or passing for the wrong reason.
- Suggestion: Take the bound names from the real wire shape. One option: serialize a sample instance through `OfferedAction.Create` and read `Arguments.Keys`. Another: honor the two attributes the way the analyzer does.
- Status: open

### Issue 5 — Severity: nit
- File: tests/container-apps/web/web-contracts-tests/features/identity/identity-contracts-serialization-tests.cs:503
- Description: The round-trip test pins the exact key set only for `RevokeCredentialOffer`. `RenameCredentialOffer` is the record carrying a `public const NicknameInput` and `UserInput`, and its wire shape is not pinned to exactly `["credentialId"]`. The web-server test checks that `Arguments["credentialId"] == Subject`, but it would not catch an extra key.
- Suggestion: Add a Rename offer to the round-trip and assert `Arguments.Keys.ShouldBe(["credentialId"])`.
- Status: open

### Issue 6 — Severity: nit
- File: source/container-apps/web/features/identity/offered-action-contracts.cs:78
- Description: No test covers the fail-closed throw in `OfferedAction.Create` when `TOffer` has no `[ActionOffer]` attribute.
- Suggestion: Add a one-line host-free test (e.g. in web-contracts-tests) asserting `InvalidOperationException` for an unattributed record.
- Status: open
