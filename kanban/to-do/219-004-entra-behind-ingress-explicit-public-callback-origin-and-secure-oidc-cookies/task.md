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

- [x] `PublicOrigin` option + validator rules + Design region
- [x] `OnRedirectToIdentityProvider` / token-redemption redirect_uri override when set
- [x] Correlation + nonce cookies `SecurePolicy.Always` on the `entra` scheme
- [x] Tests: scheme options show `Always` secure policy; challenge 302 `Location` carries
      `redirect_uri={PublicOrigin}/signin-oidc` when set and request-derived when unset;
      validator rejects non-https non-localhost `PublicOrigin`
- [x] Docs: redirect URIs per environment; user-secrets example including `PublicOrigin`
- [x] `dev build` 0/0; `dotnet test -- --filter-class Entra` green
- [x] Results and How to validate (include a manual live check behind
      `https://localhost:63610` if a tenant is available; otherwise state not exercised)
- [x] Implementation review disposition (effort 1, 2 rounds, clean)

## Session

- Created: 59889 (2026-09-15)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N
- Implementer: grok session 01a0a4e6-9218-7011-899d-00119d0396f8 (2026-09-15)
- Review oracle: grok session 01a0a4f1-e56d-7471-b0e1-fb58eb45d657 (2026-09-15)
- General reviewer (round 1): grok session 01a0a4f4-43f1-7ec2-9373-d01399b7f0f6 (2026-09-15)
- General reviewer (round 2): grok session 01a0a4fc-ae9c-7920-8d7d-fc8d17f650bc (2026-09-15)

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
- Implementation review (effort 1, `general`, 2 rounds): disposition **clean**. Trail under `review/` (`review-framework.md`, `round-1/`, `round-2/`, `disposition.md`). Round 1 M1 (auth.md cookie claim) and M2 (Design SecurePolicy wording) fixed on this id.

## Results

Named `entra` OIDC scheme works behind YARP/ACA without `UseForwardedHeaders`. Optional
`Authentication:Entra:PublicOrigin` overrides the challenge and code-redemption `redirect_uri`;
correlation and nonce cookies are `SecurePolicy.Always`. Direct launch with PublicOrigin unset is
unchanged.

**Implemented**
- `EntraAuthenticationOptions.PublicOrigin` plus `TryGetPublicRedirectUri` (`{origin}{CallbackPath}`)
- Validator: empty is valid; absolute https required; http only for `localhost` / `127.0.0.1`; path,
  query, fragment, and userinfo refused
- `OnRedirectToIdentityProvider` and `OnAuthorizationCodeReceived` set `ProtocolMessage.RedirectUri`
  / `TokenEndpointRequest.RedirectUri` from IOptions when PublicOrigin is set (handler also stores
  the challenge value in `OpenIdConnect.Code.RedirectUri` for redemption)
- `CorrelationCookie.SecurePolicy` and `NonceCookie.SecurePolicy` = `Always` on the named scheme
- `identity-session` remains DefaultScheme; RP-ID / Host handling (104-031) untouched
- `LocalReturnUrl.Sanitize` still accepts `/Profile` and refuses an absolute public origin

**Files**
- `source/container-apps/web/features/identity/entra-authentication-options-application.cs`
- `source/container-apps/web/features/identity/entra-authentication-options-validator-application.cs`
- `source/container-apps/web/features/identity/entra-authentication-registration-server.cs`
- `source/container-apps/web/platform/identity-host/local-return-url-server.cs`
- `source/container-apps/web/projects/web-server/appsettings.json` (`PublicOrigin: ""`)
- `source/container-apps/web/projects/web-spa/wwwroot/auth.md` (redirect URIs + user-secrets)
- `source/container-apps/aspire/projects/aspire-app-host/program.cs` (Design: no copy from
  `Ingress:PublicUrl`)
- Tests: `entra-scheme-registration-tests.cs`, `entra-public-origin-tests.cs`,
  `entra-authentication-options-validator-tests.cs`, `entra-challenge-tests.cs`,
  `local-return-url-tests.cs`

