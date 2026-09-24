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

- [ ] Entra sign-in (sync-hit + already-linked bootstrap) stamps last-used
- [ ] Linking stamps last-used
- [ ] Single write with the AccountHint refresh (or justified)
- [ ] Tests
- [ ] `dev build` 0/0 · `dev test` · `dev template-smoke`; no AppHost started

## Notes

- Related: 248-002 (write points), 250 (AccountHint refresh on sign-in).
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-24)
