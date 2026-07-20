# Identity unit and integration tests for ceremonies

## Parent

104

## Description

Fixie + Shouldly coverage for passkey and agent key paths (mock WebAuthn where needed). Gate Wave 1.

## Requirements

- Unit tests for domain + crypto/challenge helpers
- Integration tests for register/login or register/token
- `dev test` green for new projects

## Checklist

- [x] Unit suite
- [x] Integration suite
- [x] CI-safe

## Notes

Wave 1 exit criterion.

### Depends on

104-003, 104-004

## Session

- Created: 2026-07-16
- Started: 2026-07-20 (tw-orchestrate-task 104-006)
- Plan: 2026-07-20
- Closed: 2026-07-20

### Implementation plan (104-006)

#### Verdict
Ceremony unit + integration coverage was front-loaded into 104-003/004/005. This task is the **Wave 1 gate**: evidence matrix, D5 Design closeout, re-verify green. Optional: quarantine 403 HTTP tests.

#### Do
1. Update Design regions that still say "D5 deferred to 104-006" — ceremony stores already use TimeProvider; entity wall-clock stamps remain OK for Wave 1
2. Run identity unit + web-contracts + web-server-integration (+ CLI) suites; record counts
3. Optional G3: quarantined principal 403 on passkey auth + agent token if store access is clean
4. Results with coverage matrix; mark done

#### Not in scope
Playwright (104-022), api-server bearer (104-030), new test projects, hardware WebAuthn

## Results

### D5 disposition (closed)

Wave 1 ceremony-critical clocks (challenge/token stores) already use optional `TimeProvider`; domain entity `CreatedAt`/`RevokedAt` remain wall-clock with fuzzy tests. Full `TimeProvider` on domain entities is **not required** for the Wave 1 gate.

Design regions updated (removed "deferred to 104-006"):

| File | Disposition |
|------|-------------|
| `source/libraries/timewarp-identity/principals/principal.cs` | Closed — wall-clock Create; ceremony stores own TimeProvider |
| `source/libraries/timewarp-identity/credentials/credential.cs` | Closed — wall-clock CreatedAt/RevokedAt |
| `source/libraries/timewarp-identity/principals/trust-tier.cs` | Closed — enum has no timestamps; note only |
| `source/libraries/timewarp-identity/persistence/i-principal-store.cs` | Closed — durable port does not take TimeProvider |
| `source/libraries/timewarp-identity/persistence/in-memory-principal-store.cs` | Closed — persists entity stamps as written |

### G3 quarantine 403 (done)

Host resolves singleton `IPrincipalStore` via `WebTestServerApplication.WebApplicationHost.ServiceProvider` (same pattern as options-binding tests). After register, Get → `Quarantine()` → `UpdatePrincipalAsync`, then ceremony with valid crypto:

| Case | File | Expected |
|------|------|----------|
| Passkey auth after Quarantine | `Passkey_Authentication_Tests.Forbidden_Given_Quarantined_Principal` | 403 `Account quarantined` |
| Agent token after Quarantine | `Agent_Token_Tests.Forbidden_Given_Quarantined_Principal` | 403 `Account quarantined` |

Both green. Residual: bearer validation of already-issued tokens when principal is later quarantined remains 401 (documented on `AgentTokenAuthenticationHandler`); out of G3 scope.

### Coverage matrix (inventory — front-loaded 003/004/005 + this gate)

#### Unit — `tests/libraries/timewarp-identity-tests` (**169 passed**)

| Area | File(s) | Focus |
|------|---------|--------|
| Principal domain | `principal-tests`, `principal-id-tests` | Create, Promote, quarantine, RecordCredentialAttached, ids |
| Credential domain | `credential-tests`, `credential-id-tests` | Create, Revoke, byte[] copy-on-get, ids |
| Store | `in-memory-principal-store-tests` | Multi-cred, handle index, first-cred → Keyed, revoke list |
| Concurrency | `in-memory-principal-store-concurrency-tests` | Snapshot, stale Update, quarantine/tier races, attach version bump |
| WebAuthn reg | `webauthn-registration-tests` | ES256 happy + origin/challenge/UP/COSE/fmt negatives |
| WebAuthn auth | `webauthn-authentication-tests` | ES256/RS256, tamper, UV/UP, sign-count |
| Challenge reader | `webauthn-challenge-reader-tests` | clientDataJson parse rejects |
| Challenge store | `in-memory-webauthn-challenge-store-tests` | one-time, wrong type, TTL via TimeProvider, cap |
| Agent key | `agent-public-key-tests`, `agent-key-proof-tests` | P-256 SPKI, domain-separated proof, cross-ceremony |
| Agent challenge | `in-memory-agent-key-challenge-store-tests` | one-time, TTL, cap |
| Agent token store | `tokens/in-memory-agent-token-store-tests` | issue/validate, expiry, cap |

Soft authenticators: `ceremonies/infrastructure/software-authenticator.cs`, `software-agent-key.cs`.

#### Integration — `web-server-integration-tests` Features/Identity (**subset of 82 passed, 1 skipped suite-wide**)

| Ceremony / surface | File | Vectors |
|--------------------|------|---------|
| Passkey register | `Passkey_Registration_Tests` | cookie+session happy; challenge replay; wrong origin; validation; duplicate 409 |
| Passkey authenticate | `Passkey_Authentication_Tests` | cookie+session happy; unknown cred; tampered sig; replay; validation; **G3 quarantine 403** |
| Agent key register | `Agent_Registration_Tests` | happy; replay; bad sig; malformed key; duplicate; validation |
| Agent token issue | `Agent_Token_Tests` | bearer happy; unknown KeyId ≡ bad sig; invalid scope; null scopes; replay; **G3 quarantine 403** |
| Bearer policy | `Agent_Protected_Endpoint_Tests` | identity.read OK; no header; garbage; insufficient scope; cookie-only rejected |
| Credential list/add/revoke | `Credential_*_Tests` | cookie+bearer authz, IDOR 404, last-active 409, cross-principal handle 409 |
| Concurrency retry | `RevokeCredential_ConcurrencyRetry_Tests` | handler seam (not HTTP race) |
| Options binding | `WebAuthnOptions_Binding_Tests`, `AgentTokenOptions_Binding_Tests` | appsettings section name pins |

#### Contracts — `web-contracts-tests` (**38 passed** suite-wide)

Identity command/response serialization round-trips (ctor+Guard, scopes list, enum strings, no handle/publicMaterial on list DTOs, generated route properties).

#### CLI — `agent-identity-cli-tests` (**11 passed**)

WhoAmI wire shape; domain-separated sign vectors vs `AgentKeyProof`; local key-store sidecar round-trip.

### Verify (2026-07-20)

```
dotnet fixie tests/libraries/timewarp-identity-tests              → 169 passed
dotnet fixie tests/container-apps/web/web-contracts-tests         → 38 passed
dotnet fixie tests/container-apps/web/web-server-integration-tests → 82 passed, 1 skipped
dotnet fixie tests/tools/agent-identity-cli-tests                 → 11 passed
dev build                                                         → 0 Warning(s), 0 Error(s)
```

CI-safe: all suites are host-local Fixie/Shouldly; software authenticators only; no Playwright/hardware.

### Residual (not this task)

- Playwright virtual-authenticator e2e → 104-022
- api-server bearer host → 104-030
- Full domain `TimeProvider` injection — optional later if fuzzy asserts prove insufficient; **not a Wave 1 gate**
