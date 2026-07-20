# Support multiple credentials per principal and list revoke

## Parent

104

## Description

Many-to-one credentials from day one (phone + laptop passkeys, agent key rotation). List and revoke while authenticated.

## Requirements

- Add credential to existing principal
- List credentials
- Revoke credential
- Cannot revoke last credential without explicit policy (document choice)

## Checklist

- [ ] Add/list/revoke APIs
- [ ] Tests

## Notes

### Implementation plan (2026-07-20)

#### Context confirmed

- `IPrincipalStore` already has everything: `ListCredentialsAsync(principalId, includeRevoked)`, `GetCredentialAsync`, `AddCredentialAsync` (auto-promote), `UpdateCredentialAsync` (throws `ConcurrencyConflictException`). No store surface change.
- `Credential.Revoke()` one-shot (throws if already revoked); `IsRevoked`/`RevokedAt`; `Handle`/`PublicMaterial` are secret-ish material to omit.
- Both complete-registration handlers ALWAYS `Principal.Create` — add-to-current-principal is a genuinely new flow.
- Caller resolution split today: `IBrowserSessionService` (cookie), `IAgentCallerContext` (bearer) — but BOTH schemes write the same claim `IdentitySessionDefaults.PrincipalIdClaimType` → unified accessor is trivial.
- Policies registered in program.cs AddAuthorizationBuilder, scheme-restricted; contracts reference policy names as string literals + `// matches …` comment. DeleteRole = precedent for authenticated command with `{Id:guid}` route + IAuthApiRequest + [EndpointAuthorize].
- Fixtures IntegrationSoftwareAuthenticator / IntegrationSoftwareAgentKey / isolated-cookie / RegisterAndIssueToken reusable.

#### Decision 1 — Auth model

**Caller resolution:** new port `ICurrentPrincipalAccessor { Task<PrincipalId?> GetCurrentPrincipalIdAsync(CancellationToken); }`. web-server impl reads `IHttpContextAccessor.HttpContext.User.FindFirstValue(IdentitySessionDefaults.PrincipalIdClaimType)`. Both cookie + agent-token schemes populate that same claim, and authorization middleware merges policy schemes onto User before the handler → one impl covers both, no try-cookie-then-bearer branching. (Verify empirically in integration.)

**Policy:** new `"credential-management"` accepting EITHER scheme:
```csharp
.AddPolicy(CredentialManagementDefaults.Policy, policy => policy
  .AddAuthenticationSchemes(IdentitySessionDefaults.Scheme, AgentTokenDefaults.Scheme)
  .RequireAuthenticatedUser()
  .RequireAssertion(ctx =>
    string.Equals(ctx.User.Identity?.AuthenticationType, IdentitySessionDefaults.Scheme, StringComparison.Ordinal) // cookie = full self-control
    || ctx.User.HasClaim(AgentTokenDefaults.ScopeClaimType, AgentScopes.CredentialManage)))                        // agents: least privilege
```
RequireAssertion (not RequireClaim) because cookie principals carry no scope claim.

**Scope:** new `AgentScopes.CredentialManage = "credential:manage"` (NOT reuse identity:read — revoke/add are writes that can lock out; read-scoped token revoking = privilege escalation). One scope covers list+add+revoke (rotation needs all three). Document: credential:manage can list; identity:read-only cannot (intentional least-privilege).

**IDOR rule (load-bearing):** whose-credentials principal id comes ONLY from the accessor, never the request. List implicitly scopes to caller. Revoke takes credentialId from route; handler Gets it, verifies `credential.PrincipalId == callerId` BEFORE acting; mismatch → **404 not 403** (no existence oracle). Same 404 for unknown id.

#### Decision 2 — Cannot revoke last credential

**Choice (a): reject revoking the last ACTIVE credential** (prevent self-lockout; recovery out of scope). Count over non-revoked via `ListCredentialsAsync(callerId, includeRevoked:false)`; if target is the only one (count ≤ 1) → **409** "cannot revoke last credential". Documented in handler Design region + task.

