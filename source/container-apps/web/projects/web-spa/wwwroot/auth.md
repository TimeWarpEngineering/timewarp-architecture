# auth.md — TimeWarp.Architecture template host

**Honest agent/human auth story.** This host does **not** use email/password
registration. An account is an accepted public key (passkey or agent key), not a
form filled with an email address.

Content usage preferences: `ai-train=yes, search=yes, ai-input=yes`
(see [/robots.txt](/robots.txt)). Discovery index: [/llms.txt](/llms.txt).

---

## Mental model

| Who | How you prove who you are | Session shape |
|-----|---------------------------|---------------|
| Human (browser) | WebAuthn **passkey** | HTTP-only cookie session after ceremony |
| Agent (machine) | **ECDSA P-256** agent public key + proof-of-possession | Short-lived **opaque scoped bearer** token |
| Paid power | **x402** (TimeWarp.402) on paid routes only | Elevates trust / credits — not identity itself |

Identity answers *who*. Payment answers *did they pay*. Free and discovery routes
**never** return HTTP **402**. If payment is disabled or misconfigured, paid
routes respond **503**, not 402.

---

## Humans — passkeys first

1. Browser runs a WebAuthn registration or authentication ceremony.
2. Server verifies the assertion and issues an **identity-session cookie**.
3. Optional progressive profile comes later — not required to exist as a principal.

### Endpoints (anonymous ceremony; session after complete)

| Step | Method | Route |
|------|--------|-------|
| Start register | `POST` | `/api/identity/passkey/register/options` |
| Complete register | `POST` | `/api/identity/passkey/register` |
| Start authenticate | `POST` | `/api/identity/passkey/authenticate/options` |
| Complete authenticate | `POST` | `/api/identity/passkey/authenticate` |
| Current session | `GET` | `/api/identity/session` |

Demo UI: [/Passkeys](/Passkeys), [/Login](/Login).

Additional passkeys can be attached to an existing principal once signed in
(`POST /api/identity/credentials/passkey` — session required).

**Not offered:** email register, password reset, magic-link-only accounts as the
primary path. Microsoft Entra / MSAL may exist as a dormant non-default path and
is not the agent- or human-priority story.

---

## Agents — public key + scoped token (no human sponsor)

Agents register a public key without a browser and without a human sponsor.
Algorithm: **ECDSA P-256** (ES256). Wire: base64url **SPKI DER** public key;
signatures are **DER** (RFC 3279), not P1363. Key id = SHA-256(SPKI).

Proof of possession uses a one-time challenge and domain-separated signed data:

- Registration: UTF-8 `TimeWarp.Identity.AgentKey.Register.v1:` ‖ challenge
- Token: UTF-8 `TimeWarp.Identity.AgentKey.Token.v1:` ‖ challenge

(Use `AgentKeyProof.BuildSignedData` from **TimeWarp.Identity** — do not invent
a private prefix string.)

### Ceremony

| Step | Method | Route | Notes |
|------|--------|-------|-------|
| Start register | `POST` | `/api/identity/agent/register/options` | Returns one-time `challenge` |
| Complete register | `POST` | `/api/identity/agent/register` | Body: `publicKey`, `challenge`, `signature`, optional `label` → `principalId`, `keyId` |
| Start token | `POST` | `/api/identity/agent/token/options` | Fresh challenge (Token.v1 domain) |
| Complete token | `POST` | `/api/identity/agent/token` | Body: `keyId`, `challenge`, `signature`, `scopes` → Bearer + expiry |
| Who am I | `GET` | `/api/identity/agent/me` | Header: `Authorization: Bearer <token>`; needs scope `identity:read` |

Default token lifetime is short (minutes; configurable). There is **no** refresh
token: refresh means re-running the token ceremony. Tokens are **opaque**
store-backed grants (not JWTs in v1) so they can be cut off when a principal is
quarantined.

### Known scopes (v1)

| Scope | Purpose |
|-------|---------|
| `identity:read` | Call `/api/identity/agent/me` and similar identity reads |
| `credential:manage` | List / add / revoke credentials on the caller's own principal |
| `demo:invoke` | Reserved for metered demo capability (landing with payment waves) |

Unknown scopes are rejected with a machine-readable problem response.

### Onboarding tools

**Preferred (narrated walkthrough):** from a repo checkout:

```bash
dotnet run tools/agent-identity-cli/agent.cs -- demo
# also: keygen | register | token | whoami
# --server defaults to https://localhost:63611
```

**Manual HTTP:** same routes as above; full curl-oriented smoke sequence lives in
kanban task **104-004** Results (openssl keygen + register/token/me). Signing
cannot be pure curl — the agent must ECDSA-sign the domain-separated challenge.

Credential management (list/add/revoke) uses `/api/identity/credentials*` under a
policy that accepts either an identity-session cookie **or** a bearer token with
scope `credential:manage`.

---

## Payment — x402 (not identity)

- Package: **TimeWarp.402** (challenge, verify, settle).
- **Free / discovery surfaces never 402** (this file, `/llms.txt`, OpenAPI, health,
  identity register/token ceremonies, etc.).
- Disabled or misconfigured payment configuration → **503** on paid routes only.
- Paying is enough for an agent to buy service; a human sponsor is not required.
- Trust tiers (Keyed → Funded, etc.) separate cheap identity from expensive power.

Host-wired tip jar and metered “pay for capability” demo paths are product work
in progress. When they ship, this document and [/llms.txt](/llms.txt) will list
the exact URLs. Do not treat fictional `/api/tip` or meter paths as live until
linked.

---

## Machine-readable API catalog

- OpenAPI: [/openapi/v1.json](/openapi/v1.json)
- Scalar: [/scalar/v1](/scalar/v1)

Errors for agent APIs prefer **problem+json** (and RFC 6750 `WWW-Authenticate`
on bearer failures).

---

## What we deliberately do not claim

- No email/password “register” or “forgot password” as the primary auth model.
- No requirement that an agent find a human to create an account before calling APIs.
- No blanket block of AI training crawlers (agent-welcome Content Signals).
- No guarantee that MCP / A2A / skills cards exist yet — see [/llms.txt](/llms.txt)
  “Planned” section rather than inventing cards.
