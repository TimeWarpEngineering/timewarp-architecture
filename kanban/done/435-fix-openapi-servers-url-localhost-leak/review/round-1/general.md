# Round 1 — general
**Date:** 2026-10-29
**Scope reviewed:** branch vs master: `source/foundation/foundation-server/open-api-public-server.cs`, `common-server-module.cs`, `tests/container-apps/web/web-server-integration-tests/open-api-document-servers-tests.cs`, test `.csproj`

## Summary

A new OpenAPI document transformer in `TimeWarp.Foundation.Server` replaces `servers[0].url` with the
forwarded public origin (`X-Forwarded-Proto` / `X-Forwarded-Host`) when either header is present, and
leaves the framework's listening address when the request is direct. Scalar is unchanged and follows
the document. The behavior matches the Goal restatement and the task. Re-verified: the 3 new tests
pass locally (`dotnet test … -- --filter-class OpenApiDocument_`: 3 passed). YARP routes set both
headers (`"X-Forwarded": "Set"`) after the trusted-proxy step, so the ingress path is covered. The main
risk is shipping. The change is in a platform package, and the version was not bumped. It also adds
a third copy of the forwarded-origin rule.

## Issues

### Issue 1 — Severity: bug
- File: source/Directory.Build.props (`<Version>`), Directory.Packages.props:33
- Description: The fix lives in the platform package `TimeWarp.Foundation.Server`, but `<Version>` is still
  `2.0.0-beta.28`. That version is already on nuget.org, and `dev check-version` fails with
  "✗ Version 2.0.0-beta.28 was already released". AGENTS.md (Before a PR) requires a `<Version>` bump
  and the platform package pins in the same commit when packages change. The template excludes
  `source/foundation/**` (`.template.config/template.json:29`), so generated apps build with
  `UseFoundationPackages=true` against the published 2.0.0-beta.28, which does not have the
  transformer. The new test `tests/container-apps/web/web-server-integration-tests/open-api-document-servers-tests.cs` does ship in generated apps
  (that project is not excluded). In a fresh `dotnet new timewarp-architecture` app,
  `ForwardedPublicOrigin_Should_ReplaceTheListenServer` would fail, and the generated app's OpenAPI
  document would still leak the loopback hop.
- Suggestion: Bump `<Version>` in `source/Directory.Build.props` to the next prerelease, and bump every
  platform pin in `Directory.Packages.props` in the same commit (task-124 policy). Then re-run
  `dev check-version`.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/foundation/foundation-server/open-api-public-server.cs:56
- Description: `TryResolve` re-implements the forwarded public-origin rule that
  `HttpAppBaseUrlAccessor.GetRequestOrigin` already owns
  (`source/container-apps/web/platform/mail/http-app-base-url-accessor-server.cs:55`). Its Design region
  says that method is static "so LinkPreview shares one policy". The copies have already drifted. The new
  one rejects hosts containing `/`, `\`, `@`, `://`, whitespace, or control characters (`IsHost`), and
  mail and link previews accept those hosts.
  An OpenAPI server URL and a mail permalink can now disagree for the same request. Repo hard rule:
  "When two things must agree, generate one from the other or add a build check."
- Suggestion: Move the request-origin resolver (first-value rule plus host validation) into
  `foundation-server`, for example a public static `PublicRequestOrigin.TryGet(HttpRequest)`. Have both
  `OpenApiPublicServer` and `HttpAppBaseUrlAccessor.GetRequestOrigin` call it, so there is one policy
  and one place for the host validation. `HttpRequestHostAccessor` strips the port, so it can stay
  separate as its Design region notes, or derive from the same first-value helper.
- Status: open

