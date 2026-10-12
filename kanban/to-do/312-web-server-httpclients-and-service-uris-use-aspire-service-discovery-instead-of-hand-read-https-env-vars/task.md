# web-server HttpClients and service URIs use Aspire service discovery instead of hand-read https env vars

## Problem (found 2026-10-12 on the kind deployment)

Loading the app through the Kubernetes ingress shows the error notification "An invalid request URI
was provided. Either the request URI must be an absolute URI or BaseAddress must be set."

**Root cause.** In web-server, `program.cs:157-159` sets the server-side named `HttpClient`s'
`BaseAddress` from `ServiceUriHelper.GetServiceHttpsUri(...)`
(`source/foundation/foundation-server/extensions/service-uri-helper.cs`). That helper hand-reads the
env var `services__{resource}__https__0`.
- Local `dev run` injects an https endpoint.
- Published targets do not: Compose, Kubernetes and ACA expose **http only**. The live kind
  web-server ConfigMap has `services__web-server__http__0` but no `__https__0`.
- So `BaseAddress` is null, and the first-visit **InteractiveServer** render, the only one before
  WASM loads, fails on every server-side call.

The publish safety suites inspect output and never run the app, so CI was green.

`curl <ingress>/service-discovery` on kind returns
`{"grpc-server":null,"api-server":null,"web-server":null}`. That is the same helper, exposed
**unauthenticated** by `program.cs:516-530` and consumed by the SPA's
`services/service-uri-provider.cs` (used by `superhero-grpc-service-provider.cs` /
`superhero-module.cs`).

## Rules

- **Don't reconstruct Aspire's conventions by hand** (070-006 / 284). The service defaults already
  call `AddServiceDiscovery()` and `ConfigureHttpClientDefaults(http => http.AddServiceDiscovery())`.
  Use the documented scheme: `BaseAddress = new Uri("https+http://web-server")` (prefers https,
  falls back to http), with `ServiceNames` constants composing the host.
- Fail closed and loud: a missing service at startup or first use is a clear error, never a silent
  null `BaseAddress`.

## Requirements

- **web-server named clients** (`ServiceNames.WebServiceName`, `ApiServiceName`): use
  `https+http://<ServiceNames…>` through service discovery.
  - Check that the `IdentitySessionCookieForwardingHandler` loopback, the
    `X-TimeWarp-Circuit-Host` rule (070-008) and TLS on the loopback hop still work. The loopback
    Design notes mention https and the dev cert, so reconcile them with the http fallback on
    published targets.
- **`/service-discovery` endpoint and the SPA `ServiceUriProvider`.** Decide and document:
  - The browser never needs internal service URIs. Every browser-facing call goes to the ingress
    origin; YARP routes `/api` and `/grpc`.
  - Preferred: the SPA's grpc-web client targets the ingress origin plus the `/grpc` prefix. Delete
    the `/service-discovery` endpoint and `ServiceUriProvider`. If the endpoint must stay, it must
    not expose internal URIs unauthenticated; record why.
- **Delete `ServiceUriHelper`** once it has no callers. It is a foundation package type, so check
  the package surface and `PublicAPI` / XML docs, and note the removal for the release notes.
- **Tests.** This regression must have been catchable.
  - Add a closed-box or in-proc test that boots web-server with **http-only** service-discovery
    configuration, the shape published targets produce, and exercises a server-side named-client
    call, for example a page or handler that uses the web-server client during interactive-server
    render.
  - It must fail on today's code with the null `BaseAddress`.
  - Keep the existing suites green, including the passkey/circuit-host tests and ingress smoke.
- **Docs:** Design regions in `web-server/program.cs`, the SPA services touched, the foundation
  helper's removal; `skills/tw-deploy` only if it says anything about https between services.

## Checklist

- [ ] Named clients via `https+http://` service discovery; loopback/circuit-host behaviour verified
- [ ] `/service-discovery` + `ServiceUriProvider` removed (grpc-web via ingress origin) or justified
- [ ] `ServiceUriHelper` deleted (package surface noted)
- [ ] Regression test with http-only discovery config (fails before, passes after)
- [ ] Design regions reconciled
- [ ] Gates: `dev build` 0/0, `dev test`, `dev publish compose|kubernetes|aca`, `dev template-smoke`,
      `ganda repo audit`

## Notes

- Workers must not touch the maintainer's kind cluster `kind-timewarp-architecture` (live
  deployment). The maintainer redeploys (`dev deploy --target kubernetes`) and reloads the page
  after merge.
- Side observation, check and record only: Aspire's Kubernetes output sets
  `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` on web-server. 070-008's Design regions say "no
  global UseForwardedHeaders". Confirm what that env var turns on (X-Forwarded-For/-Proto, not
  Host, by default) and make the regions accurate.

## Session

- Created: 2026-10-12 (cockpit, per Steve's kind-deployment error report)
