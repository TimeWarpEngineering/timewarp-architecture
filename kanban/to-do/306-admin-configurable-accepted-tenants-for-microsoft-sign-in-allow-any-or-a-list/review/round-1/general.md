# Round 1 — general
**Date:** 2026-10-21
**Scope reviewed:** branch vs master (all 44 files); surrounding call sites of `SiteSettings`, `SiteSettingsEntraSignInPolicy`, `ISiteSettingsStore` (EF, in-memory, seed-on-read), `UpdateSiteSettings`, `AuthenticationPage`.

## Summary

Tenant acceptance now lives on the site-settings row: `Restricted` with an id list, or `Any`. It has a migration, contracts, an admin UI, and a one-time seed from a GUID `Authentication:Entra:TenantId`. `SiteSettingsEntraSignInPolicy` drops the config pin and calls `SiteSettings.AcceptsEntraTenant`. The design matches the goal restatement and the original task. Docs cover the authority vs trust split, multi-tenant app registration, personal accounts, and the first-passkey administrator path.

One blocking defect: the web-server integration suite is red. `entra-challenge-tests.cs` still seeds site settings without an accepted-tenant list, so every trusted-tenant ticket now gets `Untrusted tenant`. The implementer's Results list only the identity tests and runfiles, so this suite was not run.

Gates re-run by this reviewer:

- `dotnet run tools/dev-cli/dev.cs -- build`: 0 warnings, 0 errors
- policy runfile 10/10, seeder 8/8, update-site-settings 28/28, get-site-settings 5/5
- timewarp-identity-tests 292/292, web-infrastructure-tests 78/78, dev-cli-tests 257/257
- web-spa-integration-tests 302/302 (a first run that overlapped another run showed 70 failures at 0ms; the clean rerun passed)
- **web-server-integration-tests: 338 passed, 10 failed, 1 skipped**, the same on two runs

## Issues

### Issue 1 — Severity: bug
- File: tests/container-apps/web/web-server-integration-tests/features/identity/entra-challenge-tests.cs:161
- Description: `InitializeOnce` seeds `SiteSettings.Create(entraSignInEnabled: true, entraAllowBootstrap: true, passkeyPromptMode: Soft)` with no `entraAcceptedTenantIds`. The new default is Restricted with an empty list, so the policy refuses `TrustedTenantId`. Ten tests fail: `Link_Foreign_Active_Handle_Should_Merge`, `Link_Already_On_This_Account_Should_409`, `Bootstrap_Unknown_Handle_Should_Redirect_To_Choose_Without_Creating_Principal`, `Bootstrap_Choose_Create_Should_Mint_Principal_Credential_And_Session`, `Bootstrap_Sync_Hit_Should_Reuse_Existing_Principal`, `Link_With_Session_Should_Attach_And_Redirect`, `Link_Should_Store_Preferred_Username_As_Account_Hint`, `Link_Second_Active_Entra_Should_409`, `Bootstrap_When_Not_Allowed_Should_403` (gets "Untrusted tenant" instead of "Bootstrap not allowed"), and `Bootstrap_With_Public_Origin_Should_Redirect_To_Local_Return_Url`. The same file's `options.TenantId = TrustedTenantId` (line 88) no longer grants trust, because the policy no longer reads options and this fixture bypasses the seeder.
- Suggestion: Seed `entraAcceptedTenantIds: [TrustedTenantId]` there, as `entra-ticket-processor-tests.cs` and `entra-oidc-handler-round-trip-tests.cs` already do. Check the restore blocks at lines ~399–440 (they re-Get and ReplacePolicy, which keeps acceptance). Optionally add one HTTP-level case there for Any mode accepting `UntrustedTenantId`, so the end-to-end challenge/callback path covers both modes. Run the whole web-server-integration-tests assembly before reporting Done.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/features/settings/site-settings-details-contracts.cs:41
- Description: `SiteSettingsDetailsValidator` restates the list rules by hand (non-empty GUID, at most `MaxAcceptedCount`) instead of calling `EntraTenantIds.TryNormalize`, the function the aggregate runs. The two already differ: the validator counts the raw list and the aggregate counts distinct ids. The repo hard rule says two things that must agree should derive from one source. The `AssistantName` rule in the same validator already does this by calling `AssistantNameRules.IsValid`.
- Suggestion: Replace the count and per-element rules with one `Must(ids => EntraTenantIds.TryNormalize(ids, out _, out _))` (or a `Custom` rule that surfaces the returned error). Update the Design region to say the validator and the aggregate share `EntraTenantIds.TryNormalize`.
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/admin/site-settings/pages/AuthenticationPage.razor:187
- Description: The help text says personal Microsoft accounts are included "only when ... Authentication:Entra:TenantId is common". `auth.md`, `overview.md`, and the `dev entra setup` note all say `common` or `consumers`.
- Suggestion: Say "common (or consumers)" so the page matches the docs.
- Status: open

## Checked and found sound (no finding)

- EF mapping: jsonb plus a value converter and comparer; `EfSiteSettingsStore.UpdateAsync` attaches a full `Snapshot`, so the new fields persist. The round-trip test covers Any plus the list.
- Migration defaults: existing rows get Restricted, an empty list, and `Initialized=false`. The boot seeder copies a GUID authority once. `organizations` and `common` seed an empty list (fail closed, same as the old behavior).
- Reseed and `ReplacePolicy` keep tenant acceptance. Development reseed does not touch mode or list.
- Corrupt jsonb reads as an empty list (fail closed). `Guid.Empty` is refused in every mode.
- The admin page parses the textarea only in Restricted mode. Any keeps the stored list. A successful save re-syncs the draft from state.
- Agent page facts and schema advertise the new enum members (catalog-agent test).
- Goal restatement matches the original task (no misread).
