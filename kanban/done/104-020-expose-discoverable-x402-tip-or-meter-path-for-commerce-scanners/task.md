# Expose discoverable x402 tip or meter path for commerce scanners

## Parent

104

## Description

Ensure unpaid call to tip and/or meter returns proper 402; path documented and linked for agents/scanners. Free routes still never 402.

## Requirements

- Discoverable path
- Correct 402 shape
- Linked from llms/auth discovery

## Checklist

- [x] Path + alias if useful
- [x] Verify scanners/docs
- [x] No free-route 402 regressions

## Notes

isitagentready commerce (x402).

### Depends on

104-009 or 104-011

## Session

- Created: 2026-07-16
- Implement: 2026-08-04 (104-020 discovery + alias)

## Results

### Summary

Discoverable x402 commerce path for scanners/agents on **web-server** (tip 009 + meter 011 already hosted):

| Piece | Detail |
|-------|--------|
| Canonical tip | `GET\|POST /api/tip` — unpaid enabled → **402** + `PAYMENT-REQUIRED` (resource `/api/tip`) |
| Discovery alias | bare `/api` and `/api/` rewrite → `/api/tip` (`TipDiscoveryAlias` before `UseRouting`) |
| Ingress pin | exact `/api` + `/api/` → Web.Server on AppHost + standalone YARP (else bare `/api` hits Api.Server catch-all) |
| Meter (secondary) | `GET /api/demo/metered-capability` — linked; needs Bearer `demo:invoke` |
| Discovery docs | `llms.txt`, `auth.md`, `index.md`, `sitemap.xml` list live paid paths; remove 020 from Planned |
| Free-route policy | `/`, `/llms.txt`, `/auth.md`, `/api/health`, OpenAPI, `/index.md` never 402 while tip enabled |

### Build / tests

- `./bin/dev build`: **0/0**
- `dotnet run source/container-apps/web/features/tip/submit-tip/submit-tip-tests.cs`: **11/11**
  - 6 integration (incl. alias 402 + free-route never-402)
  - 2 TipDiscoveryAlias unit
  - 3 TipEnvironment unit

### Review

clean, effort 1–2 — see `review/disposition.md`

### Next

104-022 sunny-path tip in E2E; commerce scanners follow `/llms.txt` → `/api/tip`
