# Settings passkeys management UI (list/create/revoke like passkeys.io)

## Description

Maintainer screenshot `Screenshot 2026-08-06 114758.png` shows passkeys.io account
security: Passkeys section with expandable credential (e.g. Proton Pass), Delete,
Created at, Create a passkey. Wire Settings to 104-005 APIs.

## Checklist

- [x] PasskeyCeremonyClient: List / Revoke / AddPasskey
- [x] SettingsPage UI matching screenshot structure
- [x] `dev build` 0/0

## Results

### What was implemented

- **Settings** (`/Settings`) lists the signed-in principal's active passkeys (GetCredentials).
- Expandable rows: Delete (RevokeCredential), Created at.
- **Create a passkey** runs StartPasskeyRegistration + browser create + **AddPasskey**
  (attaches to existing account — not a second principal).

### Out of scope (screenshot extras without domain support)

- Rename (Credential.Label set-at-create only)
- Last used at (no LastUsedAt field)
- Emails section / Delete account

### How to validate

```bash
./bin/dev build
./bin/dev run
# Sign in → Settings
# Expect passkey list; Create a passkey; Delete (cannot delete last active)
```

## Session

- 2026-08-06 grok: implement from maintainer screenshot.
