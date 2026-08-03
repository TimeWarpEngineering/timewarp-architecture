# Review disposition — 104-020

**Effort:** 1–2 (discovery docs + thin path rewrite + exact ingress pins; tip/meter already 009/011)  
**Verdict:** clean

## Scope

Expose discoverable x402 tip (and document meter) for commerce scanners:

| Piece | Path |
|-------|------|
| Alias middleware | `web/features/tip/tip-discovery-alias-server.cs` |
| Pipeline | `UseTipDiscoveryAlias()` before `UseRouting` |
| Ingress | AppHost + YARP exact `/api` + `/api/` → Web.Server |
| Docs | `llms.txt`, `auth.md`, `index.md`, `sitemap.xml` |
| Tests | co-located Jaribu: alias 402, free-route never-402, alias unit |

## Checks

- [x] Unpaid tip → 402 + `PAYMENT-REQUIRED` (resource still `/api/tip`)
- [x] Bare `/api` alias → same 402 shape
- [x] Free routes never 402 while tip enabled (`/`, `/llms.txt`, `/auth.md`, `/api/health`, OpenAPI, `/index.md`)
- [x] Linked from llms/auth discovery
- [x] Meter path documented (agent + `demo:invoke`)
- [x] Build 0/0
- [x] Tip tests 11/11

## Findings

None open. No security reviewer beyond existing tip/meter gates (anonymous tip is intentional; free routes stay free).

## Notes

- Challenge Resource stays `/api/tip` even via alias (TipOptions.Resource).
- Bare `api` cannot be a generated ingress prefix (TWA0018); exact paths are hand-pinned only.
- isitagentready commerce: scanners following `/llms.txt` / `/auth.md` hit the live tip URL.
