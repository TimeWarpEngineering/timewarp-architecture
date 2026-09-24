# Make credentials distinguishable: nickname, registration context, fingerprint, last used

## Description

A credential row today shows the AAGUID provider name (or a caller-supplied label) and a
creation timestamp — nothing else. Three passkeys stored in the same provider render as three
identical rows, so the user cannot tell which lives on which device or which one they actually
sign in with, and revoking becomes a guess. Decision (Steve, 2026-09-23): add all four
discriminators — user nickname, registration context, a short credential fingerprint, and
last-used — and make the revoke confirmation restate that identity.

Parent task: the work splits by data lifetime. Child 248-001 covers add-time data plus a rename
action (no sign-in path change). Child 248-002 covers last-used, which is written on every
successful authentication and touches the store's write pattern. Ship -001 first.

## Requirements

- Both children done; the credential list and the revoke confirmation show nickname, provider,
  registration context, created, last used, and fingerprint consistently for passkeys and agent
  keys.
- Parent closes with a Results summary linking both PRs and one screenshot-free description of
  the final row.

## Checklist

- [x] 248-001 merged (PR #397)
- [x] 248-002 merged (PR #398)
- [x] Parent Results written

## Results

Both children merged; credentials are now distinguishable in the list and in the revoke/unlink
confirmation for passkeys, agent keys and Microsoft 365 links.

- **248-001 (PR #397)** — `Credential.Label` is the immutable provider name; new mutable
  `Nickname` with a `RenameCredential` endpoint (same auth, IDOR 404 and retry loop as revoke);
  `RegisteredWith` (authenticator attachment + browser/OS family, raw User-Agent never stored)
  captured once at registration; `Fingerprint` = last 8 hex of SHA-256 of the handle, the only
  handle-derived value on the wire. Migration `AddCredentialNicknameAndRegisteredWith`.
- **248-002 (PR #398)** — `LastUsedAt` via monotonic `MarkUsed(now)`; written on every passkey
  sign-in and agent-token issuance, coalesced to at most once per 5 minutes per credential per
  process on per-request bearer validation (`CredentialUsageRecorder`); a lost version race is
  dropped so revoke always wins. Migration `AddCredentialLastUsedAt`.
- **Final row** — title = nickname, else provider; context line = account hint · provider ·
  attachment · "Browser on OS", with any part equal to the title or an earlier part dropped and
  the line hidden when empty (follow-up task 250, PR #400, which also added the Entra
  `AccountHint` from `preferred_username`); then "Created …", "Last used … / Never used", and the
  8-hex fingerprint in monospace; Rename (inline) and Revoke/Unlink with a two-step confirmation
  restating that identity. Revoke is disabled on the last active credential (task 246).
- **Deferred** — interactive click coverage of the inline editor and two-step confirm (248-001
  review M7, accepted exception): state→markup and the endpoints are tested; a Playwright flow is
  the natural home if wanted.

## Notes

- Origin: screenshot review 2026-09-23 (Passkeys card, single "Proton Pass · Active · date" row).
- Related: 246 (disable Revoke on last credential), 247 (single notification region).
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-23)
