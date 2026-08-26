# How to configure Cloudflare as the agent-welcome outer ring

Operator notes for putting a generated TimeWarp Architecture app behind Cloudflare.
This is **not** Identity, **not** TimeWarp.402, and **not** a Worker sample rewrite.

The template already ships app-level limits (task **104-015**) and agent-welcome discovery
files (tasks **104-017**–**104-020**). Cloudflare is optional extra volumetric defense.

## Two rings

Locked product decision **11** on epic **104**: Cloudflare is the outer ring (DDoS / WAF /
crude IP limits). Identity + 402 are app law. Do not default-block all AI bots.

| Ring | Job | Not its job |
|------|-----|-------------|
| **Edge (Cloudflare)** | Volumetric DDoS, WAF, per-IP floods across many source addresses | Who the caller is; whether they paid; principal/credit ledgers |
| **App (origin)** | TimeWarp.Identity (passkey / agent key / session-token) and TimeWarp.402 (paid?) | Absorbing multi-IP volumetric floods |

The same split is recorded in the **EDGE VS APP** Design region of
`source/container-apps/web/platform/abuse/abuse-rate-limit-options-application.cs`.

- Edge catches multi-IP floods **before** they reach origin.
- App still protects origin when traffic already passed the edge (local, private path,
  misconfigured edge). Cheap rejection is structured `application/problem+json` **429**
  (`AbuseRateLimitOptions`), before ceremony work or `PaymentGate`.
