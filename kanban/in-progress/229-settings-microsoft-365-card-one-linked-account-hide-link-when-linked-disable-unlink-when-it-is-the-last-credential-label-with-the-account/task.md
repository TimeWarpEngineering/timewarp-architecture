# Settings Microsoft 365 card: one linked account, hide Link when linked, disable Unlink when it is the last credential, label with the account

## Description

Live review 2026-09-16 (Steve), `/Settings` after a Microsoft 365 bootstrap on the TimeWarp
tenant: the card shows "Microsoft 365 / 9/16/2026 7:53 PM / Unlink" **and** a "Link Microsoft 365"
CTA underneath, with no passkeys on the account. Three rules:

1. **One linked account per principal.** We support one tenant (task 227) and two accounts in the
   same tenant are two people, so a principal holds at most one `EntraAccount` credential. Hide
   "Link Microsoft 365" when an active Entra account is already linked; Unlink then Link is how
   you switch. Server side: `AddCredential` / the link ticket path must refuse a second active
   `EntraAccount` on the same principal with 409 (problem title e.g. `Microsoft 365 already
   linked`) so the rule is not UI-only.
2. **Unlink must not lock the user out.** When the Entra account is the only active credential
   (no active passkey, no agent key), render Unlink disabled with the hint "Add a passkey first".
   The server already refuses revoking the last active credential (104-005, 409); keep that as the
   backstop and add a test that the UI state and the server rule agree.
3. **Label the card with the account.** It says "Microsoft 365" because the credential label is
   empty and `DisplayEntraLabel` falls back. On bootstrap and on link, set the credential `Label`
   to `preferred_username` (UPN/email) when present, else `name`, else the fallback. The card then
   reads e.g. `Steven.Cramer@TimeWarp.Enterprises` with "Microsoft 365" as the type line. Do not
   store anything beyond the label (no tokens).

## Requirements

- `EntraTicketProcessor`: pass the label into `Credential.Create` for both the bootstrap and link
  paths (`EntraIdTokenClaims` already carries `DisplayName`; add `PreferredUsername` from the
  `preferred_username` claim; keep `MapInboundClaims=false` names). Refuse a second active
  `EntraAccount` on the same principal in link mode (409) before `AddCredentialAsync`.
- SPA `SettingsPage.razor` + `CredentialsState`: `CanLinkMicrosoft365 = Offered && ActiveEntraAccounts.Count == 0`;
  `CanUnlink(credential) = ActiveCredentialCount > 1`; disabled Unlink shows the hint. Card title
  = label, subtitle = "Microsoft 365", keep the created-at line.
- Tests: processor link-second-account 409; label set from `preferred_username` then `name`;
  SPA/prerender: Link hidden when linked, Unlink disabled when last credential, enabled when a
  passkey exists. Update `entra-challenge-tests.cs` / `entra-ticket-processor-tests.cs` fixtures
  that assumed unlabeled credentials.

## Checklist

- [x] Server: second active EntraAccount refused (409) on link; label from preferred_username/name
- [x] SPA: Link hidden when linked; Unlink disabled with hint when last active credential; card shows account label
- [x] Tests (processor + prerender/SPA); `dotnet test -- --filter-class Entra` green
- [x] `dev build` 0/0; `ganda repo audit` clean
- [x] Results and How to validate (screenshot-equivalent steps: bootstrap → Settings shows account
      label, no Link CTA, Unlink disabled; add passkey → Unlink enabled)
- [x] Implementation review: effort 1 general, round 1, disposition clean

## Session

- Created: cockpit (2026-09-16)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N
- Implementer: Grok task-work 229 (2026-09-16)
- Review oracle: grok session 01a0aa68-6f0f-77c1-9538-7770f499edf1 (2026-09-16)
- Review round 1 general: grok session 01a0aa6b-c811-7b50-a12d-4f28217b3463 (2026-09-16)

## Notes

- Files: `source/container-apps/web/features/identity/entra-ticket-processor-application.cs`
  (`Credential.Create` at ~L138 and ~L221; `SetDisplayName` at ~L214), `entra-id-token-claims-application.cs`,
  `web-spa/features/application/pages/SettingsPage.razor` (`DisplayEntraLabel`, `ActiveEntraAccounts`),
  `web-spa/features/identity/credentials-state/credentials-state.cs` (`ActivePasskeys`,
  `ActiveEntraAccounts`), `revoke-credential-handler-application.cs` (last-active 409 backstop).
- Prior: 104-005 (last-active revoke rule), 219-002 (link/bootstrap), 227 (single tenant).
- Multi-tenant (one account per tenant) is out of scope until a multi-tenant registration exists.
- Review kitchen: `review/review-framework.md`, `review/round-1/`, `review/disposition.md`.

## Results

One Microsoft 365 account per principal, last-credential Unlink guard, and UPN/name credential labels.

**What landed**

