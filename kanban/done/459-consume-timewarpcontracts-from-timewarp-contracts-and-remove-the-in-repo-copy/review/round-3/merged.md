# Round 3 — merged findings
**Date:** 2026-11-01
**Sources:** general

Round 3 re-reviews the fix pass on round 2 (`review/round-2/merged.md`, now frozen). It covers commit `285351c94` (delta `fa07128db..285351c94`). Round-2 IDs carry forward. The new finding is M3.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 1 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs:1067
- Description (round 2): the on-Settings AddPasskey call expected `InvalidOperationException`. Mediator 15 catches the activation failure in `StateTransactionBehavior` and publishes `ExceptionNotification`, so the test failed every run.
- Source: general
- Disposition notes: Fixed in `285351c94`. The test asserts `completed: true`, no `navigateTo`, no pending approval, and exactly one new `NotificationState` message, with intent `Error` and a Title that contains `IWebServerApiService`. The product is unchanged, as the goal restatement requires. The reviewer re-ran `web-spa-integration-tests`: 578/578.

### M2 — Severity: nit — Status: fixed
- File: source/analyzers/timewarp-architecture-convention-analyzers/spa-mediator-send-analyzer.cs:22
- Description (round 2): the analyzer Design region and the test stub still described the Mediator 14 `ISender`.
- Source: general
- Disposition notes: Fixed in `285351c94`. The Design text, the inline comment, and the stub use `ValueTask` and have no `CreateStream` or `IStreamRequest`. That matches the 15.0.0-beta.4 assembly, which has no `CreateStream`. The method-group test's expected column moved to 57–70. The reviewer re-ran `timewarp-architecture-analyzers-tests`: 259/259.

### M3 — Severity: nit — Status: open
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs:18
- Description: The new Design lines 18–20 (AddPasskey on Settings) were inserted between the feedback-receipt sentences and "That test drives the real path — dispatcher, approval, ActionSet handler, AgentCallOutcome, and the dispatcher's own result serialization — and asserts the JSON the agent receives." (line 21). "That test" now reads as AddPasskey, but it describes the FeedbackSpa submit-receipt test.
- Suggestion: Move lines 18–20 to after line 22. Or change "That test" to "The feedback receipt test".
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
