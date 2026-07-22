# Round-1 general review — 104-031 (commit 337527bc)

Reviewer scope: correctness / quality / conventions. Security-adversarial angle is a
separate reviewer.

Verification performed: read the full diff and all touched files in final state; confirmed
the `WebAuthnRelyingParty` record shape; empirically ran `Uri.CheckHostName` across edge
hosts; ran the two host-free suites (`WebAuthnRelyingPartySelection_.Select_Should` 3/3,
`WebAuthnOptionsValidator_.Validate_Should` 7/7 — both green, whole solution built clean to
get there); grepped for lingering `RpId` references and user-secret blast radius.

## Findings

### G1 — Standalone yarp original-host transform does not reach the passkey endpoints — minor
File: `source/container-apps/yarp/appsettings.Development.json:10-37`
The `RequestHeaderOriginalHost` transform was added to `WebRoute` (`{**catch-all}` → Web.Server)
and `WebSwaggerRoute` (`/api/web-server/**` → Web.Server), but the passkey ceremony endpoints
live under `/api/identity/...`, which matches the more-specific `ApiRoute` (`/api/{**catch-all}`
→ **Api.Server**). Api.Server does not reference web-application and hosts no identity endpoints
(its contracts come from api-contracts, not web-contracts), and `ApiRoute` carries no
original-host transform. So through the *standalone* yarp deployment: (a) `/api/identity/*` never
reaches Web.Server at all, and (b) even if it did, no original Host is forwarded. The Aspire
AppHost path is correct and complete — it explicitly adds `/api/identity/{**catch-all}` → webServer
*with* the transform (`aspire-app-host/program.cs:83-86,96`), and that is the task-112 ingress that
was actually verified. This is a **pre-existing routing gap** in the standalone yarp config (identity
was never routed to Web.Server there), not a regression this commit introduces, and the added
transform is harmless. Flagging so the team can decide whether standalone-yarp identity routing is in
scope; if it is, standalone yarp needs an `/api/identity/**` → Web.Server route (mirroring the AppHost)
before the transform means anything there.
Suggested fix: either add the `/api/identity/**` → Web.Server route (with `RequestHeaderOriginalHost`)
to the standalone yarp config, or note explicitly that standalone yarp does not serve identity and the
transform additions are inert there.

### G2 — Passkey_HostSelection SNI/dev-cert comment overstates behavior — nit
File: `tests/container-apps/web/web-server-integration-tests/Features/Identity/Passkey_HostSelection_Tests.cs:906-917`
The comment claims "Setting Headers.Host makes SocketsHttpHandler use it as the TLS SNI/target host,
so the localhost dev cert no longer name-matches." In .NET the TLS SNI and connection target derive
from the request URI authority (here `BaseAddress` = `https://localhost:7000`), not from an overridden
`HttpRequestMessage.Headers.Host`. The connection still targets localhost and the dev cert still
name-matches, so `DangerousAcceptAnyServerCertificateValidator` is very likely unnecessary. The test
still exercises exactly what it should — ASP.NET's `Request.Host` reflects the sent Host header, which
is what selection reads — so the outcome is correct; only the rationale is imprecise and the cert
override is redundant. Test-only, sound in effect.
Suggested fix: drop the custom `HttpClientHandler`/validator (use the shared client with a per-request
`Headers.Host`), or correct the comment to say the Host header override does not change SNI/target and
the validator is defensive-only.

## Clean areas (explicitly verified)

- **Options + binder append (focus 1):** `AllowedRpIds` default `["localhost"]` + config bind APPENDS
  onto the pre-initialized `List<T>` — behavior is real and the Design region's claim is accurate;
  pinned by `WebAuthnOptions_Binding_Tests` (`["localhost","webauthn-second.test"]`). Design region
  correctly warns shipped appsettings must not list `AllowedRpIds`; committed appsettings.json indeed
  drops the key (only RpName/AllowedOrigins remain). Test appsettings adds `["webauthn-second.test"]` at
  index 0 and the append still yields both — consistent.