- `EntraIdTokenClaims` reads `preferred_username` separately from `name` (`MapInboundClaims=false` short names). `CredentialLabel` is preferred_username, else name, else `"Microsoft 365"`.
- `EntraTicketProcessor` passes `claims.CredentialLabel` into `Credential.Create` on both bootstrap and link. Link of a second active `EntraAccount` on the same principal returns 409 `Microsoft 365 already linked` before `AddCredentialAsync`. Same-handle link stays idempotent. Relink after Unlink still works (revoked rows are not counted).
- Settings: `CanLinkMicrosoft365 = Offered && ActiveEntraAccounts.Count == 0` (Link CTA omitted when linked). `CanUnlink = ActiveCredentialCount > 1` (disabled Unlink + hint "Add a passkey first"). Card title is the credential label, subtitle is "Microsoft 365", created-at kept. `FetchCredentials` runs during prerender so first HTML matches those rules.
- `CredentialsState.ActiveCredentialCount` plus static `CanLinkMicrosoft365` / `CanUnlink` predicates. Server `LastCredential` 409 remains the revoke backstop.

**Files**

- `source/container-apps/web/features/identity/entra-id-token-claims-application.cs`
- `source/container-apps/web/features/identity/entra-ticket-processor-application.cs`
- `source/container-apps/web/features/identity/identity-problems-application.cs`
- `source/container-apps/web/projects/web-spa/features/application/pages/SettingsPage.razor` (+ `.razor.cs`)
- `source/container-apps/web/projects/web-spa/features/identity/credentials-state/credentials-state.cs`
- Tests: `entra-id-token-claims-tests.cs`, `entra-ticket-processor-tests.cs`, `entra-challenge-tests.cs`, `protected-page-deep-link-tests.cs`, `settings-page-microsoft-365-tests.cs`

**Deviations**

- No public AddCredential command for Entra; the product write path is the ticket processor. Store `AddCredentialAsync` stays type-agnostic.
- Concurrent link of two different oids can still both pass list-then-insert (same TOCTOU class as last-credential revoke). Documented on the processor Design region.

**Tests**

- `cd tests/container-apps/web/web-server-integration-tests && ./bin/Release/net10.0/web-server-integration-tests --filter-class Entra` — 63 passed
- `cd tests/container-apps/web/web-jaribu-tests && dotnet test -c Release -- --filter-class Entra` — 12 passed
- `cd tests/container-apps/web/web-spa-integration-tests && ./bin/Release/net10.0/web-spa-integration-Tests --filter-class SettingsPage` — 3 passed (Link hidden / Unlink last-credential agrees with `IdentityProblems.LastCredential` 409)
- `cd tests/container-apps/web/web-server-integration-tests && ./bin/Release/net10.0/web-server-integration-tests --filter-method Settings_` — 9 passed (prerender: Link hidden when linked, Unlink disabled when last, enabled when a passkey exists)
- `./bin/dev build` — 0 Warning(s) / 0 Error(s)
- `ganda repo audit` — passes (2 pre-existing advisory warnings: memsearch-scaffold, vscode-window-icon)

### How to validate

**Smoke**

1. `./bin/dev run` (Entra enabled, trusted tenant configured). Bootstrap with Microsoft 365 (no passkey on the account). Open `/Settings`.
2. On that Entra-only account, confirm Unlink is disabled. Click **Create a passkey**, then look at Unlink again.
3. With Entra still linked, confirm there is no **Link Microsoft 365** button. Unlink, then confirm the Link CTA returns.

**Expect**

- After bootstrap: card title is the UPN/email (`preferred_username`, else `name`), subtitle is `Microsoft 365`, created-at is present, **Link Microsoft 365** is absent, **Unlink** is disabled with hint `Add a passkey first`.
- After adding a passkey: **Unlink** is enabled (no hint).
- Linking a second Microsoft 365 account (same principal, different oid) is 409 with title `Microsoft 365 already linked`. Revoking the last active credential remains 409 `Cannot revoke last credential`.

**Automated**

```bash
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class Entra
# expect: all passed (63)

cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class SettingsPage
# expect: all passed (3)

cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-method Settings_
# expect: all passed (9), including Settings_Linked_Entra_With_Passkey_Should_Hide_Link_And_Enable_Unlink
# and Settings_Entra_Only_Should_Disable_Unlink_With_Hint

./bin/dev build
# expect: 0 Warning(s) 0 Error(s)

ganda repo audit
# expect: Repository passes (advisory warnings only)
```

**Depends on:** `./bin/dev run` plus a configured Entra tenant for the live UI smoke. Automated gates do not need a live tenant (FakeEntraHandler / store-seeded credentials).

**Not in scope:** multi-tenant (one account per tenant); storing tokens; changing passkey Delete last-credential UX (server 409 remains the backstop there).

### Review disposition

- **Outcome:** clean
- **Rounds:** 1
- **Effort / roster:** 1, general only
- **Final counts:** bug 0/0/0, suggestion 0/0/0, nit 0/0/0 (open/fixed/wontfix)
- **Wontfix / escalations:** none
- **Paths:**
  - `review/review-framework.md`
  - `review/round-1/general.md`
  - `review/round-1/merged.md`
  - `review/disposition.md`
