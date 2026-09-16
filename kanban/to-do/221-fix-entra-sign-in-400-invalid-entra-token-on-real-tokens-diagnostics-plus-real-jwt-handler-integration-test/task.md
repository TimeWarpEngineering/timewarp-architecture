# Fix Entra sign-in 400 Invalid Entra token on real tokens: diagnostics plus real-JWT handler integration test

## Description

Bug. A real "Continue with Microsoft 365" sign-in against the CrunchIt, LLC tenant
(`30f3971f-4719-4f20-9b6f-88916e0b95bd`, app `f6d55605-aed5-4b5e-8b54-6260de4b323a`, single
tenant, `Authentication:Entra` fully configured incl. `PublicOrigin=https://arch.timewarp.work`,
`TrustedTenants:0=<that tenant>`, `AllowBootstrap=true`) reaches `/signin-oidc` and gets:

```json
{"title":"Invalid Entra token","status":400,"detail":"The Entra ID token is missing required tid, oid, or iss claims, or iss does not match the tenant."}
```

All 36 `Entra*` integration tests pass because they drive `EntraTicketHttp` /
`EntraTicketProcessor` with a hand-built `ClaimsPrincipal` from a fake handler; no test pushes a
real signed JWT through the real `OpenIdConnectHandler`. That gap must close as part of this task.

## What has been ruled out (cockpit, 2026-09-16)

- Configuration: user secrets are correct (tenant id, client id, secret, trusted tenant, public
  origin, bootstrap on). Challenge and redirect back through the public origin work (219-004).
- Issuer mismatch: `EntraIssuerValidator.Validate` (wired as `TokenValidationParameters.IssuerValidator`)
  compares `iss` to `EntraIssuerMaterial.FromTenantId(tid)` with `Ordinal`, exactly like
  `EntraTicketProcessor.IssuerMatchesTenant`. A mismatch would throw inside token validation and
  surface as a handler exception, not this 400. So the 400 comes from `EntraIdTokenClaims.TryRead`
  returning false (or a null principal) in `EntraTicketHttp.HandleTicketAsync`.
- Framework claim mapping: a throwaway probe validated an Entra-shaped JWT with
  `JsonWebTokenHandler { MapInboundClaims = false }` (what `OpenIdConnectOptions.MapInboundClaims=false`
  configures) and `iss`, `tid`, `oid` all appear on the `ClaimsIdentity` under short names; with
  mapping on they appear under the `schemas.microsoft.com/identity/claims/*` URIs which `TryRead`
  also checks. Decompiled `JsonWebTokenHandler.CreateClaimsIdentityPrivate` and `JsonClaimSet.CreateClaims`
  add every payload claim; no `iss` filtering.
- No `OnRemoteFailure` / `OnAuthenticationFailed` maps to this problem; only two call sites produce
  `IdentityProblems.InvalidEntraToken()`.

Remaining suspects (verify, do not assume): `context.Principal` in `OnTicketReceived` not being the
token principal in .NET 10 for this configuration (e.g. `SignInScheme` interplay, or the principal
being replaced/cleared before the event); an `oid`/`tid` value shape the code does not expect;
`HandleTicketAsync` being invoked on a request that is not the callback; a stale build on the
tester's machine (confirm with a version/commit stamp in the log).

## Requirements

1. **Diagnostics first (ship regardless of root cause).**
   - `EntraIdTokenClaims.TryRead` returns a reason (enum or out string): `MissingTenantId`,
     `UnparsableTenantId`, `MissingObjectId`, `UnparsableObjectId`, `MissingIssuer`.
   - `EntraTicketHttp` logs one Warning on failure with: the reason, the scheme name, the list of
     claim **types** present on the principal (types only — never values — and never the id_token),
     whether `entraPrincipal` was null, and the authentication type of the identity. Use
     `LoggerMessage.Define` like the existing `LogConfiguredVsEnabled` pattern.
   - `EntraTicketProcessor` issuer mismatch logs Warning with the expected issuer and the token
     issuer (issuer URIs are not secrets).
   - The 400 problem `detail` names the failing check (e.g. "missing oid claim"), still without values.
   - Boot log line for the `entra` scheme registration includes the informational version/commit
     (`Assembly` informational version) so a stale build is visible.
