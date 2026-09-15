# auth.md — TimeWarp.Architecture template host

**Honest agent/human auth story.** This host does **not** use email/password
registration. An account is an accepted public key (passkey or agent key), not a
form filled with an email address.

Content usage preferences: `ai-train=yes, search=yes, ai-input=yes`
(see [/robots.txt](/robots.txt)). Discovery index: [/llms.txt](/llms.txt).
Agent skill for this ceremony: [timewarp-identity](/.well-known/agent-skills/timewarp-identity/SKILL.md)
(index: [/.well-known/agent-skills/index.json](/.well-known/agent-skills/index.json)).

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

Product CTA: [/Login](/Login) (Continue with passkey). Technical ceremony demo: [/Passkeys](/Passkeys).

Additional passkeys can be attached to an existing principal once signed in
(`POST /api/identity/credentials/passkey` — session required).

**Not offered:** email register, password reset, magic-link-only accounts as the
primary path. Microsoft Entra is **opt-in only** (`Authentication:Entra:Enabled=true`)
and is not the agent- or human-priority story. When enabled it is a named OIDC scheme
(`entra`); identity-session stays DefaultScheme. The SPA “Continue with Microsoft 365”
button is a BFF `Challenge(“entra”)` — no WASM MSAL session. Default non-mock SPA
auth projects the identity-session cookie via `GetCurrentSession`.

### Local Entra setup

From a repo checkout, with Azure CLI logged in (`az login`):

```bash
dev entra setup
# optional: --name “TimeWarp Architecture Dev” --public-origin https://arch.timewarp.work
#           --redirect-uri https://extra.example/signin-oidc --new-secret --dry-run
dev entra status
dev entra disable   # sets Authentication:Entra:Enabled=false; does not change Azure
```

`dev entra setup` finds or creates the app registration, unions redirect URIs
(`https://localhost:63611/signin-oidc`, `https://localhost:63610/signin-oidc`, plus
`--public-origin` when given), ensures a service principal, mints a client secret only
on first write or `--new-secret`, and writes Web.Server user secrets. The secret is
never printed. Then `dev run`, browse the app, and click **Continue with Microsoft 365**.

### Manual Entra configuration (redirect URIs and PublicOrigin)

Register **all three** redirect URIs on the Entra app registration for a given
environment, then set `Authentication:Entra:PublicOrigin` to the origin the
browser actually uses:

| Path | Redirect URI | `PublicOrigin` |
|------|----------------|----------------|
| Direct Web.Server | `https://localhost:63611/signin-oidc` | unset (request-derived) |
| Aspire YARP ingress | `https://localhost:63610/signin-oidc` | `https://localhost:63610` |
| Shared hostname (Caddy → YARP) | `https://<public-host>/signin-oidc` | `https://<public-host>` |

Any proxied deployment — including crunchit on Azure Container Apps — **must**
set `PublicOrigin`. Web.Server is reached over **http** behind YARP/ACA and
does not consume `X-Forwarded-*` (passkey RP-ID must not trust spoofable
forwarded headers). Unset `PublicOrigin` would send Entra `http://…/signin-oidc`.
The named `entra` scheme always writes Secure correlation/nonce cookies.

User-secrets example (Web.Server project):

```bash
dotnet user-secrets set “Authentication:Entra:Enabled” “true” --project source/container-apps/web/projects/web-server
dotnet user-secrets set “Authentication:Entra:Instance” “https://login.microsoftonline.com/” --project source/container-apps/web/projects/web-server
dotnet user-secrets set “Authentication:Entra:TenantId” “organizations” --project source/container-apps/web/projects/web-server
dotnet user-secrets set “Authentication:Entra:ClientId” “<app-id>” --project source/container-apps/web/projects/web-server
dotnet user-secrets set “Authentication:Entra:ClientSecret” “<secret>” --project source/container-apps/web/projects/web-server
dotnet user-secrets set “Authentication:Entra:CallbackPath” “/signin-oidc” --project source/container-apps/web/projects/web-server
dotnet user-secrets set “Authentication:Entra:AllowBootstrap” “true” --project source/container-apps/web/projects/web-server
dotnet user-secrets set “Authentication:Entra:TrustedTenants:0” “<tenant-id>” --project source/container-apps/web/projects/web-server
dotnet user-secrets set “Authentication:Entra:PublicOrigin” “https://arch.timewarp.work” --project source/container-apps/web/projects/web-server
```

`Ingress:PublicUrl` (AppHost) is dashboard display only — do not treat it as a
substitute for `PublicOrigin`.

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
| `demo:invoke` | Call `GET /api/demo/metered-capability` (credit or x402) |

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
  identity register/token ceremonies, SPA pages, etc.).
- Disabled or misconfigured payment configuration → **503** on paid routes only.
- Paying is enough for an agent to buy service; a human sponsor is not required.
- Trust tiers (Keyed → Funded, etc.) separate cheap identity from expensive power.

### Live paid paths (web-server)

| Path | Role | Auth | Unpaid (enabled) | Disabled |
|------|------|------|------------------|----------|
| **`GET\|POST /api/tip`** | Voluntary tip jar (canonical) | Anonymous | **402** + `PAYMENT-REQUIRED` | **503** |
| **`GET\|POST /api`** | Discovery alias → tip | Anonymous | same as `/api/tip` | same |
| **`GET /api/demo/metered-capability`** | Pay-for-capability demo | Bearer `demo:invoke` | **402** + challenge (or 200 if prepaid credit) | **503** |

Challenge resource for tips is always **`/api/tip`** (even when called via the
`/api` alias). See [/llms.txt](/llms.txt) Payment section for curl examples.

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
