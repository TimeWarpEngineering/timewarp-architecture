# Round 1 — general
**Date:** 2026-09-23
**Scope reviewed:** commit c3cc64bf vs merge-base ac506993

## Summary

The change renames the credential-list action Delete → Revoke and gates it on
`CredentialsState.CanUnlink(ActiveCredentialCount)`, where `ActiveCredentialCount` counts
`IsActive` rows across the *entire* `GetCredentials` snapshot (passkeys + agent keys + Entra) —
this is a verified exact mirror of `RevokeCredential.Handler`'s
`ListCredentialsAsync(includeRevoked: false).Count <= 1` guard (`revoke-credential-handler-
application.cs:128-133` vs `credentials-state.cs:54-56,63-64`). Wiring on `CredentialList.razor`,
`PasskeysPage.razor`, and `SettingsPage.razor` is complete and consistent (Microsoft 365 Unlink
row included), no leftover `Delete*`/`OnDelete` identifiers remain anywhere in `source/` or
`tests/`, the hint renders as a visible `<p>` (not a tooltip), and the post-revoke flip is
state-driven (Revoke → Fetch, no reload) and covered by a dedicated fact. New test infrastructure
(`credentials-spa-test-application.cs`, `credentials-state-revoke-guard-tests.cs`) follows the
established C-create / Jaribu pattern in this test tree exactly (compared against the sibling
`analytics-spa-test-application.cs` / `AnalyticsState_.Clone_Should`), and the added prerender
facts in `protected-page-deep-link-tests.cs` reuse the pre-proven `FindTagContaining(...).
ShouldContain("disabled")` idiom from task 229's Unlink coverage. Overall risk is low; this is a
faithful, well-tested implementation of the brief. Design/Purpose regions are reconciled on every
touched file.

## Issues

No issues found.
