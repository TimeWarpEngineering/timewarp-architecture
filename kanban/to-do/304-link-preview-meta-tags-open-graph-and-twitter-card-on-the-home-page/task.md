# Task 304: Link preview meta tags (Open Graph and Twitter card) on the home page

## Description

When the deployed site https://arch.timewarp.work/ is pasted into Discord, X, Slack, Teams or
similar, no link preview card appears: the home page's initial HTML has no Open Graph or Twitter
card meta tags (only `<title>TimeWarp.Architecture</title>`). Steven wants the same kind of card
https://timewarp.software/ produces: site name, title, description and a large TimeWarp
Enterprises logo image.

Add server-rendered link-preview metadata to the web app so the home page (and by default every
page) shows a proper card. Because this repo is also the project template, the values must come
from configuration / the request, not be hard-coded to arch.timewarp.work.

## Reference pattern (timewarp.software, fetched 2026-10-11)

```html
<meta name="description" content="A showcase of TimeWarp Engineering's NuGet libraries and open source projects." />
<title>TimeWarp Software</title>
<meta property="og:title" content="TimeWarp Software" />
<meta property="og:description" content="A showcase of TimeWarp Engineering's NuGet libraries and open source projects." />
<meta property="og:url" content="https://timewarp.software/" />
<meta property="og:type" content="website" />
<meta property="og:image" content="https://timewarp.software/assets/logos/timewarp-enterprises-og.png" />
<meta property="og:image:width" content="1200" />
<meta property="og:image:height" content="630" />
<meta property="og:image:type" content="image/png" />
<meta property="og:image:alt" content="TimeWarp Software" />
<meta property="og:site_name" content="TimeWarp Software" />
<meta name="twitter:card" content="summary_large_image" />
<meta name="twitter:title" content="TimeWarp Software" />
<meta name="twitter:description" content="A showcase of TimeWarp Engineering's NuGet libraries and open source projects." />
<meta name="twitter:image" content="https://timewarp.software/assets/logos/timewarp-enterprises-og.png" />
<link rel="canonical" href="https://timewarp.software/" />
```

The image is a 1200x630 PNG: the TimeWarp Enterprises logo on a dark background. Its source is in
the sibling repo `TimeWarpEngineering/timewarp-software` at
`public/assets/logos/timewarp-enterprises-og.png` (also downloadable from the URL above).

## Requirements

1. **Server-rendered.** Link-preview crawlers (Discordbot, Twitterbot, Slackbot, facebookexternalhit)
   do not run JavaScript or Blazor. The tags must be in the initial HTML response of `GET /`,
   whatever `BlazorSettings.RenderMode` / `Prerender` is set to. Prefer putting them directly in
   `source/container-apps/web/projects/web-server/components/App.razor` `<head>` (static SSR part of
   the document, not inside the interactive `HeadOutlet`), or a small static component rendered there.
   Do not rely on `<HeadContent>` from an interactive page if prerender can be off.
2. **Tags** (at minimum, matching the reference):
   - `<meta name="description">`
   - `og:site_name`, `og:title`, `og:description`, `og:type` (`website`), `og:url`
   - `og:image` (absolute https URL), `og:image:width` (1200), `og:image:height` (630),
     `og:image:type` (`image/png`), `og:image:alt`
   - `twitter:card` = `summary_large_image`, `twitter:title`, `twitter:description`, `twitter:image`
   - `<link rel="canonical">`
3. **Absolute URLs** built from a configured public base URL when set (e.g. a `SiteMetadata` /
   link-preview options section with `BaseUrl`, `SiteName`, `Title`, `Description`, `ImagePath`,
   `ImageAlt`), falling back to the incoming request's scheme + host (respect forwarded headers;
   the app runs behind YARP/Caddy, so it must not emit `http://localhost:...` in production).
   `og:url`/canonical should be the current request URL (path, no query) or at least the base URL
   for `/`.
