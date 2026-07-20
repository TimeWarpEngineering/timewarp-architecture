# TimeWarp.Identity foundation

## Description

First-party principal and credential package: humans (WebAuthn passkeys) and
agents (public keys / tokens). Progressive profile; multi-credential; template
demo; deprioritize Entra/MSAL. Replaces Passwordless.dev as the long-term center.

## Requirements

- Principal model: Id, Kind (Human|Agent|Service), TrustTier, Credentials[]
- Passkey-first human onboarding (profile later)
- Agent key registration + scoped tokens (not browser cookie sessions)
- Package builds in monorepo / publishable path decided in 098-001
- Tests for ceremonies

## Checklist

- [ ] 098-001 Design model + API
- [ ] 098-002 Scaffold package
- [ ] 098-003 WebAuthn passkeys
- [ ] 098-004 Agent keys + tokens
- [ ] 098-005 Progressive profile
- [ ] 098-006 Multi-credential + recovery soft-prompt
- [ ] 098-007 Template demo wire-up
- [ ] 098-008 Entra/MSAL flag-off or remove priority
- [ ] 098-009 Tests

## Notes

### Depends on
097 ADRs (especially 097-001).

### Unblocks
100 (composition), parts of 103.

### Out of v1
Full DID/VC stack; Entra External ID parity.

## Session

- Created: 2026-07-16
