# Ship markdown content negotiation for agent-readable pages

## Parent

104

## Description

Key pages available as markdown via Accept: text/markdown and/or .md twins (Cloudflare/agent-ready pattern).

## Requirements

- Home and/or docs/demo pages
- Curl-verifiable

## Checklist

- [x] Negotiation or twins
- [x] Smoke checks

## Notes

isitagentready content accessibility.

### Depends on

104-017 helpful but not hard block

## Session

- Created: 2026-07-16
- Started: 2026-08-04 (104-018 markdown negotiation)
- Completed: 2026-08-04

## Results

### Design

Both **static twins** and **Accept negotiation** (agent-ready / Cloudflare Markdown-for-Agents pattern without edge HTML conversion):

| Surface | How agents get markdown |
|---------|-------------------------|
| `/index.md` | Static twin of home (MapStaticAssets → `text/markdown`) |
| `/` + `Accept: text/markdown` | Middleware rewrites path to `/index.md` before `UseRouting` |
| `/auth.md` | Already shipped in 104-017 |
| Browser `Accept: text/html` | Unchanged Blazor SPA |

Middleware lives in platform cluster `web/platform/agent-discovery/` (non-Features namespace). Twin map is explicit (`/` → `/index.md`) so parameterized SPA routes never invent missing files. Prefer markdown when `text/markdown` is present with q>0 and quality ≥ `text/html` (absent html ⇒ 0). Sets `Vary: Accept` on negotiated responses.

### Placement

| Artifact | Path |
|----------|------|
| Middleware + extension | `source/container-apps/web/platform/agent-discovery/markdown-content-negotiation-server.cs` |
| Home twin | `source/container-apps/web/projects/web-spa/wwwroot/index.md` |
| Discovery index | `wwwroot/llms.txt` — negotiation section + `/index.md` link; 018 removed from “not yet” |
| Sitemap | `wwwroot/sitemap.xml` — `/index.md` |
| Pipeline wire | `web-server/program.cs` — `UseMarkdownContentNegotiation()` before `UseRouting` |
| Co-located tests | `platform/agent-discovery/markdown-content-negotiation-tests.cs` |

### Curl-verifiable

```bash
curl -sS -H 'Accept: text/markdown' https://<host>/
curl -sS https://<host>/index.md
curl -sS https://<host>/auth.md
# browser-like Accept must remain HTML/SPA, not markdown body
curl -sS -H 'Accept: text/html' https://<host>/
```

### Verify

- `./bin/dev build` → 0 Warning(s) / 0 Error(s)
- `dotnet run source/container-apps/web/platform/agent-discovery/markdown-content-negotiation-tests.cs` → 5/5 passed
  - IndexMd twin Content-Type text/markdown
  - Root + Accept: text/markdown → twin + Vary: Accept
  - Root + Accept: text/html → not markdown
  - auth.md still markdown
  - Browser-like Accept → not markdown

### Review

- Effort 1–2 (static twins + thin middleware; SPA hosting unchanged)
- Disposition: clean — see `review/disposition.md`

### How to validate

**Automated**
```bash
dotnet run source/container-apps/web/platform/agent-discovery/markdown-content-negotiation-tests.cs
# expect: twin, Accept negotiate + Vary, HTML fallthrough, auth.md, browser-like Accept
```

**Manual**
```bash
./bin/dev run
curl -sS -H 'Accept: text/markdown' https://localhost:7000/ | head -20
# expect: markdown home content (index.md)
curl -sS https://localhost:7000/index.md | head -20
curl -sS -H 'Accept: text/html' https://localhost:7000/ | head -5
# expect: SPA HTML, not forced markdown
```

