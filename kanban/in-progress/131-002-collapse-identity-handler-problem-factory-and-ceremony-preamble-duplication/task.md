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

- [ ] Inventory factories and ladder families
- [ ] Extract `IdentityProblems` (or equivalent)
- [ ] Extract ceremony helpers where ladders match
- [ ] Identity integration tests green
- [ ] `dev build` 0/0

## Notes

Parent: F-006. Ceremony order is security-critical (challenge burn, host check, auth first)
— one enforced path beats N copies.

## Session

- Created: 2026-07-28 — from task 131 disposition
