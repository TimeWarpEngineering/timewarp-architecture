# Entra behind ingress: explicit public callback origin and secure OIDC cookies

## Description

Make the named `entra` OpenID Connect scheme (219-002) work when Web.Server runs behind the
Aspire YARP ingress (`https://localhost:63610`) and behind the shared hostname
(`https://arch.timewarp.work`, Caddy → YARP http 63620), and by extension behind Azure
Container Apps ingress for crunchit (008-004). Direct launch on `https://localhost:63611`
already works; every proxied path is broken today.

## Parent

219

## Problem (verified in source)

- YARP forwards to Web.Server over **http** (`http://_http.web-server`, aspire-app-host
  `program.cs`). Web.Server deliberately does **not** call `UseForwardedHeaders`
  (task 104-031: the passkey RP-ID selection must not consume spoofable `X-Forwarded-*`;
  only the original `Host` is forwarded). So `Request.Scheme` is `http` behind any proxy.
- `OpenIdConnectHandler` builds `redirect_uri` from `Request.Scheme + Host + CallbackPath`
  → `http://arch.timewarp.work/signin-oidc`. Entra rejects non-https redirect URIs for
  non-localhost hosts, so the challenge never starts on the shared hostname.
- The handler's correlation and nonce cookies default to `SameSite=None` with
  `SecurePolicy=SameAsRequest`. Seen as http, they are written without `Secure`; the browser
  (on https) rejects `SameSite=None` cookies without `Secure` → "Correlation failed" on the
  callback. This also breaks `https://localhost:63610`, not only the shared hostname.
- 219-002 scoped out live-tenant testing ("no live tenant / app registration"), so this gap
  was never exercised.

## Requirements

- Add `Authentication:Entra:PublicOrigin` (string, optional, e.g. `https://arch.timewarp.work`).
  When set, the challenge's `redirect_uri` is `{PublicOrigin}{CallbackPath}` regardless of
  `Request.Scheme`/`Host` (set `ProtocolMessage.RedirectUri` in `OnRedirectToIdentityProvider`,
  and use the same value for the code-redemption `redirect_uri` on the callback so it matches).
  When unset, current request-derived behaviour stays (direct launch keeps working).
  Validate it as an absolute https URI (or http only for `localhost`) in
  `EntraAuthenticationOptionsValidator`. Consider defaulting from the AppHost's
  `Ingress:PublicUrl` if a clean seam exists; do not add forwarded-header processing.
- Force `CorrelationCookie.SecurePolicy` and `NonceCookie.SecurePolicy` to `Always` for the
  `entra` scheme (the browser side is always https on every supported path; direct http
  launch is not a supported Entra path).
- Keep `identity-session` as DefaultScheme; do not touch RP-ID / Host handling from 104-031.
- The `LocalReturnUrl` sanitiser must still accept the post-callback local redirect when the
  public origin differs from the internal one.
- Document in `source/container-apps/web/features/identity/` Design regions and in the
  identity guide: the three redirect URIs to register per environment (direct
  `https://localhost:63611/signin-oidc`, ingress `https://localhost:63610/signin-oidc`,
  shared `https://<public-host>/signin-oidc`), and that `PublicOrigin` must be set for any
  proxied deployment (crunchit ACA included).

## Checklist

- [ ] `PublicOrigin` option + validator rules + Design region
- [ ] `OnRedirectToIdentityProvider` / token-redemption redirect_uri override when set
- [ ] Correlation + nonce cookies `SecurePolicy.Always` on the `entra` scheme
- [ ] Tests: scheme options show `Always` secure policy; challenge 302 `Location` carries
      `redirect_uri={PublicOrigin}/signin-oidc` when set and request-derived when unset;
      validator rejects non-https non-localhost `PublicOrigin`
- [ ] Docs: redirect URIs per environment; user-secrets example including `PublicOrigin`
- [ ] `dev build` 0/0; `dotnet test -- --filter-class Entra` green
- [ ] Results and How to validate (include a manual live check behind
      `https://localhost:63610` if a tenant is available; otherwise state not exercised)

## Session

- Created: 59889 (2026-09-15)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N

## Notes

- Prior art: 219-002 Results ("Not in scope: live Entra tenant"), 104-031 (original-Host
  forwarding, no `UseForwardedHeaders`), aspire-app-host `program.cs` YARP block
  (catch-all → web-server http cluster, `WithTransformUseOriginalHostHeader(true)`),
  `Ingress:PublicUrl` comment in the same file.
- Files: `source/container-apps/web/features/identity/entra-authentication-registration-server.cs`,
  `entra-authentication-options-application.cs`, `entra-authentication-options-validator-application.cs`,
  `challenge-entra/`, `source/container-apps/web/platform/identity-host/local-return-url-server.cs`,
  tests `tests/container-apps/web/web-server-integration-tests/features/identity/entra-*.cs`.
- Do not "fix" this with `UseForwardedHeaders`; that reverses a deliberate security decision.

## Results

_Pending._

### How to validate

_Pending._
