# Login conditional passkey autofill so browser shows Passkeys from a Nearby Device

## Description

Maintainer screenshot (hanko/passkeys.io): focusing an input opens browser autofill with
local passkeys + **"Passkeys from a Nearby Device"**. That menu entry is **browser UI**,
enabled only by WebAuthn **conditional mediation** + `autocomplete="username webauthn"`.

Task 165 added a site-level outline button and hybrid hints — useful, but **not** the
screenshot UX. This task implements the real autofill path.

## Design notes

- Autofill anchor is labeled **Passkey** (not Email). Value is never used as an identifier
  (discoverable credentials). `autocomplete="username webauthn"` is required by the browser.
- Conditional get starts after interactive render when
  `PublicKeyCredential.isConditionalMediationAvailable()` (or capabilities.conditionalGet).
- Modal **Sign in with a passkey** aborts conditional, then runs modal get; re-arms
  conditional on stay.

## Checklist

- [x] `GetCredentialConditional` + feature detect + abort in web-authn.ts
- [x] PasskeyCeremonyClient conditional API
- [x] LoginPage autofill field + conditional loop
- [x] Remove misleading site "Passkeys from a nearby device" button
- [x] `dev build` 0/0

## Results

### What was implemented

- Conditional WebAuthn get (`mediation: "conditional"`) on Login when the browser supports it.
- Native autofill field with `autocomplete="username webauthn"` so the browser can show
  **Passkeys from a Nearby Device** in its dropdown (same as the hanko screenshot).
- Modal button remains as fallback (hanko also keeps “Sign in with a passkey”).

### How to validate

```bash
./bin/dev build
# expect 0/0
```

**Smoke (Chrome/Edge, real passkeys, not mock auth)**

1. `./bin/dev run` → `/Login`
2. Focus the **Passkey** field (or autofocus may open autofill).
3. Expect browser autofill listing passkeys and, when hybrid is available,
   **Passkeys from a Nearby Device** (or equivalent “phone / tablet” wording).
4. Selecting a local passkey or nearby-device path completes sign-in.
5. **Sign in with a passkey** still opens the modal dialog.

**Depends on:** secure context, conditional UI support, hybrid/BLE for phone path.

**Not in scope:** inventing a site QR widget; Windows system UI may use different copy.

## Session

- 2026-08-06 grok: implement conditional autofill after maintainer re-raised the screenshot.
