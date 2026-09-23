# Credential last-used timestamp written on every successful authentication

## Description

Record when a credential was last used to authenticate and show it in the credential list and
the revoke confirmation. This is the one signal that answers "is this the passkey I actually
use" before revoking. Child of 248; lands after 248-001.

## Requirements

- **Domain.** `Credential` gains `LastUsedAt` (nullable) with a named mutation (`MarkUsed(now)`),
  never a public setter (aggregate pattern, `tw-aggregate-pattern`).
- **Write points.** Set it on every successful assertion / validation for every credential kind:
  passkey authentication (`complete-passkey-authentication` handler), agent-key token issuance
  (`complete-agent-token-issuance`), and — decide and document — agent-token validation on each
  request (`agent-token-authentication-scheme-server.cs`). For the per-request path, do NOT write
  on every call: coalesce (write at most once per N minutes per credential) so the hot path stays
  read-mostly; record the interval in the Design region. Passkey sign-in is per-ceremony and
  writes every time.
- **Concurrency.** RevokeCredential uses snapshot + optimistic `Version` retry (see its Design
  region). `MarkUsed` must not fight it: a lost race on a last-used write is dropped, never
  retried into a revoke (last-used is advisory). State the rule in the Design region and test it
  (concurrent revoke + mark-used ⇒ revoke wins, no exception surfaces to the caller).
- **Persistence.** EF mapping + migration (postgres flag); in-memory store parity.
- **Surface.** `GetCredentials.CredentialSummary.LastUsedAt`; `CredentialList` shows "Last used
  <relative>" or "Never used"; revoke confirmation includes it; the disabled-last-credential hint
  from task 246 is unaffected.
- **Tests.** Co-located Jaribu: passkey sign-in sets LastUsedAt; agent token issuance sets it;
  per-request validation coalesces (two calls inside the interval ⇒ one write); revoke-vs-mark-used
  race; GetCredentials round-trip. Existing identity suites green.
- Gates: `dev build` 0/0, `dev test`, `dev template-smoke`.

## Checklist

- [ ] `LastUsedAt` + `MarkUsed` on the aggregate
- [ ] Write points: passkey sign-in, agent-token issuance, coalesced per-request validation
- [ ] Race rule vs revoke documented and tested
- [ ] Mapping + migration + in-memory parity
- [ ] Summary field + list + revoke confirmation
- [ ] Gates

## Depends on

- 248-001

## Notes

- Parent: 248.
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-23)
