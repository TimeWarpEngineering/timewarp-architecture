# Review disposition — 104-017

**Effort:** 1 (static discovery prose only; no C# / auth surface changes)  
**Verdict:** clean

## Scope

Ship agent discovery files for the web template host:

| File | Path |
|------|------|
| robots.txt | `web-spa/wwwroot/robots.txt` |
| sitemap.xml | `web-spa/wwwroot/sitemap.xml` |
| llms.txt | `web-spa/wwwroot/llms.txt` |
| auth.md | `web-spa/wwwroot/auth.md` |

Served via existing `MapStaticAssets()` (SPA static web assets). Content-types from SWA build: plain/xml/markdown as expected.

## Checks

- [x] Agent-welcome Content Signals (not Disallow all AI bots)
- [x] auth.md matches real model (passkey + agent key + x402 posture; no fake email register)
- [x] Onboarding points at `tools/agent-identity-cli` and 104-004 curl notes
- [x] Does not invent unpaid tip/meter URLs not yet on host
- [x] Build 0/0

## Findings

None open. No security reviewer (no auth code changed).

## Notes

Wave 4 siblings (018 markdown negotiation, 019 MCP/skills cards, 020 x402 discoverable path) will extend `llms.txt` links when they land.
