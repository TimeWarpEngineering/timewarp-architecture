# Round 1 — general
**Date:** 2026-10-05
**Scope reviewed:** branch vs master (7215eadeb)

## Summary
The implementation meets the brief. Offerable `[CatalogAction]`s now use the shared constants, the typed `[ActionOffer]` records keep the wire shape (camelCase `credentialId`, Guid "D", pinned by the contracts round-trip test), and TWA0029/TWA0030 are registered in AnalyzerReleases, AGENTS.md (table and package row), the csproj description and `source/Directory.Build.props`. I re-ran the analyzer suite (`Should_Check_Offer_Agreement` 11/11). Record `EqualityContract` is excluded correctly (non-public), and Guid vs Guid? are told apart (different types under `SymbolEqualityComparer.Default`). The open items are places where the analyzer's model differs from what TimeWarp.State's generator and the client binder actually do (false negatives today, no current instance is wrong), plus some small items in the contracts helper and tests.

## Issues

### Issue 1 — Severity: suggestion
- File: source/analyzers/timewarp-architecture-convention-analyzers/action-offer-agreement-analyzer.cs:257
- Description: `CatalogParameters` does not reproduce the generator's "first explicit constructor" rule. TimeWarp.State's `ActionSetConstructorParser` takes the first `ConstructorDeclarationSyntax` among `DescendantNodes()` of the `[CatalogAction]`-attributed class declaration (`context.TargetNode`). That means one partial declaration only, static constructors included, and constructors of nested types included. The analyzer instead takes non-implicit `InstanceConstructors` across all declarations and orders them by `SourceSpan.Start`. When the action is a partial class split across files, that compares offsets from different syntax trees, which is meaningless. The analyzer also skips a leading `static Action()`, which the generator would pick up (0 parameters). In these cases TWA0030 checks a different parameter list than the catalog the binder uses at run time.
- Suggestion: Resolve the class declaration from `target.Attribute.ApplicationSyntaxReference` (its parent `ClassDeclarationSyntax`), take the first `ConstructorDeclarationSyntax` in its `DescendantNodes()`, and get the parameter symbols through the semantic model. This mirrors the generator exactly. Alternatively, record the remaining divergence in the Design region.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/analyzers/timewarp-architecture-convention-analyzers/action-offer-agreement-analyzer.cs:247
- Description: TWA0030 accepts a record that binds an optional parameter that comes after another optional parameter left unbound. For example, take `Action(Guid credentialId, bool notify = false, string tag = "")` with an offer record `{ CredentialId, Tag }`. `ContextualActionArguments.Bind` always refuses this ("cannot bind 'tag' after an omitted optional parameter", contextual-action-arguments.cs:87). So the build passes but the offer can never run. That is the run-time refusal the analyzer exists to prevent.
- Suggestion: Report TWA0030 when a bound parameter follows an unbound optional parameter that is not covered by UserInput. Or document the gap in the Design region and the AGENTS.md row.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/web/features/identity/offered-action-contracts.cs:65
- Description: `Create<TOffer>` gets both the catalog name (`OfferName(typeof(TOffer))`) and the serialized property set (`SerializeToElement<TOffer>`) from the static type argument, not the runtime type. If `ForCredential` is called with a variable typed `ICredentialActionOffer`, `TOffer` is the interface: `GetCustomAttribute` returns null and the call throws. A base record as the static type would serialize only the base properties. Today every call site passes the concrete record, so nothing is broken yet.
- Suggestion: Use `offer.GetType()` for both the attribute lookup and the serialization (`JsonSerializer.SerializeToElement(offer, offer.GetType(), ContractSerializationDefaults.Options)`).
- Status: open

### Issue 4 — Severity: suggestion
- File: tests/analyzers/timewarp-architecture-analyzers-tests/action-offer-agreement-analyzer-tests.cs:131
- Description: Several analyzer branches have no tests: skipping `[JsonIgnore]` properties, walking inherited properties (base-record loop), and the value-type nullability mismatch (`Guid?` property vs `Guid` parameter). The camelCase port and the JsonPropertyName path are covered.
- Suggestion: Add one case each for JsonIgnore (ignored property → no TWA0030), an inherited property that binds, and a `Guid?` vs `Guid` mismatch that reports TWA0030.
- Status: open

### Issue 5 — Severity: nit
- File: source/analyzers/timewarp-architecture-convention-analyzers/action-offer-agreement-analyzer.cs:227
- Description: `SymbolEqualityComparer.Default` ignores reference-type nullable annotations. A `string? Nickname` property passes against a required `string nickname` parameter. A null value is treated as missing by the binder (`ValueKind == Null` → "needs 'nickname'"), so it fails at run time instead of at build time.
- Suggestion: For reference types, also flag when the property is annotated nullable and the parameter is not (or compare with `SymbolEqualityComparer.IncludeNullability` when both sides are annotated). Or note it as accepted in the Design region.
- Status: open

### Issue 6 — Severity: nit
- File: source/container-apps/web/features/identity/offered-action-contracts.cs:48
- Description: The Results and Design region say "the server builds offers only from those records", but the public `OfferedAction(name, label, subject, arguments)` constructor still lets server code spell a name and argument keys by hand, and no analyzer checks that path. The constructor is needed for JSON deserialization and for the forged-offer tests, so this is a convention, not something the build enforces.
- Suggestion: Say so in the Design region ("convention; the public ctor exists for deserialization and fail-closed tests"), or make it `[JsonConstructor]` internal plus InternalsVisibleTo for the tests.
- Status: open

### Issue 7 — Severity: nit
- File: source/container-apps/web/features/identity/credential-offers-application.cs:8
- Description: The edited Design region line was not reflowed and is now 160 characters, while the neighboring comment lines wrap at about 100.
- Suggestion: Reflow the paragraph.
- Status: open
