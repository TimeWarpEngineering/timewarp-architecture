# Progressive profile and agent-human handoff after more domain exists

## Description

Deferred product work pulled off epic **104** (Agent-ready Identity and x402).

**104** shipped principals, credentials, sessions/tokens, TimeWarp.402, and the agent-ready
template surface (Waves 1–4 + 023). These two items are **higher-level product** than that
kernel. Placement (which slice, which package, which host) is not honest until more of the
**domain** exists — marketplace, human account chrome beyond Settings passkeys, and any
real agent↔human workflow.

Do **not** treat this as 104 leftover polish. Do **not** implement until domain surfaces
exist to hang them on.

Folded from:

| Old child | Topic |
|-----------|--------|
| **104-024** (done — superseded stub) | Optional progressive profile after the principal exists |
| **104-025** (done — superseded stub) | Optional Agent ↔ Human link and portable humanUx handoff payload |

## Requirements

### Progressive profile (was 104-024)

- Optional display name / email / etc. **after** principal exists
- Contract/endpoint style of the template
- **Never** a gate on passkey register, agent-key register, session, or token
- Locked 104 decision 1: passkey/key first, profile later

### Agent–human link + humanUx (was 104-025)

- Optional link Agent ↔ Human and a portable humanUx JSON an agent can show its human
- Minimal link/approve mechanism
- humanUx schema in a Design region / sample JSON
- **Not** required for paid service (locked 104 decision 3: no human required if the agent pays)

## Checklist

- [ ] Enough domain exists to place profile vs identity vs a future account slice
- [ ] Progressive profile: model fields, update API, tests — still never a register/session gate
- [ ] Agent–human link: link model, minimal approve flow
- [ ] Sample humanUx payload + Design region
- [ ] Document where this lives (Identity vs template Features vs new slice) — decide then, not now

## Notes

Hold until demanded **and** until domain placement is obvious. A2A-shaped handoff.

Former Wave 5 on **104**. Cloudflare operator notes (**104-023**) stayed on 104 and are **done**.

Soft predecessors (not `## Depends on` merge-wait): 104-002 (principal model), 104-004 (agent keys),
104-016 (human passkey demo). Those are already merged.

## Session

- Created: 528392 (2026-08-26)
- Cockpit: Grok — pulled 104-024 / 104-025 off epic 104 into this independent to-do
