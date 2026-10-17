# Max out the isitagentready.com score for arch.timewarp.work

## Goal

Steve, 2026-10-13: get https://isitagentready.com/arch.timewarp.work maxed out.

arch.timewarp.work is the template's own dogfood deployment, so **everything lands in the template**
and ships to every generated app. Values that differ per deployment (public origin, hostnames)
come from configuration, never literals.

## The scanner (Cloudflare "Is Your Site Agent-Ready?")

- API: `POST https://isitagentready.com/api/scan` with body `{"url":"https://arch.timewarp.work"}`
  returns JSON. Site types: All Checks / Content Site / API / Application. Aim for the max under
  **All Checks**, and record what each site type scores.
- **No baseline yet.** On 2026-10-13 the scan returned `siteError` 502 (Bad Gateway via Cloudflare),
  because the dev instance behind arch.timewarp.work was down. The maintainer brings it up, and the
  cockpit records the baseline JSON in this task folder (`scan/baseline.json`) before or at the start
  of implementation.
- Categories and checks (from the scanner's page):
  - **Discoverability:** robots.txt, Sitemap, Link response headers, DNS for AI Discovery (DNS-AID).
  - **Content Accessibility:** Markdown content negotiation.
  - **Bot Access Control:** AI bot rules in robots.txt, Content Signals, Web Bot Auth.
  - **API / Auth / MCP:** API Catalog, OAuth discovery, OAuth Protected Resource, Auth.md, MCP
    Server Card, A2A Agent Card, Agent Skills, WebMCP, ARD manifest.
  - **Commerce:** x402, MPP, UCP, ACP.

## What already exists (cockpit inventory)

- `web-spa/wwwroot/robots.txt`, `sitemap.xml`, `llms.txt` (static; check the content against the
  checks).
- Markdown content negotiation: `web/platform/agent-discovery/markdown-content-negotiation-server.cs`
  (+ tests).
- WebMCP: `web-spa/source/features/web-mcp.ts`, `components/WebMcpAgentSurface.razor`,
  `page-agent-context.cs`.
- x402 library: `source/libraries/timewarp-402/`. Bringing it into the app is task **309**;
  coordinate, and don't duplicate.
- Agent tokens and passkeys (TimeWarp.Identity). OAuth discovery / Protected Resource metadata may
  map onto the agent-token auth, so check what is honest to advertise.

## Baseline (2026-10-17)

`scan/baseline.json`: level 4 "Agent-Integrated", All Checks. The site is not flagged as commerce.

| Result | Checks |
|--------|--------|
| pass | robotsTxt, sitemap, markdownNegotiation, robotsTxtAiRules, contentSignals, mcpServerCard, a2aAgentCard, agentSkills |
| fail | linkHeaders, dnsAid, apiCatalog, oauthDiscovery, oauthProtectedResource, authMd (no agent-registration markers), webMcp (no tools on page load at `/`), ard, personalAgentProtocol |
| neutral | webBotAuth (informational); x402, mpp, ucp, acp, ap2 (not a commerce site) |

The cockpit inventory above predates the baseline: the MCP server card, A2A card and Agent Skills
index already pass. The fail list is the remaining scope. `webMcp` and `authMd` are owned by
**343-001**. The other fails stay on this task until split into children.

## Requirements

1. **Measure.** Fetch the scanner's per-check results and recommendation text. Map each check to:
   implement here / already passing / owned by another task (309) / operator-only infra / deliberately
   not applicable. Record the map in `scan/plan.md`.
2. **Implement every app-side check honestly.** Advertise only what the app really supports; never
   publish a manifest for a capability that doesn't exist just to score. In particular:
   - robots.txt with AI bot rules, a sitemap directive and Content Signals;
   - Link response headers on the homepage for discovery (api-catalog, sitemap, llms, agent card /
     MCP card as applicable);
   - `/.well-known/` documents per their specs (api-catalog RFC 9727, oauth-protected-resource
     RFC 9728, OAuth authorization-server metadata if applicable, MCP server card, A2A agent card,
     ARD manifest, Auth.md, Agent Skills index, which can point at the shipped `skills/`);
   - Web Bot Auth (verifying signed agent requests) if feasible, or document why not;
   - Commerce: x402 through task 309. MPP / UCP / ACP: evaluate each and implement only real ones;
     record the rest as deliberately not applicable.
3. **Generated from code, not hand-maintained.** Where a document lists real endpoints or
   capabilities (API catalog, MCP card, agent card, sitemap), generate it from the app's contracts,
   routes and `[Page]` registry (existing generators), or add a build check. Never write
   agreement-by-memory JSON.
4. **Production safety.** Nothing here exposes internal service URIs, secrets or admin routes. Admin
   and developer pages stay out of the sitemap and agent cards. The publish safety suites stay green.
5. **An in-repo readiness test.** An integration test (in-process or closed-box) fetches each endpoint
   and asserts its shape, so CI proves readiness without depending on the external scanner.
6. **Operator steps** that can't be code (DNS-AID TXT/SVCB records at Cloudflare DNS, anything
   hostname-specific): document them in `skills/tw-deploy` (or a new reference), with the exact
   records for `arch.timewarp.work` in this task's notes.
7. **Final proof:** after merge and deploy, the cockpit runs the scan API again and saves
   `scan/final.json`. Any check not at max is listed with its reason (another task, operator step, or
   not applicable).

## Checklist

- [x] Baseline scan saved (`scan/baseline.json`, 2026-10-17)
- [ ] Per-check plan (`scan/plan.md`)
- [ ] 343-001: WebMCP page-load registration + HTTP discoverability + auth.md registration (folded from 382)
- [ ] Discoverability: robots/sitemap/Link headers (+ DNS-AID operator doc)
- [ ] Bot access: AI bot rules, Content Signals, Web Bot Auth decision
- [ ] Protocol discovery: well-known docs (generated), WebMCP verified, Agent Skills
- [ ] Commerce: coordinate with 309; MPP/UCP/ACP decisions recorded
- [ ] In-repo readiness test; publish safety suites green
- [ ] Final scan after deploy; gaps explained
- [ ] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`

## Session

- Created: 2026-10-13 (cockpit, per Steve). Scanner API discovered: POST /api/scan. Site returned 502 at filing.
- Baseline: scan run 2026-10-17 (site back up), saved to `scan/baseline.json`. 382 folded in as child 343-001.
