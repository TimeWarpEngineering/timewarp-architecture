# Implement first-party WebAuthn passkey register and authenticate

## Parent

104

## Description

Browser WebAuthn create/get ceremonies for Human principals. Prefer discoverable credentials (sign-in without typing email). Mint Principal on successful register. Issue browser session after authenticate. Works with password managers (e.g. Proton Pass).

## Requirements

- Register creates Principal + passkey credential
- Authenticate proves possession → session
- Challenge/origin binding correct
- Do not require email/username up front (placeholders OK for WebAuthn user.name)

## Checklist

- [x] Registration ceremony API
- [x] Authentication ceremony API
- [x] Session issuance for browser
- [x] Smoke path documented in task Results when done

## Notes

### Implementation plan (2026-07-19)

#### 0. Investigation summary (facts this plan relies on)

- Domain (104-002/027/028 landed): `Principal : Entity<PrincipalId>` (birth Provisional; `RecordCredentialAttached` → Keyed; `IsActive`/`IsQuarantined`), `Credential : Entity<CredentialId>` (immutable Type+Handle, PublicMaterial copy-on-get, one-shot Revoke), `IPrincipalStore` snapshot-on-get + `ConcurrencyConflictException` on Update*, `AddCredentialAsync` auto-promotes STORED principal Provisional→Keyed. `[TypedId]` ids are contract-safe (STJ converters).
- **No handler in this task needs Update***: registration uses AddCredentialAsync auto-promote; authentication (no sign-counter persistence) is read-only. The 104-028 conflict-contract pattern lands as a documented non-use; first real retry-policy callsite is 104-005 revoke. Do not invent a domain field to demo the pattern.
- Web-server auth today: dormant Entra registration (program lock #10); NO UseAuthentication/UseAuthorization in the pipeline — a named cookie scheme adds cleanly.
- Test host origin fixed: `https://localhost:7000` (WebTestServerApplication); dev uses `https://localhost:63611`. RP config must cover both.
- `System.Formats.Cbor` ships in ASP.NET Core 10 shared framework and as a Microsoft/MIT NuGet. ASP.NET Core 10 Identity built-in passkey types are welded to UserManager/SignInManager — rejected (inverts 104-002's Principal model).
- Template flags: app-layer additions under `source/container-apps/web/**` (already `!web`-excluded); timewarp-identity consumed as published package in generated apps → every consuming csproj needs dual-mode reference + CPM pin (accepted publish-ordering cost). No new template `#if` regions.
- web-contracts has empty scaffolded `features/auth/commands/`; SPA legacy PasswordlessService + RegisterPasskey.razor are reference-only — leave untouched.

#### 1. CENTRAL DECISION — hand-rolled minimal verifier (recommended; not an open fork)

**Hand-roll a minimal WebAuthn verifier inside timewarp-identity using BCL crypto + System.Formats.Cbor. Do not adopt Fido2NetLib. Do not adopt ASP.NET Core Identity passkeys.**

- Published-package dependency surface: TimeWarp.Identity is a trust kernel; System.Formats.Cbor is Microsoft/MIT/shared-framework (BCL-adjacent per the 104-028 foundation-domain precedent); Fido2NetLib would be a permanent public dependency.
- Repo precedent: 104-027 rejected StronglyTypedId on the same test — the library's main value (attestation formats, MDS trust chains) is exactly what the template posture doesn't use.
- Template posture bounds scope: attestation requested "none", attStmt IGNORED regardless of fmt (documented); ES256 (-7) required, RS256 (-257) accepted; no MDS/extensions/EdDSA. Registration verification = structural parsing + challenge/origin/rpIdHash binding. The only crypto verify in the task is assertion: `ECDsa.VerifyData(authData‖SHA256(clientDataJSON), sig, Rfc3279DerSequence)` (or RSA Pkcs1/SHA-256).
- Mitigations: adversarial negative-path unit vectors first-class; challenge one-time-consumed before verify; BCL `Base64Url`.
- **Revisit trigger (Design region + Notes):** if a deployment needs attestation policy/MDS, adopt Fido2NetLib at the HOST layer (feed it the stored COSE key), not inside TimeWarp.Identity.
- Seam is library-internal — swapping the verifier later moves no contracts/handlers/endpoints.

#### 2. Where the pieces live

| Piece | Location |
|---|---|
| Verifier + ceremony primitives + challenge store port | `source/libraries/timewarp-identity/ceremonies/webauthn/` (+PackageReference System.Formats.Cbor) |
| Contracts | `web-contracts/features/identity/` (commands/, queries/) + dual-mode identity ref |
| Handlers | `web-application/features/identity/` (+ dual-mode identity ref) |
| Endpoints | `web-server/features/identity/` (BaseEndpoint shims) |
| Session (cookie) | web-server services/ + Program; `IBrowserSessionService` port in web-application |
| DI (stores) | `web-infrastructure-module.cs`: singletons InMemoryPrincipalStore, InMemoryWebAuthnChallengeStore |
| SPA | `web-spa/source/features/web-authn.ts` (→ wwwroot/js, window.Spa.WebAuthn plain object) + `features/identity/` demo page |

#### 3. Library design (`ceremonies/webauthn/`)

```csharp
public sealed record WebAuthnRelyingParty(string Id, string Name, IReadOnlyList<string> Origins);
// Origin: exact match vs Origins; empty Origins ⇒ accept https origins whose host == Id (dev fallback; documented).

public enum WebAuthnCeremonyType { None = 0, Registration = 1, Authentication = 2 }
public interface IWebAuthnChallengeStore
{
  byte[] Issue(WebAuthnCeremonyType ceremonyType);                       // 32 random bytes, TTL-recorded
  bool TryConsume(WebAuthnCeremonyType ceremonyType, byte[] challenge);  // one-time; false = unknown/expired/wrong type
}
public sealed class InMemoryWebAuthnChallengeStore : IWebAuthnChallengeStore
{ public InMemoryWebAuthnChallengeStore(TimeProvider? timeProvider = null, TimeSpan? timeToLive = null, int maxEntries = 10_000); }
// keyed by base64url(challenge); prune on Issue; hard cap evicts oldest (cheap DoS bound; real rate limiting 104-015)

public static class WebAuthnRegistration
{
  public static string BuildOptionsJson(WebAuthnRelyingParty rp, byte[] challenge, byte[] userHandle, string userName, string userDisplayName);
  // pubKeyCredParams [-7,-257]; authenticatorSelection { residentKey:"preferred", userVerification:"preferred" }; attestation "none"; timeout 60000
  public static WebAuthnRegistrationResult Verify(WebAuthnRelyingParty rp, byte[] expectedChallenge, byte[] clientDataJson, byte[] attestationObject, byte[] credentialId);
}
public sealed class WebAuthnRegistrationResult
{ public bool IsValid; public WebAuthnFailureReason FailureReason; public byte[] CredentialId; public byte[] CosePublicKey; } // copy-on-get

public static class WebAuthnAuthentication
{
  public static string BuildOptionsJson(WebAuthnRelyingParty rp, byte[] challenge); // allowCredentials: [] (discoverable-first)
  public static WebAuthnAssertionResult Verify(WebAuthnRelyingParty rp, byte[] expectedChallenge, byte[] storedCosePublicKey, byte[] clientDataJson, byte[] authenticatorData, byte[] signature);
}
public static bool TryReadChallenge(byte[] clientDataJson, out byte[] challenge); // minimal public wrapper
```

Internals (all internal, tested via public surfaces): `client-data.cs` (STJ parse: type webauthn.create/get, challenge Base64Url compare, origin rule), `authenticator-data.cs` (binary parse: rpIdHash=SHA256(rp.Id), flags UP required / UV read-not-required / AT for attested data, signCount parsed-IGNORED (synced passkeys report 0; no counter on Credential; revisit 104-005/006), aaguid, credentialId ≤1023, credentialPublicKey CBOR), `cose-key.cs` (CborReader COSE_Key: EC2/RSA, alg -7/-257, P-256, x/y → import-validated at registration; attestationObject map {fmt, attStmt, authData} — attStmt ignored for every fmt).

#### 4. Challenge + session design

**Challenge:** 32 random bytes (RandomNumberGenerator), stored keyed by own base64url value + ceremony type, 5-min TTL, strictly one-time. No challenge id in contracts — finish request's clientDataJSON carries it; handler extracts, consumes BEFORE verify (replay-safe); unknown/expired/reused/wrong-type → 400. Single-instance semantics documented (distributed store out of scope).

**Session:** minimal named cookie scheme, no framework:
- `web-server/configuration/identity-session-defaults.cs`: Scheme `identity-session`, CookieName `.timewarp.identity.session`, PrincipalIdClaimType `timewarp:principal_id`.
- ConfigureServices: `AddAuthentication().AddCookie(Scheme, ...)` — HttpOnly, Secure Always, SameSite Lax, Sliding 24h, Events → 401/403 status codes not redirects. Named scheme leaves dormant Entra default untouched (lock #10 / 104-021).
- ConfigureMiddleware: `UseAuthentication(); UseAuthorization();` between UseRouting and UseAntiforgery.
- web-application port: `IBrowserSessionService { Task IssueAsync(PrincipalId, string? displayName, CancellationToken); Task<PrincipalId?> GetCurrentPrincipalIdAsync(CancellationToken); }`
- web-server impl `services/cookie-browser-session-service.cs` (IHttpContextAccessor + SignInAsync/AuthenticateAsync), scoped.
- CSRF: ceremony endpoints anonymous (they establish the session), antiforgery n/a; GetCurrentSession is a read; SameSite=Lax covers template posture. Agent tokens are 104-004.

#### 5. Contract shapes (`web-contracts/features/identity/`, namespace TimeWarp.Architecture.Features.Identity)

web-contracts.csproj + web-application.csproj gain dual-mode TimeWarp.Identity reference; CPM pin TimeWarp.Identity added (publish-ordering recorded).

All `public static partial class` + nested Command/Query, Response, Validator; all ceremony ops POST; no I*Details; base64url FORMAT validation in handler/library, not FluentValidation regex.

| Operation | Route | Request | Response |
|---|---|---|---|
| StartPasskeyRegistration | POST api/identity/passkey/register/options | (empty) | string OptionsJson |
| CompletePasskeyRegistration | POST api/identity/passkey/register | string CredentialId, ClientDataJson, AttestationObject (base64url, NotEmpty + 64KB max) | PrincipalId PrincipalId |
| StartPasskeyAuthentication | POST api/identity/passkey/authenticate/options | (empty) | string OptionsJson |
| CompletePasskeyAuthentication | POST api/identity/passkey/authenticate | string CredentialId, ClientDataJson, AuthenticatorData, Signature; string? UserHandle (unused, documented) | PrincipalId PrincipalId |
| GetCurrentSession | GET api/identity/session | (empty Query) | bool IsAuthenticated, PrincipalId? PrincipalId |

Files: commands/start-passkey-registration.cs, complete-passkey-registration.cs, start-passkey-authentication.cs, complete-passkey-authentication.cs, queries/get-current-session.cs. No mock factories (documented opt-out).

**Registration identity:** no email/username. user.name/displayName = "TimeWarp user" placeholder; user.id = 32 random bytes per options call; userHandle opaque, not persisted; account resolution is credential-handle-based (FindCredentialByHandleAsync); Principal minted at VERIFY time (abandoned ceremonies create nothing).

#### 6. Handlers (`web-application/features/identity/`)

- Start handlers: `Issue(type)` → BuildOptionsJson (RP from IOptions<WebAuthnOptions>).
- complete-passkey-registration-handler: decode (Base64Url.TryDecodeFromChars; fail → 400) → TryReadChallenge + TryConsume(Registration) BEFORE verify (fail → 400 "challenge expired or already used") → WebAuthnRegistration.Verify (fail → 400 + reason) → FindCredentialByHandleAsync exists → 409 → Principal.Create(Human) → AddPrincipalAsync → Credential.Create(id, Passkey, credentialId, cosePublicKey) → AddCredentialAsync (auto-promote; in-hand instance stale by design) → IssueAsync → Response(principal.Id).
- complete-passkey-authentication-handler: decode → consume Authentication challenge → FindCredentialByHandleAsync (null → 400 generic "authentication failed", no enumeration oracle) → IsRevoked → 400 → GetPrincipalAsync → !IsActive → 403 → WebAuthnAuthentication.Verify → fail → 400 → IssueAsync → Response.
- get-current-session-handler: GetCurrentPrincipalIdAsync → Response.
- **Concurrency note in each handler Design region:** deliberately zero Update* calls; 104-005 revoke owns the first catch-ConcurrencyConflictException-reload-retry-once policy.

Endpoints (`web-server/features/identity/`): feature-annotations.cs + five one-line shims mirroring create-role-endpoint.cs.

#### 7. Server configuration + DI

- `web-server/configuration/web-authn-options.cs` + validator (mirror SampleOptions + AddFluentValidatedOptions): RpId (default "localhost"), RpName, AllowedOrigins (empty ⇒ https+host==RpId fallback). Defaults in appsettings.json; test host covered by fallback.
- web-infrastructure-module.cs: AddSingleton IPrincipalStore/InMemoryPrincipalStore, IWebAuthnChallengeStore/InMemoryWebAuthnChallengeStore (+ dual-mode identity ref); update its "empty by design" Design region.
- Program: cookie scheme + middleware; scoped IBrowserSessionService.

#### 8. SPA (minimal demo; full CTA UX is 104-016)

- `web-spa/source/features/web-authn.ts`: plain object WebAuthn { IsSupported(); CreateCredential(optionsJson): Promise<string> (parseCreationOptionsFromJSON → navigator.credentials.create → toJSON); GetCredential(optionsJson) }. Register in spa.ts → window.Spa.WebAuthn. TS → wwwroot/js pipeline.
- `web-spa/features/identity/pages/passkeys-page/PasskeysPage.razor(.cs)`: counter-page pattern; Create passkey / Sign in buttons; GetCurrentSession state; FluentUI + CSS isolation; SimpleAlert errors; mock mode shows "not supported" (documented).

#### 9. Test plan

**Unit — tests/libraries/timewarp-identity-tests/ceremonies/** (csproj +System.Formats.Cbor):
- infrastructure/software-authenticator.cs: deterministic ES256 fixture key; builds spec-correct attestationObject (fmt "none", zero AAGUID, COSE EC2) and signs assertions; one RS256 path.
- webauthn-registration-tests: happy; wrong type/challenge/origin; rpIdHash mismatch; UP clear; AT clear; credentialId mismatch; unsupported alg (-8); malformed CBOR; fmt "packed" with garbage attStmt ACCEPTED (locks posture).
- webauthn-authentication-tests: happy ES256+RS256; tampered signature/authData; wrong challenge/origin/rpIdHash; UP clear rejected; UV clear accepted; signCount 0 and regressing both pass.
- in-memory-webauthn-challenge-store-tests: one-time; wrong type; TTL via fake TimeProvider; cap eviction.
- Base64url/TryReadChallenge edges.

**Contracts — web-contracts-tests**: round-trips for the three Responses (typed-id + ctor shapes) + one Command.

**Integration — web-server-integration-tests/Features/Identity/** (happy AND rejection per DoD):
- Infrastructure/integration-software-authenticator.cs (dynamic-challenge sibling; small deliberate duplication).
- Passkey_Registration_Tests: options→emulator→complete → 200 + Set-Cookie + principalId; GetCurrentSession with cookie → authenticated. Rejections: reused challenge 400; wrong origin 400; empty CredentialId → validator 400; duplicate handle 409.
- Passkey_Authentication_Tests: register-then-auth happy (session); unknown credential 400; bad signature 400; reused challenge 400.

**Deferred:** Playwright virtual-authenticator e2e → 104-006/104-022; TimeProvider in domain Create → 104-006 (D5); manual Proton Pass smoke → Results + 104-016.

#### 10. Ordered work items

1. CPM + csproj wiring (System.Formats.Cbor, TimeWarp.Identity pins; dual-mode refs).
2. Library ceremonies (§3).
3. Unit tests (§9) alongside 2; software authenticator first.
4. Contracts (§5) + round-trip tests.
5. Session plumbing (§4).
6. Handlers + endpoints + WebAuthnOptions + DI (§6/§7).
7. Integration tests.
8. SPA (§8).
9. Closeout: dev build 0/0; dev test; Design regions reconciled (Fido2NetLib revisit trigger, signCount rationale, single-instance challenge store, publish ordering); checklist; smoke path in Results.

Sequencing: 1→2/3 → 4 → 5/6 → 7 → 8.

#### 11. Scope boundaries — explicitly NOT in this task

No EF store; no Update* callsites/retry policy (104-005); no sign-counter field; no attestation verification/trust chains/MDS/EdDSA; no agent keys/tokens (104-004); no list/revoke (104-005); no rate limiting (104-015); no CTA/login rework (104-016); no browser e2e (104-006/022); no progressive profile (104-024); no logout/account mgmt; no Entra/MSAL changes; no removal of legacy Passwordless code (104-016/021); no mock-mode passkey factories; no distributed challenge store.

#### 12. Open Questions

None unresolvable. Two maintainer-ack items with committed defaults: (a) hand-rolled verifier over Fido2NetLib (§1); (b) web-contracts dual-mode TimeWarp.Identity reference for typed PrincipalId responses (104-027 intent).

Legacy Passwordless.dev in SPA is reference only — first-party is the goal.

### Depends on

104-002
104-027 (TypedId source generator — id JsonConverter closes a fail-open STJ gap; do not put PrincipalId/CredentialId in contracts before it lands)
104-028 (concurrency token on identity entities + store port — supersedes the D6 LWW deferral; do not write handlers against IPrincipalStore before Update* conflict semantics land)

## Results

### Implementation
- Commits: 56882153 (implementation), d2c16a74 (round-1 fixes M1–M8), d75af08b (round-2 fix M9 + sibling gap).
- ceremonies/webauthn in TimeWarp.Identity: hand-rolled minimal verifier (BCL crypto + System.Formats.Cbor; attestation-none posture — attStmt ignored all fmts; ES256 required, RS256 ≥2048-bit accepted; signCount parsed-ignored, documented). One-time 32-byte CSPRNG challenge store (TTL, type-keyed, consume-before-verify). Fido2NetLib rejected with recorded host-layer revisit trigger.
- Five contracts (features/identity/): StartPasskeyRegistration, CompletePasskeyRegistration, StartPasskeyAuthentication, CompletePasskeyAuthentication (POST), GetCurrentSession (GET) — typed PrincipalId responses (104-027 intent); size caps on all payload fields.
- Handlers (web-application) with replay-safe ordering, no account-enumeration oracle, post-verify quarantine check, duplicate-credential race → 409, documented no-Update* rule (first retry-policy callsite is 104-005). WebAuthnOptions in web-application (layering deviation, documented).
- Named identity-session cookie scheme (HttpOnly/Secure/Lax, 401/403 events, dormant Entra untouched); IBrowserSessionService port + cookie impl.
- SPA: window.Spa.WebAuthn TS interop + PasskeysPage demo (mock mode → graceful 501 fallback).
- Deviation: WebAuthnOptions/Validator in web-application/configuration (handlers consume it; web-application must not reference web-server).

### Review (Phase 4b)
- Effort 2: general + security reviewers (security warranted by hand-rolled verifier). Round 1: 8 merged findings (1 bug: config section never bound; 5 suggestions incl. pre-verify quarantine oracle, dup-registration race → 500+orphan, weak RSA acceptance; 2 nits) — 2 found independently by both reviewers; all fixed (d2c16a74). Round 2 (security re-verify): M1–M8 confirmed, 0 reopened; orphan-acceptance posture judged sound; 1 new bug M9 (empty RSA modulus crash from the M5 fix) — fixed along with a second independently discovered crash vector (empty exponent) found by neighborhood audit; EC path verified safe (d75af08b).
- Final: 9 findings, 9 fixed, 0 open, 0 wontfix. Disposition: clean (review/disposition.md). Security verdict on crypto core: sound (correct §7.1/§7.2 steps, algorithm bound to stored key, server-sourced challenges).
- Artifacts: review/review-framework.md, round-1/{general,security,merged}.md, round-2/{security,merged}.md, disposition.md.

### Verification
- dev build 0/0. timewarp-identity-tests 127/127 (was 90; +37 ceremony/vector), web-contracts-tests 14/14 (7 identity round-trips), web-server-integration-tests 34/1 skipped (12 identity: happy paths, reused-challenge, wrong-origin, validator, duplicate, unknown-credential, tampered-signature, options-binding, oversized-field). Full regression sweep green. Docker-dependent suites not runnable (pre-existing env issue).

**Smoke path** (manual, matches what `Passkey_Registration_Tests`/`Passkey_Authentication_Tests` assert
end-to-end over real HTTP): POST `api/identity/passkey/register/options` (anonymous) → returns
`OptionsJson` (PublicKeyCredentialCreationOptionsJSON, ES256+RS256, attestation "none") → browser (or
the software authenticator emulator) answers with `CredentialId`/`ClientDataJson`/`AttestationObject`
(all base64url) → POST `api/identity/passkey/register` → 200 + `Set-Cookie: .timewarp.identity.session`
+ `PrincipalId` → GET `api/identity/session` (cookie attached) → `IsAuthenticated: true` with the same
`PrincipalId`. Authentication mirrors this via `.../authenticate/options` and `.../authenticate`. The
`/Passkeys` SPA page (`web-spa/features/identity/pages/passkeys-page/`) exercises the identical flow
through `window.Spa.WebAuthn` + real `navigator.credentials.create/get` in a browser.

### Deferred
- Orphaned Provisional principal on duplicate-registration race (security-reviewed sound; store delete is 104-005). Playwright virtual-authenticator e2e → 104-006/104-022. Rate limiting → 104-015. CTA/login UX + legacy Passwordless removal → 104-016/021. Agent keys/tokens → 104-004. Distributed challenge store, EF store → later waves. Publish ordering: TimeWarp.Identity + Foundation.Domain package releases needed for package-mode consumers.
- Unblocks: 104-004, 104-005, 104-006.

## Session

- Created: 2026-07-16
- Implementation + review: ses 78b9f414 (2026-07-19), build agent a2ef2354b0fc976e7, reviewers a31739ea747756530 (general) + security-reviewer-104-003
