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

- [x] Loopback-http allowance in the empty-allowlist fallback (single shared loopback predicate)
- [x] Design regions updated
- [x] Library + integration tests
- [x] Release-note mention; tw-deploy note
- [x] Gates: `dev build` 0/0, `dev test`, `ganda repo audit`

## Notes

- Interim workaround (maintainer, until redeploy):
  `kubectl -n timewarp-architecture set env deployment/web-server-deployment WebAuthnOptions__AllowedOrigins__0=http://localhost:36371`

## Results

- `WebAuthnOrigin.IsAllowed` (empty `Origins`): the host must equal the RP id; then https is accepted,
  and http only when `LoopbackHost.IsLoopback(host)` is true. A non-empty `Origins` list is still an
  exact ordinal match.
- **One shared predicate:** new public `TimeWarp.Identity.LoopbackHost.IsLoopback`
  (`source/libraries/timewarp-identity/ceremonies/loopback-host.cs`) accepts `localhost` (any case),
  `127.0.0.1`, `[::1]`, and `::1`. TimeWarp.Identity is the lowest layer both callers share. The
  web host's Entra `PublicOrigin` validator now uses it instead of its private `IsLocalhost`, so
  that validator also accepts `http://[::1]` now. The HTTP host accessor's loopback check is a
  different rule (it trusts loopback *peer IP addresses* for forwarded headers), so it was left
  alone. This is recorded in `overview.md`.
- Design regions and XML docs updated: `webauthn-origin.cs`, `webauthn-relying-party.cs`,
  `web-authn-options-application.cs`, and `entra-authentication-options-validator-application.cs`.
- Tests: `tests/libraries/timewarp-identity-tests/ceremonies/webauthn-origin-tests.cs` (10) and
  `loopback-host-tests.cs` (2). The web-server integration tests in `passkey-registration-tests.cs`
  check that `http://localhost:36371` succeeds with an empty AllowedOrigins and that
  `http://evil.example` returns 400.
- `tw-deploy`: the `dev open` bullet now says plain-http localhost works, and that other hosts need
  https or an explicit `AllowedOrigins`.
- **Release notes (TimeWarp.Identity):** "WebAuthn: with an empty `Origins` list, `http` origins are
  now accepted on loopback hosts (`localhost`, `127.0.0.1`, `[::1]`) matching the RP id, mirroring
  the browser secure-context rule. New public `LoopbackHost.IsLoopback`." No pin change in this PR,
  because the repo dogfoods identity through ProjectReference (the 124 policy applies at release).
- Gates: `dev build` 0/0, `dev test` green (identity 269/0 failed; web-server integration 331
  succeeded, 0 failed), `ganda repo audit` clean.

- **Review disposition:** `clean`. 1 round, effort 2, roster: general. Final counts: bug 0, suggestion 0,
  nit 1 fixed (M1: rewrapped the Design-region comment in `web-authn-options-application.cs`), 0 open,
  0 wontfix. Artifacts: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.

### How to validate

**Smoke:**
```bash
./bin/dev build
dotnet run --project tests/libraries/timewarp-identity-tests
dotnet run --project tests/container-apps/web/web-server-integration-tests
```
Then do a manual check on kind. Remove the interim env override
(`kubectl -n timewarp-architecture set env deployment/web-server-deployment WebAuthnOptions__AllowedOrigins__0-`),
redeploy, run `dev open --target kubernetes`, and create an account at `http://localhost:<ingress-port>`.

**Expect:** both test runs report `failed: 0`, and the list includes `Fallback_accepts_http_localhost_with_port`
and `Ok_Given_Http_Localhost_Origin_With_Empty_AllowedOrigins`. On kind, passkey registration
completes and does not show "OriginMismatch".

## Session

- Created: 2026-10-13 (cockpit, from Steve's kind sign-up failure)
- 2026-10-13: implemented (ganda task work, implementer); gates green.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-13T04:26:07Z
- Review oracle: claude-opus-5-5, tw-implementation-review effort 2, disposition clean — 2026-10-13
