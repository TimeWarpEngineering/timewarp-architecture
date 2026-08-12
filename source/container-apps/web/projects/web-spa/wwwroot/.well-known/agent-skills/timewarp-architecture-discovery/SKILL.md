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

## What not to invent

- Do not assume email/password registration exists.
- Do not assume paid x402 tip/meter URLs until linked from `/llms.txt` / `/auth.md`.
- Free/discovery routes never return HTTP 402; disabled payment → 503 on paid routes.
