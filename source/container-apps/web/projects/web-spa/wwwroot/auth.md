# auth.md — TimeWarp.Architecture template host

**Honest agent/human auth story.** This host does **not** use email/password
registration. An account is an accepted public key (passkey or agent key), not a
form filled with an email address.

Content usage preferences: `ai-train=yes, search=yes, ai-input=yes`
(see [/robots.txt](/robots.txt)). Discovery index: [/llms.txt](/llms.txt).
Agent skill for this ceremony: [timewarp-identity](/.well-known/agent-skills/timewarp-identity/SKILL.md)
(index: [/.well-known/agent-skills/index.json](/.well-known/agent-skills/index.json)).

You are an agent. Register by enrolling a public key. There is no email form.

## Agent registration

An agent registers itself. A human does not fill in an email form first. There is no email form.

Registration endpoints:

- `POST /api/identity/agent/register/options`
- `POST /api/identity/agent/register`

## Supported methods

- **Agent key.** ECDSA P-256 public-key enrollment. No human sponsor and no email.
- **Browser passkey.** A person enrolls a WebAuthn public key in the browser at [/Login](/Login).

This host does not accept ID-JAG, verified-email, or anonymous OAuth claim registration. It does not publish an OAuth `agent_auth` block or a `register_uri`.

## Credential use

After agent-key enrollment, `POST /api/identity/agent/token/options` and `POST /api/identity/agent/token` return a short-lived opaque bearer token. Send it as `Authorization: Bearer <token>`. A completed browser passkey ceremony sets an HTTP-only session cookie instead. Read the session with `GET /api/identity/session` or the bearer with `GET /api/identity/agent/me` (scope `identity:read`).

## WebMCP

Browser WebMCP and backend MCP are separate transports. The MCP card at `/.well-known/mcp/server-card.json` stays a stub (`capabilities.tools` false). WebMCP tools register in the page with `document.modelContext` (fallback `navigator.modelContext`). The generated list is [`/.well-known/webmcp.json`](/.well-known/webmcp.json). `/` registers `navigate` on page load. `/Feedback` and `/Settings` add route-scoped tools that run only on that SPA route. Inspector: `typeof navigator.modelContext`, Chrome Canary flag `#enable-webmcp-testing` or the MCP-B polyfill, then Copy trace.

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
(`POST /api/identity/credentials/passkey` — session required). Settings also offers
**Add an existing passkey** (`POST /api/identity/credentials/passkey/existing/options`
then `POST /api/identity/credentials/passkey/existing`): assert a passkey that belongs
to another **active** principal, re-parent that credential, and merge the other
principal into the current one. Proof is the assertion itself. A passkey already on
this account is **409**; a merged or quarantined source is **403**. After merge the
source cannot sign in; a stale identity-session cookie for it is rejected. Signing in
with the moved passkey authenticates as the surviving principal (lookup is by
credential handle, not authenticator user handle).

**Not offered:** email register, password reset, magic-link-only accounts as the
primary path. Microsoft 365 is offered when `SignInOptions.Methods` lists `Microsoft365`
and is not the agent- or human-priority story. When listed it is a named OIDC scheme
(`entra`); identity-session stays DefaultScheme. The SPA “Continue with Microsoft 365”
button is a BFF `Challenge(“entra”)` — no WASM MSAL session. Default non-mock SPA
auth projects the identity-session cookie via `GetCurrentSession`.

### Microsoft 365 bootstrap choice

When Entra is enabled and site settings `AllowBootstrap` is on, an **unknown**
Microsoft 365 handle does **not** mint a principal on the OIDC ticket. The server
parks the validated claims (10-minute, single-use, HttpOnly cookie) and redirects to
[/Login/Microsoft365/Choose](/Login/Microsoft365/Choose):

| Choice | What happens |
|--------|----------------|
| **Create a new account** | `POST /api/identity/entra/choice/create` — original bootstrap (principal + EntraAccount + session) |
| **I already have an account** | Passkey assertion, then `POST /api/identity/entra/choice/existing` — attach the parked EntraAccount onto that principal (link semantics; **409** if the handle is owned elsewhere) |

