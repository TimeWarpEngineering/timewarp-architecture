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
- The fix ships in the next prerelease. `<Version>` in `source/Directory.Build.props` and `timewarp-templates/Directory.Build.props`, and every platform pin in `Directory.Packages.props`, are `2.0.0-beta.29`. `dev check-version` passes. Generated apps restore `TimeWarp.Foundation.Server` from that pin, so they pick up the transformer instead of the published beta.28 package.
- One resolver in `foundation-server` owns the forwarded public-origin rule. `OpenApiPublicServer` and `HttpAppBaseUrlAccessor.GetRequestOrigin` both call it. A forwarded host that contains `/`, `\`, `@`, `://`, whitespace, or a control character is rejected. Mail and link previews still fall back to the request when no forwarded origin is present. OpenAPI still leaves the listen address in place in that case.

## Description

Exposed OpenAPI at https://arch.timewarp.work/openapi/v1.json declares
`"servers": [{"url": "http://localhost:63621/"}]`.
Public consumers and codegen get a localhost base URL.

## Checklist

- [x] Set servers URL to https://arch.timewarp.work (or make it environment-derived)
- [x] Verify /openapi/v1.json shows the public origin
- [x] Check Scalar UI base URL too
- [x] Bump `<Version>` and the platform pins to `2.0.0-beta.29` so generated apps get the fix
- [x] One `PublicRequestOrigin` in foundation-server, used by OpenAPI and mail

## Session

- Created: ses_e7c126e60ffeQQKOK3tf1mfEiJ (2026-10-28)
- Implementation: task-work implementer (2026-10-29)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session 01a18438-403a-7a70-9241-40d39f76d180, max-turns 500, Done — 2026-10-28T20:50:49Z
- Review oracle round 1: general reviewer (claude-opus-5-5, headless ganda task work), effort 2 — 2026-10-29
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-28T20:56:44Z
- Fix pass (implement-fix-2): grok task-work (2026-10-29). M1 bumps `<Version>` and the platform pins to `2.0.0-beta.29`. M2 moves the forwarded-origin rule to `PublicRequestOrigin`. `ganda repo audit` exited 0. The advisory `kanban-landed` warning is eight other claims with no worktree (070-001, 104-024, 104-025, 104-026, 123, 141, 198, 244). Left untouched.
- Review oracle round 2: general reviewer (claude-opus-5-5, headless ganda task work), effort 1. Disposition clean — 2026-10-29

## Results

`OpenApiPublicServer` runs as a document transformer from `CommonServerModule.AddOpenApi`.
When `X-Forwarded-Host` or an http(s) `X-Forwarded-Proto` is present, `servers[0].url` becomes
that origin (`PublicRequestOrigin` via `UriHelper.BuildAbsolute`, so `https://arch.timewarp.work/`).
A direct request keeps the listening address. A forwarded host that is not a host is refused, and
the listen address stays. Scalar still does not set its own `Servers` list, so the UI base URL is
the document entry.

`HttpAppBaseUrlAccessor.GetRequestOrigin` calls the same `PublicRequestOrigin.Resolve`. Mail and
link previews keep the configured base URL first, then that origin, and still fall back to the
request when no forwarded origin is present. A forwarded host with `/`, `\`, `@`, `://`,
whitespace, or a control character returns null instead of a second copy of the rule.

`<Version>` in `source/Directory.Build.props` and `timewarp-templates/Directory.Build.props`, and
the eleven platform pins in `Directory.Packages.props`, are `2.0.0-beta.29`. Generated apps
restore `TimeWarp.Foundation.Server` from that pin. The live host keeps `http://localhost:63621/`
until this build is deployed; YARP already sends the forwarded headers on the web catch-all.

### How to validate

Smoke: from `tests/container-apps/web/web-server-integration-tests` (its `global.json` selects the Microsoft.Testing.Platform runner), `dotnet test web-server-integration-tests.csproj -- --filter-class OpenApiDocument_`, then the same command with `--filter-class HttpAppBaseUrlAccessor_` and `--filter-class LinkPreview_`. `./bin/dev check-version`.

Expect: OpenAPI 4 passed, including `MalformedForwardedHost_Should_KeepTheListenAddress` (`http://127.0.0.1:` and not `evil.test`). `ForwardedPublicOrigin_Should_ReplaceTheListenServer` requires `https://arch.timewarp.work/`. `NoForwardedHeaders_Should_KeepTheListenAddress` requires `http://127.0.0.1:` and not the public host. `ScalarPage_Should_NotEmbedALoopbackServer` requires `openapi/v1.json` in the page and no `localhost:63621`, `127.0.0.1`, or `"servers"` override. Accessor 8 passed, including `Return_Null_For_A_Forwarded_Host_That_Is_Not_A_Host`. Link preview 9 passed. `check-version` reports source `2.0.0-beta.29` one prerelease ahead of NuGet `2.0.0-beta.28`. `./bin/dev build` completes with 0 warnings and 0 errors.

After deploy, `curl -s https://arch.timewarp.work/openapi/v1.json` should show `"url": "https://arch.timewarp.work/"`.

### Review

- Round 1: effort 2, roster general. Smoke re-run by the reviewer: 3/3 passed.
- Counts at round 1: bug 1 open (M1), suggestion 1 open (M2), nit 0.
- Fix pass implemented both. M1: both `<Version>` properties and the eleven platform pins are `2.0.0-beta.29`, and `dev check-version` passed. M2: `PublicRequestOrigin` in `foundation-server` is the only forwarded-origin parser; OpenAPI calls `ForwardedServerUrl` and mail calls `Resolve`.
- Round 2: effort 1, roster general. Re-verified M1 and M2 fixed on the fix delta. No new findings. Re-ran OpenApiDocument_ 4, HttpAppBaseUrlAccessor_ 8, LinkPreview_ 9, and IdentitySessionCookieForwarding_ 9 (a third `GetRequestOrigin` caller); all passed. `dev check-version` passed.
- Final counts (round 2): bug 0 open / 1 fixed, suggestion 0 open / 1 fixed, nit 0, wontfix 0.
- Disposition: **clean** (2 rounds, 0 open).
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/round-2/general.md`, `review/round-2/merged.md`, `review/disposition.md`.
- Goal restatement vs task: consistent. "Environment-derived" was read as request-derived from the forwarded headers. No misread.

## Review

- Round 1: open M1, M2. Fix pass implemented both.
- Round 2: Done
## Notes

Evidence: fetched 2026-10-28, openapi 3.1.1, title
TimeWarp.Architecture Web.Server API v1, 60 paths.
