# Round 2 — general
**Date:** 2026-09-04
**Scope reviewed:** post-fix delta for M1–M3; scan for new defects

## Summary

Re-verified the uncommitted fix delta against M1–M3. Filtered unique open-link index, migration/Designer/snapshot agreement, EF unique-violation→`InvalidOperationException`+detach, in-memory `Lock` + second-open reject, handler 409 mapping, store test (second-add throw + success after Deny), `Empty_HumanPrincipalId_Should_FailValidation`, gate-test `IAgentHumanLinkStore` pins on all four handlers, and template-smoke web-jaribu expected count 127 all hold. No new defects found; M4 remains wontfix (SPA does not drive `RequestAgentHumanLink`).

## Prior IDs

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/agent-links/agent-human-link-entity-type-configuration-infrastructure.cs:44-46
- Description: Entity config uses `.IsUnique().HasFilter($"\"Status\" IN ({(int)Pending}, {(int)Approved})")` → `"Status" IN (1, 2)` matching enum values. Migration, Designer, and `PostgresDbContextModelSnapshot` all agree on `unique: true` + `filter: "\"Status\" IN (1, 2)"`. `EfAgentHumanLinkStore.AddAsync` catches `DbUpdateException` when `IsUniqueViolation`, detaches, throws `InvalidOperationException`. `InMemoryAgentHumanLinkStore` uses `Lock` on Add/Update and rejects a second Pending/Approved pair. `RequestAgentHumanLink.Handler` maps that throw to `AgentLinkProblems.AlreadyLinked()` (409). Store test `Add_SecondOpenPair_Should_Throw_And_SucceedAfterDeny` covers throw + success after Deny.
- Status: fixed

### M2 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/agent-links/agent-human-link-tests.cs:79-85
- Description: Contracts-tagged `Empty_HumanPrincipalId_Should_FailValidation` builds `Command { HumanPrincipalId = Guid.Empty }`, runs `RequestAgentHumanLink.Validator`, asserts `IsValid` false. Matches `NotEmpty()` on the contract.
- Status: fixed

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/identity/identity-progressive-profile-gate-tests.cs:43-72
- Description: Passkey, agent-key, token, and metered tests each call `AssertHandlerDoesNotTake(..., typeof(IAgentHumanLinkStore))` alongside `IProfileStore`.
- Status: fixed

### M4 — Severity: nit — Status: wontfix (unchanged)
- File: source/container-apps/web/features/agent-links/request-agent-human-link/request-agent-human-link-contracts.cs:19
- Description: Still no `GetMockResponseFactory`. SPA AgentLinks surface only Fetch/Approve/Deny — does not call Request — so mock factory remains optional opt-in; do not re-open.
- Status: wontfix

## Issues

<!-- No new findings. -->
