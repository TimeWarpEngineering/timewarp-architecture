# Credential row repeats the label: dedupe context line, drop Microsoft 365 subtitle, show the linked account

## Description

On Settings, the Microsoft 365 card renders "Microsoft 365" four times: card heading, row title,
context line, and a subtitle. The API has one field with that value (`label`). Sources:

- Row title — `CredentialRowPresenter.Title`: `nickname` ?? `label` ?? fallback. Entra rows have
  no nickname, so this is `label`.
- Context line — `CredentialRowPresenter.ContextLine`: provider (`label`) · attachment ·
  browser/OS. Entra rows have Unknown attachment and null browser/OS, so only `label` remains.
- Subtitle — hard-coded `Subtitle="Microsoft 365"` in
  `source/container-apps/web/projects/web-spa/features/application/pages/SettingsPage.razor:160`
  (not data; the card heading already says it).

The 248-001 row layout assumes a passkey (nickname title, provider + device context line). A
credential with no nickname and no registration context collapses to the label everywhere. The
same happens for an un-renamed passkey whose context is Unknown (title = provider, context line =
provider).

## Requirements

- **Presenter.** `ContextLine` omits the provider part when it equals the rendered title
  (ordinal-ignore-case); the row hides the context `<p>` when the resulting line is empty. The
  revoke/unlink confirmation text follows the same rule (no "Microsoft 365, Microsoft 365").
- **Subtitle.** Remove `Subtitle="Microsoft 365"` from the Entra list on Settings; if the
  `Subtitle` parameter then has no caller, delete it from `CredentialList` rather than keep dead
  surface.
- **Identify the linked account.** The Entra credential stores only `tid:oid`
  (`EntraAccountHandle`); the ID token already exposes `preferred_username` (UPN/email) and
  `name` (`entra-id-token-claims-application.cs`). Capture `preferred_username` at link time as
  display-only account text (never a join key — join stays tid+oid per RFC 219 fork 1), store it
  on the credential beside `RegisteredWith` (EF mapping + migration, postgres-gated; in-memory
  parity), expose it on `GetCredentials.CredentialSummary` (e.g. `AccountHint`), and render it as
  the Entra row's context line ("steve@contoso.com"). Existing links show no hint until the next
  sign-in refreshes it — decide whether sign-in (not only link) updates it and record the choice.
  Treat it as PII: summary only for the caller's own credentials (existing IDOR rule), not in logs.
- **Tests.** Presenter: title==provider ⇒ no duplicate; empty context ⇒ line hidden; confirmation
  dedup. SPA render test for an Entra row (title, account hint, no repeated label). Contract
  round-trip for the new field. Server test that linking stores `preferred_username`.
- Reconcile Design regions (`credential-row-presenter.cs`, `CredentialList.razor`, the Entra
  ticket processor). Gates: `dev build` 0/0, `dev test`, `dev template-smoke`.
- **Do not start an AppHost** (`dev run`, `aspire run`, `dotnet run` of aspire-app-host): task
  worktrees share the master user-secrets id and a worker's AppHost rewrote the postgres password
  on 2026-09-23. Record the manual page check as not performed.

## Checklist

- [x] ContextLine dedupes provider vs title; empty line hidden; confirmation deduped
- [x] Settings Microsoft 365 `Subtitle` removed (parameter deleted if unused)
- [x] `preferred_username` captured, stored, mapped, migrated, exposed, rendered
- [x] Tests (presenter, render, contract, server link)
- [x] `dev build` 0/0 · `dev test` · `dev template-smoke`; no AppHost started

## Notes

- Origin: screenshot review 2026-09-24 (Settings → Microsoft 365 card, label shown three times).
- Related: 248-001 (row layout), 248-002 (last used).
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-24)
- Implemented under `ganda task work` (implement oracle, headless Claude) 2026-09-24.

## Results

**Presenter / list.** `CredentialRowPresenter.ContextLine` is now account hint · provider ·
attachment · client, and it drops any part that equals the rendered title or an earlier part
(ordinal-ignore-case). It returns `""` when nothing is left, and `CredentialList` hides the context
`<p>` in that case. `RevokeConfirmation` follows the same rule: "Unlink “Microsoft 365”?
steve@contoso.com, created …", and "Delete “Proton Pass”? Created …" for an un-renamed passkey.
The `Subtitle` parameter had no other caller, so it is deleted from `CredentialList`, and the
Settings Microsoft 365 card heading is the fixed text "Microsoft 365" (it no longer swaps in the
account label).

