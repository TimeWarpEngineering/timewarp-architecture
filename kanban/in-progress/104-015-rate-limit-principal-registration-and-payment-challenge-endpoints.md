# Rate-limit principal registration and payment challenge endpoints

## Parent

104

## Description

App-level rate limits so unpaid 402 floods and mass register cannot melt origin. Keep 402 responses cheap. Edge (Cloudflare) is extra later (023).

## Requirements

- Limits on register
- Limits on payment challenge
- Configurable defaults
- Structured 429 for agents where applicable

## Checklist

- [ ] Middleware or policy
- [ ] Tests or verified manual notes
- [ ] Design region: edge vs app

## Notes

Cheap identity, expensive power — also cheap rejection.

### Depends on

104-003, 104-004, 104-008

## Session

- Created: 2026-07-16
