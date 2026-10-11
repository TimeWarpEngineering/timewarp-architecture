# Task 305: Honor trusted X-Forwarded-Proto at the YARP ingress so link previews and mail use https behind Caddy

## Description

Follow-up to task 304 (PR #462, merged as `3aeb796e6`). On the live shared hostname
https://arch.timewarp.work/ the link-preview tags come out as **http**, so Discord/X/Slack get
`http://` image and canonical URLs. Verified 2026-10-11 11:50 BKK via Globalping (DE probe,
`GET /`, `Accept: text/html`; response `via: 1.1 Caddy`, `server: Kestrel`):

```html
<meta property="og:url" content="http://arch.timewarp.work/" />
<meta property="og:image" content="http://arch.timewarp.work/images/og-image.png" />
<meta name="twitter:image" content="http://arch.timewarp.work/images/og-image.png" />
<link rel="canonical" href="http://arch.timewarp.work/" />
```

The host is right (X-Forwarded-Host works); only the scheme is wrong.

## Parent

304

## Root cause (verify, don't assume it is complete)

Live chain (task 112 / 219-004): browser → **Caddy :443 (TLS)** → `localhost:63620` **Aspire YARP
ingress over plain http** → YARP → Web.Server over http.

- Caddy sends `X-Forwarded-Proto: https` to YARP.
- Every Web.Server route in YARP has the `"X-Forwarded": "Set"` transform (task 070-008;
  `source/container-apps/yarp/appsettings.Development.json`, generated routes in
  `source/container-apps/yarp/program.cs`). `Set` **overwrites** X-Forwarded-Proto/Host with what
  YARP itself saw: Host = arch.timewarp.work (correct, Caddy preserves Host) but
  Proto = `http` (YARP's own inbound hop from Caddy is http).
- Web.Server then reads the first X-Forwarded-Proto value
  (`HttpAppBaseUrlAccessor.GetRequestOrigin`, shared by mail and `LinkPreview`) and gets `http`.

So adding `UseForwardedHeaders` only in Web.Server would **not** fix it: the `https` from Caddy
is already gone by the time the request reaches Web.Server. Web.Server also deliberately does
not call `UseForwardedHeaders` (task 104-031 / 070-008, see the Design region of
`source/container-apps/web/platform/identity-host/http-request-host-accessor-server.cs`):
passkey RP-ID selection must not trust spoofable client headers. Do not weaken that.

Same root cause affects mail permalinks (`HttpAppBaseUrlAccessor`) behind Caddy; 219-004 worked
around it for Entra with an explicit `PublicOrigin`.

## Requirements

1. **Fix at the hop that loses the scheme.** The YARP ingress must honor `X-Forwarded-Proto`
   (and `X-Forwarded-Host` if relevant) **only from a trusted upstream proxy**, so that the
   value it Sets toward Web.Server is the client-facing scheme. Idiomatic option: in the YARP
   app, `UseForwardedHeaders` with `ForwardedHeaders = XForwardedProto | XForwardedHost` (consider
   `XForwardedFor`), `ForwardLimit = 1`, and `KnownProxies`/`KnownNetworks` from configuration
   (default: loopback only, since Caddy runs on the same machine; must be configurable for
   Azure Container Apps / other deployments; do NOT just clear KnownNetworks/KnownProxies to
   trust everyone), placed before `MapReverseProxy` so YARP's `Request.Scheme` becomes `https`
   and its `Set` transform forwards `https`. Keep `Set` semantics (a client that talks to YARP
   directly, not through a trusted proxy, must still not be able to inject a proto/host).
   If you find a better approach (e.g. a YARP transform that forwards the trusted upstream proto),
   record why in Results.
2. Web.Server: keep its current trust model. If you add `ForwardedHeadersOptions` there, it must
   be equally restricted to the trusted ingress and must not change passkey host selection
   behaviour (existing identity / passkey host tests must still pass). Prefer not to.
3. `LinkPreviewMeta` (and mail `GetRequestOrigin`) must produce `https://<public host>/...`
   absolute URLs when the client-facing request was https through Caddy → YARP → Web.Server.
   `SiteMetadata:BaseUrl` still wins when set. Don't hard-code arch.timewarp.work.
4. **End-to-end test through the real pipeline**: a request that arrives at the YARP ingress
   over http from a trusted proxy address carrying `X-Forwarded-Proto: https` and
   `X-Forwarded-Host: <some public host>` (use a test host like `public.example.test`) yields
   `og:url`, `og:image`, `twitter:image` and canonical as `https://public.example.test/...` in
   the HTML of `GET /` from Web.Server. Use the existing ingress test infrastructure
   (`tests/container-apps/aspire/aspire-tests/ingress-smoke-tests.cs`,
   `tests/container-apps/yarp/yarp-integration-tests/standalone-yarp-ingress-smoke-tests.cs`)
   or an equivalent that runs YARP's real middleware + transforms in front of Web.Server.
   Also test the negative: the same headers from an **untrusted** source are ignored (scheme
   stays what YARP saw, host stays YARP's Set value). Unit tests of the header parser alone are
   not enough.
5. Document the new trusted-proxy setting in the YARP appsettings (with a comment) and in the
   relevant docs/runbook (task 112 public-path runbook) so the deployed Caddy setup works without
   extra config.
6. Follow repo skills (`tw-csharp`, `tw-blazor`, `tw-ganda-walk`). Build 0 warnings,
   `ganda repo audit` clean.

## Workaround until deployed

Setting `SiteMetadata__BaseUrl=https://arch.timewarp.work` on Web.Server fixes the link-preview
tags only (not mail).

## Results (required before done)

[What was implemented, files changed, key decisions, test outcomes]

### How to validate (required before done)

**Smoke** (after `./bin/dev run`, simulating Caddy on loopback):

```bash
curl -s -H 'Accept: text/html' -H 'X-Forwarded-Proto: https' -H 'X-Forwarded-Host: public.example.test' \
  http://localhost:63620/ | grep -E 'og:url|og:image"|twitter:image|canonical'
```

**Expect**: all four are `https://public.example.test/...`. After deploy, Globalping
`GET https://arch.timewarp.work/` shows `https://arch.timewarp.work/...` in the same tags.

## Session

- Created: 507654 (2026-10-11)
