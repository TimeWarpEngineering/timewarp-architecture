# Round 1 — merged findings
**Date:** 2026-10-11
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 3 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-server/components/link-preview.cs:93
- Description: The forwarded-origin policy is a copy of `HttpAppBaseUrlAccessor.GetBaseUrl` (mail). The two must agree.
- Suggestion: Add one static request-origin helper on `HttpAppBaseUrlAccessor`, called by mail and `LinkPreview`.
- Source: general (Issue 1)
- Disposition notes: Fixed. Added static `HttpAppBaseUrlAccessor.GetRequestOrigin(HttpRequest)`. Mail `GetBaseUrl` and `LinkPreview.ResolveOrigin` both call it. The copied `FirstValue` and forwarded parsing were removed from link-preview.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-server/components/link-preview.cs:118
- Description: An unresolvable origin (malformed X-Forwarded-Host, or no Host) throws during `App.razor` render, so every page returns 500.
- Suggestion: Return null and render no link-preview tags.
- Source: general (Issue 2)
- Disposition notes: Fixed. `LinkPreview.Resolve` returns null when no origin resolves, and `LinkPreviewMeta` renders no tags. Tests `Return_Null_When_No_Origin_Can_Be_Resolved` and `Return_Null_For_A_Malformed_Forwarded_Host` cover this.

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-server/components/LinkPreviewMeta.razor:24
- Description: The expression-bodied `Tags` property runs `Resolve` about 16 times per render.
- Suggestion: Resolve once in `OnInitialized`.
- Source: general (Issue 3)
- Disposition notes: Fixed. Resolved once in `OnInitialized` into a field.

### M4 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-server/components/link-preview.cs:16
- Description: These files are under `components/`, but their namespace is `Configuration`.
- Suggestion: Move them to `configuration/`.
- Source: general (Issue 4)
- Disposition notes: Fixed. `git mv` moved link-preview.cs and link-preview-values.cs to `web-server/configuration/`.

## Duplicates / conflicts

- None (single reviewer).
