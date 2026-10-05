# Round 1 — general
**Date:** 2026-10-05
**Scope reviewed:** `git diff master...HEAD` excluding kanban/. Server: `CredentialOffers`, `CredentialRules`, `EntraSignInOffer`, `GetCredentials` contract and handler, `OfferedAction` / `OfferedActionNames`, `GetEntraSignInOffered`. SPA: `CredentialsState` (Offers, FindOffer, IsOffered, Fetch/Revoke/Rename/Link), `CredentialOffer`, `CredentialOfferRows`, `CredentialsContextSource`, `CommandPaletteContext.RefusalAsync`, `CommandPaletteRunner.RunContextualAsync`, `CommandPaletteRoster.IsPermittedAsync`, `command-palette-state.open` dedup, `SettingsPage`, `PasskeysPage`, `CredentialList`, `program.cs`, NavMenu and `_Imports`, and the tw-blazor skill section. I also checked the call sites that were not changed: `AddPasskeyPrompt`, `RevokeCredential.Handler`, `EntraTicketProcessor` and the roster's required-parameter filter. Lab leftovers were checked with `git grep`.

## Summary
The change implements Requirements 1–5 and 7 as recorded. The server offers come from the same `CredentialRules` predicates the enforcing handlers use: both count `ListCredentialsAsync(includeRevoked: false)` across every type. The SPA has no copy of the rules left. Buttons and palette rows share one fail-closed runner path with the M4 Visibility/Permissions gate, and the follow-up refresh is sequenced by the runner, not a handler. No lab or approach-C code is left in source, tests or skills. I found no correctness bugs. There are two suggestions: a doc comment that does not match the palette's behaviour on /Settings, and one Rename path that does not go through the offer. There are also two nits.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/identity/credentials-state/credentials-state.link-microsoft-365.cs:12 (behaviour: source/container-apps/web/projects/web-spa/features/application/command-palette-state/command-palette-state.open.cs:51)
- Description: The Design region says the static roster lists "Credentials: Link Microsoft 365" "on every other page", meaning every page except Settings. But `Open` drops the static Command row only when the page contributes a contextual row with the same Target. On /Settings, when the server does not offer Link (already linked, or Entra off), there is no contextual Link row, so the static "Credentials: Link Microsoft 365" command still appears in Ctrl-K on /Settings. That is the case the server just said does not apply, and the Settings page hides the button. Running it falls back to the challenge flow's 404/403/already-linked handling, which is safe but inconsistent with the page. The Design text in `command-palette-state.open.cs` ("would otherwise appear twice") covers only the offered case.
- Suggestion: Either suppress the static row on pages whose context source "owns" that target even when it contributes no row (for example, a source-declared set of owned targets), or correct the Design regions to say the static row stays on /Settings when Link is not offered.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/identity/components/AddPasskeyPrompt.razor:88
- Description: `AddPasskeyPrompt` is rendered by `TimeWarpPage`, so it appears on Settings and Passkeys too. Its "Name this passkey" Save still dispatches `RenameCredential` directly and then `FetchCredentials`. It does not go through the Rename offer (`CredentialOfferRows.RunAsync` → `RunContextualAsync` with the M4 gate). Today this has no effect, because Rename is offered for every active credential. But it contradicts the claim in `credential-offer-rows.cs:21` that "a page has no second path that could run an action the server did not offer", and Requirement 2 says Rename runs through the catalog. If the server ever restricts Rename, the prompt would ignore that.
- Suggestion: Route the prompt's Save through `CredentialOfferRows.RunAsync(OfferedActionNames.RenameCredential, …, NicknameInput(...))` like the list editor. Otherwise, record in the prompt's Design region why it is exempt, and narrow the "no second path" wording.
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/identity/credential-offer-rows.cs:106
- Description: Every offer row, including the page-level Link Microsoft 365, gets `FetchCredentials` as its `FollowUpTarget`. `LinkMicrosoft365` does a forceLoad navigation to the BFF challenge, so the runner then sends a GetCredentials request while the page unloads. The request is wasted, and possibly cancelled. Cancellation (499) is not painted, so the user sees nothing.
- Suggestion: Leave `FollowUpTarget` null for page-level offers that navigate away (or for `LinkMicrosoft365` specifically), or note in the Design region why the extra fetch is acceptable.
- Status: open

### Issue 4 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/pages/SettingsPage.razor.cs:25
- Description: The edit merged the new sentence into an existing Design-region line, which is now 153 characters long. The surrounding region wraps near 100 characters.
- Suggestion: Re-wrap the line.
- Status: open
