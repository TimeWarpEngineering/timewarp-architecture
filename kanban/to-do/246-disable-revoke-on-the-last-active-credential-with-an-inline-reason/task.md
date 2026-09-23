# Disable Revoke on the last active credential with an inline reason

## Description

On the Passkeys page a user with a single active credential can click **Delete**, and the
server correctly answers 409 `LastCredential` ("Revoking this credential would leave the account
with no way to authenticate."), which surfaces as an error bar after the fact. The action can
never succeed in that state, so the UI should not offer it. Decision (Steve, 2026-09-23): disable
the button when it is the last active credential, and say why next to it. The server guard stays
exactly as it is — the disabled button is a courtesy for the honest client, not the rule.

## Requirements

- **Same count as the server.** `RevokeCredential.Handler`
  (`source/container-apps/web/features/identity/revoke-credential/revoke-credential-handler-application.cs:129-133`)
  refuses when `ListCredentialsAsync(includeRevoked: false).Count <= 1` — i.e. it counts ALL
  active credentials of every `CredentialType` (passkeys AND agent keys). The SPA must disable on
  the same number: count `IsActive` entries across the whole `GetCredentials.Response.Credentials`
  list, not just the passkeys the page displays. Do not add a new endpoint or a server-computed
  flag unless the SPA cannot see all kinds from the existing response (it can — the contract
  Design region says the endpoint lists passkeys + agent keys).
- **Wire the existing parameters.** `CredentialList.razor` already exposes `DeleteDisabled`,
  `DeleteDisabledHint`, `DeleteDisabledHintDataQa`
  (`source/container-apps/web/projects/web-spa/features/identity/components/CredentialList.razor:21-26`).
  `PasskeysPage.razor:125-127` (and any agent-key page that renders the same list) passes
  `DeleteDisabled` when the active count is 1 and a hint such as "Add another passkey or agent key
  before revoking this one." The hint renders as visible text beside/under the button (not only a
  tooltip); a disabled control with no reason is the anti-pattern this task removes.
- **Vocabulary.** The server says *revoke*; the button says *Delete*. Rename the action to
  **Revoke** everywhere the SPA exposes it (`DeleteLabel` default, data-qa names may stay for
  test stability — decide and record), so the UI, the handler, and the problem title agree.
- **Keep the server path tested.** The existing 409 `LastCredential` coverage in
  `revoke-credential` tests remains; add SPA coverage: with one active credential the list renders
  the Revoke control disabled with the hint; with two active credentials (one passkey + one agent
  key) it is enabled; after a successful revoke that leaves one, the remaining row becomes disabled
  without a reload (state-driven).
- Reconcile Design regions on `PasskeysPage.razor.cs` / `CredentialList.razor` (why the client
  mirrors the server count; server remains the authority).
- Gates: `dev build` 0/0, `dev test`; manual check of the page in `dev run` recorded in Results
  (screenshot-free description is fine).

## Checklist

- [ ] Active-credential count computed from the full `GetCredentials` response (all kinds)
- [ ] `CredentialList` disabled + visible hint wired on Passkeys (and agent-key list if it shares the component)
- [ ] Delete → Revoke wording
- [ ] SPA tests: disabled at 1, enabled at 2 mixed kinds, flips after revoke
- [ ] Server 409 path untouched and still tested
- [ ] `dev build` 0/0 · `dev test` · manual page check

## Notes

- Origin: screenshot review 2026-09-23 (Passkeys page, single Proton Pass credential, red error bar after Delete).
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-23)