Parked claims expire; the page shows “Session expired, sign in with Microsoft 365 again.”
Sync-hit (handle already owned) and `AllowBootstrap` off are unchanged — the choose
page is never reached. **Link Microsoft 365** from Settings, when the handle is owned
by another active unmerged principal, **merges** that principal into the caller
instead of 409 (the Entra sign-in is the proof). 409 remains for already-on-this-account
and for merged/quarantined owners; a second Entra handle on a principal that already
has one is 409 “Microsoft 365 already linked.”

### Configured vs enabled

**Configured** (deploy-time, secret-bearing) stays in `Authentication:Entra:*`:
client id, secret, authority tenant, and `PublicOrigin`. Listing `Microsoft365` in
`SignInOptions.Methods` is the scheme-registration gate. `dev entra setup` writes
that list plus the Entra user secrets.

**Enabled for users** is runtime admin policy on the site-settings singleton
(`/Admin/Authentication`). First boot copies whether `Microsoft365` is listed and `AllowBootstrap`
into settings once; after that the admin page is the source of truth for those
two. Disabling sign-in at runtime refuses **new** challenges (403 `Sign-in disabled`);
existing Entra credentials and sessions stay.

**Tenant acceptance** is the same admin page, not `Authentication:Entra:TenantId`.
That setting is only the OpenID Connect authority (`{instance}/{tenant}/v2.0`):
a home-tenant GUID, `organizations` (work and school), or `common` (work, school,
and personal Microsoft accounts). The token issuer must still match the token's
own `tid`.

| Mode | Token `tid` |
|------|-------------|
| Restricted | Must be in the accepted-tenant list. An empty list refuses every tenant (403 `Untrusted tenant`). One id is the single-tenant case. |
| Any | Any non-empty GUID. |

A GUID authority completes OpenID Connect only for that tenant. Accepting another
tenant, or any tenant, needs authority `organizations` or `common` and an app
registration whose supported account types are multi-tenant. `dev entra setup`
creates `AzureADMyOrg` (this tenant only). Change supported account types to
`AzureADMultipleOrgs` or `AzureADandPersonalMicrosoftAccount` before other
tenants can finish sign-in.

Any tenant means any account that can finish OpenID Connect against this app
registration can sign up or link, still subject to the bootstrap and link
toggles. There is no tenant blocklist. Personal Microsoft accounts use tenant
`9188040d-6c67-4c5b-b112-36a304b66dad`. They reach the app only when the
authority is `common` or `consumers`. `organizations` never issues those tokens.
Restricted mode accepts that id only when an administrator lists it.

Each list entry is a tenant id and an optional domain. Sign-in still matches the
token `tid` only. On `/Admin/Authentication`, look up the customer's primary
domain. The server reads the tenant id from the OpenID discovery `issuer`
(`https://login.microsoftonline.com/{tenant-id}/v2.0`) and stores the domain with
it. The list shows `contoso.com (guid)`. A known tenant id can still be added by
hand and is shown as the bare GUID until its domain is looked up, which stores
the domain on that same row. A different domain for a tenant that already has
one leaves the list unchanged. The browser does not compute the id.

First boot copies a GUID `TenantId` into the restricted list with no domain.
`organizations` and `common` seed an empty list until an administrator chooses a
list or Any. A database row that stored only GUID strings becomes entries with
no domain; which tenants may sign in does not change.
The first human passkey registration becomes Administrator
(`TryClaimFirstAdministratorAsync`) and can open `/Admin/Authentication`.

Listing `Microsoft365` with settings `EntraSignInEnabled=false` (or
the reverse) logs a warning at boot so the two are never silently confused.

### Local Entra setup

From a repo checkout, with Azure CLI logged in (`az login`):

