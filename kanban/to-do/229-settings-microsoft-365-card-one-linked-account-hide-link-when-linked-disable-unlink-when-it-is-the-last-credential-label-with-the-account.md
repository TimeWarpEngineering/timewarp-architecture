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

- [ ] Server: second active EntraAccount refused (409) on link; label from preferred_username/name
- [ ] SPA: Link hidden when linked; Unlink disabled with hint when last active credential; card shows account label
- [ ] Tests (processor + prerender/SPA); `dotnet test -- --filter-class Entra` green
- [ ] `dev build` 0/0; `ganda repo audit` clean
- [ ] Results and How to validate (screenshot-equivalent steps: bootstrap → Settings shows account
      label, no Link CTA, Unlink disabled; add passkey → Unlink enabled)

## Session

- Created: cockpit (2026-09-16)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N

## Notes

- Files: `source/container-apps/web/features/identity/entra-ticket-processor-application.cs`
  (`Credential.Create` at ~L138 and ~L221; `SetDisplayName` at ~L214), `entra-id-token-claims-application.cs`,
  `web-spa/features/application/pages/SettingsPage.razor` (`DisplayEntraLabel`, `ActiveEntraAccounts`),
  `web-spa/features/identity/credentials-state/credentials-state.cs` (`ActivePasskeys`,
  `ActiveEntraAccounts`), `revoke-credential-handler-application.cs` (last-active 409 backstop).
- Prior: 104-005 (last-active revoke rule), 219-002 (link/bootstrap), 227 (single tenant).
- Multi-tenant (one account per tenant) is out of scope until a multi-tenant registration exists.

## Results

_Pending._

### How to validate

_Pending._
