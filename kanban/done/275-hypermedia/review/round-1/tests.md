# Round 1 — tests
**Date:** 2026-10-05
**Scope reviewed:** hypermedia-lab-tests.cs (web-spa-integration-tests), hypermedia-lab-endpoint-tests.cs (web-server-integration-tests), hypermedia-lab-contracts-serialization-tests.cs (web-contracts-tests), the action-catalog-tests.cs / command-palette-tests.cs changes, and credential-revoke-tests.cs against the CredentialRules refactor. Read-only; no suites run.

## Summary
Requirement 8 is covered on every bullet. Offered set vs server rule is pinned against the real handlers in the server suite (the SPA suite's scripted API restates the rule, which is acknowledged in its Design region). Invoking the real operation, the follow-up changing the set, non-offered refusal, B unknown name / bad args, C cross-origin refusal and Ctrl-K appear/clear are all asserted with assertions that can fail. The refactored `CredentialRules.CanRevoke` is still covered by `credential-revoke-tests.cs` (`Conflict_Given_Last_Active_Credential`, 409). The new Validators are empty `AbstractValidator<Query>` (no inputs), so there is no validation-rejection case to write; ctor-Guard rejection is covered by the two `Reject_*` deserialization tests. No bugs. Two small coverage gaps.

## Issues
### Issue 1 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/hypermedia-lab/hypermedia-lab-tests.cs:1
- Description: Requirement 3 says offered actions appear as buttons on the page, and only the latest payload's actions. All SPA tests drive the state/runner/palette; nothing renders `HypermediaLabPage` (or asserts the page's button set per tab equals the offered set, or that a non-offered action has no button). The button path is only exercised indirectly via `RunContextualAsync`.
- Suggestion: Add one render test (existing `credential-list-render-tests.cs` pattern) for each tab: two credentials show Revoke buttons, after revoke down to one none remain; or drop the claim from the task's validation list.
- Status: open

### Issue 2 — Severity: nit
- File: tests/container-apps/web/web-server-integration-tests/features/hypermedia-lab/hypermedia-lab-endpoint-tests.cs:225
- Description: End-to-end follow of the server-written link is only exercised for Revoke. The Rename link (href plus `nickname` field alongside the `userId` body template) is never posted against the real endpoint, so drift between the C rename href/body shape and `RenameCredential` would only be caught by the SPA's scripted API, which matches the hrefs by string equality, not the real route.
- Suggestion: Add a sibling test that posts the rename command's href with `Fields` filled in and asserts 200 and the new nickname in a re-read of `Self`.
- Status: open
