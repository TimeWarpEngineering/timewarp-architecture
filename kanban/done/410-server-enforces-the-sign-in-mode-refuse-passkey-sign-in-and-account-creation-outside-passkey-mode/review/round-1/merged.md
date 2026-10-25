# Round 1 — merged findings
**Date:** 2026-10-25
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 4 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:426
- Description: The publish-mode env callback resolves `entra-enabled` at publish time and writes
  literal `SignInOptions__Methods__0=Passkey` (plus `__1=Microsoft365` only when the publishing
  machine set the parameter). The deployed `ENTRA_ENABLED` / Helm value / ACA param no longer controls
  Microsoft 365, which breaks the 070-003 rule "published Entra settings are parameters, never
  literals". Index 0 is also forced to `Passkey`, which overrides an app's checked-in
  `[ "Microsoft365" ]`. `skills/tw-deploy/SKILL.md:313` is stale, and no publish test covers the
  variable.
- Suggestion: Keep the published value parameter-backed (or drop the AppHost injection), stop forcing
  index 0, add a publish assertion, and update tw-deploy and the 070-003 comment.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/application/pages/SettingsPage.razor:120
- Description: `CreatePasskey` and `AddExistingPasskey` still show when `Passkey` is not listed, and
  both always 403 through the new gate.
- Suggestion: Gate both on `SignInState.PasskeyOffered is true`, and add a render case.
- Source: general
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: source/container-apps/web/features/identity/passkey-sign-in-method-gate-application.cs:22
- Description: `CompleteEntraBootstrapExisting` verifies a passkey assertion and signs in the passkey
  principal, but it is outside the gated set. It is protected only because the gated
  `StartPasskeyAuthentication` is the sole minter of `Authentication` challenges, and nothing records
  that.
- Suggestion: Add it to the set and add a test case, or document and pin the transitive argument.
- Source: general
- Disposition notes:

### M4 — Severity: suggestion — Status: open
- File: tools/dev-cli/endpoints/entra-setup-command.cs:544
- Description: `dev entra setup` overwrites `SignInOptions:Methods:0` with `Passkey`, which
  re-enables passkey for a Microsoft-365-only app. The task says setup adds Microsoft365.
- Suggestion: Append `Microsoft365` to the existing list only when it is absent, mirror that in
  disable, and add a test.
- Source: general
- Disposition notes:

### M5 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/admin/site-settings/pages/AuthenticationPage.razor:141
- Description: The admin drift banner still names "Authentication:Entra Enabled", a removed key.
  Stale Design text: `AuthenticationPage.razor:14`, `i-entra-sign-in-policy-application.cs:9`,
  `site-settings-seeder-tests.cs:12`.
- Suggestion: Reword the banner and the comments around `SignInOptions:Methods` / Microsoft365.
- Source: general
- Disposition notes:

### M6 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/identity/components/SignInChoices.razor:33
- Description: A failed offered-methods read leaves a blank login card with no message. It also turns
  off the Required-passkey shell gate for that render.
- Suggestion: Show a short "could not load sign-in options" line, or a retry, on a completed failure.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. A single reviewer.
