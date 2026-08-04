# Ship minimal MCP card Agent Skills index and A2A agent card

## Parent

104

## Description

Minimal discoverable JSON/docs for MCP server card, Agent Skills index, A2A agent card. Stubs OK if honest; link from llms.txt.

## Requirements

- URLs stable
- Valid minimal shapes
- Linked from discovery

## Checklist

- [x] MCP card
- [x] Skills index
- [x] A2A card
- [x] Link from llms.txt

## Notes

Signed A2A cards later. v1 = discoverable minimal.

### Depends on

104-017

## Session

- Created: 2026-07-16
- Started: 2026-08-04 (104-019 protocol cards)
- Completed: 2026-08-04

## Results

### Placement (stable well-known URLs)

All under `source/container-apps/web/projects/web-spa/wwwroot/`:

| Surface | Path | Alias |
|---------|------|-------|
| MCP Server Card | `/.well-known/mcp/server-card.json` | `/.well-known/mcp.json` |
| Agent Skills index | `/.well-known/agent-skills/index.json` | — |
| skill-md | `/.well-known/agent-skills/timewarp-identity/SKILL.md` | — |
| skill-md | `/.well-known/agent-skills/timewarp-architecture-discovery/SKILL.md` | — |
| A2A Agent Card | `/.well-known/agent-card.json` | `/.well-known/agent.json` |

Served by existing `MapStaticAssets()` (SPA static web assets). SWA endpoints:

- `*.json` → `application/json`
- `SKILL.md` → `text/markdown`

### Honesty (stubs)

- **MCP:** `capabilities.tools/resources/prompts=false`, `tools=[]`, `status: "stub"`. Description forbids calling `/mcp` for tools until a transport ships. Card shape matches Cloudflare/SEP-1649 scanner expectations.
- **A2A:** Unsigned v1 card (`supportedInterfaces` → OpenAPI + `agent/me` as HTTP+JSON; no fake JSON-RPC A2A endpoint). Skills map to live REST identity ceremonies + discovery. Signatures deferred.
- **Agent Skills:** Live skill-md artifacts (not stubs) with v0.2.0 index digests (`sha256:` of raw file bytes).

### Discovery links

- `llms.txt` — Protocol discovery section + curl
- `index.md` — agent-first surfaces table
- `auth.md` — skill link for identity ceremony
- `sitemap.xml` — well-known card/index locs

### Verify

- `dotnet build …/web-spa.csproj -c Release` → 0 Warning(s) / 0 Error(s)
- SWA endpoints JSON includes all well-known routes with correct Content-Types
- JSON validated (`json.load`) for all five JSON documents; skill digests re-verified against files
- Full `./bin/dev build` was red due to **concurrent Wave 2/3 work** in the same worktree (402 metered ctor + passkey SPA mid-edit) — not caused by this static-docs change. web-spa alone is green with these assets.

### Commit

- `1adc8aa4` feat(web): ship minimal MCP card, Agent Skills index, A2A agent card (104-019)

### Review

- Effort 1 (static well-known JSON/md only)
- Disposition: clean — see `review/disposition.md`

### How to validate

**Static cards**
```bash
./bin/dev run
curl -sS https://localhost:7000/.well-known/mcp/server-card.json | head -40
curl -sS https://localhost:7000/.well-known/agent-skills/index.json | head -40
curl -sS https://localhost:7000/.well-known/agent-card.json | head -40
curl -sS https://localhost:7000/llms.txt | rg -n 'well-known|MCP|A2A|skills'
```

**Expect**
- Valid minimal JSON; MCP/A2A stubs honest (`status: stub` / no fake live tools)
- Linked from llms.txt / index.md

**Automated:** `./bin/dev build` 0/0.

