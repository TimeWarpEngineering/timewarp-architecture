# Round 2 — general
**Date:** 2026-10-11
**Scope reviewed:** the round-1 fix delta: `platform/mail/http-app-base-url-accessor-server.cs`,
`web-server/configuration/link-preview.cs` (moved), `web-server/configuration/link-preview-values.cs` (moved),
`web-server/components/LinkPreviewMeta.razor`, and
`tests/.../web-server-integration-tests/link-preview-tests.cs`.

## Summary

All four round-1 findings are fixed. There is now one forwarded-origin policy,
`HttpAppBaseUrlAccessor.GetRequestOrigin`, and mail behavior is unchanged: the configured
`PublicBaseUrl` still wins, and a missing request still returns null. An unresolvable origin now
omits the tags instead of failing `App.razor`. `Resolve` runs once per render. The folder now
matches the namespace. Verified: `./bin/dev build` 0 warnings / 0 errors; `--filter-class
LinkPreview` 13/13 passed (adds the malformed X-Forwarded-Host case); `--filter-class
GetBaseUrl_Should` (mail) 7/7 passed. No new findings.

## Prior findings

| ID | Status | Verified |
|----|--------|----------|
| M1 | fixed | `LinkPreview.ResolveOrigin` delegates to `HttpAppBaseUrlAccessor.GetRequestOrigin`; no copied header parsing remains |
| M2 | fixed | `Resolve` returns `LinkPreviewValues?`; the Razor `@if (Tags is not null)` guard; `a b` forwarded host returns null in a test |
| M3 | fixed | `OnInitialized` assigns the `Tags` field |
| M4 | fixed | Both files are under `configuration/` |

## Issues

None.
