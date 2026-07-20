# Round 1 — security

**Date:** 2026-07-20
**Scope reviewed:** commit 3b0b52b4 vs parent (security lens)

## Summary

The IDOR/ownership model is sound. Across all four handlers the caller principal id is sourced
exclusively from `ICurrentPrincipalAccessor` (never from `command.UserId`, the route, or the query),
revoke performs a `credential.PrincipalId == callerId` ownership check before any mutation and
returns an identical `404` for both unknown-id and other-principal cases (no existence oracle), and
`CredentialSummary` structurally omits `Handle`/`PublicMaterial` with a wire-level `ShouldNotContain`
test pinning it. The either-scheme `credential-management` policy fails closed: a pure agent token
without `credential:manage` gets `403`, proven by a live test. No exploitable findings; two
low-severity items (a documentation imprecision around the dual-auth case, and one missing
cross-principal-collision test) are noted for hardening.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/web-application/abstractions/i-current-principal-accessor.cs:41-44; source/container-apps/web/web-server/services/http-current-principal-accessor.cs:36-46
- Description: The Design regions assert that after authorization "HttpContext.User already IS the
  winning scheme's ClaimsPrincipal" and "which scheme won is no longer a question." This is precise
  only when exactly one scheme authenticates. For a policy built with
  `AddAuthenticationSchemes(identity-session, agent-token)`, if a single request carries BOTH a valid
  session cookie (principal A) AND a valid agent bearer token (principal B), ASP.NET Core's
  `PolicyEvaluator` authenticates every listed scheme and merges the results into one
  multi-identity `ClaimsPrincipal` assigned to `HttpContext.User`. `HttpCurrentPrincipalAccessor`
  then calls `user.FindFirstValue(PrincipalIdClaimType)`, which returns the claim of whichever
  identity the merge placed first — a framework-ordering detail, not "the winning scheme." So in the
  dual-auth case the accessor silently attributes the operation to one of two principals by merge
  order, and the policy's first assertion arm (`AuthenticationType == identity-session`, which reads
  only the primary identity) may not even be the arm that admitted the request.
- Why this is NOT a bug: reaching this state requires the caller to legitimately hold valid
  credentials for BOTH principals simultaneously — there is no privilege escalation, since the caller
  can already act as either principal independently. The behavior fails safe (it can only resolve to
  a principal the caller controls).
- Suggestion: Tighten the Design-region wording to acknowledge the multi-identity merge, and
  optionally have the accessor read the claim off `HttpContext.User.Identities` in a defined
  precedence (or reject when two distinct principal-id claims are present) so the resolution is
  explicit rather than dependent on framework merge order. Documentation/robustness only.
- Status: open

### Issue 2 — Severity: nit
- File: tests/container-apps/web/web-server-integration-tests/Features/Identity/Credential_Add_Tests.cs:82-120
- Description: The add-passkey duplicate-handle 409 path is tested only for a handle already owned by
  the CALLER's own principal (`Conflict_Given_Same_Passkey_Handle_Registered_Twice` reuses one
  authenticator within a single session). The security-relevant claim in add-passkey-handler.cs:44-49
  and add-agent-key-handler.cs — that a handle belonging to a DIFFERENT principal yields the SAME
  409, preventing cross-principal account linking and giving no ownership oracle — has no direct
  covering test. I verified the code path is correct: `FindCredentialByHandleAsync`
  (in-memory-principal-store.cs:169-182) resolves by a global `HandleIndex` with no principal
  filter, so a foreign-principal handle is found and returns 409 identically, and `Credential.Create`
  always binds `callerId.Value` — there is no request-supplied principal path to attach onto another
  account. The behavior is sound; only the regression test is absent.
- Suggestion: Add an integration test where principal A registers a passkey, then principal B (a
  separate authenticated session) submits an AddPasskey ceremony resolving to A's already-registered
  handle, asserting `409` and that A's credential set is unchanged. This would fail if
  `FindCredentialByHandleAsync` were ever narrowed to caller scope.
- Status: open

## Verified as sound (no issue)

- **IDOR / horizontal escalation (headline risk):** every handler resolves `callerId` from
  `ICurrentPrincipalAccessor` and ignores `command.UserId`. `GetCredentials.Handler` scopes by
  construction (`ListCredentialsAsync(callerId.Value, ...)`; the store filters strictly on
  `PrincipalId` — in-memory-principal-store.cs:193). `RevokeCredential.Handler` checks
  `credential.PrincipalId != callerId.Value` BEFORE `Revoke()` and returns the same `NotFound()` for
  unknown-id and foreign-id (revoke-credential-handler.cs:570-575). Add handlers bind
  `Credential.Create(callerId.Value, ...)` — no request-supplied principal id. The
  `NotFound_Given_Another_Principals_Credential` test genuinely proves the property: removing the
  ownership check would flip the result from 404 to 409 (attacker's own last-active guard) and the
  test would fail.
- **Key-material leakage:** `CredentialSummary` (get-credentials.cs) has no `Handle`/`PublicMaterial`
  members; the handler's projection only ever reads Id/Type/Label/CreatedAt/RevokedAt/IsRevoked. The
  `Never_Serializes_Handle_Or_PublicMaterial` integration test asserts on the RAW response body
  (`json.ToLowerInvariant().ShouldNotContain("handle"/"publicmaterial")`), a real wire-level check.
- **Scope privilege escalation:** the `RequireAssertion` (program.cs:710-712) admits a cookie by
  `AuthenticationType == identity-session` OR an agent by `HasClaim(scope, credential:manage)`. An
  identity:read-only agent token satisfies neither arm → `403`, proven live in both
  `Credential_List_Tests.Forbidden_Given_IdentityReadOnly_Bearer_Token` and the revoke sibling. A
  pure agent token cannot satisfy the cookie arm (its AuthenticationType is agent-token). New
  `credential:manage` scope is added to `AgentScopes.All`/`IsKnown` correctly.
- **Auth-context trust:** `HttpCurrentPrincipalAccessor` reads a claim only ever written by the two
  authenticated scheme handlers under a shared claim type; it is null-safe on absent HttpContext,
  unauthenticated identity, and missing/empty/unparsable guid → clean 401, no throw, no
  null-principal proceed (http-current-principal-accessor.cs:38-46). Guid.Empty is explicitly
  rejected.
- **Revoke concurrency:** the bounded (`MaxAttempts=3`) catch-reGet-retry loop re-Gets a fresh
  caller-owned snapshot each iteration, re-checks ownership + IsRevoked + last-active every attempt,
  and translates exhausted contention to a `409` — `ConcurrencyConflictException` never escapes to a
  500. The `RevokeCredential_ConcurrencyRetry_Tests` cover both "one stale retry then success" and
  "always-throws → 409 after exactly 3 attempts, credential remains active." No double-revoke (a lost
  race re-Gets `IsRevoked=true` → 409 AlreadyRevoked).
- **Add-credential ceremony:** challenge consume-before-verify preserved in both add handlers
  (`TryConsume` runs before `Verify`), matching the 104-003/004 posture; no oracle in the
  registration-shaped flow. Cross-principal handle collision returns identical 409 (see Issue 2).
- **Self-lockout:** last-active guard counts active-only via `includeRevoked:false` and is honest
  about its limit — the multi-revoke count TOCTOU (two concurrent revokes of two DIFFERENT
  credentials each passing the >1 guard) is explicitly documented as an accepted Wave-1 residual in
  the handler Design + Open Questions, consistent with the repo's documented-race convention. A
  single caller cannot drive itself to zero on one row (the guard + retry loop catch it); the residual
  is genuinely the two-different-rows race only.
