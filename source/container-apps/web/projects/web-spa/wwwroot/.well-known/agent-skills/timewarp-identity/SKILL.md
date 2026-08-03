---
name: timewarp-identity
description: Register an ECDSA P-256 agent public key and issue short-lived scoped opaque bearer tokens on a TimeWarp.Architecture host. Use when onboarding a machine agent without a browser or human sponsor.
---

# TimeWarp.Identity — agent onboarding

This host is **passkey / agent-key first**. Agents do not use email/password.
Algorithm: **ECDSA P-256** (ES256). Public key wire format: base64url **SPKI DER**.
Signatures: **DER** (RFC 3279), not P1363. Key id = SHA-256(SPKI).

Full auth story: [/auth.md](/auth.md). Discovery index: [/llms.txt](/llms.txt).

## Ceremony (no browser)

| Step | Method | Route |
|------|--------|-------|
| Start register | `POST` | `/api/identity/agent/register/options` |
| Complete register | `POST` | `/api/identity/agent/register` |
| Start token | `POST` | `/api/identity/agent/token/options` |
| Complete token | `POST` | `/api/identity/agent/token` |
| Who am I | `GET` | `/api/identity/agent/me` |

`GET /api/identity/agent/me` requires `Authorization: Bearer <token>` with scope `identity:read`.

## Proof-of-possession domains

Do not invent the signed payload prefix. Prefer `AgentKeyProof.BuildSignedData` from **TimeWarp.Identity**:

- Registration: UTF-8 `TimeWarp.Identity.AgentKey.Register.v1:` ‖ challenge
- Token: UTF-8 `TimeWarp.Identity.AgentKey.Token.v1:` ‖ challenge

## Known scopes (v1)

| Scope | Purpose |
|-------|---------|
| `identity:read` | `/api/identity/agent/me` and similar reads |
| `credential:manage` | List / add / revoke own credentials |
| `demo:invoke` | Reserved for metered demo capability |

Unknown scopes are rejected with a machine-readable problem response. Tokens are short-lived opaque store-backed grants (not JWTs in v1); refresh means re-running the token ceremony.

## Preferred tooling (repo checkout)

```bash
dotnet run tools/agent-identity-cli/agent.cs -- demo
# also: keygen | register | token | whoami
# --server defaults to https://localhost:63611
```

## Humans (browser)

WebAuthn passkeys: UI at [/Passkeys](/Passkeys) and [/Login](/Login); ceremony under `/api/identity/passkey/*`. Session cookie after complete.

## Payment

Identity answers *who*. **x402** (TimeWarp.402) answers *did they pay* on paid routes only. Free and discovery routes **never** return HTTP **402**.
