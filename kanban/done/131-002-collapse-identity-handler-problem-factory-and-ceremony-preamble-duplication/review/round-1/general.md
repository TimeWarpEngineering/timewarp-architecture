# Round 1 — general
**Date:** 2026-07-29
**Scope reviewed:** commit `7d4653b0` (refactor(identity): extract IdentityProblems and registration ceremony helpers); live tree under `source/container-apps/web/features/identity/`; baseline strings/order from master worktree private factories

## Summary

The de-dup matches task 131-002 / F-006 and the review-framework security gates. `IdentityProblems` is the sole problem factory for identity application handlers; passkey and agent-key **registration** ladders share one preamble each; Complete vs Add remain four distinct handlers with auth-guard / RP-select / mint-or-attach / session-or-not kept at the caller. Security-critical order is preserved (and now single-path): challenge consume before verify; Add* auth before ceremony; passkey RP select before ceremony; helpers do not Create/AddCredential or issue sessions. Title/Status/Detail were checked against the pre-refactor private factories on master — parameterized expansions match the prior hard-coded strings. Zero `private static SharedProblemDetails` remain under `features/identity`. Design regions moved ordering rationale into the ceremony helpers and honestly describe residual handler-only differences (orphan Principal on Complete races, no IssueAsync on Add). No bugs found.

### Must-verify checklist

| Check | Result |
|-------|--------|
| 1. Security ordering | **Pass** — see §1 |
| 2. No handler merges | **Pass** — four handlers still separate |
| 3. Problem Title/Status/Detail | **Pass** — §2 verbatim matrix |
| 4. Ceremony helpers lack Create/AddCredential/session | **Pass** — return `Materials` only |
| 5. Zero private static SharedProblemDetails under features/identity | **Pass** — only `IdentityProblems` public statics |
| 6. Design regions honest | **Pass** — §3 |

### 1. Security ordering (re-verified line-by-line)

**Passkey registration ceremony** (`passkey-registration-ceremony-application.cs`):
1. Decode CredentialId / ClientDataJson / AttestationObject → MalformedPayload
2. `TryReadChallenge` + `TryConsume(Registration)` **before** `WebAuthnRegistration.Verify`
3. Verify against caller-supplied `WebAuthnRelyingParty`
4. `FindCredentialByHandleAsync` → CredentialAlreadyRegistered
5. Return `Materials` only — no Principal/Credential/session

**AddPasskey** (`add-passkey-handler-application.cs`):
1. `ICurrentPrincipalAccessor` → Unauthenticated **before** RP select and ceremony
2. `WebAuthnRelyingPartySelection.Select` **before** ceremony (disallowed host never burns challenge)
3. `PasskeyRegistrationCeremony.TryCompleteAsync`
4. `Credential.Create(callerId, …)` + `AddCredentialAsync` try/catch → 409 (handler-owned)

**CompletePasskeyRegistration**:
1. RP select **first** (no auth guard — anonymous mint)
2. Ceremony
3. `Principal.Create` → `AddPrincipalAsync` → `Credential.Create` → `AddCredentialAsync` try/catch → `BrowserSessionService.IssueAsync`

**Agent-key registration ceremony** (`agent-key-registration-ceremony-application.cs`):
1. Decode PublicKey / Challenge / Signature
2. `TryConsume(Registration)` **before** Verify
3. `AgentPublicKey.TryParse` **before** `AgentKeyProof.Verify` (104-004 §5)
4. Verify Registration
5. `FindCredentialByHandleAsync` → 409
6. Return `Materials` only

**AddAgentKey**: auth first → ceremony → attach to caller (no Issue token/session).
**CompleteAgentKeyRegistration**: ceremony → mint Agent principal → AddCredential → Response with KeyId (no cookie).

Compared to master pre-refactor ladders, step order is unchanged; only the shared middle was extracted.

### 2. Problem Title/Status/Detail (verbatim vs master private factories)

| Factory | Call-site args | Expanded Detail / Title | Master baseline |
|---------|----------------|-------------------------|-----------------|
| Unauthenticated | — | Title Unauthenticated, 401, "No authenticated principal." | identical |
| Unauthorized | — | 401, "A valid agent bearer token is required." | identical |
| MalformedPayload | field list | `"{fields} must be valid base64url."` | same strings with those exact field lists at each site |
| ChallengeInvalid | `"registration"` / `"authentication"` / `"token issuance"` | `"The {label} challenge is unknown, expired, or already used."` | identical |
| CredentialAlreadyRegistered | `"passkey"` / `"agent key"` | `"This {kind} is already registered to an account."` | identical |
| PasskeyRegistrationVerificationFailed | reason | Title + `"Verification failed: {reason}."` | was `VerificationFailed(WebAuthn…)` |
| AgentKeyRegistrationVerificationFailed | reason | Title + same Detail shape | was `VerificationFailed(AgentKey…)` |
| InvalidPublicKey, AuthenticationFailed, Quarantined, InvalidScope, IssuanceFailed, NotFound, AlreadyRevoked, LastCredential, TooMuchContention | — | match master factories byte-for-byte |

Parameterized only where master already had intentional wording variants. No silent “improvements.”

### 3. Design regions

- Ceremony helpers own the SECURITY-CRITICAL ORDER and residual-race / M5 (Registration reuse) notes.
- Handlers slim to real differences: auth-first + attach vs mint + session (passkey) / no token issuance on AddAgentKey; orphan Principal residual only on Complete* paths.
- CompletePasskeyAuthentication and CompleteAgentTokenIssuance correctly document “problems only, no ceremony helper” (single-consumer ladders).
- No stale “Order mirrors Complete… exactly” consistency-by-comment left as the sole guarantee of ladder agreement.

### 4. Scope / placement

- `identity-problems-application.cs`, `passkey-registration-ceremony-application.cs`, `agent-key-registration-ceremony-application.cs` at identity slice root, escape-hatch `<name>-application.cs`, `internal static`, namespace `…Features.Identity.Application` — no TWA0009 surface.
- Passkey-auth and token-issuance ladders intentionally not extracted (plan non-goal / single consumer).
- Host-not-allowed remains on `WebAuthnRelyingPartySelection` (never a per-handler private factory).
- `agent-token-authentication-scheme-server.cs` still owns its 401/403 challenge/forbid problems (server auth layer, not application handler factories) — out of task scope.

## Issues

None.