2. **Real-JWT integration test through the real handler.** In
   `tests/container-apps/web/web-server-integration-tests/features/identity/`, add a test that
   registers the named `entra` scheme against a **local stub OIDC authority** (in-process test
   server or `HttpMessageHandler` on `OpenIdConnectOptions.Backchannel`/`ConfigurationManager`
   that serves `.well-known/openid-configuration`, `jwks`, and a token endpoint returning a
   **signed** id_token built with `JsonWebTokenHandler` and an RSA key), then performs the full
   round trip: `GET /api/identity/entra/challenge?mode=bootstrap` → parse `Location` (state,
   nonce, code_challenge) → `GET /signin-oidc?code=...&state=...` with the correlation cookie →
   expect 302 + identity-session cookie. Token claims mirror Entra v2 exactly
   (`iss`, `aud`, `tid`, `oid`, `sub`, `name`, `preferred_username`, `ver`, `nonce`, `exp/nbf/iat`).
   If this test reproduces the 400, fix the root cause and keep the test as the regression guard.
   If it passes, the test still ships, and the diagnostics from (1) are the deliverable for the
   next live attempt; record that explicitly in Results.
3. **Live verification.** If the implementer can obtain a real id_token (the tester runs
   `dev run` and signs in with an `@crunchitfs.com` account), read the new Warning log to get the
   reason and fix accordingly. Do not paste tokens or claim values into the kitchen.
4. Keep `identity-session` as DefaultScheme; do not touch RP-ID / Host handling (104-031); do not
   introduce `UseForwardedHeaders`; keep `MapInboundClaims=false`.

## Checklist

- [x] `TryRead` reason + Warning logs (types only) + problem detail names the failing check
- [x] Boot log stamps informational version for the `entra` scheme
- [x] Real-JWT round-trip integration test with stub authority (signed RSA id_token, PKCE, nonce)
- [x] Root cause found and fixed, or explicitly recorded as not reproduced with the test in place
- [x] `dotnet test -- --filter-class Entra` green; `dev build` 0/0; `ganda repo audit` clean
- [x] Results and How to validate (include the exact log line to look for on the next live attempt)

## Session

- Created: 711075 (2026-09-16)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N
- Implementer: Grok 4.6 session 01a0a790-a9d0-7bd3-a11b-1b4dd11b9a35 (2026-09-16)

## Notes

- Files: `source/container-apps/web/features/identity/entra-id-token-claims-application.cs`,
  `entra-ticket-http-server.cs`, `entra-ticket-processor-application.cs`,
  `entra-authentication-registration-server.cs`, `entra-issuer-validator-server.cs`,
  `identity-problems-application.cs`; tests `entra-challenge-tests.cs` (fake handler at ~L390).
- Prior tasks: 219-002 (scheme), 219-004 (PublicOrigin, secure cookies), 219-006 (policy seam).
- Related follow-up (separate task, not this one): `dev entra setup` should show the tenant display
  name/domain and take `--tenant`; the tester first hit "user does not exist in tenant CrunchIt, LLC"
  by picking an account from another tenant.

## Results

Root cause: `OpenIdConnectOptions` defaults `ClaimActions.DeleteClaim("iss")`. After `OnTokenValidated`,
the handler runs those ClaimActions against empty JSON `{}`, which **strips `iss` from the id_token
identity**. `EntraIdTokenClaims.TryRead` then returned false (`MissingIssuer`) and
`EntraTicketHttp` wrote the generic 400. The 36 fake-handler tests never hit this because they
build a `ClaimsPrincipal` with `iss` and skip `OpenIdConnectHandler`. The new real-JWT round-trip
reproduced `{"title":"Invalid Entra token","status":400,"detail":"The Entra ID token is missing the iss claim."}`
before the fix; it now 302s with an identity-session cookie.

Fix (named `entra` scheme only):

