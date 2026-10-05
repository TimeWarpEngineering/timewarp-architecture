# Compile-time agreement for server offers: shared catalog names, typed offer arguments, and when-to-use rules

## Description

Task 279 adopted hypermedia approach B on the Credentials pages: the server offers catalog
actions, and the client runs them through `IActionCatalog`. The server↔client agreement is
currently enforced by **strings plus one test**:

- **Names:** `OfferedActionNames` (`source/container-apps/web/features/identity/offered-action-contracts.cs`)
  hard-codes `"Credentials.RevokeCredential"`, `"Credentials.RenameCredential"` and
  `"Credentials.LinkMicrosoft365"`. On the client, the TimeWarp.State catalog generator **derives**
  the same names from the type names (`CredentialsState` + `RevokeCredentialActionSet`). The
  `[CatalogAction]` attributes in `web-spa/features/identity/credentials-state/*.cs` do **not**
  reference the constants, so renaming an action set silently changes the client name.
- **Arguments:** the argument key `OfferedActionNames.CredentialIdArgument = "credentialId"` is a
  string with no compile-time link to the target action's constructor parameter.
  `ContextualActionArguments` only checks it at run time.

`nameof` can't bridge this, because server-side code must not reference SPA types such as
`CredentialsState`. The single source of truth has to live in the **shared contracts** both sides
already reference. Steve approved this on 2026-10-05 ("prefer analyzers/source generators over
agreement-by-memory").

## Requirements

1. **One name source.** Every `[CatalogAction]` whose action the server can offer sets
   `Name = OfferedActionNames.<X>` from the shared contracts. Renaming the constant then updates
   both sides together. Keep `OfferedActionNames` as the server's whole vocabulary, but consider
   generalizing its location and naming beyond Credentials, for example a contracts-level
   `OfferableActions` per slice. Record the shape in the Design region.
2. **Typed offer arguments.** Replace the string argument keys with a small record per offerable
   action in contracts, for example `RevokeCredentialOffer(Guid CredentialId)`,
   `RenameCredentialOffer(Guid CredentialId)` and a parameterless offer for Link Microsoft 365.
   - The server builds offers from these records. `OfferedAction.ForCredential` / `ForPage` become
     typed, or are generated from the records.
   - The JSON wire shape may stay the same; record it either way.
   - The client binder keeps working: it can bind by the record's property names, and should stay
     fail-closed.
3. **Compile-time check (analyzer).** In the SPA compilation, which can see both the shared
   contracts and the `[CatalogAction]` actions, report a build error when:
   - an offerable name constant has no `[CatalogAction]` with that `Name`;
   - an offer record's properties don't match the target action's catalog parameters by name and
     type, or a required parameter has no property.

   Decide where the analyzer lives and record why. Options: this repo's convention analyzers (next
   free TWA id, if it is template-specific), or TimeWarp.State's analyzers (TWS range, if
   "offerable actions" becomes a library concept). Either way, register the id in the owning
   descriptor SSOT, release notes and docs. If it lands in this repo, update the AGENTS.md table
   and the Analyzers package row too.
4. **Remove the redundant test** only once the analyzer covers what it checked, or keep it as an
   integration check, and say which.
5. **When-to-use rules in the skill.** Add these to the `tw-blazor` "Server-offered actions" section
   (public skill: rules and reasoning only):
   - **An offer isn't a different kind of action.** It is the server saying "you may run this
     action now, with these arguments"; the action is still a normal TimeWarp.State action.
   - **Use an offer** when deciding whether the action is available needs a server-owned rule:
     server data, other users' actions or business invariants. Examples: revoking a non-last
     credential, linking Microsoft 365 when the site allows it and you aren't linked,
     approve/refund/cancel depending on state, editing only what you own. Test: if the client
     would have to copy a server rule to decide whether to show the button, use an offer.
   - **Use a plain action** for purely local or UI actions (counter, theme, toggles, navigation,
     modals), for static permission checks (`[CatalogAction(Permissions = …)]` plus
     `AuthorizeView` or the catalog filter is enough), and for reads (fetches are never offered).
   - **Agents (task 271):** anything an agent should run only when the server allows it should be
     an offer, so the agent gets server-checked tool calls.
   - **How to add an offerable action:** the shared name constant, the typed offer record, and
     `[CatalogAction(Name = …)]`; the analyzer enforces the agreement.

## Checklist

- [x] Offerable `[CatalogAction]`s use `Name = <shared constant>`; constant location/shape recorded
- [x] Typed offer records in contracts; server builds offers from them; client binder still fail-closed
- [x] Analyzer: unknown name, and offer-record ↔ action-parameter mismatch, are build errors (id registered)
- [x] Analyzer tests (positive + each failure); existing offers tests still green
- [x] Redundant name-resolution test removed or kept as integration (decision recorded)
- [x] `tw-blazor` "Server-offered actions": when-to-use rules + how-to-add
- [x] Purpose/Design regions reconciled
- [x] Gates: `dev build` 0/0 (an analyzer registry change means a full rebuild), `dev test`,
      `dev template-smoke`, `ganda repo audit`, `dev check-version` if the analyzers package ships
- [x] Do **not** start an AppHost
- [ ] Implementation review; host `open-pr`

## Notes

- Origin: Steve's review on 2026-10-05 of task 279's `OfferedActionNames` string constants; 275
  and 279 had both flagged the argument-name gap.
