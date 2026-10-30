# Round 1 — merged findings
**Date:** 2026-10-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/Directory.Build.props (`<Version>`), Directory.Packages.props:33
- Description: The fix is in the platform package `TimeWarp.Foundation.Server`, but `<Version>` stays at
  the already-published `2.0.0-beta.28`, and `dev check-version` fails. Generated apps consume the
  package, because `source/foundation/**` is excluded from the template. They would keep the loopback
  leak. They would also ship the new `ForwardedPublicOrigin_Should_ReplaceTheListenServer` test, which
  fails against beta.28.
- Suggestion: Bump `<Version>` to the next prerelease and every platform pin in
  `Directory.Packages.props` in the same commit (task-124 policy). Then `dev check-version` must pass.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/foundation/foundation-server/open-api-public-server.cs:56
- Description: This is a third copy of the forwarded public-origin rule, next to
  `HttpAppBaseUrlAccessor.GetRequestOrigin`, which mail and link previews share. The copies have
  already drifted: the new copy's `IsHost` validation exists only here. The repo rule is "When two
  things must agree, generate one from the other or add a build check."
- Suggestion: Put one public request-origin resolver in `foundation-server`. Have
  `OpenApiPublicServer` and `HttpAppBaseUrlAccessor.GetRequestOrigin` both call it.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer). I dropped a nit about the `_ = cancellationToken;` discard because that
  idiom is used throughout `source/` (29 sites).
