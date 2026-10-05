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

- [ ] Offerable `[CatalogAction]`s use `Name = <shared constant>`; constant location/shape recorded
- [ ] Typed offer records in contracts; server builds offers from them; client binder still fail-closed
- [ ] Analyzer: unknown name, and offer-record ↔ action-parameter mismatch, are build errors (id registered)
- [ ] Analyzer tests (positive + each failure); existing offers tests still green
- [ ] Redundant name-resolution test removed or kept as integration (decision recorded)
- [ ] `tw-blazor` "Server-offered actions": when-to-use rules + how-to-add
- [ ] Purpose/Design regions reconciled
- [ ] Gates: `dev build` 0/0 (an analyzer registry change means a full rebuild), `dev test`,
      `dev template-smoke`, `ganda repo audit`, `dev check-version` if the analyzers package ships
- [ ] Do **not** start an AppHost
- [ ] Implementation review; host `open-pr`

## Notes

- Origin: Steve's review on 2026-10-05 of task 279's `OfferedActionNames` string constants; 275
  and 279 had both flagged the argument-name gap.
- If the analyzer goes to TimeWarp.State, that is a timewarp-state task plus a release; this repo
  then pins the new beta. File that from here if that is the choice.
- Memory discipline: run builds serially; `dotnet build-server shutdown` before finishing.

## Session

- Created: 2026-10-05 (cockpit, per Steve)

## Results

*(fill when done)*

### How to validate

*(required before done)*
