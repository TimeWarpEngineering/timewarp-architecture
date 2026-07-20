# Implement agent public-key registration and scoped API tokens

## Parent

104

## Description

Agents register a public key (or equivalent) without a browser ceremony. Receive short-lived scoped bearer (or similar) tokens — not cookie sessions. Machine-readable errors.

## Requirements

- Register Agent principal + key
- Issue/validate scoped tokens with expiry
- No human sponsor required at registration

## Checklist

- [x] Register endpoint/handler
- [x] Token issue + validate
- [x] Tests for happy path + reject bad key/token

## Notes

### Implementation plan (2026-07-20)

#### 0. Investigation summary (facts this plan relies on)

- **Domain sufficient as-is.** `CredentialType.AgentKey` exists; Credential needs NO new fields — SPKI DER is self-describing (DER AlgorithmIdentifier encodes the algorithm). `Principal.Create(Agent)` works; `AddCredentialAsync` auto-promotes Provisional → Keyed kind-agnostically. **Zero Update\* calls in this task** — first ConcurrencyConflictException retry callsite remains 104-005.
- **Ed25519 is NOT in the .NET 10 BCL** (verified vs Microsoft.NETCore.App.Ref 10.0.10 — only composite ML-DSA identifiers). Supporting it needs NSec/BouncyCastle → forbidden.
- **JwtBearer is NOT in the ASP.NET Core shared framework** (verified vs Microsoft.AspNetCore.App.Ref 10.0.10; only BearerToken ships in-box). JWT costs a package AND signing-key management.
- **Ingress**: `/api/identity/{**catch-all}` route already exists (fe722050) — staying under `/api/identity/agent/*` needs NO ingress change (task 107 gotcha avoided).
- **Host split**: identity DI in web-infrastructure-module; identity-session scheme wired in web-server program.cs. Nothing in 104 backlog binds token validation to api-server → agent endpoints + bearer validation land on web-server beside 104-003.
- Patterns to mirror: 104-003 contracts/handlers/endpoint shims/options (section name == type name!)/fixtures/ports.

#### 1. Decision — key format + proof of possession