```bash
dev entra setup
# when more than one tenant is visible:
dev entra setup --tenant contoso.com
# optional: --name “TimeWarp Architecture Dev” --public-origin https://arch.timewarp.work
#           --redirect-uri https://extra.example/signin-oidc --new-secret --dry-run
dev entra status
dev entra disable   # removes Microsoft365 from SignInOptions:Methods; does not change Azure
```

`dev entra setup` finds or creates the app registration, unions redirect URIs
(`https://localhost:63611/signin-oidc`, `https://localhost:63610/signin-oidc`, plus
`--public-origin` when given), ensures a service principal, mints a client secret on
first write, when the stored tenant or app registration differs from the one setup is
writing, or with `--new-secret`, and writes Web.Server user secrets. Switching tenants
(or otherwise changing `ClientId` / `TenantId`) re-mints automatically; `--new-secret`
is only for rotating a secret on the same app. The secret is never printed. First run
seeds the Microsoft365 list entry and AllowBootstrap into site settings; after that use
`/Admin/Authentication` to offer or hide Microsoft 365 sign-in. Then
`dev run`, browse the app, and click **Continue with Microsoft 365** when offered.

Pass `--tenant <id|domain|name>` to select which organisation the registration belongs
to. When more than one tenant is visible, setup refuses with a candidate table and
requires `--tenant` (non-interactive; it does not guess). `az account list` only shows
tenants that have a subscription; `az account tenant list` also lists subscription-less
tenants the identity can access. Sign in with an account from that tenant's default
domain (for example `@contoso.com`); other tenants' accounts must be invited as
guests first.

### Manual Entra configuration (redirect URIs and PublicOrigin)

Register **all three** redirect URIs on the Entra app registration for a given
environment, then set `Authentication:Entra:PublicOrigin` to the origin the
browser actually uses:

| Path | Redirect URI | `PublicOrigin` |
|------|----------------|----------------|
| Direct Web.Server | `https://localhost:63611/signin-oidc` | unset (request-derived) |
| Aspire YARP ingress | `https://localhost:63610/signin-oidc` | `https://localhost:63610` |
| Shared hostname (Caddy → YARP) | `https://<public-host>/signin-oidc` | `https://<public-host>` |

Any proxied deployment — for example, Azure Container Apps — **must**
set `PublicOrigin`. Web.Server is reached over **http** behind YARP/ACA and
does not consume `X-Forwarded-*` (passkey RP-ID must not trust spoofable
forwarded headers). Unset `PublicOrigin` would send Entra `http://…/signin-oidc`.
The named `entra` scheme always writes Secure correlation/nonce cookies.

User-secrets example (Web.Server project):

```bash
dotnet user-secrets set “SignInOptions:Methods:0” “Passkey” --project source/container-apps/web/projects/web-server
dotnet user-secrets set “SignInOptions:Methods:1” “Microsoft365” --project source/container-apps/web/projects/web-server
dotnet user-secrets set “Authentication:Entra:Instance” “https://login.microsoftonline.com/” --project source/container-apps/web/projects/web-server
dotnet user-secrets set “Authentication:Entra:TenantId” “<tenant-id>” --project source/container-apps/web/projects/web-server
dotnet user-secrets set “Authentication:Entra:ClientId” “<app-id>” --project source/container-apps/web/projects/web-server
dotnet user-secrets set “Authentication:Entra:ClientSecret” “<secret>” --project source/container-apps/web/projects/web-server
dotnet user-secrets set “Authentication:Entra:CallbackPath” “/signin-oidc” --project source/container-apps/web/projects/web-server
dotnet user-secrets set “Authentication:Entra:AllowBootstrap” “true” --project source/container-apps/web/projects/web-server
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
- The OpenAPI document lists **402** and **503** on the paid paths in the table below.
  Fetching `/openapi/v1.json` never returns 402.
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
- MCP, A2A, and Agent Skills cards are published. The MCP card is a stub: `capabilities.tools` is false. Browser WebMCP is a separate transport; see the WebMCP section above and [/llms.txt](/llms.txt).
