# Ship agent discovery robots Content Signals sitemap llms.txt auth.md

## Parent

104

## Description

Static/discovery: robots.txt with Content Signals (agent-welcome stance), sitemap, llms.txt, auth.md that states passkey/agent-key/x402 truthfully (no fake 'register with email'). Align with timewarp.software posture where applicable.

## Requirements

- Files served in template/demo host
- auth.md matches real auth model
- Content-Signal policy explicit

## Checklist

- [x] robots + signals
- [x] sitemap
- [x] llms.txt
- [x] auth.md

## Notes

isitagentready discoverability + bot access categories.

### Agent onboarding reference (from 104-029)

Point `auth.md` / `llms.txt` agent-onboarding prose at:
- **Demo CLI (preferred human walkthrough):** `tools/agent-identity-cli/` — `dotnet run tools/agent-identity-cli/agent.cs -- demo`
  Commands: `keygen`, `register`, `token`, `whoami`, `demo`. Signs via `AgentKeyProof.BuildSignedData` (TimeWarp.Identity).
- **Manual curl sequence:** Results section of done task **104-004** (openssl keygen + register/token/me HTTP steps).

### Depends on

Can parallelize with late Wave 1 once story is stable.

## Session

- Created: 2026-07-16
- Started: 2026-08-04 (104-017 discovery files)

## Results

### Placement
Static discovery files under `source/container-apps/web/projects/web-spa/wwwroot/`:
- `robots.txt` — agent-welcome; Content-Signal `ai-train=yes, search=yes, ai-input=yes` for major AI bots + `*`
- `sitemap.xml` — root-relative locs (template has no fixed public origin)
- `llms.txt` — discovery index: auth, identity API, OpenAPI/Scalar, CLI onboarding, honest “not yet” for MCP/x402 host wiring
- `auth.md` — passkey / agent-key / x402 truth; no email register; scopes include `identity:read`, `credential:manage`, `demo:invoke`

Served by existing `MapStaticAssets()` on web-server (SPA static web assets). Build endpoints JSON:
- `robots.txt` → `text/plain`
- `llms.txt` → `text/plain`
- `sitemap.xml` → `text/xml`
- `auth.md` → `text/markdown`

### Content stance (vs timewarp.software)
- Same Content-Signal allow-all-AI posture as timewarp.software
- **Different auth.md:** this host has real passkey + agent-key identity (not “no auth required”)
- x402: library exists; tip/meter host paths not invented — pointed at future tasks 009/011/020

### Verify
- `./bin/dev build` → 0 Warning(s) / 0 Error(s)
- SWA manifest includes all four routes under web-spa wwwroot

### Review
- Effort 1 (static docs only); disposition clean — see `review/disposition.md`