**Decisions / deviations**
- Did **not** default PublicOrigin from AppHost `Ingress:PublicUrl`. That key is a dashboard
  display URL and can differ from the origin in use (`https://localhost:63610` vs
  `https://arch.timewarp.work`). Auto-copy would break one of those paths. Set PublicOrigin
  explicitly on Web.Server for any proxied Entra deployment (crunchit ACA included).
- Did **not** add `UseForwardedHeaders`.
- Direct http launch remains an unsupported Entra path.
- Review M1: `auth.md` splits unset-PublicOrigin http `redirect_uri` from the named scheme's always-Secure cookies.
- Review M2: Design pins `SecurePolicy.Always` without implying net10 omits Secure.

**Test outcomes**
- `dotnet run tools/dev-cli/dev.cs -- build`: 0 Warning(s), 0 Error(s)
- `cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class Entra`: 35 passed, 0 failed

**Not exercised:** live Entra tenant / app registration behind `https://localhost:63610` (no
confidential client in this session). Isolated TestServer covers the 302 `Location` and Secure
cookie assertions without a tenant.

### How to validate

**Smoke**

```bash
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class EntraSchemeRegistration_
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class EntraPublicOrigin_
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class EntraAuthenticationOptionsValidator_
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class EntraLocalReturnUrl_
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class EntraChallenge_
```

**Expect**
- `EntraSchemeRegistration_`: with Entra enabled, `OpenIdConnectOptions` for scheme `entra` has
  `CorrelationCookie.SecurePolicy` and `NonceCookie.SecurePolicy` = `Always`; DefaultScheme stays
  `identity-session`
- `EntraPublicOrigin_`: challenge 302 `Location` query has
  `redirect_uri=http://localhost/signin-oidc` when PublicOrigin is unset, and
  `redirect_uri=https://arch.timewarp.work/signin-oidc` when set; Set-Cookie for correlation and
  nonce includes `Secure` on the http TestServer
- `EntraAuthenticationOptionsValidator_`: `https://arch.timewarp.work` and
  `http://localhost:63620` valid; `http://arch.timewarp.work` and a relative host invalid
- `EntraLocalReturnUrl_`: `/Profile` accepted; `https://arch.timewarp.work/Profile` sanitizes to `/`
- `EntraChallenge_`: bootstrap with PublicOrigin set still 302s to local `/` (not the public origin)

**Automated gate**

```bash
dotnet run tools/dev-cli/dev.cs -- build
# expect: Build succeeded. 0 Warning(s) 0 Error(s)

cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class Entra
# expect: passed, failed 0 (35 succeeded in the implementer session)
```

**Manual (live tenant; optional)**

With Web.Server user secrets `Authentication:Entra:Enabled=true`, a real ClientId/secret/trusted
tenant, and `Authentication:Entra:PublicOrigin=https://localhost:63610`, run AppHost and:

```bash
curl -skI 'https://localhost:63610/api/identity/entra/challenge?mode=bootstrap&returnUrl=%2F'
```

Expect HTTP 302, `Location` host `login.microsoftonline.com`, query
`redirect_uri=https://localhost:63610/signin-oidc`, and correlation/nonce `Set-Cookie` with
`Secure`. Complete the Entra login in a browser on `https://localhost:63610` and land on `/`
without "Correlation failed".

**Not in scope:** live Entra tenant / app registration in CI (not exercised this session). Do not
enable `UseForwardedHeaders` to "fix" scheme.

**Review**
- Rounds: 2 · Effort: 1 · Roster: general
- Counts (final): bug 0 open / 0 fixed / 0 wontfix; suggestion 0 / 1 / 0; nit 0 / 1 / 0
- Disposition: **clean** (M1 and M2 fixed on this task id; no exceptions)
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/round-2/general.md`, `review/round-2/merged.md`, `review/disposition.md`