**Linked account.** An Entra credential's `Label` is now the provider (`EntraIdTokenClaims.ProviderLabel`,
"Microsoft 365"), the same rule passkeys follow since 248-001. The account is the new
`Credential.AccountHint`: the trimmed `preferred_username`, or null. It is display-only and never a
join key (the join stays tid+oid in `Handle`). It is truncated at `MaxAccountHintLength` = 256
instead of rejected, and `Snapshot` copies it. EF maps it as an unindexed `character varying(256)`.
Migration `20260923180147_AddCredentialAccountHint` adds the column and a data step: an existing
Entra `Label` that looks like an email moves to `AccountHint`, and every Entra `Label` becomes
"Microsoft 365". Down reverses both. The in-memory store gets parity through `Snapshot`.
`GetCredentials.CredentialSummary.AccountHint` exposes it (last ctor parameter, default null), and
it stays scoped to the caller's own credentials under the existing IDOR rule.

**Refresh decision (recorded in the `entra-ticket-processor-application.cs` Design region).**
Sign-in updates the hint as well as link. When a sign-in resolves to an existing active Entra
credential (the sync-hit path and the already-linked return in bootstrap), the stored hint is
refreshed if the token's `preferred_username` differs. This backfills links made before this task
and follows UPN renames. A token without the claim keeps the stored hint. The write happens only
when the value changes, and a lost Version race is dropped (advisory, like last-used). The hint is
never logged.

**Tests.** Presenter: title==provider dedupe (including a case-insensitive nickname), empty line,
Entra hint-only line, confirmation dedupe. Render: an Entra row renders title "Microsoft 365" and
context "steve@contoso.com", with "Microsoft 365" appearing once in the whole markup; an un-renamed
passkey has no context `<p>`. Contract round-trip carries `AccountHint`. Server: link and bootstrap
store the provider Label plus the `preferred_username` hint (the ticket processor and the
challenge endpoint); sync-hit backfills, skips an unchanged write, keeps the hint when the claim is
absent, and follows a rename. Existing tests that pinned the old Label = UPN/name behavior are updated.

**Gates.** `dev build` 0 warnings / 0 errors · `dev test` exit 0 (21 suites, 1392 passed, 0
failed) · `dev template-smoke` SUCCEEDED · `ganda repo audit` 29/29 (after
`--fix --checks bin-dev` built the gitignored local `bin/dev`).

**Not performed:** the manual Settings page check. No AppHost was started (`dev run` / `aspire run`
were not used). Note: `dev test` includes the existing closed-box `web-spa-integration-tests` lane,
which boots the AppHost via `Aspire.Hosting.Testing` as part of the standard suite.

### How to validate

- **Smoke:** from the task worktree, `cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class CredentialList_Should_`,
  then `-- --filter-class Row_Should_`; and `cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class Bootstrap_Given_`.
- **Expect:** all pass, including `Render_Entra_Row_With_Account_Hint_And_No_Repeated_Label`
  (title "Microsoft 365", context "steve@contoso.com", label rendered once),
  `Hide_Context_Line_When_Nothing_Differs_From_Title`, `Revoke_Confirmation_Does_Not_Repeat_The_Title`,
  and `Sync_Hit_Should_Refresh_Account_Hint_And_Keep_It_When_Claim_Absent`. Manual check (not performed
  here): with a linked Microsoft 365 account, Settings shows "Microsoft 365" once as the card heading,
  once as the row title, and the account email as the context line, with no subtitle.

### Review disposition

- **Effort / roster:** 1 — `general` (1 round)
- **Final counts:** bug 1 fixed · suggestion 1 fixed · nit 3 fixed · 0 open · 0 wontfix
- **Disposition:** `clean`
- **Fixes:** SettingsPage.razor.cs Design region reconciled; Settings Entra deep-link tests seed
  Label "Microsoft 365" + AccountHint; migration Down documents lossy rollback; BOMs stripped from
  4 test files; two comment lines re-wrapped. Post-fix gates: `dev build` 0/0,
  web-server-integration-tests 255/255, web-spa-integration-tests 68/68, web-contracts-tests 44/44.
- **Artifacts:** `review/review-framework.md`, `review/round-1/general.md`,
  `review/round-1/merged.md`, `review/disposition.md`
