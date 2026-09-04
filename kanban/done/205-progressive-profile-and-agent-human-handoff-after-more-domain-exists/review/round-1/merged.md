# Round 1 — merged findings
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
- File: source/container-apps/web/features/agent-links/agent-human-link-entity-type-configuration-infrastructure.cs:42
- Description: The `(AgentPrincipalId, HumanPrincipalId)` index is non-unique, and neither store `AddAsync` enforces “at most one Pending/Approved row per pair.” `RequestAgentHumanLink` relies on TOCTOU `FindOpenAsync` then `AddAsync`. Concurrent requests can insert duplicate open links. After Deny, multiple Denied rows are intentional, so a plain unique pair index is wrong without a filtered unique index on open statuses.
- Suggestion: Filtered unique index for Pending/Approved (`Status IN (1, 2)`), regenerate/amend the unreleased migration + snapshot, EF `AddAsync` maps unique violations like `EfProfileStore`, in-memory store rejects a second open pair, handler maps that throw to 409 `AlreadyLinked`.
- Source: general
- Disposition notes: Filtered unique index on Pending/Approved; unreleased migration + snapshot amended; EF unique-violation → InvalidOperationException; in-memory Lock + reject second open pair; handler maps throw to 409. Store test covers second-add throw and success after Deny.

### M2 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/agent-links/agent-human-link-tests.cs:27
- Description: DoD requires happy path **and** validation rejection. `RequestAgentHumanLink.Validator` rejects empty `HumanPrincipalId`, but the runfile has no validator-rejection case.
- Suggestion: Contracts-tagged test: `HumanPrincipalId = Guid.Empty` → `Validator` `IsValid` false.
- Source: general
- Disposition notes: `Empty_HumanPrincipalId_Should_FailValidation` added (Contracts-tagged). Runfile 9 → 11 passed.

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/identity/identity-progressive-profile-gate-tests.cs:43
- Description: File comment and metered test claim handlers must not take `IProfileStore` or `IAgentHumanLinkStore`. Passkey/agent-key/token tests only assert `IProfileStore`. Handlers were re-checked and take neither; the pin is incomplete.
- Suggestion: `AssertHandlerDoesNotTake(..., typeof(IAgentHumanLinkStore))` on those three tests.
- Source: general
- Disposition notes: All four gate tests now assert both forbidden stores. 4 passed.

### M4 — Severity: nit — Status: wontfix
- File: source/container-apps/web/features/agent-links/request-agent-human-link/request-agent-human-link-contracts.cs:19
- Description: Sibling AgentLinks hosted contracts ship `GetMockResponseFactory()`; `RequestAgentHumanLink` does not.
- Suggestion: Add a Pending mock factory.
- Source: general
- Disposition notes: Wontfix — `tw-web-api-contracts` §10: `GetMockResponseFactory()` is per-endpoint opt-in for SPA mock mode, not mandatory ceremony. This endpoint is agent-token-only and not SPA-driven. Decided by review oracle.

## Duplicates / conflicts

- None. M4 wontfix'd at merge (skill: factories are opt-in).
