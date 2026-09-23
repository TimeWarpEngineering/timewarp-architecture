# Credential nickname, registration context, and fingerprint

## Description

Add-time identity for a credential plus a rename action, so identical-provider rows become
distinguishable without touching the sign-in path. Child of 248.

## Requirements

- **Nickname.** `Credential` (`source/libraries/timewarp-identity/credentials/credential.cs`) gains a
  mutable `Nickname` distinct from the provider `Label` (keep `Label` = AAGUID/provider name;
  today it is also overwritten by a caller-supplied label — separate the two). New endpoint
  `RenameCredential` (endpoint-centric contract per `tw-web-api-contracts`; `[ApiEndpoint]` +
  `[EndpointAuthorize]` with the same policy as RevokeCredential; validator: 1–64 chars, trimmed;
  IDOR rule as RevokeCredential — caller may only rename their own). The add flows
  (`AddPasskeyPrompt.razor`, `credentials-state.add-passkey.cs`, the register-passkey and
  agent-key flows) prompt for a nickname pre-filled with the provider name; `AddPasskey.Command.Label`
  becomes the nickname input (rename the property if that reads better; contracts test updated).
- **Registration context.** Capture once at registration: authenticator attachment
  (platform / cross-platform from the WebAuthn response / transports), and the browser + OS
  family from the request User-Agent (parsed server-side into two short strings, never the raw UA).
  Store on the credential (`RegisteredWith` or similar record: Attachment, Browser, Os). Agent keys
  record the client the key was registered from if the ceremony exposes it; else null. EF mapping in
  `credential-entity-type-configuration-infrastructure.cs` + a migration (postgres flag). In-memory
  store parity.
- **Fingerprint.** Expose a short, stable discriminator derived from the credential id (e.g. last
  8 hex of SHA-256 of the id bytes) on `GetCredentials.CredentialSummary` as `Fingerprint`; never the
  raw id or public key (the contract's Design region forbids material on the wire — keep that test).
- **UI.** `CredentialList.razor` row: nickname as the title (fallback provider label), provider +
  attachment + browser/OS as a secondary line, created date, fingerprint in monospace, a Rename
  action (inline edit) beside Revoke. Revoke confirmation restates: nickname, provider, created,
  fingerprint. Uses the shell notification region for outcomes (task 247 rules) — no page-local bars.
- **Tests.** Co-located Jaribu for RenameCredential (happy path, validation rejection, cannot
  rename another principal's credential = 404-shaped like revoke), GetCredentials round-trip with
  the new fields and the material-never-on-wire assertion, registration ceremony test asserting
  attachment/browser/OS captured, SPA test for the row content and inline rename. Existing
  identity suites stay green.
- Design regions reconciled (Credential, contracts, CredentialList). Gates: `dev build` 0/0,
  `dev test`, `dev template-smoke` (postgres flag off must still build: migration + mapping stay
  in the postgres-gated files).

## Checklist

- [ ] Nickname field + RenameCredential endpoint + validator + tests
- [ ] Add flows prompt for nickname (pre-filled provider name)
- [ ] Registration context captured, stored, mapped, migrated
- [ ] Fingerprint on the summary; no material on the wire
- [ ] CredentialList row + inline rename + revoke confirmation text
- [ ] Gates: build 0/0, test, template-smoke

## Notes

- Parent: 248. Sibling 248-002 (last used) depends on this landing first.
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-23)
