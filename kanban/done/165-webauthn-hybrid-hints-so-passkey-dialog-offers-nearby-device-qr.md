# WebAuthn hybrid hints so passkey dialog offers nearby device / QR

## Description

147-005 verified we do not *suppress* hybrid (no `authenticatorAttachment: platform`,
empty `allowCredentials`) but never *requested* nearby-device UI. Hanko/passkeys.io
surfaces **"Passkeys from a Nearby Device"**; our login only had a single soft button.

## Requirements

1. Server options include WebAuthn Level 3 `hints: ["client-device", "hybrid"]` for
   registration and authentication.
2. Login exposes an explicit **Passkeys from a nearby device** control that runs
   authentication with hybrid-first hints (`["hybrid"]`).
3. Unit tests lock hybrid-safe options shape (hints present, no attachment pin).
4. Preserve existing `data-qa` hooks; add `ContinueWithPasskeyNearby`.

## Checklist

- [x] Server registration + authentication BuildOptionsJson hints
- [x] SPA JS preferHybrid on GetCredential / CreateCredential
- [x] PasskeyCeremonyClient preferHybrid parameter
- [x] Login nearby-device button
- [x] Identity unit tests for BuildOptionsJson
- [x] `dev build` 0/0

## Results

### What was implemented

- **Server:** `WebAuthnAuthentication` / `WebAuthnRegistration.BuildOptionsJson` emit
  `hints: ["client-device", "hybrid"]`.
- **SPA:** `web-authn.ts` optional `preferHybrid` forces `hints: ["hybrid"]`.
- **Login:** Outline button **Passkeys from a nearby device**
  (`data-qa="ContinueWithPasskeyNearby"`) → `AuthenticateAsync(preferHybrid: true)`.
- Primary **Sign in with a passkey** still uses soft dual hints from the server.

### How to validate

**Automated**

```bash
./bin/dev build
# expect 0/0

cd tests/libraries/timewarp-identity-tests && dotnet test -c Release -- --filter-class BuildOptionsJson
# expect 2 passed
```

**Smoke (real passkeys, not mock auth)**

```bash
./bin/dev run
# open /Login
```

1. Click **Sign in with a passkey** — browser dialog should allow local and/or nearby.
2. Click **Passkeys from a nearby device** — expect hybrid-focused UI (phone QR / nearby)
   on Chrome with BLE/platform support.
3. Create account still hybrid-capable via server hints.

**Depends on:** real WebAuthn (mock auth yields 501); Chrome/Edge hybrid support; BLE often
required for phone path.

**Not in scope:** building a site-owned QR widget (browser provides hybrid UI); Windows may
ignore hints when system UI owns the dialog.

## Session

- 2026-08-06 grok: implement after maintainer noted missing nearby-device option vs hanko screenshot.
