# Implement first-party WebAuthn passkey register and authenticate

## Parent

104

## Description

Browser WebAuthn create/get ceremonies for Human principals. Prefer discoverable credentials (sign-in without typing email). Mint Principal on successful register. Issue browser session after authenticate. Works with password managers (e.g. Proton Pass).

## Requirements

- Register creates Principal + passkey credential
- Authenticate proves possession → session
- Challenge/origin binding correct
- Do not require email/username up front (placeholders OK for WebAuthn user.name)

## Checklist

- [ ] Registration ceremony API
- [ ] Authentication ceremony API
- [ ] Session issuance for browser
- [ ] Smoke path documented in task Results when done

## Notes

Legacy Passwordless.dev in SPA is reference only — first-party is the goal.

### Depends on

104-002
104-027 (TypedId source generator — id JsonConverter closes a fail-open STJ gap; do not put PrincipalId/CredentialId in contracts before it lands)
104-028 (concurrency token on identity entities + store port — supersedes the D6 LWW deferral; do not write handlers against IPrincipalStore before Update* conflict semantics land)

## Session

- Created: 2026-07-16
