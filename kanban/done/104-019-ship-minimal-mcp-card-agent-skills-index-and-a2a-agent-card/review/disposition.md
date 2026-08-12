# Review disposition — 104-019

**Effort:** 1 (static well-known JSON/markdown only; no C# / auth surface changes)  
**Verdict:** clean

## Scope

Ship discoverable MCP Server Card, Agent Skills index (+ two skill-md artifacts), and A2A Agent Card under `web-spa/wwwroot/.well-known/`, linked from discovery.

| Surface | Path |
|---------|------|
| MCP card | `/.well-known/mcp/server-card.json` (+ `mcp.json` alias) |
| Skills index | `/.well-known/agent-skills/index.json` |
| Skills | `timewarp-identity`, `timewarp-architecture-discovery` |
| A2A card | `/.well-known/agent-card.json` (+ `agent.json` alias) |

Served via existing `MapStaticAssets()`. Content-types from SWA: `application/json` / `text/markdown`.

## Checks

- [x] Stable well-known URLs (scanner conventions: MCP server-card, agent-skills/index, agent-card)
- [x] Valid minimal JSON shapes (schema fields present; digests match skill-md bytes)
- [x] Honest stubs (no invented live MCP tools or A2A JSON-RPC task endpoint)
- [x] Linked from `llms.txt` / `index.md` / `auth.md` / `sitemap.xml`
- [x] web-spa build 0/0 with assets in SWA endpoints manifest

## Findings

None open. No security reviewer (no auth code changed). Full solution build noise in this worktree is concurrent 013/016/020, not this task.

## Notes

When a real MCP streamable-HTTP transport or A2A JSON-RPC endpoint ships, update the cards (flip capabilities, real interface URL, optional signatures) in the same commit as the endpoint.
