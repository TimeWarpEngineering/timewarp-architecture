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

## Results (required before done)

[What was implemented, files changed, key decisions, test outcomes]

### How to validate (required before done)

**Smoke**

```bash
./bin/dev run   # then:
curl -s https://localhost:<port>/ | grep -E 'og:|twitter:|rel="canonical"|name="description"'
```

**Expect**

- All tags above present in the raw HTML with absolute https URLs.
- `og:image` URL returns 200 `image/png`, 1200x630.
- After deploy, pasting https://arch.timewarp.work/ into Discord shows a large-image card
  (site name, title, description, TimeWarp Enterprises logo).

## Session

- Created: 453480 (2026-10-11)