4. **Defaults** suitable for the template: site name/title `TimeWarp.Architecture`, description along
   the lines of the home page welcome card ("A distributed .NET reference application and project
   template by TimeWarp Enterprises."). Keep the visible `<title>` as it is unless there is a
   reason to change it.
5. **Preview image**: add a 1200x630 PNG (reuse the timewarp-software `timewarp-enterprises-og.png`,
   TimeWarp Enterprises logo on a dark background) to the web app's static assets (e.g.
   `source/container-apps/web/projects/web-spa/wwwroot/images/og-image.png` or wherever static
   files are served from for `/`), and confirm it is served with `200` and `image/png` at the URL
   the tags point to. Keep it under ~300 KB.
6. **Test**: add an automated test that requests `/` from the running web server (integration test
   project, e.g. `tests/container-apps/web/web-spa-integration-tests`) and asserts the
   server-rendered HTML contains each tag above with non-empty content, that `og:image` and
   `og:url` are absolute URLs, and that the `og:image` URL returns 200 `image/png`. Do not just
   string-match the .razor source.
7. Follow the repo's skills (`tw-blazor`, `tw-csharp`, `tw-ganda-walk`). Build with 0 warnings and
   `ganda repo audit` clean.

## Not in scope

- Per-page titles/descriptions for every route (nice follow-up; leave a note if the design makes it easy).
- JSON-LD structured data (timewarp.software has it; optional follow-up).
- Deploying to arch.timewarp.work.

## Results

Link-preview crawlers now get Open Graph and Twitter card tags in the initial HTML of every
page. `LinkPreviewMeta` is rendered from `App.razor` `<head>`, outside `HeadOutlet` and outside
the interactive render mode, so the tags are in the document even when `Prerender` is false.
Copy and the image path come from the `SiteMetadata` options section. Absolute `og:url`,
canonical, and `og:image` URLs use `SiteMetadata.BaseUrl` when it is an absolute http(s) URL,
otherwise the first `X-Forwarded-Proto` / `X-Forwarded-Host` values (http or https only; port
kept), otherwise the request scheme, host, and path base. The query string is omitted. The
visible `<title>` stays `TimeWarp.Architecture`.

The preview image is the TimeWarp Enterprises logo on a dark background (the timewarp.software
`timewarp-enterprises-og.png`), checked in at
`source/container-apps/web/projects/web-spa/wwwroot/images/og-image.png` (49,425 bytes, PNG
IHDR 1200×630). `MapStaticAssets` serves it as `image/png`.

### Files

- `source/container-apps/web/projects/web-server/components/App.razor` — static `<LinkPreviewMeta />`
- `source/container-apps/web/projects/web-server/components/LinkPreviewMeta.razor`
- `source/container-apps/web/projects/web-server/components/link-preview.cs`
- `source/container-apps/web/projects/web-server/components/link-preview-values.cs`
- `source/container-apps/web/projects/web-server/configuration/site-metadata.cs`
- `source/container-apps/web/projects/web-server/configuration/site-metadata-validator.cs`
- `source/container-apps/web/projects/web-server/program.cs` — `AddFluentValidatedOptions<SiteMetadata, SiteMetadataValidator>` with `ValidateOnStart`
- `source/container-apps/web/projects/web-server/appsettings.json` — template defaults; `BaseUrl` left unset
- `source/container-apps/web/projects/web-spa/wwwroot/images/og-image.png`
- `tests/container-apps/web/web-server-integration-tests/link-preview-tests.cs` — host-free origin cases
- `tests/container-apps/web/web-server-integration-tests/link-preview-meta-tests.cs` — `GET /` HTML plus the image
- `tests/container-apps/web/web-server-integration-tests/appsettings.json` — `SiteMetadata:Title` = `Integration Test Site` so a missed bind cannot pass

### Decisions

- Tags stay in the static shell. A later per-route title must be rendered there too. `HeadContent`
  inside an interactive page is absent when prerender is off. Per-route copy is not implemented.
