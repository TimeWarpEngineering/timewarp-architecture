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

- [ ] `[Offerable(UserInput = …)]` attribute (Attributes package)
- [ ] Generator emits `Offer` record + `OfferName` (+ offer metadata); incremental/cached; TWE diagnostics registered
- [ ] Analyzer links each offerable contract to its client action's `[CatalogAction(Name = …OfferName)]`; build errors registered
- [ ] Revoke + Rename migrated; hand-written records and their name constants deleted
- [ ] Link Microsoft 365 stays a hand-written `[ActionOffer]` (escape hatch documented)
- [ ] Generator tests (emitted shape, auth-field exclusion, bad UserInput, no Command), analyzer
      tests, and the existing offers tests green
- [ ] Skills updated (`tw-blazor`, `tw-web-api-contracts`); Purpose/Design regions reconciled
- [ ] Gates: `dev build` 0/0 (generator or analyzer change means a full rebuild), `dev test`,
      `dev template-smoke`, `ganda repo audit`, `dev check-version` if the generator or analyzer
      packages ship
- [ ] Do **not** start an AppHost
- [ ] Implementation review; host `open-pr`

## Notes

- Origin: Steve's review on 2026-10-06 of `credential-action-offer-contracts.cs` versus
  `rename-credential-contracts.cs`.
- Related: 279 (approach B adopted), 280 (TWA0029/TWA0030, typed offers), 271 (agentic UI:
  offerable contracts are natural agent tools).
- Memory discipline: run builds serially; `dotnet build-server shutdown` before finishing.

## Session

- Created: 2026-10-06 (cockpit, per Steve)

## Results

*(fill when done)*

### How to validate

*(required before done)*
