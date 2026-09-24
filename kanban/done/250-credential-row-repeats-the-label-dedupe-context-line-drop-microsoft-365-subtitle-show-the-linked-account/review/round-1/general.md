# Round 1 — general
**Date:** 2026-09-24
**Scope reviewed:** master...HEAD (da44809e)

## Summary
The change is sound: the migration Up/Down, the snapshot and the Designer agree (`character varying(256)`, nullable, unindexed), and `Type = 3` is `CredentialType.EntraAccount`. The aggregate carries `AccountHint` through Create, Snapshot and the private ctor, so the EF replacement path and the in-memory store stay consistent. The sign-in refresh is advisory and CAS-safe, and `CredentialUsageRecorder` re-reads the credential, so the refresh cannot stale it. The hint is never logged, and it is exposed only through the caller-scoped GetCredentials. One Design region the change touches in behavior was not reconciled, and one page-level fixture still seeds the pre-250 data shape. The rest are nits.

## Issues
### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/application/pages/SettingsPage.razor.cs:20
- Description: The Design region still says the Microsoft 365 "card title is Credential.Label, subtitle is \"Microsoft 365\"". This diff removed `Microsoft365CardTitle` and `DisplayEntraLabel`, so the card heading is now the fixed text "Microsoft 365", and it removed the `Subtitle` parameter. The region now describes the old behavior, which breaks the AGENTS.md reconcile-on-edit rule. The brief's reconcile list did not name this file, but the page's markup changed.
- Suggestion: Rewrite the Task 229 sentence. The card heading is the fixed text "Microsoft 365". The row title is the provider Label. The linked account (AccountHint) is the row's context line, and there is no subtitle (task 250).
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/container-apps/web/web-server-integration-tests/features/identity/protected-page-deep-link-tests.cs:197
- Description: `Settings_Linked_Entra_With_Passkey_Should_Hide_Link_And_Enable_Unlink` and `Settings_Entra_Only_Should_Disable_Unlink_With_Hint` still seed an Entra credential through `AddEntraAccountAsync(principalId, "Steven.Cramer@...")` with that UPN as its Label. After task 250 (and the migration), production never writes that shape. The tests still pass: the UPN renders as the row title, and ">Microsoft 365<" matches the card heading. So the only page-level prerender coverage checks legacy data, not the new Label = "Microsoft 365" plus AccountHint = UPN row.
- Suggestion: Have `AddEntraAccountAsync` create `Credential.Create(..., EntraIdTokenClaims.ProviderLabel, accountHint: upn)`. Then assert that the UPN renders inside `data-qa="CredentialContext"` and that the row title is "Microsoft 365".
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/platform/postgres/migrations/20260923180147_AddCredentialAccountHint.cs:36
- Description: Down cannot restore a name-claim Label (for example "Steven Cramer") that Up dropped. Those rows come back as "Microsoft 365", which was the old fallback label. This is acceptable because the data is display-only, but no comment says Down is lossy for them.
- Suggestion: Add one comment line saying the name-claim labels Up dropped are not restored and that the rows come back with the old "Microsoft 365" fallback.
- Status: open

### Issue 4 — Severity: nit
- File: tests/container-apps/web/web-contracts-tests/features/identity/identity-contracts-serialization-tests.cs:1
- Description: The diff adds a UTF-8 BOM to three existing test files: identity-contracts-serialization-tests.cs, entra-challenge-tests.cs, and credential-list-render-tests.cs. Most of the repo is BOM-less, and `.editorconfig` sets `charset = "utf-8"`, not `utf-8-bom`.
- Suggestion: Strip the BOMs from these three test files. The EF-generated migration files keep theirs, like the earlier migrations.
- Status: open

### Issue 5 — Severity: nit
- File: source/libraries/timewarp-identity/credentials/credential.cs:39
- Description: The edited Design line is about 170 characters long and breaks the region's roughly 100-column wrapping. The Purpose line at line 3 of credential-row-presenter-tests.cs has the same problem.
- Suggestion: Re-wrap both.
- Status: open
