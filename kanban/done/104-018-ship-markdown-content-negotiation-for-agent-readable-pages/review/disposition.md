# Review disposition — 104-018

**Effort:** 1–2 (static twins + thin Accept-rewrite middleware; no SPA contract changes)  
**Verdict:** clean

## Scope

Ship agent-readable markdown for key pages:

| Piece | Path |
|-------|------|
| Middleware | `web/platform/agent-discovery/markdown-content-negotiation-server.cs` |
| Home twin | `web-spa/wwwroot/index.md` |
| llms.txt / sitemap | discovery links updated |
| Pipeline | `UseMarkdownContentNegotiation()` before `UseRouting` |
| Tests | co-located Jaribu HTTP smoke (5 cases) |

## Checks

- [x] Accept: text/markdown on `/` returns twin (`text/markdown`, body matches index.md)
- [x] Direct `/index.md` twin works
- [x] Browser Accept (text/html / browser-like) does **not** get markdown — SPA hosting intact
- [x] Vary: Accept on negotiated responses
- [x] auth.md still served as markdown (104-017)
- [x] Build 0/0
- [x] Smoke tests 5/5

## Findings

None open. No security reviewer (discovery/static content only; no auth surface change).

## Notes

- Middleware rewrites path only; MapStaticAssets owns Content-Type for `.md`.
- Extending twins: add entries to `MarkdownContentNegotiation.TwinPaths` + wwwroot file.
- Wave 4 siblings 019/020 still “not yet” in llms.txt.
