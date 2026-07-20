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

- [ ] robots + signals
- [ ] sitemap
- [ ] llms.txt
- [ ] auth.md

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
