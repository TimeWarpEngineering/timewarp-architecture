# Round 1 — general
**Date:** 2026-10-25
**Scope reviewed:** `ab3e326e8..HEAD` (product commit `becbd6929`), plus call sites of the changed types

## Summary

The change replaces `SignInMode` with a server-owned `SignInOptions.Methods` list of `SignInMethod`.
It removes `Authentication:Entra:Enabled` / `UseEntra`, gates the six passkey ceremonies through one
mediator behavior (order 400) that returns 403 `sign-in-method-disabled`, and has the SPA render from
`GET api/identity/entra/offered`. The server side matches the task and the restatement. The gate,
validator and render tests pass when re-run. The main defect is in the AppHost publish path: the
sign-in list is baked into the published artifact at publish time, so the deploy-time `entra-enabled`
parameter no longer does anything, and the AppHost overwrites the app's checked-in list. The other
findings are surfaces the change left behind: Settings passkey buttons, the Entra choose-existing
passkey path, `dev entra setup`, and stale copy.

Re-verified in this worktree: `./bin/dev build` 0 warnings / 0 errors. `PasskeySignInMethodGate` 1/1,
`EntraSchemeRegistration` 4/4, `SignInChoices_Should` 5/5, `SignInOptions_` 4/4, and
`Microsoft365AccountChoice` 2/2 pass. The acceptance `rg` returns only `EntraSignInMode` hits, which the
task keeps. A throwaway config probe confirmed that `"Methods": []` gives an existing section with no
children, so `ApplyConfiguredMethods` yields an empty list and the validator rejects it, as intended.
`template-smoke` and `ganda repo audit` were not re-run.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:426
- Description: In publish mode the `WithEnvironment(async context => …)` callback awaits
  `entraEnabledParameter.Resource.GetValueAsync` and writes plain strings into
  `context.EnvironmentVariables`. The published Compose / Helm / Bicep output therefore carries the
  literal `SignInOptions__Methods__0=Passkey`. It also carries `SignInOptions__Methods__1=Microsoft365`
  only when `Parameters:entra-enabled` was true on the publishing machine. It no longer carries a
  parameter reference. Before this change the variable was `Authentication__Entra__Enabled: ${ENTRA_ENABLED}`,
  a deploy-time knob. The block's own Design comment (line 411, task 070-003) says "published Entra
  settings are parameters, never literals", and this breaks that rule. Two effects follow:
  (a) flipping `ENTRA_ENABLED` in the deployed `.env`, a Helm value, or the ACA parameter no longer
  turns on Microsoft 365, and the `entra-enabled` parameter is now dead at deploy time;
  (b) `SignInOptions__Methods__0=Passkey` always overrides index 0 of the generated app's checked-in
  `SignInOptions:Methods`, so an app that lists `[ "Microsoft365" ]` deploys as passkey-only. That
  contradicts "the server owns the list". `skills/tw-deploy/SKILL.md:313` still describes
  `entra-enabled` as the Entra switch. No publish test asserts the web-server sign-in env.
- Suggestion: Keep the published value parameter-backed and stop forcing index 0. One option is to
  drop the AppHost injection and let the web-server config own the list. Another is to publish the
  list through parameter references (for example one parameter per index, or a single parameter the
  web-server splits) so the deployed `.env`, values or Bicep param still controls it. Add a
  Compose / K8s / ACA publish assertion on the web-server sign-in variables. Update the tw-deploy
  skill line and the 070-003 comment to match.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/pages/SettingsPage.razor:120
- Description: The Passkeys section always shows `CreatePasskey` (→ `CredentialsState.AddPasskey`,
  which starts with `StartPasskeyRegistration`) and `AddExistingPasskey` (→ `StartAddExistingPasskey`).
  When `Passkey` is not listed, both actions now always hit the new 403 gate. That leaves dead-end
  buttons on a page the task otherwise drives from the offered list. Only the shell prompt
  (`TimeWarpPage`) was gated on `SignInState.PasskeyOffered`.
- Suggestion: Hide or disable both buttons unless `SignInState.PasskeyOffered is true` (the page
  already fetches the list), and add a render case.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/web/features/identity/passkey-sign-in-method-gate-application.cs:22
- Description: `CompleteEntraBootstrapExisting` verifies a passkey assertion and issues an
  identity-session on the passkey's principal (its Purpose: "Verifies a passkey assertion …"). That is
  a passkey sign-in, and the task says an existing passkey must not sign in while `Passkey` is not
  listed. It is not in `PasskeyCommands`. Today it is safe only by implication, because it consumes an
  `Authentication` challenge and only the gated `StartPasskeyAuthentication` mints one. The gate's
  Design region does not record that reasoning, so a future challenge minter would silently reopen
  passkey sign-in.
- Suggestion: Add `CompleteEntraBootstrapExisting.Command` to the gated set and a refusal case in
  `PasskeySignInMethodGate`. Or state the transitive argument in the gate's Design region, with a test
  that pins it.
- Status: open

### Issue 4 — Severity: suggestion
- File: tools/dev-cli/endpoints/entra-setup-command.cs:544
- Description: `dev entra setup` always writes `SignInOptions:Methods:0=Passkey` and
  `SignInOptions:Methods:1=Microsoft365` into user secrets. If an operator configured Microsoft 365
  only (`Methods:0=Microsoft365`), setup silently re-enables passkey. The task says setup *adds*
  `Microsoft365`. The fixed-index keys also only merge correctly while the checked-in appsettings list
  is exactly `[ "Passkey" ]`.
- Suggestion: Read the current list (the command already lists user secrets) and append
  `Microsoft365` only when it is absent, without rewriting other entries. Mirror that in
  `dev entra disable`, and add a dev-cli test for the M365-only case.
- Status: open

### Issue 5 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/admin/site-settings/pages/AuthenticationPage.razor:141
- Description: The user-facing drift banner still says "Saved policy differs from Authentication:Entra
  Enabled or AllowBootstrap". That key is removed; the drift is now "SignInOptions lists Microsoft365"
  against `EntraSignInEnabled`. The acceptance regex (`Entra:Enabled`) misses the space-separated form.
  The same stale wording sits in Design text at `AuthenticationPage.razor:14`,
  `features/identity/i-entra-sign-in-policy-application.cs:9` ("Configuration still owns scheme
  registration (Enabled)"), and `features/identity/site-settings-seeder-tests.cs:12`.
- Suggestion: Reword the banner to name `SignInOptions:Methods` / Microsoft365, update the three
  Design comments, and check any render test that pins the banner text.
- Status: open

### Issue 6 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/identity/components/SignInChoices.razor:33
- Description: When the offered-methods read fails, `OfferedMethods` stays null. The login card then
  paints nothing, with no message. Before this change a passkey app still showed its passkey button.
  The same null also turns off the Required-passkey shell gate (`TimeWarpPage.razor` `BlockForPasskey`)
  for that render. Both behaviours are documented as intentional, but a blank sign-in card gives the
  user no hint.
- Suggestion: Render a short "Sign-in options could not be loaded" line, or a retry, when the read
  completed with a failure (as distinct from not loaded yet).
- Status: open

## Notes (not findings)

- Spec conflict, resolved by the implementer and disclosed in Results: the task's Shape says the
  template default is `[ "Passkey", "Microsoft365" ]`. The Decisions say listing Microsoft365 without
  Entra settings fails start. The checked-in appsettings has an empty `ClientId`, so that default
  would not boot. The implementer kept the initializer at both and checked in `[ "Passkey" ]`, which
  matches the old effective default (Entra `Enabled: false`). I accept this. It is recorded under
  task.md `## Learnings`.