- App partition is `Connection.RemoteIpAddress`. Behind Cloudflare that is the proxy hop
  unless you restore the visitor IP (see [Client IP behind Cloudflare](#client-ip-behind-cloudflare)).
  That collapse is accepted for v1: it still bounds origin melt; edge handles multi-IP.

Sybil defense stays in the app: infinite free principals are OK if useless; power costs
payment or earned trust. Rate-limit register + 402 challenge at origin even if the CDN is
perfect.

## Agent-welcome (do not block all bots)

This template is meant to score on [isitagentready.com](https://isitagentready.com/) via
**real surfaces** (discovery files, markdown twins, auth.md, x402 tip/meter — 017–020),
not by hiding behind a “block all bots” toggle.

Origin already serves `robots.txt` with Content Signals
`ai-train=yes, search=yes, ai-input=yes` for major AI crawlers and `*`. `auth.md` states
there is **no blanket block of AI training crawlers**.

Cloudflare defaults and one-click bot products often do the opposite. Leave them off or
set them to **Allow**.

### Dashboard posture

In **Security** → **Settings**, filter **Bot traffic**:

| Control | Agent-welcome setting | Why |
|---------|----------------------|-----|
| **Bot Fight Mode** (Free) | **Off** | Challenges known-bot patterns on the whole zone. Skip/Allow custom rules **cannot** bypass it. Agents and verified crawlers get JS challenges or blocks. |
| **Super Bot Fight Mode** (Pro+) | Optional. **Verified bots = Allow**. Prefer **Allow** (or at most Managed Challenge) for Definitely/Likely automated on agent/API paths | You can Skip SBFM with a WAF custom rule; BFM cannot. |
| **Block AI bots** / **Configure AI bot policies** | **Allow** for Search, Agent, and Training | “Block AI bots” blocks verified training crawlers (legacy control; Cloudflare is replacing it with Search / Agent / Training policies). Blocking Agent or Training fights the template’s Content Signals. |
| **Set your preference to block training in robots.txt** (managed `robots.txt`) | **Off** | When on, Cloudflare **prepends** `Disallow: /` for GPTBot, ClaudeBot, Google-Extended, CCBot, … in front of origin `robots.txt`. That overrides the shipped `ai-train=yes` signals. |
| **AI Crawl Control** | **Allow** crawlers (do not Block-all) | Use it to *observe* crawlers. Blocking here is enforcement, not a `robots.txt` hint. |

If you enable Super Bot Fight Mode, add a **Skip** custom rule for discovery and agent
API prefixes so HTML challenges never sit in front of `robots.txt`, `llms.txt`, `auth.md`,
or JSON APIs:

- Paths: `/robots.txt`, `/sitemap.xml`, `/llms.txt`, `/auth.md`, `/index.md`
- Prefixes: `/api/identity/`, `/api/tip`, `/api/demo/metered-capability`, `/api/agent/`

Verified / signed bot identity (Web Bot Auth, Cloudflare verified-bot list) is a *plus*
for allowing known crawlers. It is **not** a substitute for paying: an agent that needs
power still registers a key and settles 402. Do not require a human CAPTCHA as the only
path onto those APIs.

## Align edge rate limits with app limits (104-015)

Do **not** change app code for this guide. Defaults live in `AbuseRateLimitOptions`
(`web-server` `appsettings.json`; master switch `AbuseRateLimitOptions:Enabled`).

| App policy | Paths | App default |
|------------|-------|-------------|
| `principal-registration` | `api/identity/passkey/register[/options]`, `api/identity/agent/register[/options]` | **10 / 60s** sliding |
| `payment-challenge` | `api/tip`, `api/demo/metered-capability` | **30 / 60s** sliding |

Rejected app traffic is HTTP **429** `application/problem+json` with a `policy` extension
and optional `Retry-After`. Edge blocks are usually Cloudflare **1015** (HTML), which
agents cannot parse as problem+json.

**Coarse means more permissive, not tighter.** Edge limits must be **at least as coarse**
as the table above (permit ≥ app permit over a comparable window). A single well-behaved
IP should hit the **app 429** before Cloudflare 1015. Edge still matters for **many IPs**
each under the app window, and for DDoS that never should reach origin.

### Suggested WAF rate-limit rules

Dashboard: **Security** → **Security rules** → **Create rule** → **Rate limiting rules**.
Docs: [Rate limiting rules](https://developers.cloudflare.com/waf/rate-limiting-rules/).

Characteristic: **IP** (Free is IP-only). Action: **Block** (Free) or Block / Managed
Challenge (Pro+). Prefer **Block** on API paths — a Managed Challenge is as hostile to
agents as Bot Fight Mode.

**Pro+ (60s period available)** — start ~3× the app window so origin 429 still fires first:

1. **Principal registration** — when path is one of
   `/api/identity/passkey/register`, `/api/identity/passkey/register/options`,
   `/api/identity/agent/register`, `/api/identity/agent/register/options`
   (include method GET and POST). **≥ 10 requests / 60s**; suggested **30 / 60s**.
2. **Payment challenge** — when path is `/api/tip` or `/api/demo/metered-capability`
   (and the discovery alias `/api` if you expose it at the zone). **≥ 30 / 60s**;
   suggested **90 / 60s**.

**Free plan** — one rate-limit rule, **10s** period and mitigation only. Combine the
paths above into that single expression. Scale the floor: 10/60s ≈ **2 / 10s**,
30/60s ≈ **5 / 10s**. Suggested starting point: **10 requests / 10s** on the combined
expression (coarser than both app policies; DDoS/WAF still absorb the rest).

Keep WAF **managed rules** on for ordinary CVE/exploit signatures. That is not a bot
block-all.

## Optional Worker front door

Not required. The template already hosts tip and metered capability on **web-server**
(task **104-009** / **104-011**).

If you still want an edge Worker (the timewarp.software `worker/tip.js` pattern;
archived epic **102-002**):

- Isolate payment: only the tip/meter **resource** may return **402**.
- Free discovery (`/`, `/robots.txt`, `/llms.txt`, `/auth.md`, `/index.md`, static SPA)
  must stay cheap — static or a short-circuit, never a payment cold start.
- Payment **disabled or misconfigured → 503**, never 402 on those free routes
  (locked decision **8** on epic **104**).
- Do not move the principal ledger, credits, or token store into the Worker.

This is a pointer, not a sample to copy into the template.

## What never lives only at the edge

These stay origin (or a shared store the origin owns):

- Principal records (`IPrincipalStore`) and credentials
- Credit ledger / settle → Funded (TimeWarp.402)
- Browser sessions (`identity-session`) and agent bearer tokens (`IAgentTokenStore`)
- `PaymentGate` policy (402 vs 503)

Cloudflare may cache public discovery files. Do **not** cache `api/identity/*` or
unpaid 402 challenges as if they were static.

## Client IP behind Cloudflare

Until forwarded headers / PROXY protocol are trusted at origin, every client behind
Cloudflare can share one app rate-limit partition. Restore visitor IP only from
**Cloudflare’s published ranges** (see
[Restoring original visitor IPs](https://developers.cloudflare.com/support/troubleshooting/restoring-visitor-ips/restoring-original-visitor-ips/)).
Do not blindly trust `X-Forwarded-For` from the public internet. Ingress notes:
task **112**.

## Related

- Epic **104** locked decisions 8, 11, 12
- Task **104-015** — app `principal-registration` / `payment-challenge` limiter
- `source/container-apps/web/platform/abuse/` — options, module, co-located tests
- [How to split agent identity across web-server and api-server](how-to-agent-identity-host-split-web-vs-api.md)
