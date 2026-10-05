# Round 2 — general
**Date:** 2026-10-05
**Scope reviewed:** 3f9df71a8 fix delta + re-verification of M1–M13

Gate run: `tests/analyzers/timewarp-architecture-analyzers-tests` with `--filter-class Should_Check_Offer_Agreement` passes 23/23.
I compared the analyzer with the decompiled TimeWarp.State 12.0.0-beta.8 source generator (`ActionSetConstructorParser`,
`ActionCatalogSourceGenerator`), with `ContextualActionArguments.Bind`, and with `CommandPaletteRunner.Bind`.

## Prior findings
| ID | Status | Note |
|----|--------|------|
| M1 | fixed | `CatalogParameters` now takes the first `ConstructorDeclarationSyntax` in `DescendantNodes()` of the attributed class declaration. That is what `ActionSetConstructorParser.GetParameters` does with the `ForAttributeWithMetadataName` TargetNode. Static and nested-type constructors are covered by `ConstructorsWithin`. The constructor is matched by syntax tree and span, which is correct. Tests cover multiple constructors and partial declarations. See M14 for one edge where the target is a record. |
| M2 | fixed | `omittedOptional` reports a present argument after an absent optional parameter. This matches `Bind`'s `skippedOptional` refusal. UserInput is counted as present only for required parameters, and optional UserInput is reported separately, so the result agrees with the palette's `TryAdd` merge. Tested. |
| M3 | fixed | `Create` uses `offer.GetType()` for both the serialization and the name lookup. Pinned by `Use_The_Runtime_Record_For_An_Interface_Typed_Offer`. |
| M4 | fixed | New tests: JsonIgnore, inherited property, `Guid?`, multiple constructors, JsonPropertyName naming no parameter, gate set to `false`. |
| M5 | fixed (narrow) | Flags a nullable reference property against a NotAnnotated reference parameter. Tested. The rule is narrower than its own reason ("binder treats null as missing"); see M15. |
| M6 | fixed | The Design region states the convention and why the constructor stays public. Verified: a forged `new OfferedAction(...)` exists in credential-offers-tests.cs:265. |
| M7 | fixed | Design region reflowed. |
| M8 | fixed | `Given_Attribute_In_A_Third_Assembly_Still_Checks_Offers` exercises the `OfferAssemblies` branch where the contracts assembly references the defining assembly. |
| M9 | fixed | Early return removed. `Given_No_Catalog_Actions_Reports_TWA0029` pins it. The only remaining early exits are the WASM gate, a missing `CatalogActionAttribute` type, and no visible `ActionOfferAttribute`, all legitimate. |
| M10 | fixed | Names declared more than once produce TWA0029 with an ordinal-sorted list, so the output is deterministic. The `{2}` placeholder is used for both message variants, and the descriptor title, AnalyzerReleases.Unshipped.md, AGENTS.md row and skill all agree. TWS0006 ("Action catalog name '{0}' is used by more than one [CatalogAction]") confirmed in the TimeWarp.State analyzer. |
| M11 | fixed | Keys come from the real `OfferedAction.Create`. `GetUninitializedObject` is safe here: `ContractSerializationDefaults` sets no `DefaultIgnoreCondition`, so null properties still emit keys, and the three current records have only auto/positional properties. A computed getter that dereferences a field would throw in a future record, but that fails loudly, not silently. |
| M12 | fixed | `Arguments.Keys.ShouldBe(["credentialId"])`. |
| M13 | fixed | `Refuse_A_Type_Without_ActionOffer`. |

**Accepted residual (SPA only references Attributes transitively):** not realistic. SDK-style ProjectReferences are transitive by default, and the `TwArchitectureAttributesPackageId` PackageReference in web-contracts.csproj has no `PrivateAssets`. So the Attributes assembly reaches the SPA's csc in both source and package mode. The SPA already depends on this: web-spa.csproj:105-111 adds a global `Using` for the Attributes namespace and uses `[PageLocalMessageBar]` / `[DirectComponentSideEffect]` / `[SideEffectService]` with no direct reference ("already flows transitively from web-contracts"). If the flow broke, the SPA would fail to compile before the analyzer could go silent.

## Summary
All 13 round-1 findings are fixed and the gate is green. The fix delta adds no bug. Three small new items:
- M14 (nit): an edge in the new constructor lookup.
- M15 (suggestion): the M5 nullability rule keys on the parameter's annotation instead of on whether it is required, so it misses nullable value types and nullable required parameters.
- M16 (nit): the JsonIgnore skip pre-dates the fix but is now pinned by a test, and it ignores conditional `JsonIgnore`.

The Design regions, AGENTS.md TWA0029/0030 rows and skills/tw-blazor text match the code.

## Issues
### M14 — Severity: nit
- File: source/analyzers/timewarp-architecture-convention-analyzers/action-offer-agreement-analyzer.cs:305
- Description: `FirstAncestorOrSelf<ClassDeclarationSyntax>()` starts at the attribute and climbs. When `[CatalogAction]` is on a non-class declaration, for example a nested `record Action(...)` (AttributeTargets.Class allows records), it skips the record and lands on the enclosing `*ActionSet` class. `DescendantNodes()` then picks that class's first constructor, typically the nested `Handler(IStore store)`, so TWA0030 compares the offer against the wrong parameters. Separately, TWA0029 accepts the name, but TimeWarp.State's generator only takes `ClassDeclarationSyntax` targets, so no catalog entry exists at run time.
- Suggestion: Use the attribute's own declaration: `attributeSyntax.Parent?.Parent as ClassDeclarationSyntax`. Treat a non-class target as "not cataloged": exclude it from `byName`, so the offer gets TWA0029. Optionally add a one-line test.
- Status: open

### M15 — Severity: suggestion
- File: source/analyzers/timewarp-architecture-convention-analyzers/action-offer-agreement-analyzer.cs:255
- Description: `Bind` treats a JSON null as missing for every parameter, and it refuses a missing parameter whenever `IsRequired` is true (no default value). The parameter's nullable annotation plays no part. The new check only fires for a reference-type property that is Annotated against a NotAnnotated parameter, so these cases still slip through to a run-time refusal:
  - a `string?` property feeding a required `string? comment` parameter;
  - a `Guid?` property feeding a required `Guid?` parameter (same type, value-type `Nullable<T>`, never reaches the else branch).

  A nullable property bound to an optional parameter has a similar gap: when its value is null at run time it counts as omitted, which can open the positional hole that the M2 ordering check assumes cannot happen.
- Suggestion: Key the rule on `!parameter.HasExplicitDefaultValue`, covering both an annotated reference property and a `Nullable<T>` property, rather than on the parameter's annotation. For optional parameters, either treat a nullable property as possibly omitted in the ordering pass or document that it is not checked. Then update the skill sentence "a nullable property cannot feed a non-nullable parameter" to match.
- Status: open

### M16 — Severity: nit
- File: source/analyzers/timewarp-architecture-convention-analyzers/action-offer-agreement-analyzer.cs:332
- Description: `ArgumentProperties` skips any property that has a `JsonIgnoreAttribute`, whatever its `Condition`. `[JsonIgnore(Condition = Never | WhenWritingNull | WhenWritingDefault)]` still serializes the property onto the wire, yet the analyzer does not check its name or type. `Bind` would then refuse "has no parameter '…'" at run time.
- Suggestion: Skip only when `Condition` is absent or `JsonIgnoreCondition.Always`, the same condition-reading pattern as `ExplicitName`/`UserInput`. Otherwise treat the property as an argument.
- Status: open
