# Round 1 — general
**Date:** 2026-10-11
**Scope reviewed:** branch vs master (dd79d5a17): `LinkPreviewMeta.razor`, `link-preview.cs`,
`link-preview-values.cs`, `site-metadata.cs`, `site-metadata-validator.cs`, `program.cs`,
`appsettings.json` (web-server and integration tests), `og-image.png`, `link-preview-tests.cs`,
`link-preview-meta-tests.cs`. Call sites checked: `HttpAppBaseUrlAccessor` (mail) and
`HttpRequestHostAccessor.GetPublicHost` (identity-host).

## Summary

The change puts link-preview tags in the static SSR head (`App.razor`, outside `HeadOutlet`), so
they are present when prerender is off. It binds a validated `SiteMetadata` section and resolves
absolute URLs from `BaseUrl`, then forwarded headers, then the request. Requirements coverage and
tests are solid. Razor encodes attribute values, so a forged forwarded host cannot inject markup.
The main themes: the forwarded-origin policy is now duplicated from mail, and an unresolvable
origin throws during render, which fails the whole page.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-server/components/link-preview.cs:93
- Description: `ResolveOrigin` re-implements the forwarded-origin policy line for line from
  `HttpAppBaseUrlAccessor.GetBaseUrl` (`platform/mail/http-app-base-url-accessor-server.cs:38`):
  first X-Forwarded-Proto (http/https only), first X-Forwarded-Host with port, else Request.Scheme
  + Host, plus PathBase. `FirstValue` is copied verbatim. The two must agree (mail links and
  canonical URLs are the same public origin). AGENTS.md hard rule: "When two things must agree,
  generate one from the other or add a build check."
- Suggestion: Expose the request half as one static helper on `HttpAppBaseUrlAccessor` (the
  `HttpRequestHostAccessor.GetPublicHost` pattern). Have both mail and `LinkPreview` call it.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-server/components/link-preview.cs:118
- Description: When no origin can be formed, `ResolveOrigin` throws `InvalidOperationException`.
  That can happen with a malformed `X-Forwarded-Host` (e.g. `a b`, which `Uri.TryCreate` rejects)
  or an HTTP/1.0 request without Host. `LinkPreviewMeta` renders inside `App.razor`, so the throw
  turns every page into a 500, even though the tags are optional metadata. Mail returns null for
  the same inputs and falls back.
- Suggestion: Return null when no origin resolves. Render no link-preview tags in that case. The
  page still renders.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/web/projects/web-server/components/LinkPreviewMeta.razor:24
- Description: `Tags` is an expression-bodied property, so `LinkPreview.Resolve` runs once per
  use: about 16 header parses and Uri allocations per render.
- Suggestion: Resolve once in `OnInitialized` and render from a field.
- Status: open

### Issue 4 — Severity: nit
- File: source/container-apps/web/projects/web-server/components/link-preview.cs:16
- Description: `link-preview.cs` and `link-preview-values.cs` live under `components/` but declare
  `namespace TimeWarp.Architecture.Configuration`. Their sibling `site-metadata.cs` lives in
  `configuration/`. The folder and the namespace disagree.
- Suggestion: Move both files to `configuration/` beside `SiteMetadata`.
- Status: open