**Concurrency caveat (documented residual):** per-credential Version check does NOT serialize concurrent revokes of DIFFERENT credentials — each sees 2 active, revokes a different row, no version conflict → principal reaches 0. Real TOCTOU on the count. Wave-1: guard the common single-actor case; document the multi-revoke race as accepted (true fix = principal-level guard or store-level atomic revoke-unless-last, deferred; consistent with the repo's documented-race style). Record in Design + Open Questions.

#### Decision 3 — Revoke concurrency (the 104-028 showcase)

Bounded catch-reGet-retry:
```
const int MaxAttempts = 3;
for attempt in 0..MaxAttempts:
    cred = await store.GetCredentialAsync(credentialId)
    if cred is null || cred.PrincipalId != callerId:  return NotFound()        // 404 no oracle
    if cred.IsRevoked:                                 return AlreadyRevoked()  // 409
    active = await store.ListCredentialsAsync(callerId, includeRevoked:false)
    if active.Count <= 1:                              return LastCredential()  // 409
    cred.Revoke()
    try   { await store.UpdateCredentialAsync(cred);  return Ok(); }            // 200/204
    catch (ConcurrencyConflictException) { continue; }                          // stale → re-Get retry
return TooMuchContention()   // 409 after exhausting attempts (retryable)
```
Snapshot-on-get hands a fresh caller-owned Credential; a concurrent writer advances stored Version → UpdateCredentialAsync throws → re-Get + retry. Lost race → top-of-loop IsRevoked → 409 AlreadyRevoked (natural). Bound 3. Cite i-principal-store.cs Design ("Conflict policy stays with callers").

**Already-revoked:** return **409** (not idempotent 204) — Revoke() models one-shot as throw; caller holds stale state; retry loop needs the branch anyway. Note idempotent-204 as defensible alternative.

#### Decision 4 — Add-credential ceremonies

YES, build authenticated add-to-current-principal ceremonies (the "add credential" requirement). Mirror complete-registration handlers but source principalId from ICurrentPrincipalAccessor, SKIP Principal.Create:
- decode → consume challenge → verify → FindCredentialByHandleAsync (exists → 409 identical response either principal, no oracle) → Credential.Create(callerPrincipalId, ...) → AddCredentialAsync (catch InvalidOperationException → 409 same-handle race). No session re-issue. Zero Update* → no retry loop.
- **Reuse existing anonymous Start** (StartPasskeyRegistration / StartAgentKeyRegistration — side-effect-free challenge minters, no principal created). Only the two complete-add endpoints are new + authenticated. Document Wave-1 simplification: generic Registration challenge → no excludeCredentials populated (follow-up).
- Agent rotation = authenticated AddAgentKey (new key, credential:manage token) → GetCredentials (find old id) → RevokeCredential (old).

#### Contract shapes (all under /api/identity — no AppHost change)

**GetCredentials** (queries/get-credentials.cs): [ApiEndpoint] + [EndpointAuthorize(Policy="credential-management")]; [ApiRoute("api/identity/credentials", Get)] Query : IAuthApiRequest, ... with optional `bool IncludeRevoked` (default false). Response { IReadOnlyList<CredentialSummary> Credentials }. `record CredentialSummary(CredentialId Id, CredentialType Type, string? Label, DateTimeOffset CreatedAt, DateTimeOffset? RevokedAt, bool IsActive)`. **OMIT Handle + PublicMaterial** — never serialized; enforce with json.ShouldNotContain in round-trip test.

**RevokeCredential** (commands/revoke-credential.cs): [ApiEndpoint] + [EndpointAuthorize(Policy="credential-management")]; [ApiRoute("api/identity/credentials/{CredentialId:guid}/revoke", Post)] Command : IAuthApiRequest, ...; CredentialId route-generated (DeleteRole precedent); POST+/revoke = soft-revoke not delete. Response; (empty). Validator NotEmpty CredentialId.

**AddPasskey** (commands/add-passkey.cs): [ApiEndpoint] + [EndpointAuthorize(Policy="credential-management")]; [ApiRoute("api/identity/credentials/passkey", Post)]; `string CredentialId, ClientDataJson, AttestationObject; string? Label` (same caps as CompletePasskeyRegistration). Response(CredentialId newCredentialId).

**AddAgentKey** (commands/add-agent-key.cs): [ApiEndpoint] + [EndpointAuthorize(Policy="credential-management")]; [ApiRoute("api/identity/credentials/agent-key", Post)]; `string PublicKey, Challenge, Signature; string? Label` (same caps as CompleteAgentKeyRegistration). Response(CredentialId newCredentialId, string keyId).

All four: IAuthApiRequest (client/mock signal) + [EndpointAuthorize] — DeleteRole posture; TWA0014 permits (only forbids IAuthApiRequest + [EndpointAllowAnonymous]).

#### Ordered work items

1. **agent-scopes.cs**: add `CredentialManage = "credential:manage"` to constants + All; Design region (why a write scope distinct from identity:read).
2. **i-current-principal-accessor.cs** (web-application/abstractions): `ICurrentPrincipalAccessor` + regions.
3. **http-current-principal-accessor.cs** (web-server/services): reads claim, PrincipalId.From, null on empty/unparsable; scoped registration.
4. **credential-management-defaults.cs** (web-server/configuration): `Policy = "credential-management"` + Design region (either-scheme + assertion + scope rule).
5. **program.cs**: register accessor; add credential-management policy; reconcile Design region.
6. **Contracts** (4 files per shapes) with regions + `// matches CredentialManagementDefaults.Policy`.
7. **Handlers** (web-application/features/identity/): get-credentials-handler (resolve → List → CredentialSummary[], drop Handle/material; null caller → 401); revoke-credential-handler (Decision 3 loop; richest Design region — 104-028 showcase + count race); add-passkey-handler + add-agent-key-handler (Decision 4, minus Principal.Create).
8. **Tests** (below).
9. **Docs/regions**: task decisions; Purpose/Design on all new files; 0/0.

#### Test plan

- **Unit** (in-memory-principal-store-tests): ListCredentialsAsync filters/includes revoked, orders by CreatedAt; revoke-then-list reflects.
- **Retry-loop unit** (web-application handler test): fake store decorator throws ConcurrencyConflictException once then delegates → handler retries + succeeds; always-throws fake → 409 after MaxAttempts. (Drive at handler seam; HTTP conflict non-deterministic.)
- **Integration** (web-server-integration-tests/Features/Identity/): reuse fixtures + RegisterAndIssueToken with [CredentialManage]:
  - Credential_List_Tests: list via cookie; list via bearer; includeRevoked toggles; **response never contains handle/material** (json.ShouldNotContain); 401 unauth.
  - Credential_Add_Tests: add-passkey to cookie principal → 2 active; add-agent-key to bearer principal → 2; agent rotation (add + revoke old).
  - Credential_Revoke_Tests: revoke own cookie+bearer → 200 + reflected; **another principal's cred → 404**; **last active → 409**; **already-revoked → 409**; 401 unauth; identity:read-only token (no credential:manage) → 403.
  - Status codes primary; multi-scheme WWW-Authenticate looser than single-scheme suites (verify empirically).

DoD: happy + rejection integration for list, revoke, add.

#### Scope boundaries

In: authenticated add-passkey/add-agent-key to current principal; list; revoke; last-credential guard; unified accessor; either-scheme policy + credential:manage scope; 104-028 retry showcase.
Out (documented): account recovery/soft-prompt (deferred); hard delete / orphan cleanup (no port delete; orphans inert); EF store; excludeCredentials in add-passkey; multi-revoke count TOCTOU (accepted residual); dedicated authenticated Start ceremony (reuse anonymous Start).

#### Open Questions

None blocking. Documented-not-unresolved: (1) already-revoked → 409 (idempotent-204 noted alternative); (2) last-credential count race accepted Wave-1 residual, true fix deferred. Both in revoke handler Design/Open-Questions for the effort-2 (general + security) review.

Recovery soft-prompt for humans can wait; multi-credential is the structural fix.

### Depends on

104-003, 104-004

## Session

- Created: 2026-07-16
