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

- [ ] 248-001 merged
- [ ] 248-002 merged
- [ ] Parent Results written

## Notes

- Origin: screenshot review 2026-09-23 (Passkeys card, single "Proton Pass · Active · date" row).
- Related: 246 (disable Revoke on last credential), 247 (single notification region).
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-23)
