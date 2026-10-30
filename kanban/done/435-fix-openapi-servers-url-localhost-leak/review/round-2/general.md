# Round 2 — general
**Date:** 2026-10-29
**Scope reviewed:** fix-pass delta since round 1 (base `69576b371`, working tree):
`Directory.Packages.props`, `source/Directory.Build.props`, `timewarp-templates/Directory.Build.props`,
`source/foundation/foundation-server/public-request-origin.cs` (new),
`source/foundation/foundation-server/open-api-public-server.cs`,
`source/container-apps/web/platform/mail/http-app-base-url-accessor-server.cs`,
`source/container-apps/web/projects/web-server/configuration/link-preview.cs` (Design text only),
`tests/.../open-api-document-servers-tests.cs`, `tests/.../features/mail/http-app-base-url-accessor-tests.cs`

## Summary

The fix pass bumps both `<Version>` properties and all eleven platform pins to `2.0.0-beta.29`.
It also moves the forwarded-origin rule into one public `PublicRequestOrigin` in `foundation-server`.
OpenAPI calls `ForwardedServerUrl`, which only returns a URL when a forwarded value is present, so a
direct request keeps the listen address. `HttpAppBaseUrlAccessor.GetRequestOrigin` calls `Resolve`,
so mail, link previews, and identity cookie forwarding share it. The host validation (`/`, `\`, `@`,
`://`, whitespace, control characters) now applies to all of them, and each one has a test.

Re-verified:

- `dotnet test -- --filter-class` passed for OpenApiDocument_ (4), GetBaseUrl_Should /
  HttpAppBaseUrlAccessor_ (8), LinkPreview_ (9), and IdentitySessionCookieForwarding_ (9). That last
  class is a third caller of `GetRequestOrigin` that the fix pass did not list. It already handles null.
- `./bin/dev check-version` reported source `2.0.0-beta.29`, one prerelease ahead of NuGet
  `2.0.0-beta.28`, with all twelve packages checked.
- `git grep 2.0.0-beta.28` outside kanban and markdown found one historical comment in
  `template-publish-smoke-command.cs`, which is correct to leave.
- Behavior is preserved. `UriHelper.BuildAbsolute` with an empty path yields `/` (or the bare path
  base), which is the same `Uri` the old string concat normalized to. A non-http(s) request scheme now
  returns null, which is stricter and harmless.

The fix-pass product changes are uncommitted in the claim worktree. The host commits them; this is
not a finding.

## Issues

<!-- None. Prior M1 and M2 verified fixed; no new defects on the fix delta. -->
