# Round 2 — merged findings
**Date:** 2026-10-25
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 4 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:435
- Description: The publish env baked a literal sign-in method list in at publish time.
- Disposition notes: Fixed in `7bb4c3c0a`. Publish passes the `entra-enabled` parameter as
  `SignInOptions__IncludeMicrosoft365`, and the server appends Microsoft365. No index is forced.
  The Compose, Kubernetes, ACA, and ACA Neon publish tests pin this. tw-deploy is updated.
- Source: general (round 1)

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/pages/SettingsPage.razor:118
- Disposition notes: Fixed. Both buttons are gated on `PasskeyOffered is true`. Render test
  `SettingsPagePasskeyButtons_Should_` passes 2/2.
- Source: general (round 1)

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/identity/passkey-sign-in-method-gate-application.cs:31
- Disposition notes: Fixed. `CompleteEntraBootstrapExisting` is in the gated set, and the gate test
  asserts its 403.
- Source: general (round 1)

### M4 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/endpoints/entra-setup-command.cs:553
- Disposition notes: Fixed. Setup appends at the next free index of the merged list. Disable
  removes only user-secret Microsoft365 entries. `Microsoft365MethodList_Given_` passes 5/5.
- Source: general (round 1)

### M5 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/site-settings/pages/AuthenticationPage.razor:141
- Disposition notes: Fixed. The banner and three Design comments name `SignInOptions:Methods` /
  Microsoft365. The stale-prose `rg` is empty.
- Source: general (round 1)

### M6 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/identity/components/SignInChoices.razor:38
- Disposition notes: Fixed. A completed failed read shows a message and Try again. Not-yet-loaded
  paints nothing. `SignInChoices_Should` passes 6/6.
- Source: general (round 1)

## New findings

None.

## Duplicates / conflicts

- None. A single reviewer.
