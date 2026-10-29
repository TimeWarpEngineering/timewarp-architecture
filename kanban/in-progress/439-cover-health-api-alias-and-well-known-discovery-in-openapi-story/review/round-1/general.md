# Round 1 — general
**Date:** 2026-10-29
**Scope reviewed:** commit 35c6b5749 (same as framework), plus the ingress call sites the new operations depend on (`source/container-apps/yarp/program.cs`, `source/container-apps/yarp/appsettings*.json`, `source/container-apps/aspire/projects/aspire-app-host/program.cs`, `goldens/ingress/WebServerApiRoutePrefixes.g.cs`).

## Summary

The change adds an opt-in document transformer (`OpenApiDiscoveryCoverage`) to web-server's OpenAPI pipeline. It adds `GET /api/health` and `GET|POST /api` as operations, appends the alias sentence to `/api/tip`, and lists the six `/.well-known/*` documents in `info.description` and `x-timewarp-excluded-routes`. `llms.txt` and the discovery skill now point at that exclusion. The code is clean, idempotent, and only on for web-server. Both validation commands pass locally: `OpenApiDiscoveryCoverage_` passed 6 of 6, and `submit-tip-tests.cs` passed 12 of 12, including `ApiHealth_Should_ReturnPlaintextStatus`.

The main risk is that every new test calls web-server directly. The OpenAPI `servers` entry is the public ingress origin (task 435), so a client resolves each operation against ingress. In the default variant (`api=true`), two of the routes the document now advertises fail at the public origin. Both failures are in existing ingress routing. This diff turns them into machine-readable claims, which goes against the Goal's line that `/api/health` "must be a real route".

Live evidence (read-only GETs to https://arch.timewarp.work, 2026-10-29):

| Path | Status | Body |
|------|--------|------|
| `/api/health` | 404 | empty |
| `/api` | 500 | `AmbiguousMatchException … Matches: TipDiscoveryAlias-api, TipDiscoveryAlias-api-slash, ApiRoute, WebRoute` |
| `/api/` | 500 | (same) |
| `/web-server/alive` | 200 | `Healthy` |

## Issues

### Issue 1 — Severity: bug
- File: source/foundation/foundation-server/open-api-discovery-coverage.cs:283 (with source/container-apps/yarp/program.cs:84 and source/container-apps/aspire/projects/aspire-app-host/program.cs:837)
- Description: The document advertises `GET /api/health` on the public server. Ingress never sends that path to web-server. `health` is not an `[ApiRoute]` prefix, so it is missing from `WebServerApiRoutePrefixes.All`, and no literal ingress route exists for it. With `api=true` (the template default), `/api/health` matches Api.Server's `/api/{**catch-all}`, and api-server does not map that path. The live origin returns 404. `ApiHealth_Should_ReturnPlaintextStatus` hits web-server's base address directly, so it does not cover the path an agent reading the OpenAPI document or `llms.txt` would use. Checklist item 3 ("Verify /api/health exists") is satisfied only when web-server is called directly.
- Suggestion: Add an exact `/api/health` → web-server ingress route in both ingress builders: the yarp project's in-memory routes beside the `TipDiscoveryAlias` routes, and the AppHost `#if web` block beside `/api`. Add a test through ingress, or lock the route list in a unit test. Remember the Neon note in `tools/dev-cli/services/deploy-pipeline.cs:35`: a public route that opens Postgres is the intended behavior only if that note's reasoning still holds. The deploy smoke stays off it either way. The alternative is to drop the operation and say in the document that `/api/health` is web-server-only and not reachable through ingress. That conflicts with `llms.txt`, which links it on the public origin.
- Status: open

### Issue 2 — Severity: bug
- File: source/foundation/foundation-server/open-api-discovery-coverage.cs:311 (with source/container-apps/yarp/program.cs:104-117 and source/container-apps/aspire/projects/aspire-app-host/program.cs:857-859)
- Description: The new `GET|POST /api` operations say the alias rewrites to `/api/tip` with the same 200/402/503 responses. At the public origin, `/api` and `/api/` return 500 `AmbiguousMatchException`. The ingress registers `"/api"` and `"/api/"` as separate routes. ASP.NET routing parses them as the same template, so they tie with each other, and on the live host also with `ApiRoute` and `WebRoute`. The alias came from task 104-020 and was broken before this diff. This diff publishes it as a documented operation with listed response codes, and no test calls it through ingress. The 500 body is also a full stack trace in production (see the out-of-scope note).
- Suggestion: Keep a single exact `/api` route on web-server in each ingress builder; ASP.NET routing already matches the trailing slash for that template. Make sure it outranks `/api/{**catch-all}` and `{**catch-all}`, for example with an explicit `Order`. Add a test through ingress, or a route-table test, that `/api` and `/api/` reach web-server. If the ingress fix does not belong on this id, remove the alias operations until the route works. Shipping a documented operation that returns 500 is the wrong state.
- Status: open

## Out-of-scope observations (not findings against this diff)

- The production YARP 500 body is a plaintext developer exception page with a stack trace. That is information disclosure on the live host. Worth a separate task.
- `wwwroot/.well-known/agent-skills/index.json` `digest` values do not match `sha256sum` of either SKILL.md, and they did not match before this commit either (the discovery skill was `39f5876…` before the commit, `7cf36f0…` after, and the index says `bc7508d…`). This diff edits the discovery SKILL.md, so the mismatch continues. Per AGENTS.md ("When two things must agree, generate one from the other or add a build check"), this is a follow-up candidate. It may be that the digest is computed over the template-processed output.

## Goal restatement check

The restatement matches the task: decide include or exclude for each group, document exclusions where Scalar and `llms.txt` can see them, and make `/api/health` real and machine-readable. No misread. The gap is in verification: routes were checked on web-server, not at the public origin the document advertises.