- **Validator (focus 1):** `Uri.CheckHostName(entry) == UriHostNameType.Dns` correctly rejects
  scheme/port/path/empty/IP-literal (empirically: `https://h`, `host:443`, `a/b`, ``, `127.0.0.1`, `::1`
  all → Unknown/IPv4/IPv6) and accepts legitimate DNS names. **No false negatives on valid hostnames:**
  trailing-dot FQDN (`arch.timewarp.work.`), underscore hosts, IDN punycode (`xn--...`), and mixed case
  all → Dns. A >63-char label → Unknown, but that is an invalid DNS label anyway (spec max 63), so
  rejecting it is correct. No dedup rule — documented as intentional (append can yield harmless
  duplicates; first-match-wins). `NotEmpty()` on the list is justified.
- **Selection (focus 2):** pure static, correct OneOf usage, returns the **canonical allowlist entry**
  (not the request's casing) via `OrdinalIgnoreCase`, no fallback, fail-closed 400 with no host echo.
  RpName/AllowedOrigins flow straight through. Unit tests assert canonical casing, null-host problem, and
  no host echo — all pass.
- **Five handlers (focus 3):** all select via `WebAuthnRelyingPartySelection.Select(GetRequestHost(),
  Options.Value)` and short-circuit on `IsT1` **before** any `ChallengeStore.Issue`/`Consume`. Start
  handlers correctly wrap the problem in `Task.FromResult` (sync `Handle`); complete handlers return
  directly (async). The old inline `new WebAuthnRelyingParty(options.RpId, ...)` is removed from every
  handler (no dead code). AddPasskey's deviation (selection AFTER the auth guard) is genuinely still
  before the challenge consume — it sits immediately after the `Unauthenticated()` check and before
  `TryDecode`/`Consume`; the auth-first reasoning is sound and its Design region documents it.
  Injection and Design-region updates are consistent across all five.
- **Request-host accessor (focus 4):** `IRequestHostAccessor` port in web-application (no ASP.NET dep),
  impl in web-server following the `ICurrentPrincipalAccessor` precedent; `Request.Host.Host` correctly
  strips the port; returns null outside an HTTP request (fail-closed); registered `AddScoped` alongside
  the sibling accessors. Correct.
- **AppHost transforms (focus 5):** `WithTransformUseOriginalHostHeader(true)` applied to the four
  Web.Server-owned literal `/api/*` routes plus the Web.Server catch-all; NOT to the grpc route and NOT
  to the Api.Server catch-all. Correct routes targeted; Design region accurate. (Standalone yarp caveat
  is G1.)
- **Hermetic test host (focus 6):** stripping `JsonConfigurationSource` with `Path == "secrets.json"` is
  the exact match for the user-secrets provider (added by WebApplicationBuilder in Development); iterates
  backwards while mutating (safe); removes only the user-secrets file source, leaving env vars intact.
  Blast radius across all WebApplicationHost suites is safe: CI already runs without a developer
  secrets.json (env-only), so any suite green in CI stays green; the `ApiSecret` placeholder
  (`"Overriden with User Secrets"`) is used symmetrically within the host, so no suite depends on a
  secrets.json-only value. Binding test's `NoUserSecrets_Source_Given_HermeticHost` pins it.
- **Tests (focus 7):** the full-ceremony `Ok_Register_And_Authenticate_Under_Second_Allowed_Host` is
  real, not shallow — `IntegrationSoftwareAuthenticator.BuildAuthenticatorData(SecondHost, ...)` hashes
  the selected RP ID into authenticatorData and `Sign` produces a real ES256 signature over the second
  host's origin; the empty-AllowedOrigins host==selected-RP-ID rule then accepts it. The 400 rejection
  and the X-Forwarded-Host-ignored assertions are meaningful (the latter checks minted `rp.id` stays
  `localhost`). Binding append + no-secrets assertions are correct.
- **Regions / dead code (focus 8):** all three new source files carry `#region Purpose` (TWA0004);
  Design regions on the accessor, selection, options, hermetic host, and updated handlers are accurate
  and reflect the new approach; PasskeysPage credential-scoping note added; 112 runbook workaround
  retired. No stale `RpId` claims — the only remaining `RpId` tokens are intentional history comments in
  web-authn-options.cs and unrelated test-local `RpId = "localhost"` constants. No dead code.

## Summary

- critical: 0
- major: 0
- minor: 1 (G1)
- nit: 1 (G2)

No correctness blockers. Both findings are non-blocking; G1 is a pre-existing standalone-yarp routing
gap surfaced (not introduced) by this change, G2 is a comment/redundancy nit in a test.
