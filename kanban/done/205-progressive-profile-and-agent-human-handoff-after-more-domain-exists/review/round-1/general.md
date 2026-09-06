# Round 1 — general
**Date:** 2026-09-04
**Scope reviewed:** origin/feature/overnight...HEAD (task 205 product + kitchen)

## Summary

Task 205 hangs optional progressive profile (`Features.Profiles`) and optional Agent↔Human links + humanUx (`Features.AgentLinks`) on existing domain without gating identity register/session/token or paid invoke. Contracts, auth markers, slice isolation, SPA action-sets, permission seeds/migration, and placement docs largely match the brief. Dominant residual risk is open-link uniqueness (application `FindOpen` only) plus one DoD test gap on `RequestAgentHumanLink` validation rejection.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/features/agent-links/agent-human-link-entity-type-configuration-infrastructure.cs:42
- Description: The `(AgentPrincipalId, HumanPrincipalId)` index is non-unique, and neither `InMemoryAgentHumanLinkStore.AddAsync` nor `EfAgentHumanLinkStore.AddAsync` enforces “at most one Pending/Approved row per pair.” `RequestAgentHumanLink` relies on a TOCTOU `FindOpenAsync` then `AddAsync`. Concurrent requests (or any path that skips the handler check) can insert duplicate open links for the same agent–human pair; after Deny, multiple Denied rows are intentional, so a plain unique pair index is also wrong without a filtered unique index on open statuses.
- Suggestion: Add a filtered unique index for open statuses (Pending=1, Approved=2), e.g. `HasIndex(...).IsUnique().HasFilter(...)`, regenerate the migration/snapshot, and make EF `AddAsync` map unique violations to the same 409 path as the handler (mirror `EfProfileStore` unique handling). Optionally harden the in-memory store the same way.
- Status: open

### Issue 2 — Severity: bug
- File: source/container-apps/web/features/agent-links/agent-human-link-tests.cs:27
- Description: Definition of Done requires co-located Jaribu coverage for happy path **and** validation rejection on new endpoints. `RequestAgentHumanLink.Validator` rejects empty `HumanPrincipalId`, but `agent-human-link-tests.cs` has no validator-rejection case (unlike `update-profile-tests.cs`, which covers empty alias / invalid email). Happy-path and several authz/domain cases are present; the validation half for the request contract is missing.
- Suggestion: Add a Contracts-tagged test that builds `RequestAgentHumanLink.Command { HumanPrincipalId = Guid.Empty }`, runs `new RequestAgentHumanLink.Validator().Validate(...)`, and asserts `IsValid` is false (Shouldly).
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/web/features/identity/identity-progressive-profile-gate-tests.cs:43
- Description: File comment and metered test claim handlers must not take `IProfileStore` **or** `IAgentHumanLinkStore`. Passkey register, agent-key register, and token issuance only assert absence of `IProfileStore`. Handlers themselves were re-checked and do not take either store; the pin for `IAgentHumanLinkStore` on those three is incomplete in the gate suite.
- Suggestion: Call `AssertHandlerDoesNotTake(..., typeof(IAgentHumanLinkStore))` in the three identity registration/token tests (same as the metered test).
- Status: open

### Issue 4 — Severity: nit
- File: source/container-apps/web/features/agent-links/request-agent-human-link/request-agent-human-link-contracts.cs:19
- Description: Sibling AgentLinks hosted contracts (`Approve`/`Deny`/`List`/`GetHumanUx`) ship `GetMockResponseFactory()`; `RequestAgentHumanLink` does not. Agent-token-only and not SPA-driven, so low impact, but mock-registry / backend-less demos are inconsistent.
- Suggestion: Add a `GetMockResponseFactory` returning a Pending `Response` with a fixed Guid, matching the other contracts.
- Status: open
