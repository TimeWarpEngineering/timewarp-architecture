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

- [x] Active-credential count computed from the full `GetCredentials` response (all kinds)
- [x] `CredentialList` disabled + visible hint wired on Passkeys (and agent-key list if it shares the component)
- [x] Delete → Revoke wording
- [x] SPA tests: disabled at 1, enabled at 2 mixed kinds, flips after revoke
- [x] Server 409 path untouched and still tested
- [x] `dev build` 0/0 · `dev test` · manual page check (manual browser check NOT run headless — see Results)

## Notes

- Origin: screenshot review 2026-09-23 (Passkeys page, single Proton Pass credential, red error bar after Delete).
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-23)
- Implemented: headless `ganda task work 246` implementer (Claude Fable 5.1), 2026-09-23.
  Note: `ganda kanban move 246 in-progress` in the claim worktree dropped the git-ignored
  `task-work.journal.json` that sat beside the to-do kitchen (the CLI moved only the tracked
  file). Not hand-deleted; no other copy found. Host resume state for this walk may be gone.

## Results

### What changed

- **Count = server count.** Both pages bind `RevokeDisabled` to
  `!CredentialsState.CanUnlink(CredentialsState.ActiveCredentialCount)` — the count spans every
  `CredentialType` in the `GetCredentials` snapshot (passkeys + agent keys + Entra), exactly the
  set `RevokeCredential.Handler` counts with `ListCredentialsAsync(includeRevoked: false)`.
  No new endpoint, no server-computed flag. The server 409 guard is untouched.
- **Wired the existing parameters.** `PasskeysPage.razor` and the passkey list on
  `SettingsPage.razor` pass `RevokeDisabled` + hint
  "Add another passkey or agent key before revoking this one." (`data-qa="RevokePasskeyHint"`),
  rendered as visible text under the button by `CredentialList`. The Microsoft 365 Unlink row
  keeps its own hint ("Add a passkey first") on the same predicate.
- **Vocabulary: Delete → Revoke.** `CredentialList` parameters renamed `RevokeLabel` /
  `RevokeDataQa` / `RevokeDisabled` / `RevokeDisabledHint` / `RevokeDisabledHintDataQa` /
  `OnRevoke`; default label "Revoke"; default data-qa **renamed** `DeletePasskey` → `RevokePasskey`
  (decision: nothing in `tests/` or e2e referenced the old name, so agreement won over
  stability). `CredentialsState.RevokeCredential` status string is now "Credential revoked."
  (it also serves Entra unlink). Old `CanUnlink` page property on Settings folded into `CanRevoke`.
- **Design regions reconciled:** `CredentialList.razor`, `PasskeysPage.razor.cs`,
  `SettingsPage.razor.cs`, `credentials-state.cs`, `credentials-state.revoke-credential.cs`.

### Tests

- New `tests/container-apps/web/web-spa-integration-tests/features/identity/`
  - `credentials-spa-test-application.cs` — in-proc C-create SPA host with a scripted
    `IWebServerApiService` (GetCredentials answers with a chosen mix; RevokeCredential flips the
    row to revoked for the next fetch) and a signed-in `AuthenticationStateProvider`.
  - `credentials-state-revoke-guard-tests.cs` (4 facts): disabled at 1 active passkey; revoked
    rows in an includeRevoked snapshot do not count; enabled at 1 passkey + 1 agent key; after
    Revoke → Fetch leaves one, the guard flips to disabled (state-driven, no reload) and
    StatusMessage is "Credential revoked.".
- `protected-page-deep-link-tests.cs` (+4 prerender HTML facts): Settings single passkey → the
  `RevokePasskey` button carries `disabled` and the hint text is in the HTML; Settings passkey +
  agent key (store-level `AddAgentKeyAsync`) → enabled, no hint; same pair on the Developer-gated
  `/Passkeys` page. Old `DeletePasskey` data-qa asserted absent.
- Server 409: `credential-revoke-tests.cs` `Conflict_Given_Last_Active_Credential` untouched and
  green (8/8 in that class).

### Gates

- `dev build` — 0 warnings / 0 errors.
- `dev test` — 14 suites, 989 tests, 0 failed (full run; SPA and web-server suites included).
- `ganda repo audit` — passes (after `dev self-install` into the claim worktree's `bin/`).
- **Manual `dev run` page check: NOT performed.** This headless session cannot complete a real
  WebAuthn ceremony (no virtual-authenticator harness in the repo). Rendered-markup proof comes
  from the prerender HTML facts above, which hit the real web-server host. Human should confirm
  once in the browser (steps below).

### Review disposition

- Review oracle: headless `ganda task work 246` review node (Claude Fable 5.1), 2026-09-23.
- Rounds: 1 · Effort: 1 · Roster: general (Claude subagent, read-only).
- Final counts: bug 0 / suggestion 0 / nit 0 — 0 open, 0 fixed, 0 wontfix.
- **Disposition: clean** (no findings raised; no exceptions, no escalations).
- Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`,
  `review/disposition.md` (all under this task folder).

### How to validate

**Smoke**

```bash
dev build
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class CredentialsStateRevokeGuard
cd ../web-server-integration-tests && dotnet test -c Release -- --filter-class ProtectedPageDeepLink
dotnet test -c Release -- --filter-class CredentialRevoke
```

Then `dev run`, sign in with a single passkey, open **Settings** (and **Pages → Passkeys** as a
Developer).

**Expect**

- Build 0/0; SPA class 4/4; deep-link class 16/16 (includes the 4 new `*Revoke*` facts);
  credential-revoke class 8/8 with `Conflict_Given_Last_Active_Credential` green.
- In the browser with one active credential: the row button reads **Revoke**, is disabled, and
  the text "Add another passkey or agent key before revoking this one." is visible under it —
  no red error bar is reachable.
- Create a second passkey (or register an agent key): Revoke becomes enabled on both rows with
  no hint. Revoke one: the remaining row flips back to disabled with the hint without a reload,
  and the success bar says "Credential revoked.".
- Microsoft 365 Unlink keeps its "Add a passkey first" hint when it is the last active credential.
