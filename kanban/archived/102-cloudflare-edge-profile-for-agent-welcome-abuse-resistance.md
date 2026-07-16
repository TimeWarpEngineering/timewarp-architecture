# Cloudflare edge profile for agent-welcome abuse resistance

## Description

Document (and optionally implement) Cloudflare as the outer ring: DDoS, WAF,
rate limits, AI crawl classes. Posture is agent-welcome + abuse-resistant — not
default-block-all-AI-bots. Optional Worker front door for 402/static agent assets.

## Requirements

- Written recommended config for template operators
- Rate-limit guidance for register + 402 challenge endpoints
- AI crawl / bot class notes (Search vs Training vs Agent)
- Optional Worker front door design
- Web Bot Auth / verified-bot notes as applicable

## Checklist

- [ ] 102-001 Recommended CF config doc
- [ ] 102-002 Optional Worker front door
- [ ] 102-003 Web Bot Auth notes

## Notes

### Depends on
097-003 ADR.

### Does not replace
App-level Identity + 402 quotas (see 100-003).

## Session

- Created: 2026-07-16
