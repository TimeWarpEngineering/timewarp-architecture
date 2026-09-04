# Round 2 — merged findings
**Date:** 2026-09-04
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/agent-links/agent-human-link-entity-type-configuration-infrastructure.cs:44
- Description: Re-verified: filtered unique index on Pending/Approved; migration/Designer/snapshot agree; EF unique-violation maps to InvalidOperationException; in-memory Lock rejects a second open pair; handler returns 409; store test covers throw + success after Deny.
- Suggestion: (applied in round-1 fix)
- Source: general
- Disposition notes: Holds after re-review.

### M2 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/agent-links/agent-human-link-tests.cs:79
- Description: Re-verified: `Empty_HumanPrincipalId_Should_FailValidation` asserts Validator `IsValid` false for `Guid.Empty`.
- Suggestion: (applied)
- Source: general
- Disposition notes: Holds after re-review.

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/identity/identity-progressive-profile-gate-tests.cs:43
- Description: Re-verified: passkey, agent-key, token, and metered tests all pin absence of `IAgentHumanLinkStore`.
- Suggestion: (applied)
- Source: general
- Disposition notes: Holds after re-review.

### M4 — Severity: nit — Status: wontfix
- File: source/container-apps/web/features/agent-links/request-agent-human-link/request-agent-human-link-contracts.cs:19
- Description: No `GetMockResponseFactory` on RequestAgentHumanLink.
- Suggestion: Add a Pending mock factory.
- Source: general
- Disposition notes: Unchanged. Factories are SPA-mock opt-in (`tw-web-api-contracts` §10). SPA does not call this agent-token endpoint. Round 2 did not re-open.

## Duplicates / conflicts

- No new findings. Prior IDs carried forward.
