# Round 1 — general
**Date:** 2026-10-01
**Scope reviewed:** branch task/260-every-spa-user-interaction-is-a-timewarpstate-acti vs master (727dc24f)

## Summary
I compared the new actions with the old page code: SignInState (8 actions), CredentialsState.LinkMicrosoft365 and DismissPasskeySoftPrompt(rememberForSession). Behavior matches in every case I checked:
- Return URLs are still guarded against open redirects. Handlers re-collapse the path with LoginPage.GetSafeReturnUrl; GetSafeReturnUrl(null) gives "/", so Login can never send the empty "stay on page" value by accident.
- forceLoad is kept on both challenge navigations; the post-sign-in navigations stay soft, as before.
- After creating a Microsoft 365 account, the handler notifies IdentitySessionAuthenticationStateProvider before it navigates.
- "Expired" problems still flip the choice to invalid, JSException still becomes an Error outcome, and all outcomes go through NotificationState (TWA0025).
- No handler dispatches another action.
- The catalog attributes are right (Human, credential.manage.self, the same permission as the sibling AddPasskey / AddExistingPasskey entries).
- The skill text is public-safe: it states the rule and its reasoning, with no history.

The findings are one test-coverage honesty gap and a few small nits. There are no behavioral bugs.

## Issues

### Issue 1 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/identity/sign-in-state-tests.cs:36
- Description: The checked checklist item says "Each converted action is dispatched headless and asserts its effect". The suite has 7 facts and covers only part of that. Not dispatched anywhere:
  - `CreateAccountWithPasskey`. This has the most new logic: "/" maps to /Settings, StayOnPage publishes "Passkey registered.", and it sets `LastRegisteredCredentialId` / `LastRegisteredProviderLabel`, which PasskeysPage sequences on.
  - `UseExistingAccountForMicrosoft365`
  - `FetchSession`
  - The failure / fail-closed path of `FetchMicrosoft365Choice` (it is only used as a setup step, on the valid path).
  - The RouteState conversions (RoleForm, TodoItemFormContainer, ChooseMicrosoft365Page "Sign in again").

  The Microsoft 365 create-success test uses `SignedInAuthenticationStateProvider`, which is not an `IdentitySessionAuthenticationStateProvider`, so the `NotifySessionChanged` branch the brief calls out is never exercised. The open-redirect collapse is tested only for SignInWithMicrosoft365, not for the passkey actions. The Results section lists the 7 facts accurately; the overstatement is in the checklist wording.
- Suggestion: Add headless facts for the missing handlers. At minimum add the first-HTTP-leg failure paths for CreateAccountWithPasskey and UseExistingAccountForMicrosoft365; that is the same technique as the existing SignInWithPasskey failure fact. Add FetchSession true/false/null and FetchMicrosoft365Choice failure → false. If feasible, script the fake IJSRuntime so a registration completes; that would pin "/" → /Settings, the unsafe-return collapse and LastRegistered*. Registering a real IdentitySessionAuthenticationStateProvider (or a subclass) would let a test assert the notification. Otherwise, narrow the checklist wording to what is covered.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/pages/SettingsPage.razor.cs:54
- Description: The `[CrossSliceReference(typeof(SiteSettingsState), …)]` reason still says "Microsoft 365 section is gated on GetEntraSignInOffered". This diff moved that gate to `SignInState.Microsoft365Offered`, which has its own CrossSliceReference, so the clause no longer describes the SiteSettingsState reference.
- Suggestion: Trim the reason to "Settings reads site settings for the passkey prompt."
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/identity/pages/passkeys-page/PasskeysPage.razor:15
- Description: `IsListBusy => ActionTrackingState.IsActive` now also covers the two ceremony actions, because they are `[TrackAction]` and the old page used a local `IsBusy` bool. So the CredentialList rename/revoke controls are disabled while a Register / Sign in ceremony runs. The effect is harmless and arguably better, but it is a behavior change that the Design region does not record.
- Suggestion: Accept it and add a line to the Design region, or scope IsListBusy to the credential actions if the old behavior mattered.
- Status: open

### Issue 4 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/identity/sign-in-state/sign-in-state.fetch-microsoft-365-choice.cs:22
- Description: `Microsoft365ChoiceValid` now lives in store-global state, and the fetch does not reset it to null before the call. If the Choose page is visited again in the same SPA session, it paints the previous verdict instead of "Checking sign-in…" until the fetch finishes. The old page field always started at null. Impact is low, because the Entra challenge is a forceLoad and reloading resets the store.
- Suggestion: Set `SignInState.Microsoft365ChoiceValid = null` at the start of the handler, or accept this and note it in the Design region.
- Status: open

### Issue 5 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/command-palette/command-palette-roster.cs:119
- Description: The generic display-name rule lowercases the brand, so the palette row reads "Credentials: Link microsoft 365". The test pins that string. It is consistent with the existing sentence-case rule, but it is a visible wording regression from the button label "Link Microsoft 365".
- Suggestion: Optional. Accept it, or add a display-name override hook later. It does not block this task.
- Status: open
