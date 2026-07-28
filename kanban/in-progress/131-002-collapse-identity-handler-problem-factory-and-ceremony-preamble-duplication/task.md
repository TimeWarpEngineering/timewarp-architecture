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

### Implementation plan (2026-07-29)

#### Defaults
- `internal static` helpers; namespace `…Features.Identity.Application`
- Slice-root escape-hatch files: `identity-problems-application.cs`,
  `passkey-registration-ceremony-application.cs`,
  `agent-key-registration-ceremony-application.cs`
- **Problems only** for passkey-auth + agent-token issuance (single-consumer ladders)
- Copy Title/Status/Detail **verbatim** — no wording “improvements”
- Ceremony helpers start **after** caller auth-guard + RP select; AddCredential try/catch stays in handlers

#### Inventory (summary)
- Factories: Unauthenticated×4, MalformedPayload×6 (param field list), ChallengeInvalid×6
  (param ceremony label), CredentialAlreadyRegistered×4 (param kind), VerificationFailed
  passkey×2 / agent×2, InvalidPublicKey×2, Quarantined×2, plus unique auth/token/revoke/agent-identity
- Ladders to extract: **passkey registration** (Complete + Add), **agent-key registration**
  (Complete + Add). Not extract: passkey auth, token issuance (problems only).

#### Steps
1. Baseline identity integration tests + build
2. Extract `IdentityProblems`; replace all private factories; commit
3. Extract passkey registration ceremony helper; slim Add/Complete passkey reg handlers
4. Extract agent-key registration ceremony helper; slim Add/Complete agent key handlers
5. Grep zero private factories; Design regions reconciled; identity tests + `dev build` 0/0

#### Test gate
`tests/container-apps/web/web-server-integration-tests` Features/Identity/* (Passkey_*,
Agent_*, Credential_*, Revoke_*, HostSelection)

#### Non-goals
Merge handlers; generic all-ceremonies framework; AgentTokenAuthenticationHandler; grammar
registry changes.

## Session

- Created: 2026-07-28 — from task 131 disposition
- Plan: 2026-07-29 — tw-orchestrate-task Phase 2/3
