# Implement agent public-key registration and scoped API tokens

## Parent

104

## Description

Agents register a public key (or equivalent) without a browser ceremony. Receive short-lived scoped bearer (or similar) tokens — not cookie sessions. Machine-readable errors.

## Requirements

- Register Agent principal + key
- Issue/validate scoped tokens with expiry
- No human sponsor required at registration

## Checklist

- [ ] Register endpoint/handler
- [ ] Token issue + validate
- [ ] Tests for happy path + reject bad key/token

## Notes

Paid elevation is Wave 3 (013–014). Here: Keyed agent can exist.

### Depends on

104-002

## Session

- Created: 2026-07-16
