# Generate server offers from an Offerable flag on API contracts

## Description

Follow-up to task 280 (PR #432). Server offers are now typed and checked at compile time
(TWA0029 / TWA0030), but the offer records are **hand-written copies of the contract shape**:

- `source/container-apps/web/features/identity/credential-action-offer-contracts.cs` declares
  `RevokeCredentialOffer(Guid CredentialId)` and `RenameCredentialOffer(Guid CredentialId)`
  (with `UserInput = ["nickname"]`), plus the `OfferedActionNames` name constants.
- `source/container-apps/web/features/identity/rename-credential/rename-credential-contracts.cs`
  already defines the same operation: `RenameCredential.Command` with route parameter
  `CredentialId`, field `Nickname`, and the auth-filled `UserId` (`IAuthApiRequest`).

An offer is the contract's Command **minus server-filled fields**, split into "bound by the server"
and "typed by the user". Steve decided on 2026-10-06 to generate it from a flag on the contract
rather than maintain parallel records, per the prefer-source-generators directive.

## Requirements

1. **`[Offerable]` attribute** in the Attributes package (sourceName-safe like the other platform
   attributes), on the contract's static partial class:

   ```csharp
   [ApiEndpoint, EndpointAuthorize(...)]
   [Offerable(UserInput = [nameof(Command.Nickname)])]
   public static partial class RenameCredential { ... }
   ```

   `UserInput` names Command properties the user supplies. Everything else that isn't auth-filled
   is bound by the server.
2. **Generator** (`source/foundation/foundation-contracts-generators/contracts-generator.cs`, which
   already emits the route members onto contract partials) emits onto the partial:
   - a nested `Offer` record with the server-bound properties: route parameters plus non-user
     Command properties, **excluding** auth-filled ones (`UserId` from `IAuthApiRequest`, and any
     other members the contract pattern fills from identity). Record the exclusion rule in the
     Design region;
   - an `OfferName` constant. Choose a stable naming scheme, for example
     `"<Slice>.<Operation>"`, and record it. It does not have to match today's
     `"Credentials.RenameCredential"`; the client `[CatalogAction(Name = RenameCredential.OfferName)]`
     adopts it;
   - the `[ActionOffer]` metadata (name + user-input list) that TWA0029 / TWA0030 already
     understand, or an equivalent the analyzers read directly.

   The generator stays incremental and value-equatable with a cached pipeline, so it doesn't
   regress the 239-001 lesson. It reports a diagnostic in the TWE range, registered in the
   descriptor SSOT and the AGENTS.md generator table, for a `UserInput` entry that names no Command
   property, and for `[Offerable]` on a contract without a Command.
3. **Contract → client action link (analyzer).** For each `[Offerable]` contract, find the client
   action whose handler requests that contract (`DefaultApiHandler<TAction, TContract.Command,
   TResponse>`, or the equivalent typed request) and require
   `[CatalogAction(Name = <Contract>.OfferName)]` on it. An offerable contract with no such client
   action, or a client action naming a different `OfferName`, is a build error. Extend
   TWA0029/TWA0030 or add the next free TWA id; register it everywhere the TWA ids are listed. Keep
   TWA0030's field-versus-constructor agreement working for the generated records.
4. **Migrate Revoke and Rename:**
   - flag `RevokeCredential` and `RenameCredential`;
   - the server builds offers from `RevokeCredential.Offer` / `RenameCredential.Offer`;
   - the client actions use the generated `OfferName`;
   - delete the hand-written `RevokeCredentialOffer` / `RenameCredentialOffer` and their
     `OfferedActionNames` entries.

   The wire shape may change name-wise if the naming scheme changes. Update the round-trip and
   offers tests accordingly.
5. **Keep the escape hatch:** hand-written `[ActionOffer]` records stay supported for offers with no
   contract Command. **Link Microsoft 365** is a browser redirect to a hand-written challenge
   endpoint, so `LinkMicrosoft365Offer` stays hand-written; the same applies to client-only
   actions. Document both paths.
6. **Skill:** update `tw-blazor` "Server-offered actions" → "How to add an offerable action": the
   primary path is `[Offerable]` on the contract; the escape hatch is a hand-written
   `[ActionOffer]` record for non-contract actions. Also update the contract skill
   (`tw-web-api-contracts`) with the `[Offerable]` attribute (public skills: rule plus reasoning).

## Checklist

- [x] `[Offerable(UserInput = …)]` attribute (Attributes package)
- [x] Generator emits `Offer` record + `OfferName` (+ offer metadata); incremental/cached; TWE diagnostics registered
- [x] Analyzer links each offerable contract to its client action's `[CatalogAction(Name = …OfferName)]`; build errors registered
- [x] Revoke + Rename migrated; hand-written records and their name constants deleted
- [x] Link Microsoft 365 stays a hand-written `[ActionOffer]` (escape hatch documented)
- [x] Generator tests (emitted shape, auth-field exclusion, bad UserInput, no Command), analyzer
      tests, and the existing offers tests green
- [x] Skills updated (`tw-blazor`, `tw-web-api-contracts`); Purpose/Design regions reconciled
- [x] Gates: `dev build` 0/0 (generator or analyzer change means a full rebuild), `dev test`,
      `dev template-smoke`, `ganda repo audit`, `dev check-version` if the generator or analyzer
      packages ship
- [x] Do **not** start an AppHost
- [x] Implementation review (disposition: clean)
- [ ] Host `open-pr`

## Notes

- Origin: Steve's review on 2026-10-06 of `credential-action-offer-contracts.cs` versus
  `rename-credential-contracts.cs`.
- Related: 279 (approach B adopted), 280 (TWA0029/TWA0030, typed offers), 271 (agentic UI:
  offerable contracts are natural agent tools).
- Memory discipline: run builds serially; `dotnet build-server shutdown` before finishing.

## Session

- Created: 2026-10-06 (cockpit, per Steve)
- 2026-10-06 implementer (claude, headless `ganda task work`): implemented all requirements; gates
  green (see Results). No AppHost started (`dev run` not used; `dev test` ran its existing
  closed-box suites as usual).
- 2026-10-06 review oracle (claude, headless `ganda task work`): tw-implementation-review, effort 3,
  roster general (Claude subagents); 2 rounds; disposition clean.

## Results

**Shipped**

- `[Offerable(UserInput = …)]` — `source/analyzers/timewarp-architecture-attributes/offerable-attribute.cs`
  (Attributes package, matched by simple name; `UserInput` takes Command property names via `nameof`).
- Generator — `source/foundation/foundation-contracts-generators/contracts-generator.offerable.cs`
  (partial of `ContractsGenerator`). Emits `{fqn}.Offer.g.cs` onto the contract:
  `public const string OfferName = "<Slice>.<Operation>"` and
  `[ActionOffer(OfferName, UserInput = ["camelCase"])] public sealed partial record Offer(…)`.
  - Bound properties: `[ApiRoute]` parameters of the Command (parsed from the template — route
    members are generated in the same pass) + Command public settable instance properties, minus
    `UserInput`, minus auth-filled `UserId` (Command implements `IAuthApiRequest` or has
    `[AuthApiRequest]`). Rule recorded in the Design region.
  - Naming scheme: `<namespace segment after the last "Features">.<contract name>` →
    `Identity.RenameCredential`, `Identity.RevokeCredential`.
  - `[ActionOffer]` resolved from the `[Offerable]` attribute's own namespace (sourceName-safe).
  - Record is `partial` so a contract adds interfaces (`partial record Offer : ICredentialActionOffer;`).
  - Incremental: syntax predicate + equatable `OfferTarget` (sequence-equal arrays, `DiagnosticInfo`
    instead of `Location`); trivia-only edits produce no New/Modified outputs (test).
  - **TWE012** (UserInput names no Command property) / **TWE013** (`[Offerable]` without Command),
    Error, fail-closed (nothing emitted). Declared in the TWE SSOT `diagnostic-descriptors.cs`, which
    the contracts-generators project now links (RS2008 off there; release tracking stays beside the
    SSOT in `timewarp-architecture-analyzers/AnalyzerReleases.Unshipped.md`). AGENTS.md generator table updated.
- Analyzer — **TWA0031** in `ActionOfferAgreementAnalyzer` (SPA/WASM-gated, CompilationEnd): for
  each `[Offerable]` contract, the client action = first type argument of a generic base (e.g.
  `DefaultApiHandler<TAction, Contract.Command, TResponse>`) that also has the Command as a type
  argument and carries `[CatalogAction]`; its Name must equal `OfferName`. No such action → TWA0031
  (Location.None, contract is metadata); different/implicit name → TWA0031 on the `[CatalogAction]`.
  TWA0029/TWA0030 check the generated records unchanged. Registered in AGENTS.md, Unshipped.md,
  `source/Directory.Build.props` and the convention-analyzers package description (TWA0020–0031).
- Migration: `RevokeCredential` / `RenameCredential` (`UserInput = [nameof(Command.Nickname)]`)
  flagged; server (`credential-offers-application.cs`, GetCredentials mock factory) builds
  `RenameCredential.Offer` / `RevokeCredential.Offer`; SPA actions use the generated `OfferName`;
  pages use `RevokeCredential.OfferName` / `RenameCredential.OfferName`; nickname input key derives
  from `nameof(RenameCredential.Command.Nickname)` (`CredentialOfferRows.NicknameParameter`).
  Deleted `RevokeCredentialOffer`, `RenameCredentialOffer` and their `OfferedActionNames` constants;
  `OfferedActionNames` keeps `LinkMicrosoft365` + `All` (now includes the generated names).
  Wire names changed: `Credentials.RevokeCredential` → `Identity.RevokeCredential`,
  `Credentials.RenameCredential` → `Identity.RenameCredential`.
- Escape hatch: `LinkMicrosoft365Offer` stays hand-written (`credential-action-offer-contracts.cs`
  Design region + the Link action's Design region say why).
- Skills: `tw-blazor` "How to add an offerable action" (primary `[Offerable]` path + hand-written
  escape hatch), `tw-web-api-contracts` new "Server offers (`[Offerable]`…)" section + checklist item.
- Tests: `contracts-generator-offerable-tests.cs` (6: emitted shape, auth exclusion incl.
  `[AuthApiRequest]` and no-auth `UserId`, TWE012, TWE013, compiles with partial interface,
  trivia-only caching); 5 TWA0031 analyzer tests; serialization/offers/catalog/palette tests updated
  for the generated records and new names (pins `"Identity.RenameCredential"`).

**Implementation review**

- Effort 3 (by-diff, 1452 lines), roster: general; 2 rounds.
- Final counts: bug 0, suggestion 2 fixed, nit 4 fixed; 0 open, 0 wontfix.
- Disposition: **clean**.
- Fixes: round 1 in a9f2a03cd (TWE014 fail-closed `[Offerable]` declaration shape; TWE012 reason argument;
  `Credentials.LinkMicrosoft365` → `Identity.LinkMicrosoft365` and `<Slice>.<Operation>` documented for
  hand-written offers; 9 new generator tests). Round 2 in the disposition commit: TWE014 also rejects generic
  contracts and containers, and the palette roster Design wording is updated. The palette label for Link is now
  "Identity: Link Microsoft 365".
- Post-fix gates: `./bin/dev build --clean` 0/0; sourcegenerator suite 119/119; analyzers suite 225/225;
  full `./bin/dev test` (round-1 fix) exit 0, 21 suites, 0 failed; `ganda repo audit` passes.
- Artifacts: `review/review-framework.md`, `review/round-1/{general,merged}.md`,
  `review/round-2/{general,merged}.md`, `review/disposition.md`.

**Gates (2026-10-06, this worktree)**

- `./bin/dev build --clean` → 0 Warning(s), 0 Error(s)
- `./bin/dev test` → exit 0, 21 suites, 0 failed
- `./bin/dev template-smoke` → Template smoke SUCCEEDED
- `ganda repo audit` → passes all audit checks
- `./bin/dev check-version` → source 2.0.0-beta.20 vs NuGet 2.0.0-beta.19: new, no bump needed

### How to validate

**Smoke**

```bash
./bin/dev build --clean
cd tests/analyzers/timewarp-architecture-sourcegenerator-tests && dotnet test -c Release -- --filter-class ContractsGeneratorOfferable
cd ../timewarp-architecture-analyzers-tests && dotnet test -c Release -- --filter-class Should_Check_Offer_Agreement
cd ../../container-apps/web/web-contracts-tests && dotnet test -c Release
# negative check: break the link and watch TWA0031 + TWA0029 fail the SPA build
sed -i 's/      Name = OfferName, /      Name = "Identity.Wrong", /' source/container-apps/web/projects/web-spa/features/identity/credentials-state/credentials-state.rename-credential.cs
dotnet build source/container-apps/web/projects/web-spa/   # expect TWA0031 + TWA0029
git checkout -- source/container-apps/web/projects/web-spa/features/identity/credentials-state/credentials-state.rename-credential.cs
```

**Expect**

- Build 0/0; `RenameCredential.Offer(Guid CredentialId)` / `RevokeCredential.Offer(Guid CredentialId)`
  exist only as generated code (no hand-written `RenameCredentialOffer` / `RevokeCredentialOffer`).
- Generator suite 119/119 (Offerable class 16), analyzer offer suite 33/33, web-contracts-tests 50/50.
- The negative check fails with `TWA0031 … sets Name = "Identity.Wrong"; set Name = RenameCredential.OfferName`
  on the `[CatalogAction]`, plus `TWA0029` for `RenameCredential.Offer`; restoring the file builds clean.
- In the running app (optional, not done here): Settings / Passkeys Rename and Revoke buttons still
  appear per server offer; GetCredentials offers carry names `Identity.RenameCredential` /
  `Identity.RevokeCredential` with arguments `{ "credentialId": … }`.
