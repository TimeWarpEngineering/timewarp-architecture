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

The scheme was lost on the YARP hop, not in Web.Server. `"X-Forwarded": "Set"` copies `Request.Scheme`, and the hop from Caddy is plain http, so Caddy's `X-Forwarded-Proto: https` was replaced before Web.Server read it. `HttpAppBaseUrlAccessor.GetRequestOrigin` (mail and `LinkPreview`) still reads that first forwarded value. Web.Server still does not call `UseForwardedHeaders`.

The yarp project now calls `UseTrustedProxyForwardedHeaders` before `MapReverseProxy`. It applies `X-Forwarded-Proto` and `X-Forwarded-Host` with `ForwardLimit` 1, and only when the peer is in `TrustedProxy:KnownProxies` or `TrustedProxy:KnownNetworks`. An absent section trusts loopback (`127.0.0.1` and `::1`), which is Caddy on this machine. An explicit section replaces that default. Both lists empty skips the middleware: the framework treats two empty allow-lists as trust-everyone, and this ingress must not. A bad address or CIDR fails startup. `X-Forwarded-Host` is honored because Caddy sets it to the request Host and the public-origin smoke sends it without replacing `Host`. An untrusted peer still cannot inject proto or host; `Set` then forwards what the ingress itself accepted. `X-Forwarded-For` is not applied. Web.Server rate limiting reads `Connection.RemoteIpAddress` and does not use that header. `ASPNETCORE_FORWARDEDHEADERS_ENABLED` is not used: it omits Host and clears the allow-lists.

Run mode executes the yarp project (`AddProject`, launch profile `Yarp`, `DisableForwardedHeaders`) so this middleware is the process on the pinned port. The http endpoint is not DCP-proxied: a proxy hop in front of it can replace `X-Forwarded-Proto` with the scheme that hop saw (http) and `ForwardLimit` 1 would drop Caddy's https. The resource name stays the literal `"ingress"` (`ServiceNames.IngressServiceName`) so the dev-cli text scan and TWA0007 still agree. Publish mode keeps the AddYarp image, so Compose, Kubernetes, and ACA manifests are unchanged. That image does not contain this middleware. A TLS-terminating edge in front of it (ACA managed ingress, a Kubernetes ingress controller) may still produce `http` tags; that path is not verified and is out of scope here. `SiteMetadata:BaseUrl` and `PublicOrigin` cover link previews and Entra there. Mail permalinks have no override. A deployment of the yarp project behind a proxy that is not on loopback sets `KnownNetworks` to that proxy's CIDR.

`SiteMetadata:BaseUrl` still wins for link-preview tags when it is set. Nothing hard-codes `arch.timewarp.work`.

Files: `source/container-apps/yarp/trusted-proxy-forwarded-headers.cs`, `program.cs`, `global-usings.cs`, `appsettings.json`; AppHost `program.cs`, `constants.cs`, `aspire-app-host.csproj`; `ServiceNames.IngressServiceName`; in-proc http listener (`InProcTestPorts.YarpHttpUrl`, default `http://localhost:8444`); standalone and Aspire ingress smokes; task 112 `runbook-public-path.md`; `skills/tw-deploy`. Design notes on the mail accessor, link preview, and passkey host accessor record that the trust check stays on the ingress.

Tests: `yarp-integration-tests` 10/10 (trusted `GET http://localhost:8444/` renders the four `https://public.example.test` tags; the same headers with remote address `203.0.113.5` stay `http://localhost:8444` and do not mention `public.example.test`). Aspire `IngressSmoke` 10/10 through the AppHost `ingress` http endpoint, including the same four tags. `aspire-app-host` and `yarp` build with 0 warnings. The untrusted remote address is a test-only `IStartupFilter`; it is not in the product pipeline.

### Review disposition

Implementation review ran 2 rounds at effort 2 (by-diff budget) with the `general` reviewer. Disposition: **clean**. Final counts: bug 1 fixed, suggestion 2 fixed, nit 0, open 0, wontfix 0.

- M1 (bug): run mode loads ingress routes from the yarp project's `appsettings.Development.json`, which had no template conditionals. `--api false` / `--grpc false` apps sent `/api/**` and `/grpc/**` to services that do not exist. The routes and clusters are now gated with `//#if (api|web|grpc)`. `dev template-smoke` SmokeNoApi confirms the output.
- M2 (suggestion): JSON `[]` reads as absent through `Exists()`, so both lists empty trusted loopback. `ReadList` now checks the declared key. `trusted-proxy-configuration-tests.cs` adds 5 tests (no lists declared, both empty, network-only, scalar, malformed CIDR).
- M3 (suggestion): the ACA "already https" claim was unverified. The docs now say the published AddYarp path may forward http and point to `SiteMetadata:BaseUrl` / `PublicOrigin`. That path is out of scope here.

Gates after fixes: `dev build` clean; `yarp-integration-tests` 15/15; `dev template-smoke` SUCCEEDED; `ganda repo audit` pass. The Aspire `IngressSmoke` suite was not re-run in review: for the default flags the route config only gained comment lines.

Artifacts: `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`.

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
- Review oracle: Claude Opus 5.5 (ganda task work, headless), effort 2, roster general, 2 rounds
