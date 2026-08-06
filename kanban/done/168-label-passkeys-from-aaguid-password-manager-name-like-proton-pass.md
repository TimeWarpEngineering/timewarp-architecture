# Label passkeys from AAGUID (password manager name like Proton Pass)

## Description

passkeys.io labels credentials with the password manager name (e.g. Proton Pass). That comes
from the WebAuthn **AAGUID** in attested credential data + the community AAGUID list — not from
reading the manager’s product UI.

## Results

- `WebAuthnRegistrationResult` exposes AAGUID
- `PasskeyProviderNames` + embedded names-only `aaguid.json` (51 providers)
- Registration / AddPasskey set `Credential.Label` from the map
- Settings already displays `Label` (falls back to "Passkey")

### How to validate

```bash
./bin/dev build
cd tests/libraries/timewarp-identity-tests && dotnet test -c Release -- --filter-class Resolve
# Create account with Proton Pass → Settings should show "Proton Pass"
```

## Session

- 2026-08-06 grok: implement after maintainer noted password manager naming on passkeys.io.