**ECDSA P-256 (ES256), wire = base64url SPKI DER, DER (RFC 3279) signatures. Single algorithm Wave 1.**
- Not Ed25519 (no BCL; revisit trigger in Design region). Not COSE (agent SDKs don't speak CBOR; SPKI is native output of openssl/Python/Node/Go/WebCrypto). Not JWK (ImportSubjectPublicKeyInfo is one BCL call + on-curve validation; wrap with M5/M9 guards: empty/oversize BEFORE import, trailing-DER reject via bytesRead == length, curve OID must be P-256).
- Signature: DER Rfc3279DerSequence only (P1363 rejected — no dual-format malleability).
- Key id (Credential.Handle) = SHA-256(SPKI DER), server-computed, 32 bytes → duplicate registration is natural 409; agent echoes KeyId (base64url) at token time. PublicMaterial = raw SPKI DER.
- **Proof of possession (both ceremonies)**: one-time 32-byte challenge; agent signs UTF8(prefix) ‖ challenge with domain-separated prefixes:
  - "TimeWarp.Identity.AgentKey.Register.v1:"
  - "TimeWarp.Identity.AgentKey.Token.v1:"
  Domain separation + ceremony-typed one-time challenges = no cross-endpoint replay.

#### 2. Decision — token design

**Opaque reference tokens validated against server-side `IAgentTokenStore`. No JWT, no per-request self-signing.**
- JWT costs package + key mgmt for zero Wave-1 benefit (one validating host, no third-party audience). Opaque matches in-memory posture, zero key mgmt, LIVE-REVOCABLE — x402 needs it (104-013 quota at settle time) and quarantine cuts off issued tokens immediately (validation re-reads principal); a JWT survives quarantine for its lifetime.
- **At-rest**: store keyed by SHA-256(token), never raw bearer.
- **Expiry 15 min (configurable), no refresh tokens** — refresh IS the token ceremony (re-sign a fresh challenge). Documented.
- **Scopes**: plain string list; constants `identity:read` (this task's protected endpoint) + `demo:invoke` (reserved; named consumer 104-011; enables real 403 insufficient-scope test). Keyed tier may hold both; 104-013 gates demo:invoke USAGE by credits, not scope removal (recorded, revisitable).
- Revisit trigger: multi-instance/third-party validator → distributed store or signed tokens at host layer; port seam keeps contracts/handlers fixed.

#### 3. Library additions (source/libraries/timewarp-identity/) — nothing 104-003-breaking

Only touched existing file: in-memory-webauthn-challenge-store.cs (behavior-preserving delegation refactor).

### 3a. ceremonies/agent-key/
```csharp
public enum AgentKeyCeremonyType { None = 0, Registration = 1, TokenIssuance = 2 }
public interface IAgentKeyChallengeStore { byte[] Issue(AgentKeyCeremonyType t); bool TryConsume(AgentKeyCeremonyType t, byte[] challenge); }
public sealed class InMemoryAgentKeyChallengeStore(TimeProvider? = null, TimeSpan? ttl = null, int maxEntries = 10_000);
public enum AgentKeyFailureReason { None = 0, MalformedPublicKey = 1, UnsupportedAlgorithm = 2, SignatureInvalid = 3 }
public static class AgentKeyProof {
  public static byte[] BuildSignedData(AgentKeyCeremonyType, byte[] challenge);  // public: fixtures/SDKs share exact construction
  public static AgentKeyProofResult Verify(AgentKeyCeremonyType, byte[] publicKeySpki, byte[] challenge, byte[] signature); }
public sealed class AgentKeyProofResult { bool IsValid; AgentKeyFailureReason FailureReason; }
public static class AgentPublicKey { public static bool TryParse(byte[] spkiDer, out byte[] keyId); }
// guards: empty, >2KB, trailing DER, curve != P-256 OID; keyId = SHA256(spki)
```
Verify internals: guard empties first (never throw on adversarial input), TryParse, ImportSubjectPublicKeyInfo, VerifyData(SHA256, Rfc3279DerSequence), cose-key.cs try/dispose/catch shape.

**Challenge-store generalization (minimal)**: extract body → `internal sealed class InMemoryChallengeStoreCore<TCeremonyType> where TCeremonyType : struct, Enum` (ceremonies/in-memory-challenge-store-core.cs); WebAuthn store keeps exact public surface, delegates; agent store = second ~15-line wrapper. IWebAuthnChallengeStore/WebAuthnCeremonyType untouched. Fallback if review objects: duplicate ~90 lines in sibling store.

### 3b. tokens/
```csharp
public static class AgentScopes { const string IdentityRead = "identity:read"; const string DemoInvoke = "demo:invoke"; IReadOnlyList<string> All; bool IsKnown(string); }
public sealed record AgentTokenGrant(PrincipalId PrincipalId, IReadOnlyList<string> Scopes, DateTimeOffset ExpiresAt);
public interface IAgentTokenStore {
  string Issue(PrincipalId, IReadOnlyCollection<string> scopes, TimeSpan lifetime);  // base64url(32 CSPRNG); stores SHA256(token) → grant
  AgentTokenGrant? Validate(string token); }                                          // null uniformly
public sealed class InMemoryAgentTokenStore(TimeProvider? = null, int maxEntries = 100_000);  // prune-on-Issue + evict cap
```
Design regions: hash-at-rest; single-instance semantics; no revoke API Wave 1 (quarantine cutoff at validation; per-credential revoke = 104-005); sync methods per challenge-store precedent.

#### 4. Contracts (web-contracts/features/identity/)

Four commands + one query, passkey Start/Complete shapes, size-capped from day one:

| Operation | Route | Request | Response |
|---|---|---|---|
| StartAgentKeyRegistration | POST api/identity/agent/register/options | (empty) | string Challenge |
| CompleteAgentKeyRegistration | POST api/identity/agent/register | PublicKey (b64url SPKI ≤2KB), Challenge (≤256), Signature (≤1KB), Label? (≤64) | PrincipalId, string KeyId |
| StartAgentTokenIssuance | POST api/identity/agent/token/options | (empty) | string Challenge |
| CompleteAgentTokenIssuance | POST api/identity/agent/token | KeyId (≤256), Challenge (≤256), Signature (≤1KB), List<string> Scopes (≤16 × ≤64) | AccessToken, TokenType "Bearer", ExpiresInSeconds, Scopes, PrincipalId |
| GetAgentIdentity | GET api/identity/agent/me | (empty Query) | PrincipalId, Kind, TrustTier, Scopes |

Files: commands/start-agent-key-registration.cs, complete-agent-key-registration.cs, start-agent-token-issuance.cs, complete-agent-token-issuance.cs, queries/get-agent-identity.cs. Design regions: explicit challenge travel, DER-only signatures, refresh-is-reissuance. No mock factories (documented opt-out).

#### 5. Handlers (web-application/features/identity/)

- Start handlers: Issue(type) → base64url → Response.
- complete-agent-key-registration-handler: decode (reuse WebAuthnPayloadDecoder) → **TryConsume(Registration) BEFORE verify** → AgentPublicKey.TryParse (400 machine-readable; enumeration-safe pre-account) → AgentKeyProof.Verify(Registration) (400) → FindCredentialByHandleAsync(AgentKey, keyId) non-null → 409 → Principal.Create(Agent) → AddPrincipalAsync → Credential.Create(pid, AgentKey, keyId, spkiDer, label) → AddCredentialAsync (auto-promote) → Response. No sponsor, no cookie.
- complete-agent-token-issuance-handler: decode → consume TokenIssuance challenge → validate scopes (unknown → 400 invalid_scope listing allowed) → FindCredentialByHandleAsync; null OR revoked → generic 400 (no oracle) → GetPrincipalAsync null → same 400 → AgentKeyProof.Verify(TokenIssuance) fail → same 400 → **quarantine AFTER Verify**: !IsActive → 403 (possession proven; same reviewed posture as passkey auth) → IAgentTokenStore.Issue(id, scopes, lifetime from IOptions<AgentTokenOptions>) → Response.
- get-agent-identity-handler: reads new port `IAgentCallerContext` (web-application/abstractions/i-agent-caller-context.cs: `AgentCaller? GetCurrentCaller();` record AgentCaller(PrincipalId, IReadOnlyList<string> Scopes)) → null → 401-shaped (defense-in-depth) → GetPrincipalAsync → Response.
- web-application/configuration/agent-token-options.cs + validator: TokenLifetimeMinutes = 15 (1–60). **Section name `AgentTokenOptions`** — binding regression test like WebAuthnOptions_Binding_Tests.
- Design regions record zero-Update* rule.

#### 6. web-server: scheme, validation, endpoints, DI

- configuration/agent-token-defaults.cs: Scheme "agent-token"; ScopeClaimType "timewarp:scope"; IdentityReadPolicy "agent-scope:identity:read"; principal-id claim reuses IdentitySessionDefaults.PrincipalIdClaimType.
- services/agent-token-authentication-handler.cs : AuthenticationHandler<AuthenticationSchemeOptions>:
  - HandleAuthenticateAsync: no header → NoResult; Bearer → IAgentTokenStore.Validate; null → Fail; GetPrincipalAsync; null or !IsActive → Fail (quarantine = live cutoff; explicit 403 arrives at next issuance — mapping documented). Success → claims: principal id + one timewarp:scope claim per scope.
  - HandleChallengeAsync: 401 + WWW-Authenticate: Bearer error="invalid_token" (bare Bearer when absent) + problem+json (RFC 6750).
  - HandleForbiddenAsync: 403 + error="insufficient_scope" + problem+json.
- program.cs: AddScheme on existing AddAuthentication chain (Entra untouched, lock #10); AddAuthorizationBuilder policy IdentityReadPolicy (scheme agent-token, RequireAuthenticatedUser, RequireClaim scope identity:read); AddFluentValidatedOptions<AgentTokenOptions>; appsettings AgentTokenOptions section.
- services/agent-caller-context.cs: IAgentCallerContext over IHttpContextAccessor (scoped).
- features/identity/: five shims; ceremony endpoints anonymous; get-agent-identity-endpoint carries [Authorize(Policy = IdentityReadPolicy)] — end-to-end proof of validate + scope enforcement.
- web-infrastructure-module.cs: singletons IAgentKeyChallengeStore, IAgentTokenStore.

#### 7. Test plan

- **Fixture** ceremonies/infrastructure/software-agent-key.cs: fixed P-256 keypair literals (no RNG; second fixed key for wrong-key vectors); SpkiPublicKey, KeyId, Sign(type, challenge); canned bad material: RSA SPKI, P-384 SPKI, truncated DER, trailing bytes, empty arrays.
- **Unit**: agent-public-key-tests (happy; empty/oversized/truncated/trailing/RSA/P-384 rejected; keyId = SHA-256); agent-key-proof-tests (happy both types; tampered sig; wrong challenge; **cross-ceremony replay fails**; wrong key; empties never throw; P1363 rejected); in-memory-agent-key-challenge-store-tests (one-time, wrong type, TTL FakeTimeProvider, cap; WebAuthn store tests re-run to pin refactor); tokens/in-memory-agent-token-store-tests (round-trip, expiry, unknown/garbage/empty → null, scopes copied, cap).
- **Contracts**: round-trips for ceremony Responses + token Response + one Command.
- **Integration**: Infrastructure/integration-software-agent-key.cs; Agent_Registration_Tests (happy → PrincipalId+KeyId; reused challenge 400; tampered sig 400; malformed key 400; duplicate key 409; caps 400); Agent_Token_Tests (register→token happy → Bearer + ExpiresIn≈900 + scopes; unknown KeyId and bad sig → identical generic 400; unknown scope → 400 invalid_scope; reused challenge 400); Agent_Protected_Endpoint_Tests (identity:read token → GET agent/me 200 Kind Agent/TrustTier Keyed; no header → 401 + WWW-Authenticate + problem+json; garbage token → 401 invalid_token; demo:invoke-only token → 403 insufficient_scope; cookie-session request → 401 scheme isolation). Token-expiry E2E stays unit-level (documented).

#### 8. Ordered work items

1. Library ceremonies/agent-key/ + challenge-store core refactor + tokens/ (§3).
2. Unit tests + fixture (§7); confirm existing 127 identity tests green (refactor pin).
3. Contracts (§4) + round-trips.
4. Handlers + AgentTokenOptions + IAgentCallerContext (§5).
5. web-server scheme/handler/policy/endpoints/DI/appsettings (§6).
6. Integration tests (§7).
7. Closeout: dev build 0/0; full sweep; Design regions (Ed25519 revisit, opaque-vs-JWT rationale + distributed revisit, quarantine 401-at-validation/403-at-issuance mapping, refresh-is-reissuance, scope-vs-credit note for 104-013); curl smoke sequence in Results (feeds 104-014/104-017).

#### 9. Scope boundaries — NOT in this task

No payment/402/quota (104-008..014); no Funded promotion/credit claims (104-013); no rate limiting (104-015); no key/token revoke/list APIs, no Update*/retry (104-005); no api-server bearer validation; no JWT/JWKS; no refresh tokens; no Ed25519/secp256k1/RSA agent keys; no distributed stores; no SPA surface; no discovery docs (104-017); no Entra changes.

#### 10. Open Questions

None unresolvable. Committed-default acks: (a) opaque store-backed tokens over JWT; (b) demo:invoke scope reserved now for 104-011/013.

Paid elevation is Wave 3 (013–014). Here: Keyed agent can exist.

### Depends on

104-002

## Results

### Implementation
- Commits: 16beaa46 (implementation), 5960a942 (round-1 fixes M1-M5 + comment hygiene).
- ceremonies/agent-key in TimeWarp.Identity: ES256 (P-256 by OID) over base64url SPKI DER, DER-only signatures, proof-of-possession with domain-separated prefixes (Register.v1/Token.v1 — cross-ceremony replay impossible, tested both directions); shared InMemoryChallengeStoreCore extracted (WebAuthn store public surface unchanged, 127 prior tests pin the refactor). Ed25519 rejected (absent from .NET 10 BCL — revisit trigger recorded).
- tokens/: opaque store-backed scoped bearer grants — 32-byte CSPRNG, SHA-256 at rest, 15-min default expiry (configurable AgentTokenOptions, binding-tested), no refresh tokens (refresh = re-run ceremony); scopes identity:read + reserved demo:invoke (104-011/013 consumers). JWT rejected (package + key mgmt, not live-revocable). Store owns expiry/grants only; liveness/quarantine is the caller's (explicit 104-013 warning in port docs).
- Five contracts under /api/identity/agent/* (no ingress change needed); handlers mirror passkey posture: consume-before-verify, no enumeration oracle (Title-pinned), quarantine 403 post-verify at issuance / silent 401 at bearer validation; duplicate key → 409; scope canonicalization (ordinal Distinct); zero Update* calls (104-005 remains first retry consumer).
- Named agent-token AuthenticationHandler + agent-scope:identity:read policy; RFC 6750 WWW-Authenticate + problem+json on 401/403; Entra/cookie schemes untouched; GET /api/identity/agent/me proves validate + scope enforcement end-to-end.

### Review (Phase 4b)
- Effort 2 (general + security, same roster as 104-003). Round 1: 5 merged findings — 2 bugs (null-scopes → validator NRE → 500, found independently by BOTH reviewers, fixed three-layered with cross-command audit confirming single occurrence repo-wide; token-store quarantine-cutoff doc misattribution, load-bearing for 104-013), 1 suggestion, 2 nits — all fixed (5960a942). 0 open, 0 wontfix. Disposition: clean (review/disposition.md).
- Security verdict: no auth bypass, no verification gap; all 104-003 M-series lessons verified applied from day one.
- Artifacts: review/review-framework.md, round-1/{general,security,merged}.md, disposition.md.

### Verification
- dev build 0/0. timewarp-identity-tests 168/168 (was 127; +41), web-contracts-tests 21/21 (+7), web-server-integration-tests 53/1 skipped (+19 incl. null-scopes, scheme-isolation, options-binding). Regression sweep green. Docker-dependent suites not runnable (pre-existing env issue).

**Curl-level smoke sequence** (mirrors what `Agent_Registration_Tests`/`Agent_Token_Tests`/
`Agent_Protected_Endpoint_Tests` assert end-to-end over real HTTP; `$SIGN` stands in for
"ECDSA P-256, SHA-256, DER/Rfc3279DerSequence signature over the given bytes" — not literally
scriptable in bare curl, an agent SDK does this with its private key):

```bash
# 1. Start registration — mint a one-time challenge.
curl -s -X POST https://localhost:7000/api/identity/agent/register/options
# => {"challenge":"<b64url-32-bytes>"}

# 2. Agent signs UTF8("TimeWarp.Identity.AgentKey.Register.v1:") || challengeBytes
#    with its ECDSA P-256 private key (DER signature), and exports its public key as SPKI DER.
CHALLENGE="<from step 1>"
PUBLIC_KEY="<b64url SPKI DER>"
SIGNATURE="<b64url DER signature over the Register.v1 prefix + decoded challenge>"

# 3. Complete registration — mints Principal (Agent, Provisional->Keyed) + Credential (AgentKey).
curl -s -X POST https://localhost:7000/api/identity/agent/register \
  -H "Content-Type: application/json" \
  -d "{\"publicKey\":\"$PUBLIC_KEY\",\"challenge\":\"$CHALLENGE\",\"signature\":\"$SIGNATURE\"}"
# => {"principalId":"<guid>","keyId":"<b64url-sha256-of-spki>"}

# 4. Start token issuance — mint a second, domain-separated challenge.
curl -s -X POST https://localhost:7000/api/identity/agent/token/options
# => {"challenge":"<b64url-32-bytes>"}

# 5. Agent signs UTF8("TimeWarp.Identity.AgentKey.Token.v1:") || challengeBytes with the SAME key.
KEY_ID="<KeyId from step 3>"
TOKEN_CHALLENGE="<from step 4>"
TOKEN_SIGNATURE="<b64url DER signature over the Token.v1 prefix + decoded challenge>"

# 6. Complete token issuance — proves possession, mints a scoped opaque bearer token.
curl -s -X POST https://localhost:7000/api/identity/agent/token \
  -H "Content-Type: application/json" \
  -d "{\"keyId\":\"$KEY_ID\",\"challenge\":\"$TOKEN_CHALLENGE\",\"signature\":\"$TOKEN_SIGNATURE\",\"scopes\":[\"identity:read\"]}"
# => {"accessToken":"<opaque token>","tokenType":"Bearer","expiresInSeconds":900,"scopes":["identity:read"],"principalId":"<guid>"}

# 7. Call the protected endpoint with the bearer token.
ACCESS_TOKEN="<accessToken from step 6>"
curl -s https://localhost:7000/api/identity/agent/me \
  -H "Authorization: Bearer $ACCESS_TOKEN"
# => {"principalId":"<guid>","kind":"Agent","trustTier":"Keyed","scopes":["identity:read"]}

# 8. No token -> 401 with a bare "Bearer" WWW-Authenticate challenge + RFC 7807 body.
curl -si https://localhost:7000/api/identity/agent/me | head -1
# HTTP/1.1 401 Unauthorized  (WWW-Authenticate: Bearer)

# 9. Garbage token -> 401 invalid_token.
curl -si https://localhost:7000/api/identity/agent/me -H "Authorization: Bearer garbage" | head -1
# HTTP/1.1 401 Unauthorized  (WWW-Authenticate: Bearer error="invalid_token")
```

### Deferred
- Key/token revoke + list, Update*/retry policy → 104-005. Rate limiting (challenge/token store caps are DoS bounds only; valid-grant eviction under flood documented) → 104-015. Paid elevation/credit claims → 104-013/014. **api-server bearer validation → 104-030** (also: verify task-108 string-enum wire holds through FastEndpoints). Ed25519 → BCL availability. Distributed stores → multi-instance wave. Discovery docs consume the curl smoke → 104-017.
- Unblocks: 104-005, 104-011, 104-013/014 groundwork.

## Session

- Created: 2026-07-16
- Implementation + review: ses 78b9f414 (2026-07-20), build agent a2ef2354b0fc976e7, reviewers a31739ea747756530 (general) + security-reviewer-104-003
