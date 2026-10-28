# Fix OpenAPI servers URL (localhost leak)

## Goal (implementer restatement)

The public OpenAPI document must advertise the origin the caller actually used.
`GET /openapi/v1.json` on https://arch.timewarp.work currently lists
`http://localhost:63621/` because the document's `servers` entry is built from the
downstream request host. Behind the ingress that host is the internal web-server hop
(`launchSettings` http URL), while the browser origin arrives on `X-Forwarded-Proto`
and `X-Forwarded-Host`. Codegen and Scalar then call localhost.

Done when:

- The servers URL is taken from that forwarded public origin when the headers are present, and stays the listening address when the app is reached directly. No hardcoded public hostname.
- `GET /openapi/v1.json` with the forwarded public origin returns that origin, not the loopback hop.
- Scalar does not add its own server list, so its base URL is the document's servers entry.

## Description

Exposed OpenAPI at https://arch.timewarp.work/openapi/v1.json declares
`"servers": [{"url": "http://localhost:63621/"}]`.
Public consumers and codegen get a localhost base URL.

## Checklist

- [x] Set servers URL to https://arch.timewarp.work (or make it environment-derived)
- [x] Verify /openapi/v1.json shows the public origin
- [x] Check Scalar UI base URL too

## Session

- Created: ses_e7c126e60ffeQQKOK3tf1mfEiJ (2026-10-28)
- Implementation: task-work implementer (2026-10-29)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session 01a18438-403a-7a70-9241-40d39f76d180, max-turns 500, Done — 2026-10-28T20:50:49Z

## Results

`OpenApiPublicServer` runs as a document transformer from `CommonServerModule.AddOpenApi`.
When `X-Forwarded-Host` or an http(s) `X-Forwarded-Proto` is present, `servers[0].url` becomes
that origin (`UriHelper.BuildAbsolute`, so `https://arch.timewarp.work/`). A direct request
keeps the listening address. Scalar still does not set its own `Servers` list, so the UI base
URL is the document entry. The live host keeps `http://localhost:63621/` until this build is
deployed; YARP already sends the forwarded headers on the web catch-all.

### How to validate

Smoke: `dotnet test tests/container-apps/web/web-server-integration-tests/web-server-integration-tests.csproj -- --filter-class OpenApiDocument_` from the test project directory (its `global.json` selects the Microsoft.Testing.Platform runner).

Expect: 3 passed. `ForwardedPublicOrigin_Should_ReplaceTheListenServer` requires `servers[0].url` `https://arch.timewarp.work/`. `NoForwardedHeaders_Should_KeepTheListenAddress` requires `http://127.0.0.1:` and not the public host. `ScalarPage_Should_NotEmbedALoopbackServer` requires `openapi/v1.json` in the page and no `localhost:63621`, `127.0.0.1`, or `"servers"` override.

After deploy, `curl -s https://arch.timewarp.work/openapi/v1.json` should show `"url": "https://arch.timewarp.work/"`.

## Notes

Evidence: fetched 2026-10-28, openapi 3.1.1, title
TimeWarp.Architecture Web.Server API v1, 60 paths.
