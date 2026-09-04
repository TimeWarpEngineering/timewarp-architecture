# Disposition — task 205

**Date:** 2026-09-04
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

Effort-1 general review of `origin/feature/overnight...HEAD` (progressive profile + agent–human links). Round 1 raised two bugs (open-link uniqueness; missing Request validation rejection), one suggestion (gate tests omit `IAgentHumanLinkStore` on identity handlers), and one nit (missing Request mock factory). M1–M3 were fixed on this task id and re-verified in round 2. M4 remains wontfix. No escalations.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M4 | nit | `GetMockResponseFactory()` is per-endpoint opt-in for SPA mock mode (`tw-web-api-contracts` §10), not mandatory. RequestAgentHumanLink is agent-token-only; SPA AgentLinks chrome only Fetch/Approve/Deny. Round 2 confirmed SPA does not call Request. | review oracle |

## Escalations

- None
