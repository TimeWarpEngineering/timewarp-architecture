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

- [ ] ContextLine dedupes provider vs title; empty line hidden; confirmation deduped
- [ ] Settings Microsoft 365 `Subtitle` removed (parameter deleted if unused)
- [ ] `preferred_username` captured, stored, mapped, migrated, exposed, rendered
- [ ] Tests (presenter, render, contract, server link)
- [ ] `dev build` 0/0 · `dev test` · `dev template-smoke`; no AppHost started

## Notes

- Origin: screenshot review 2026-09-24 (Settings → Microsoft 365 card, label shown three times).
- Related: 248-001 (row layout), 248-002 (last used).
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-24)
