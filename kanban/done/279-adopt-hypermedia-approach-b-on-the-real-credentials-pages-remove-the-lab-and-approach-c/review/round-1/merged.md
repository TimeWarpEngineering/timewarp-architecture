# Round 1 — merged findings
**Date:** 2026-10-05
**Sources:** general, tests, security

## Counts (at merge)

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 5 | 0 | 0 |
| nit | 6 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/identity/credentials-state/credentials-state.link-microsoft-365.cs:12 (behaviour: command-palette-state.open.cs:51)
- Description: The Design region says the static "Credentials: Link Microsoft 365" row appears only on other pages. In fact it stays in Ctrl-K on /Settings whenever the server does not offer Link, because the static row is dropped only when the page contributes a row with the same Target.
- Suggestion: Correct the Design regions, or suppress the static row on pages that own the target.
- Source: general
- Disposition notes: Design regions in credentials-state.link-microsoft-365.cs and command-palette-state.open.cs now state the real behaviour. Behaviour is unchanged.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/identity/components/AddPasskeyPrompt.razor:88
- Description: AddPasskeyPrompt's "Name this passkey" Save dispatches `RenameCredential` directly instead of going through the offer runner, which contradicts the "no second path" claim in `credential-offer-rows.cs:21`.
- Suggestion: Route it through the runner, or record the exemption and narrow the wording.
- Source: general
- Disposition notes: Not rerouted. The prompt renders on every TimeWarpPage, so the page-scoped runner would refuse it. The wording in credential-offer-rows.cs is narrowed, and the AddPasskeyPrompt Design region records the exemption, with the server handler as the boundary.

### M3 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/identity/credential-offer-rows.cs:106
- Description: The page-level Link Microsoft 365 offer gets a `FetchCredentials` follow-up even though Link navigates away, so the refresh is wasted.
- Suggestion: Use no follow-up for Link, or document it.
- Source: general
- Disposition notes: Orchestrator: the extra fetch after a forceLoad navigation is harmless (a cancellation is never painted). Giving page-level offers a different follow-up would complicate the row-equality check for no user-visible gain.

### M4 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/pages/SettingsPage.razor.cs:25
- Description: A Design-region line is 153 characters long.
- Suggestion: Re-wrap it.
- Source: general
- Disposition notes: Re-wrapped.

### M5 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/identity/credentials-spa-test-application.cs:115
- Description: The scripted BFF hand-copies the server offer rule, so the SPA tests can drift silently from `CredentialOffers.For`.
- Suggestion: Call `CredentialOffers.For` directly.
- Source: tests
- Disposition notes: The scripted BFF calls CredentialOffers.For, then applies SuppressOffer and ExtraOffers.

### M6 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-server-integration-tests/features/identity/credential-offers-tests.cs:178
- Description: The test, its Design region and task.md say "Link offered" cannot be reached end to end. It can: protected-page-deep-link-tests turns Entra on in-proc. No API-level test covers "Link offered".
- Suggestion: Add an API-level Link-offered test (and a linked case), and correct the gap text.
- Source: tests
- Disposition notes: Added the API tests Link_Microsoft365_Given_Entra_Offered_And_Not_Linked and No_Link_Microsoft365_Given_Entra_Offered_And_An_Active_Account_Linked. Gap text corrected in the tests, CredentialOffers Design region and task.md.

### M7 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/identity/credential-offers-tests.cs:72
- Description: `Hold_The_Servers_Offers_Not_A_Client_Rule` cannot tell "follow the server" from "count on the client", because the script applies the same count rule.
- Suggestion: Add a case where the server's offers contradict the count.
- Source: tests
- Disposition notes: Added Follow_The_Server_When_Its_Offers_Contradict_The_Count: Revoke suppressed with two passkeys, and Revoke injected with one.

### M8 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/command-palette/command-palette-context.cs:79
- Description: The M4 gate's unauthenticated "sign in first" branch has no test.
- Suggestion: Add an anonymous-principal refusal test.
- Source: tests
- Disposition notes: Added Refuse_An_Offered_Row_When_Nobody_Is_Signed_In (switchable auth provider, sign-in-first warning, no request).

### M9 — Severity: nit — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/identity/credential-offers-tests.cs:282
- Description: The "row from another page" refusal test checks only that no Revoke was sent. It does not check the warning or the absence of a follow-up fetch.
- Suggestion: Assert the request count is unchanged and the warning was shown.
- Source: tests
- Disposition notes: Asserts the unchanged request count and the "not offered here now" warning.

### M10 — Severity: nit — Status: fixed
- File: tests/container-apps/web/web-contracts-tests/features/identity/identity-contracts-serialization-tests.cs:521
- Description: The mock round-trip assumes every offer has `credentialId`, which would throw on a page-level offer, and it never compares `Label`.
- Suggestion: Compare `Label` and the full Arguments key set.
- Source: tests
- Disposition notes: Compares Label, Subject, and the full Arguments key set and values by key.

### M11 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/identity/credential-offer-rows.cs:73
- Description: The SPA accepts any Human/Both, permitted catalog action a `GetCredentials` response names, not only `OfferedActionNames.All`. Only a compromised server could exploit this.
- Suggestion: Optionally skip names outside `OfferedActionNames.All` in `ForPage`.
- Source: security
- Disposition notes: Orchestrator: matches the recorded M4 decision (catalog Visibility/Permissions instead of an offerable-names list; see task.md Decisions). The server is the trust boundary, and every action it names still passes the gate and its own server-side authorization.

## Duplicates / conflicts

- M7 depends on M5: once the script calls the real rule, M7 needs an explicit override (`ExtraOffers` / suppression) to diverge from the count.
- No overlaps between the security and general findings.