- The image is site-wide. An absolute `ImagePath` is used as-is. A root-relative path is combined
  with the public origin, not with the current page path.
- Forwarded headers are read directly. `UseForwardedHeaders` is not called, matching the mail
  public-origin posture: YARP sets the headers, and a client that can reach web-server directly
  can forge them.
- `og:type` is `website` and `twitter:card` is `summary_large_image`. Those are protocol constants,
  not operator copy. `og:title` / `twitter:title` follow `SiteMetadata.Title`.
- Nothing is hard-coded to `arch.timewarp.work`. Set `SiteMetadata:BaseUrl` only when the request
  host is not the public host.

### Tests

`cd tests/container-apps/web/web-server-integration-tests && dotnet test --nologo -- --filter-class LinkPreview`

12 passed, 0 failed (8 host-free, 4 against the running web server). The home-page case asserts
every required tag has non-empty content, that `og:url` and `og:image` are absolute, that the
visible title is still `TimeWarp.Architecture`, and that `og:image` returns 200 `image/png` with
a 1200×630 IHDR under 300 KB. Forwarded host, query stripping, and `/Login` are covered.
`./bin/dev build` (Release): 0 warnings, 0 errors. `ganda repo audit`: 31 passed, 0 failed.

### How to validate

**Smoke**

```bash
./bin/dev run
# Aspire ingress is https://localhost:63610. web-server direct is https://localhost:63611.
# -k accepts the local dev certificate. Accept text/html so the markdown twin is not served.
curl -sk -H 'Accept: text/html' https://localhost:63610/ \
  | grep -E 'og:|twitter:|rel="canonical"|name="description"'
curl -skI https://localhost:63610/images/og-image.png | head -20
```

**Expect**

- The raw HTML contains `name="description"`, `og:site_name`, `og:title`, `og:description`,
  `og:type` (`website`), `og:url`, `og:image`, `og:image:width` (`1200`), `og:image:height`
  (`630`), `og:image:type` (`image/png`), `og:image:alt`, `twitter:card`
  (`summary_large_image`), `twitter:title`, `twitter:description`, `twitter:image`, and
  `<link rel="canonical">`.
- `og:url`, canonical, and `og:image` are absolute `https://localhost:63610/...` URLs (no query).
  Through ingress, `og:image` is `https://localhost:63610/images/og-image.png`.
- That image URL returns 200 with `content-type: image/png`. The file is 1200×630 and 49,425 bytes.
- Site name and title are `TimeWarp.Architecture`. The description is
  `A distributed .NET reference application and project template by TimeWarp Enterprises.`
- After deploy (not part of this task), pasting https://arch.timewarp.work/ into Discord shows a
  large-image card with that site name, title, description, and the TimeWarp Enterprises logo.
  YARP must keep sending `X-Forwarded-Proto` and `X-Forwarded-Host`, or `SiteMetadata:BaseUrl`
  must be the public origin. Otherwise the card URLs would be the internal hop.

**Automated gate**

```bash
cd tests/container-apps/web/web-server-integration-tests && dotnet test --nologo -- --filter-class LinkPreview
./bin/dev build
ganda repo audit
```

Run the test from that project directory. The repo root `dotnet test` still targets VSTest, which
this repo no longer supports. Expect 12 passed. Expect build 0 warnings and audit 0 failed.

**Depends on**

- `./bin/dev run` for the curl smoke (Aspire ingress on port 63610).
- The integration test starts its own web host (default `https://localhost:7000`). It does not
  need Aspire. `TIMEWARP_TEST_PORT_BASE` may move that port. Assertions use the client's base
  address.

**Not in scope**

- Per-route titles and descriptions. The static shell is the place to add them later.
- JSON-LD.
- Deploying to arch.timewarp.work and confirming the live Discord card.

## Session

- Created: 453480 (2026-10-11)
- Implementer: Grok session 01a128f7-c094-7702-bacf-e5acb228de83 (2026-10-11)
