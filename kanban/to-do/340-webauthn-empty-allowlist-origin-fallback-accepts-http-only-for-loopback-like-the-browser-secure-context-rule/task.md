# WebAuthn empty-allowlist origin fallback accepts http only for loopback, like the browser secure-context rule

## Problem (2026-10-13, kind deployment)

Creating an account on the kind deployment, opened with `dev open --target kubernetes` at
`http://localhost:36371`, fails with **"Passkey registration verification failed: OriginMismatch"**.

**Cause:**
- The deployment has `WebAuthnOptions:AllowedOrigins` empty (committed `appsettings.json`).
- `WebAuthnOrigin.IsAllowed` (`source/libraries/timewarp-identity/ceremonies/webauthn/internal/webauthn-origin.cs`)
  then applies the dev fallback: accept any **https** origin whose host equals the RP id.
- The RP id is correctly `localhost` (selection via X-Forwarded-Host works), but the browser origin is
  **http**, because `kubectl port-forward` is plain HTTP. So it is rejected.
- `dev run` works because it goes through `https://localhost:63610`.

Browsers allow WebAuthn on `http://localhost` (a secure context), so the server's fallback is
stricter than the platform.

## Requirements

- **Library (`TimeWarp.Identity`):** in the empty-`Origins` fallback, accept:
  - `https://<rp-host>[:port]` (unchanged);
  - `http://<host>[:port]` **only** when the origin host is loopback: `localhost` (case-insensitive),
    `127.0.0.1`, or `[::1]`, **and** equals the selected RP id.

  Never accept http for any non-loopback host. An explicit non-empty `Origins` list stays an exact,
  ordinal allow-list (unchanged).
- Reuse one loopback predicate if the repo already has one (for example the Entra `PublicOrigin`
  validator's "http allowed only for localhost" rule, or the host accessor's loopback check). Don't
  add a third copy. If they live in different assemblies, put the predicate in the lowest shared
  layer and record that.
- Update the Design regions:
  - `WebAuthnRelyingParty` / `webauthn-origin.cs` (fallback rationale: mirrors the browser
    secure-context rule);
  - `web-authn-options-application.cs` (the "accept any https origin" wording).
- **Tests** (`tests/libraries/timewarp-identity-tests`):
  - http and https localhost with a port accepted;
  - http 127.0.0.1 and http [::1] accepted when the RP id matches;
  - http on a non-loopback host rejected;
  - https on a foreign host rejected;
  - a non-empty `Origins` list stays exact, so http localhost is rejected unless listed.
  - Plus a web-server integration test: registration over an `http://localhost:<port>` origin
    succeeds with an empty allowlist.
- **Packaging:** this is a published-package change (`TimeWarp.Identity`). Note it for the release
  notes. The repo already dogfoods identity through ProjectReference, so no pin change is needed in
  this PR (124 policy applies at release).
- **`tw-deploy` skill:** note that local `dev open` over plain HTTP works on localhost, and that
  non-localhost hosts need https or an explicit `AllowedOrigins`.

## Checklist

- [ ] Loopback-http allowance in the empty-allowlist fallback (single shared loopback predicate)
- [ ] Design regions updated
- [ ] Library + integration tests
- [ ] Release-note mention; tw-deploy note
- [ ] Gates: `dev build` 0/0, `dev test`, `ganda repo audit`

## Notes

- Interim workaround (maintainer, until redeploy):
  `kubectl -n timewarp-architecture set env deployment/web-server-deployment WebAuthnOptions__AllowedOrigins__0=http://localhost:36371`

## Session

- Created: 2026-10-13 (cockpit, from Steve's kind sign-up failure)
