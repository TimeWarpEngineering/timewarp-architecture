# Round 1 — security
**Date:** 2026-10-12
**Scope reviewed:** `git diff master...HEAD` (64eb82d29). Read in full: `http-request-host-accessor-server.cs`,
`identity-session-cookie-forwarding-server.cs`, `i-request-host-accessor-application.cs`,
`web-authn-options-application.cs` (Design delta), `web-authn-relying-party-selection-application.cs`,
`web-server/program.cs` (named clients, removed `/service-discovery`), `aspire-service-defaults/extensions.cs`,
`platform/mail/http-app-base-url-accessor-server.cs` (another X-Forwarded-Host reader), the test-host changes, and
master's accessor (`git show master:…/http-request-host-accessor-server.cs`). Decompiled
Microsoft.Extensions.ServiceDiscovery 10.10.0 to check pass-through and `https+http` scheme selection.
Tests run (all pass): `--filter-class Copies_` (7/7), `GetRequestHost_Should` (8/8), `Returns_` (135/135).

## Summary

No security regressions found in the RP-ID, origin, or cookie paths.

- **RP-ID spoofing:** this change does not widen it. On master, `GetPublicHost` (X-Forwarded-Host, else
  Request.Host) already applied to every non-loopback peer. On a loopback Host, master went further and
  trusted the client-supplied `X-TimeWarp-Circuit-Host` from any caller. With that branch gone, the only
  host input left is X-Forwarded-Host, which master already accepted. A spoofer gains nothing new.
- **The value only selects from the allowlist:** `WebAuthnRelyingPartySelection.Select` matches against
  `WebAuthnOptions.AllowedRpIds` and returns the allowlist entry's own casing. An unlisted or empty host
  gets a fixed 400 that does not echo the host back. A forged header can therefore pick only an
  operator-approved RP ID.
- **Origin validation:** it uses the signed clientDataJSON origin, checked against `AllowedOrigins` or the
  selected RP ID. No request header is involved.
- **Other host readers:** nothing in source sets a cookie Domain or calls `UseForwardedHeaders` in
  Web.Server. The only other X-Forwarded-Host reader is `HttpAppBaseUrlAccessor` (mail and link-preview
  links). That code already trusted the header on master and is outside this diff, except for the
  self-call effect in Issue 1.
- **Cookie forwarding:** `IdentitySessionCookieForwardingHandler` is attached only to the
  `ServiceNames.WebServiceName` client (program.cs:165–166). The api-server and grpc-server clients do not
  forward Cookie, the mock principal or the host. The base address is the fixed `https+http://web-server/`,
  with no user-controlled URI.
  - When `Services:web-server` config is missing, the pass-through provider sends the call to
    `https://web-server` (the first scheme in the decompiled `ResolvingHttpDelegatingHandler`). TLS
    validation then protects the cookie.
  - On published targets the cookie crosses the internal network over cleartext http, like the
    ingress → web-server hop already does. This is documented in program.cs.
- **ASPNETCORE_FORWARDEDHEADERS_ENABLED (publish mode):** it processes only X-Forwarded-For and
  X-Forwarded-Proto, not Host. The accessor reads X-Forwarded-Host raw, so this setting has no effect on
  RP-ID selection. It does matter for scheme on the self-call (Issue 1).
- **`/service-discovery` removal is complete:** `ServiceUriHelper`, `ServiceUriProvider`,
  `MockAuthenticationDefaults.CircuitHostHeader` and the `X-TimeWarp-Circuit-Host` literal are all gone.
  `git grep` outside kanban finds only explanatory comments, and nothing in non-.cs files (YARP JSON,
  manifests, TS).

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/platform/identity-host/identity-session-cookie-forwarding-server.cs:91
- Description: The self-call now sets `X-Forwarded-Host` to `GetPublicHost(...)`, which strips the port, and
  sets no `X-Forwarded-Proto`. A second X-Forwarded-Host reader, `HttpAppBaseUrlAccessor.GetRequestOrigin`
  (platform/mail/http-app-base-url-accessor-server.cs:60–75), keeps the port and builds the scheme from
  X-Forwarded-Proto, else `Request.Scheme`. It now sees this header on the hop.
  `SubmitFeedback` goes through `IWebServerApiService`, which uses the web-server named client. So an
  InteractiveServer feedback submission with `Mail:PublicBaseUrl` unset builds its emailed permalink from
  the hop. The result depends on the environment:
  - **Run mode:** `https://localhost/...` with the port lost, so the link is broken. Master produced
    `https://localhost:<port>/...` from Request.Host.
  - **Published:** `http://<public-host>/...`, an https→http downgrade. The hop is plain http, and with no
    X-Forwarded-Proto, publish-mode forwarded-headers leaves Scheme as http.

  The recipient is the filer's own address, so the security impact is low. Mainly this is link
  correctness and a scheme downgrade caused by the new header, not by spoofing.
- Suggestion: On the self-call, forward the circuit request's public origin, not only a port-stripped host:
  - Copy the inbound first X-Forwarded-Host value with its port (still use `GetPublicHost` for RP-ID
    selection, which strips the port anyway).
  - Copy X-Forwarded-Proto, else the inbound Request.Scheme.

  Alternatively, document that `Mail:PublicBaseUrl` is required wherever InteractiveServer mail runs.
  Either way, add a handler test that asserts the proto and port reach the hop.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/projects/web-server/program.cs:165
- Description: Master failed closed when the discovery env var was absent: a null BaseAddress meant no
  request, so no cookies left the process. Now, with no `Services:web-server:*` config (for example
  Web.Server started outside Aspire with a mis-set environment), the pass-through provider resolves
  `web-server` through DNS, including search-domain expansion. The forwarding handler then sends the
  Cookie and mock-principal headers to whatever that name resolves to.
  - **Usual case:** the scheme is `https`, the first in `https+http`, so TLS name validation for
    `web-server` blocks a non-self endpoint unless an internal CA issues that name. Real exposure is
    therefore low.
  - **Exception:** an operator who sets `Services:web-server:http:0` to an external URL would receive
    cleartext cookies. That needs control of configuration.
- Suggestion: Optional hardening, either of:
  - Have the forwarding handler copy credentials only when the resolved request URI is the expected
    self endpoint.
  - Disable pass-through for this client so a missing endpoint fails loudly, the same as on master.

  Otherwise, record the pass-through behavior in the Design note at program.cs:44–57.
- Status: open
