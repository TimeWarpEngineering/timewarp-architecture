# Support multiple credentials per principal and list revoke

## Parent

104

## Description

Many-to-one credentials from day one (phone + laptop passkeys, agent key rotation). List and revoke while authenticated.

## Requirements

- Add credential to existing principal
- List credentials
- Revoke credential
- Cannot revoke last credential without explicit policy (document choice)

## Checklist

- [ ] Add/list/revoke APIs
- [ ] Tests

## Notes

Recovery soft-prompt for humans can wait; multi-credential is the structural fix.

### Depends on

104-003, 104-004

## Session

- Created: 2026-07-16
