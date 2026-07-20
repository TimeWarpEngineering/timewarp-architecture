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

- [x] Add/list/revoke APIs
- [x] Tests

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

## Results

### Summary

Implemented all 4 decisions from the plan exactly as specified. Credential-management is now a
real, tested surface: `GetCredentials` (list, own-credentials-only by construction),
`RevokeCredential` (soft-revoke with the 104-028 bounded-retry showcase), `AddPasskey`/`AddAgentKey`
(attach to the CALLER's existing principal — never `Principal.Create`). A new
`ICurrentPrincipalAccessor` port resolves the caller scheme-agnostically (cookie or agent bearer)
by reading `HttpContext.User` post-authorization, and a new `credential-management` policy
(`RequireAssertion`: cookie = full self-control, agent = requires the new `credential:manage`
scope) gates all four endpoints. Every IDOR/security load-bearing rule from the dispatch holds:
principal id never comes from the request, mismatch and unknown-id both 404, `identity:read`-only
tokens get 403, and `Handle`/`PublicMaterial` are structurally + wire-level pinned as never
serialized.

### Work items

1. `agent-scopes.cs`: added `CredentialManage = "credential:manage"` + Design region explaining why
   it's a distinct write scope from `IdentityRead` (least-privilege — a read-only token must not be
   able to revoke credentials).
2. `i-current-principal-accessor.cs` (web-application/abstractions, new): `ICurrentPrincipalAccessor`
   port, async signature matching `IBrowserSessionService` for call-site uniformity.
3. `http-current-principal-accessor.cs` (web-server/services, new): reads
   `HttpContext.User.FindFirstValue(IdentitySessionDefaults.PrincipalIdClaimType)` directly —
   deliberately does NOT re-authenticate against one named scheme (unlike
   `CookieBrowserSessionService`) since the authorization middleware already resolved which scheme
   won by the time a handler runs.
4. `credential-management-defaults.cs` (web-server/configuration, new): `Policy =
   "credential-management"` + Design region documenting the either-scheme/assertion/scope shape.
5. `program.cs`: registered the accessor (`AddScoped`) and the policy
   (`AddAuthenticationSchemes(identity-session, agent-token).RequireAuthenticatedUser()
   .RequireAssertion(...)`).
6. 4 new contracts (`get-credentials.cs`, `revoke-credential.cs`, `add-passkey.cs`,
   `add-agent-key.cs`) — DeleteRole posture (`IAuthApiRequest` client/mock signal +
   `[EndpointAuthorize(Policy="credential-management")]`), size caps on every base64url field from
   day one, `CredentialSummary` DTO structurally omits `Handle`/`PublicMaterial`.
7. 4 new handlers — `GetCredentials`/`RevokeCredential`/`AddPasskey`/`AddAgentKey`, all sourcing the
   caller from `ICurrentPrincipalAccessor`, never the request. `RevokeCredential.Handler` carries the
   richest Design region in the codebase: the bounded (`MaxAttempts=3`) catch-reGet-retry loop, the
   404-not-403 IDOR rule, already-revoked→409 (documented alternative: idempotent 204), the
   last-active-credential guard, and the multi-revoke count TOCTOU accepted as a documented Wave-1
   residual (with an Open Questions note on when to revisit).
8. Tests (see Build/tests below for counts) — unit (store ordering), retry-loop unit (handler seam,
   fake `IPrincipalStore` decorator), contract round-trips (incl. the no-secret-material pin),
   integration (list/add/revoke × happy + every rejection branch named in the dispatch).
9. Docs/regions: every new file carries Purpose (+ Design where there are real decisions); no
   external doc changes were needed beyond in-file regions — this task reuses task 110's
   `[EndpointAuthorize]`/`IAuthApiRequest` conventions and TWA0013/0014 enforcement as-is, adding no
   new architectural vocabulary for AGENTS.md/the skill to describe.

### Deviations from the plan (with rationale)

- **Shared test-ceremony helper, not per-file duplication.** The plan's test list implied the usual
  per-file duplication convention (see `Roles_Authorization_Tests.cs`'s own Design region), but three
  new files each needed BOTH "mint a passkey session" and "mint an agent bearer token with given
  scopes," plus a second "build a fresh WebAuthn/agent-key proof" primitive for the add-credential
  flows — duplicating that ~150+ times over would have meaningfully raised the risk of a
  byte-layout mistake diverging between copies. Extracted `CredentialCeremonyHelpers.cs`
  (Infrastructure/) instead, with a Design region explaining the departure from the small-duplication
  convention and why this is a different scale, not a change of principle.
- **`CredentialSummary` implemented as a plain class (ctor + Guard), not a C# `record`.** The plan's
  shorthand (`record CredentialSummary(...)`) was read as descriptive of the fields, not a literal
  directive — no DTO anywhere in web-contracts uses `record` (grepped to confirm), and `RoleDto`
  (the closest precedent, a list-item DTO on `GetRoles`) is a ctor+Guard class. Matched that
  convention for consistency.
- **`BuildPasskeyAttestationAsync` gained an optional `IntegrationSoftwareAuthenticator?` parameter**
  (not in the original helper sketch) after `Credential_Add_Tests.Conflict_Given_Same_Passkey_
  Handle_Registered_Twice` first failed 400 instead of 409 — replaying the exact same
  already-consumed challenge hits the one-time-challenge check before the duplicate-handle check.
  Fixed by letting two independent ceremonies share one authenticator instance (same credential
  handle, different challenges), which is the only way to exercise that specific 409 path.
- No design issues required stopping to report — the plan's 4 decisions all implemented cleanly as
  specified.

### Follow-ups (recorded, out of scope here)

- Multi-revoke count TOCTOU (two different credentials revoked concurrently can both pass the
  last-active guard) — accepted Wave-1 residual per the plan; true fix (principal-level lock or a
  store-level atomic revoke-unless-last) deferred until a real caller needs the stronger guarantee.
- `AddPasskey`'s reused anonymous `StartPasskeyRegistration` challenge has no `excludeCredentials`
  populated (it doesn't know the caller is signed in) — cosmetic UX gap only; the handler's
  `FindCredentialByHandleAsync` check still catches a same-credential resubmission as 409.
- No admin/bulk credential operations, no account-recovery flow, no hard delete — all explicitly out
  of scope per the plan's scope boundaries.

### Build / tests

- `dev build` (full solution): 0 Warning(s), 0 Error(s)
- `timewarp-identity-tests`: 169 passed (+1 new `Lists_in_ascending_CreatedAt_order` ordering test)
- `web-contracts-tests`: 38 passed (+12 new round-trip tests across the 4 new contracts)
- `web-server-integration-tests`: 78 passed, 1 skipped, 0 failed (+21 new: 2 retry-loop unit tests,
  6 `Credential_List_Tests`, 5 `Credential_Add_Tests`, 8 `Credential_Revoke_Tests`)
- `timewarp-architecture-analyzers-tests`: 82 passed (unaffected — no analyzer changes)
- `timewarp-architecture-sourcegenerator-tests`: 40 passed (unaffected — no generator changes)
- Regression sweep — `foundation-domain-tests`: 37; `foundation-contracts-tests`: 2;
  `foundation-application-tests`: 13; `foundation-infrastructure-tests`: 1; `web-domain-tests`: 26 —
  all unaffected, 0 failures
- Docker-dependent suites: out of scope, not run

## Session

- Created: 2026-07-16
