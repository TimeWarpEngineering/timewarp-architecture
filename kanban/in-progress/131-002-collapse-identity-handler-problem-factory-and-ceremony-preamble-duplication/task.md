# Collapse identity handler problem-factory and ceremony-preamble duplication

## Parent

131

## Description

De-duplicate identity application handlers (task 131 F-006). The identity slice is the
template's flagship pattern — problem factories and ceremony ladders are copy-pasted across
handlers and kept consistent by Design-region comments.

## Requirements

- Shared `IdentityProblems` statics (or equivalent) in the identity application layer for
  pure problem data; parameterize intentional wording variants (e.g. ChallengeInvalid
  registration vs authentication).
- Ceremony-preamble helpers per family (passkey registration, agent-key, passkey auth)
  where ladders truly match — decode → consume challenge → verify → handle-exists.
- **Do not** merge distinct handlers; keep auth-guard placement and post-verify actions
  per-handler.
- Move ordering rationale into the helper Design regions; slim handler Design regions to
  genuine differences.
- Stay inside the identity slice (no TWA0009 surface).

## Checklist

- [x] Inventory factories and ladder families
- [x] Extract `IdentityProblems` (or equivalent)
- [x] Extract ceremony helpers where ladders match
- [x] Identity integration tests green (97 passed, 1 skipped)
- [x] web-application + web-server build 0/0
- [x] Phase 4b review disposition clean

## Notes

Parent: F-006. Ceremony order is security-critical (challenge burn, host check, auth first)
— one enforced path beats N copies.

### Implementation plan (2026-07-29)

Executed: IdentityProblems + passkey/agent-key registration ceremony helpers; problems-only
for single-consumer auth/token ladders.

## Session

- Created: 2026-07-28 — from task 131 disposition
- Plan: 2026-07-29 — tw-orchestrate-task Phase 2/3
- Implement: 2026-07-29 — Phase 4 (`7d4653b0`)
- Review: 2026-07-29 — Phase 4b general, disposition clean

## Results

**What shipped**
- `identity-problems-application.cs` — 16 shared factories; 36 private factories removed from
  9 handlers (0 remaining).
- `passkey-registration-ceremony-application.cs` — shared preamble for AddPasskey +
  CompletePasskeyRegistration.
- `agent-key-registration-ceremony-application.cs` — shared preamble for AddAgentKey +
  CompleteAgentKeyRegistration.
- Auth/token handlers use IdentityProblems only (no ceremony extract).
- Design regions: ordering rationale on helpers; handlers keep genuine differences.

**Tests:** web-server-integration-tests **97 passed**, 1 skipped; web-application and
web-server **0/0**.

**Review:** effort 1 general; round-1 **0 open**; disposition **clean**. Paths under
`review/review-framework.md`, `review/round-1/{general,merged}.md`, `review/disposition.md`.