- If the analyzer goes to TimeWarp.State, that is a timewarp-state task plus a release; this repo
  then pins the new beta. File that from here if that is the choice.
- Memory discipline: run builds serially; `dotnet build-server shutdown` before finishing.

## Session

- Created: 2026-10-05 (cockpit, per Steve)
- 2026-10-05 implement (ganda task work, headless): attribute + records + analyzer + skill; all gates green.

## Results

**Shape (recorded in the Design regions of `offered-action-contracts.cs`, `action-offer-attribute.cs`
and `action-offer-agreement-analyzer.cs`):**

- **One name source.** `OfferedActionNames` (web contracts, Identity) stays the server's whole
  vocabulary. Revoke / Rename / Link Microsoft 365 `[CatalogAction]`s now set
  `Name = OfferedActionNames.<X>`. Location kept in Identity, which is still the only slice that
  makes offers. `ActionOfferAttribute` and the analyzer are slice-agnostic, so a second slice adds
  its own names class and records, and `OfferedAction` moves to a shared contracts tier at that
  point.
- **Typed offer records** (`credential-action-offer-contracts.cs`): `RevokeCredentialOffer(Guid CredentialId)`,
  `RenameCredentialOffer(Guid CredentialId)` with `UserInput = ["nickname"]`, and the parameterless
  `LinkMicrosoft365Offer`, each tagged `[ActionOffer(OfferedActionNames.X)]`. The new attribute
  lives in the TimeWarp.Architecture.Attributes package. The server builds offers only from records:
  `OfferedAction.Create<T>` / `ForCredential<T : ICredentialActionOffer>` / `ForPage<T>`.
  `OfferedActionNames.CredentialIdArgument` is deleted.
  **Wire shape is unchanged:** the record serializes with `ContractSerializationDefaults` into the
  Arguments map (`{"credentialId":"<guid D>"}`), so the client binder (`ContextualActionArguments`)
  is untouched and still fail-closed. A contracts round-trip test pins the shape.
- **Analyzer — TWA0029 / TWA0030** (`ActionOfferAgreementAnalyzer`, convention analyzers):
  TWA0029 fires when an `[ActionOffer]` name matches no **explicit** `[CatalogAction(Name = …)]`
  in the SPA compilation. The derived default name doesn't count, so this also enforces
  requirement 1. TWA0030 fires when a record property, by camelCase or `[JsonPropertyName]` name,
  is not a parameter of the action's first explicit constructor or has a different type, when a
  required parameter is neither bound nor listed in `UserInput`, or when a `UserInput` entry is not
  an unbound required parameter. It is gated on the Blazor WASM SDK, like TWA0022.
  **Why TWA rather than TWS:** "offer" belongs to this template's hypermedia contract
  (`OfferedAction`, the binder and the palette runner); TimeWarp.State only supplies the catalog.
  The ids are registered in `AnalyzerReleases.Unshipped.md`, the AGENTS.md table, the Analyzers
  package row, the csproj description and `source/Directory.Build.props`. Version
  2.0.0-beta.20 is unreleased, so no bump was needed.
- **Redundant test: kept, reframed as an integration check.**
  `CredentialOffers_Should_.Name_Only_Catalog_Entries_A_Person_May_Run` now uses reflection to
  enumerate the `[ActionOffer]` records. It asserts:
  - there is one record per `OfferedActionNames.All`;
  - the **generated runtime** catalog agrees with the analyzer's model (same names, parameters and
    UserInput);
  - each offered entry is Human/Both-visible. The analyzers do not check visibility.
- **Skill:** `tw-blazor` → "Server-offered actions" gains "When to offer" (an offer is not a
  different kind of action; use an offer for server-owned rules; use plain actions for local/UI
  actions, static permissions and reads; agents) and "How to add an offerable action".
- Boyscout: `ganda repo audit --fix --checks memsearch-scaffold` refreshed `.githooks/*.cs`.

**Gates:**

- `dev build`: 0 warnings, 0 errors (full).
- `dev test`: every suite passed (0 failed).
- Analyzer suite `Should_Check_Offer_Agreement`: 11/11.
- `dev template-smoke`: passed.
- `ganda repo audit`: passes all checks.
- `dev check-version`: 2.0.0-beta.20 is new.
- Negative check in the real SPA build: removing `Name =` from Revoke → `CSC : error TWA0029`;
  emptying Rename's `UserInput` → `TWA0030` at `credentials-state.rename-credential.cs(28,6)`.
- No AppHost was started.

### How to validate

**Smoke** (each line from the repo root):

```bash
cd tests/analyzers/timewarp-architecture-analyzers-tests && dotnet test -c Release -- --filter-class Should_Check_Offer_Agreement
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class CredentialOffers_Should_
# Negative: delete `Name = OfferedActionNames.RevokeCredential,` from
# web-spa/features/identity/credentials-state/credentials-state.revoke-credential.cs, then:
cd source/container-apps/web/projects/web-spa && dotnet build
```

**Expect:** 11/11 analyzer tests and 17/17 offers tests pass. The negative build fails with
`error TWA0029: Offer '…RevokeCredentialOffer' names catalog action 'Credentials.RevokeCredential',
but no [CatalogAction] in this compilation sets Name = …`. Restore the line and the build is 0/0.
