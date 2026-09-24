# Stamp credential last-used on Microsoft 365 (Entra) sign-in

## Description

Reported 2026-09-24: after signing in with Microsoft 365 the Settings row still says "Never used".
Task 248-002 wired `CredentialUsageRecorder` into passkey sign-in
(`complete-passkey-authentication-handler-application.cs`), agent-token issuance
(`complete-agent-token-issuance-handler-application.cs`) and agent-token validation (web + api
bearer handlers). The Entra ticket processor
(`source/container-apps/web/features/identity/entra-ticket-processor-application.cs`) was never a
write point — a gap in the 248-002 brief, not a regression. Task 250 already refreshes the Entra
`AccountHint` on the sign-in paths there, which is why the email now shows.

## Requirements

- Call `CredentialUsageRecorder.RecordAsync(store, credential, ct)` on every successful Entra
  sign-in that resolves to an existing active credential (the sync-hit path and the
  already-linked return in bootstrap), and when a new Entra credential is linked. Place it after
  the same liveness/quarantine checks the other ceremonies use, so a quarantined principal never
  stamps.
- Combine with task 250's `AccountHint` refresh so a sign-in does at most one credential UPDATE
  (stamp + hint in one write) — or justify two writes in the Design region. Same race rule:
  a lost version race is dropped, never retried (advisory).
- Register/inject the recorder where the ticket processor is constructed (it is already a
  singleton in `InMemoryIdentityStoresModule`).
- Tests (existing `entra-ticket-processor-tests.cs` / challenge tests): sign-in on an existing
  link sets `LastUsedAt`; linking sets it; a second sign-in advances it; hint + stamp in one write
  when both change; quarantined principal does not stamp.
- Reconcile Design regions (ticket processor; `CredentialUsageRecorder` lists its callers).
- Gates: `dev build` 0/0, `dev test`, `dev template-smoke`.
- **Do not start an AppHost** (`dev run`, `aspire run`, `dotnet run` of aspire-app-host). Record the
  manual check as not performed.

## Checklist

- [x] Entra sign-in (sync-hit + already-linked bootstrap) stamps last-used
- [x] Linking stamps last-used
- [x] Single write with the AccountHint refresh (or justified)
- [x] Tests
- [x] `dev build` 0/0 · `dev test` · `dev template-smoke`; no AppHost started

## Notes

- Related: 248-002 (write points), 250 (AccountHint refresh on sign-in).
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-24)
- Implemented (headless `ganda task work` implement oracle, 2026-09-24).

## Results

`EntraTicketProcessor` now takes the singleton `CredentialUsageRecorder` and stamps
`Credential.LastUsedAt` via `RecordAsync` on every successful Entra sign-in / link:

- **Sign-in:** sync-hit, the already-linked early return of `CompleteBootstrapCreateAsync`, and the
  bootstrap-create race winner. Each stamp runs after that path's principal-liveness/quarantine
  check, so a quarantined principal never stamps.
- **Link:** a new link (`AttachEntraToPrincipalAsync`, both the link mode and the already-have path),
  the bootstrap mint, and link-merge of a foreign owner. The merge path re-reads the row first,
  because `MergePrincipalAsync` re-parents and re-versions it.
- **Single write:** `RefreshAccountHintAsync` is now `RecordSignInAsync`. It applies `SetAccountHint`
  to the same snapshot, and the recorder's stamp UPDATE persists hint and stamp together (one Version
  bump per sign-in). A lost Version race is dropped inside the recorder and never retried. As a
  result every sign-in writes once, not only when the hint changes. A new link is INSERT and then
  the stamp UPDATE; the recorder owns the clock and the race rule, so Create does not pre-stamp.
  This is recorded in the Design region.
- **DI:** web-server already registers the recorder singleton in `InMemoryIdentityStoresModule`, so
  there is no product DI change. The two isolated test hosts (`entra-challenge-tests`,
  `entra-oidc-handler-round-trip-tests`) register it explicitly.
- **Design regions:** reconciled in the ticket processor (task-250 hint text plus the new task-252
  paragraph) and in `CredentialUsageRecorder` (Purpose mentions Entra; new callers list).
- **Tests:** `entra-ticket-processor-tests.cs` gains 6 tests: sync-hit stamps and a second sign-in
  advances it; hint + stamp in one write; already-linked bootstrap stamps; link + bootstrap mint
  stamp; link-merge stamps; quarantined principal does not stamp and does not write. The existing
  sync-hit hint test now expects one stamp write per sign-in. The challenge tests assert
  `LastUsedAt` end-to-end on sync-hit and link.

Gates: `dev build` 0 warnings / 0 errors; `dev test` exit 0 (all suites, web-server-integration
267 total); `dev template-smoke` SUCCEEDED. `dev` was invoked as `dotnet run tools/dev-cli/dev.cs --`
because the worktree has no `bin/dev`. **No AppHost was started. The manual check was not performed.**

### How to validate

**Smoke:**

```bash
cd tests/container-apps/web/web-server-integration-tests
dotnet test -c Release -- --filter-class EntraTicketProcessor
dotnet test -c Release -- --filter-class EntraChallenge
```

**Expect:** both runs pass, including `Sync_Hit_Should_Stamp_Last_Used_And_Advance_On_Next_Sign_In`,
`Sync_Hit_Should_Persist_Hint_And_Stamp_In_One_Write`, and
`Quarantined_Principal_Should_Not_Stamp_Last_Used`. Manually (not performed here): sign in with
Microsoft 365, then open Settings. The Microsoft 365 row shows a last-used time instead of
"Never used".