- `options.ClaimActions.Remove("iss")` so the id_token issuer stays on the principal.
- `OnTokenValidated` copies `SecurityToken.Issuer` onto the identity when `iss` is still missing.
- Local return path is stashed on `AuthenticationProperties.Items` (`entra.return_url`) because the
  real handler overwrites `Properties.RedirectUri` with the protocol `redirect_uri` (would land on `/`).

Diagnostics (ship regardless):

- `EntraIdTokenClaims.TryRead` reports `MissingTenantId` / `UnparsableTenantId` / `MissingObjectId` /
  `UnparsableObjectId` / `MissingIssuer`.
- `EntraTicketHttp` logs one Warning (types only, never values / never the id_token).
- `EntraTicketProcessor` logs issuer mismatch with expected vs token issuer URIs.
- 400 `detail` names the failing check (e.g. "missing the oid claim", "missing the iss claim").
- Boot: `EntraSchemeRegistrationLogHostedService` logs informational version at `StartingAsync`.

Unchanged: `identity-session` DefaultScheme, `MapInboundClaims=false`, no `UseForwardedHeaders`,
no RP-ID / Host handling.

Live `@crunchitfs.com` sign-in was not run in this session (no tester id_token). The stub test is
the regression guard; the next live attempt should confirm the boot stamp and a 302 instead of 400.

**Files:** `entra-id-token-claim-read-failure-application.cs` (new),
`entra-id-token-claims-application.cs`, `identity-problems-application.cs`,
`entra-ticket-http-server.cs`, `entra-ticket-processor-application.cs`,
`entra-authentication-registration-server.cs`,
`entra-scheme-registration-log-hosted-service-server.cs` (new),
`entra-link-defaults-server.cs`, `challenge-entra-endpoint-server.cs`;
tests `entra-oidc-handler-round-trip-tests.cs` (new), `entra-id-token-claims-tests.cs` (new),
`entra-challenge-tests.cs`, `entra-ticket-processor-tests.cs`, `entra-scheme-registration-tests.cs`.

**Tests:** `cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class Entra` → 48 passed. `./bin/dev build` → 0/0. `ganda repo audit` → pass (2 pre-existing advisory warnings: memsearch-scaffold, vscode-window-icon).

### How to validate

**Automated**

```bash
cd tests/container-apps/web/web-server-integration-tests
dotnet test -c Release -- --filter-class Entra
# Expect: 48 passed, including Signed_Id_Token_Through_Real_Handler_Should_Issue_Identity_Session
./bin/dev build
# Expect: 0 Warning(s), 0 Error(s)
```

**Smoke (next live Microsoft 365 attempt)**

1. `./bin/dev run` with the CrunchIt Entra secrets (`Authentication:Entra` enabled, trusted tenant,
   `AllowBootstrap=true`, `PublicOrigin=https://arch.timewarp.work`).
2. On boot, look for this Information line (stale build if the informational version does not match
   this branch):

```text
Registered named OpenID Connect scheme entra (informational version {InformationalVersion}).
```

3. Sign in with Continue with Microsoft 365 (`@crunchitfs.com`). Expect 302 from `/signin-oidc` and
   an `identity-session` cookie — not 400 Invalid Entra token.
4. If 400 still appears, read the Warning (claim **types** only — no values, no id_token):

```text
Entra ticket claim read failed. Reason={Reason} Scheme=entra ClaimTypes={ClaimTypes} PrincipalNull={PrincipalNull} AuthenticationType={AuthenticationType}
```

`Reason` is one of `MissingTenantId`, `UnparsableTenantId`, `MissingObjectId`, `UnparsableObjectId`,
`MissingIssuer`, or `MissingPrincipal`. Issuer mismatch (if TryRead succeeded) is:

```text
Entra ticket issuer does not match tenant. ExpectedIssuer={ExpectedIssuer} TokenIssuer={TokenIssuer}
```

**Expect:** after this fix, live callback should not 400 for missing `iss`. A remaining 400 names
the failing check in `detail` and in the Warning `Reason`.

**Not in scope:** live token / claim values in the kitchen; `dev entra setup --tenant` display-name
follow-up; RP-ID / Host / `UseForwardedHeaders`.
