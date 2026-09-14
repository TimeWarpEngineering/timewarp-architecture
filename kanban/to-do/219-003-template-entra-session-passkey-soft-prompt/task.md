# Template: Entra-session passkey soft-prompt

## Parent

219

## Description

Fold-in of architecture RFC 219 **D8** (098-006 shape). After an Entra-issued identity-session, if `GetCredentials` has no `Passkey`, show a dismissible “Add a passkey” prompt that drives existing `AddPasskey`. Never a gate.

## Requirements

- Detect Entra-only (or Entra-without-passkey) via `GetCredentials` Type list — not a new TrustTier, not quarantine
- Banner/CTA on post-login home or profile; dismissible for the session (and optionally persisted as “later”)
- Uses existing StartPasskeyRegistration + AddPasskey
- Template only; crunchit may reuse the same pattern in its auth child
- Tests: Entra-only principal sees prompt; principal with passkey does not; dismissing does not block routes

## Checklist

- [ ] Soft-prompt UI wired to existing add-passkey ceremony
- [ ] Not a route/session gate
- [ ] Tests for shown/hidden/dismissed

## Notes

RFC: `rfc/rfc.md` §4 D8, §7.1. Fork 1: passkey is a prompt afterward, never a gate.

## Depends on

- 219-002

## Session

- Created: 3425584 (2026-09-14)
