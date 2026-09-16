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

- [ ] `TryRead` reason + Warning logs (types only) + problem detail names the failing check
- [ ] Boot log stamps informational version for the `entra` scheme
- [ ] Real-JWT round-trip integration test with stub authority (signed RSA id_token, PKCE, nonce)
- [ ] Root cause found and fixed, or explicitly recorded as not reproduced with the test in place
- [ ] `dotnet test -- --filter-class Entra` green; `dev build` 0/0; `ganda repo audit` clean
- [ ] Results and How to validate (include the exact log line to look for on the next live attempt)

## Session

- Created: 711075 (2026-09-16)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N

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

_Pending._

### How to validate

_Pending._
