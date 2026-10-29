---
name: timewarp-architecture-discovery
description: Locate agent-facing discovery surfaces on a TimeWarp.Architecture template host (llms.txt, auth.md, OpenAPI, MCP/A2A cards, Agent Skills index, markdown negotiation). Use when scanning or onboarding against this host.
---

# TimeWarp.Architecture — discovery map

Stable root-relative paths. Resolve against the origin that served this skill.

## Start here

| Surface | Path |
|---------|------|
| Discovery index | `/llms.txt` |
| Auth story | `/auth.md` |
| Home (markdown twin) | `/index.md` (also `Accept: text/markdown` on `/`) |
| Crawl policy + Content Signals | `/robots.txt` |
| Sitemap | `/sitemap.xml` |
| OpenAPI | `/openapi/v1.json` |
| Scalar UI | `/scalar/v1` |
| Health | `/api/health` |

`/openapi/v1.json` includes the FastEndpoints operations, `GET /api/health`, and `GET|POST /api` (rewrite alias of `/api/tip`, including `/api/`). It excludes the protocol cards below and `/.well-known/webmcp.json`. That exclusion is the document description and `x-timewarp-excluded-routes`. Scalar (`/scalar/v1`) shows the description. The index is `/llms.txt`.

## Protocol cards (v1 minimal)

| Card | Path |
|------|------|
| MCP Server Card | `/.well-known/mcp/server-card.json` (alias `/.well-known/mcp.json`) |
| Agent Skills index | `/.well-known/agent-skills/index.json` |
| A2A Agent Card | `/.well-known/agent-card.json` (alias `/.well-known/agent.json`) |

**Honesty:** the MCP card is a discoverability stub until a streamable-HTTP MCP transport ships (do not invent tools). The A2A card is unsigned v1 discovery metadata; full A2A JSON-RPC task protocol is not hosted yet. Agent Skills listed in the index are live markdown artifacts.

## Markdown negotiation

```bash
curl -sS -H 'Accept: text/markdown' https://<host>/
curl -sS https://<host>/index.md
curl -sS https://<host>/auth.md
```

Browser `Accept: text/html` still serves the Blazor SPA.

## Content usage preferences

`ai-train=yes, search=yes, ai-input=yes` — see `/robots.txt`.

## WebMCP (browser tools)

Backend MCP (`/mcp`, card `/.well-known/mcp/server-card.json`) and frontend WebMCP are separate transports. The card stays a stub: `capabilities.tools` is false and `tools` is empty. Do not call `/mcp` expecting `tools/list`.

WebMCP tools register in the browser with `document.modelContext.registerTool` (fallback `navigator.modelContext`). The generated list is `/.well-known/webmcp.json` (from the page registry, not a hand-written tool list). `/` registers `navigate` during HTML parse. `site_map` and `page_context` are on every page once it is interactive. Route-scoped tools run only on the matching SPA route: `/Feedback` and `/Settings` add their own actions, and a call from another route returns a navigate offer.

Inspector: `typeof navigator.modelContext`. Chrome Canary flag `#enable-webmcp-testing`, or the MCP-B polyfill. Then Copy trace.

```bash
curl -sS https://<host>/.well-known/webmcp.json
```

## What not to invent

- Do not assume email/password registration exists.
- Do not assume paid x402 tip/meter URLs until linked from `/llms.txt` / `/auth.md`.
- Free/discovery routes never return HTTP 402; disabled payment → 503 on paid routes.
