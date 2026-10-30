# Round 2 — merged findings
**Date:** 2026-10-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/Directory.Build.props, timewarp-templates/Directory.Build.props, Directory.Packages.props:29-50
- Description: (round 1) The platform package fix shipped without a version bump.
- Source: general
- Disposition notes: Both `<Version>` properties and the eleven platform pins are `2.0.0-beta.29`.
  `./bin/dev check-version` passes (one prerelease ahead of `2.0.0-beta.28`). Verified round 2.

### M2 — Severity: suggestion — Status: fixed
- File: source/foundation/foundation-server/public-request-origin.cs
- Description: (round 1) There was a third copy of the forwarded public-origin rule.
- Source: general
- Disposition notes: `PublicRequestOrigin` is the only parser. `OpenApiPublicServer` calls
  `ForwardedServerUrl` and `HttpAppBaseUrlAccessor.GetRequestOrigin` calls `Resolve`. The host
  validation applies to both, with tests (`MalformedForwardedHost_Should_KeepTheListenAddress`,
  `Return_Null_For_A_Forwarded_Host_That_Is_Not_A_Host`). Verified round 2.

## Duplicates / conflicts

- None. No new findings on the fix delta.
