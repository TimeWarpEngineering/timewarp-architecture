# Round 1 — general
**Date:** 2026-10-06
**Scope reviewed:** branch vs master

## Summary
The implementation meets Requirements 1–6. I found no correctness bug for the shapes the template uses: static partial contracts in a namespace, with a nested partial Command and a single `[ApiRoute]`. The `OfferTarget` model is equatable (no ISymbol, Location or SyntaxNode; arrays are compared by sequence). TWE012/TWE013 are declared in the SSOT, listed in AnalyzerReleases and in the AGENTS.md table. TWA0031 is registered in AGENTS.md, Unshipped.md, `source/Directory.Build.props` and the package description. The generator never spells the sourceName-rewritten Attributes namespace. I re-ran the generator suite (6/6) and the analyzer offer suite (33/33); both are green. The findings below are a fail-open edge, misleading diagnostic text, a naming inconsistency, and test-coverage gaps.

## Issues
### Issue 1 — Severity: suggestion
- File: source/foundation/foundation-contracts-generators/contracts-generator.offerable.cs:112
- Description: `[Offerable]` can be silently ignored. Three cases:
  - The class is not `partial`: the syntax predicate requires `IsPartialClass`.
  - The class is a `record`: the predicate only accepts `ClassDeclarationSyntax`.
  - The contract is in the global namespace: `ToTarget` returns null at line 165.
  In each case the generator emits no Offer and no OfferName, and reports no TWE. TWA0031 then skips the contract too, because `offerName is null` triggers `continue` (action-offer-agreement-analyzer.cs:262). The analyzer's Design region assumes "the generator already failed it (TWE012/TWE013)", but here nothing failed. This contradicts the "fail-closed" claim in both Design regions. Real contracts are partial because the route generator needs that, so the exposure is small.
- Suggestion: Do one of the following:
  - Widen the syntax predicate to any `TypeDeclarationSyntax` carrying `Offerable`, and report TWE013 (or a new TWE) when the target is not a partial class in a namespace.
  - Or have TWA0031 report an `[Offerable]` contract that has a Command but no `OfferName` const, instead of skipping it.
- Status: open

### Issue 2 — Severity: nit
- File: source/foundation/foundation-contracts-generators/contracts-generator.offerable.cs:193
- Description: TWE012 says "'X' … is not a property of {0}.Command" in two cases where that is false:
  - A `UserInput` entry is listed twice. The first match already removed the candidate, so the second lookup fails.
  - `UserInput` names the auth-filled `UserId`. It was removed before the lookup.
  Both cases still fail closed, but the message sends the author looking for a property that does exist.
- Suggestion: Distinguish the cases in the message (or add an argument): "listed twice", and "UserId is auth-filled and never offered".
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/features/identity/offered-action-contracts.cs:97
- Description: The server's offer vocabulary now uses two prefixes:
  - The generated names use `<Slice>.<Operation>` (`Identity.RevokeCredential`, `Identity.RenameCredential`).
  - The hand-written escape-hatch name stays `Credentials.LinkMicrosoft365`.
  The escape-hatch steps in tw-blazor (skills/tw-blazor/SKILL.md:178) give no naming scheme, so new hand-written offers will keep diverging from generated ones.
- Suggestion: Do one of the following:
  - Rename the constant to `Identity.LinkMicrosoft365`. Its tests and the Link action's `Name` use the constant, so the change is mechanical.
  - Or state in the skill and the Design region that hand-written names should follow `<Slice>.<Operation>`.
  - Or record why Link keeps the `Credentials.` prefix.
- Status: open

### Issue 4 — Severity: suggestion
- File: tests/analyzers/timewarp-architecture-sourcegenerator-tests/contracts-generator-offerable-tests.cs:1
- Description: The generator tests do not cover several code paths:
  - A contract nested in a container class (the `Containers` emission loop in `WrapOffer`).
  - Inherited Command properties (the `CommandProperties` base-type walk, including dedupe of hidden properties).
  - A `UserInput` entry that names a route parameter (allowed by the code, never exercised).
  - The TWE013 diagnostic location.
  Separately, the TWA0031 analyzer tests hand-write the generator's output shape (`OfferName` const plus nested `[ActionOffer] Offer`) instead of running the generator. If that shape changes, only the real SPA build would catch the drift, not a unit test.
- Suggestion: Add generator cases for nested containers, inherited properties and route-parameter user input. Optionally, add one test that runs `ContractsGenerator` on a contracts compilation and feeds its output to `ActionOfferAgreementAnalyzer`.
- Status: open
