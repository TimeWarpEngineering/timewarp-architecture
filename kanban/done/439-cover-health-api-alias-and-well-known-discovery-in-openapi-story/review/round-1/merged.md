# Round 1 — merged findings
**Date:** 2026-10-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 2 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/foundation/foundation-server/open-api-discovery-coverage.cs:283; ingress: source/container-apps/yarp/program.cs:84, source/container-apps/aspire/projects/aspire-app-host/program.cs:837
- Description: OpenAPI advertises `GET /api/health` on the public server, but ingress sends `/api/health` to Api.Server's `/api/{**catch-all}` (`health` is not in `WebServerApiRoutePrefixes`, and there is no literal route). Live origin: 404. The new tests call web-server directly, so the public path is untested.
- Suggestion: Add an exact `/api/health` → web-server route in both ingress builders (yarp in-memory routes and the AppHost `#if web` block), with a test through ingress or a route-table test. Alternatively, drop the operation and document `/api/health` as web-server-only, and fix `llms.txt` to match.
- Source: general
- Disposition notes:

### M2 — Severity: bug — Status: open
- File: source/foundation/foundation-server/open-api-discovery-coverage.cs:311; ingress: source/container-apps/yarp/program.cs:104-117, source/container-apps/aspire/projects/aspire-app-host/program.cs:857-859
- Description: The new `GET|POST /api` alias operations promise the `/api/tip` responses, but at the public origin `/api` and `/api/` return 500 `AmbiguousMatchException`. The duplicate `"/api"` and `"/api/"` ingress routes tie with each other (and with `ApiRoute`/`WebRoute`). The breakage predates this diff (104-020), but the diff publishes it as a documented operation.
- Suggestion: One exact `/api` route on web-server in each ingress builder, ordered above the catch-alls, with a test through ingress or a route-table test for `/api` and `/api/`. Otherwise remove the alias operations until the route works.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. M1 and M2 share a root theme (the new operations were verified against web-server, not the ingress origin in `servers`), but they need separate ingress fixes.

## Out-of-scope (recorded, not counted)

- Production YARP returns a developer exception page (stack trace) on 500.
- `agent-skills/index.json` digests did not match either SKILL.md before this commit and still do not.
