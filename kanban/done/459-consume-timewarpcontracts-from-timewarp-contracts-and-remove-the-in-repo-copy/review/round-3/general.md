# Round 3 — general
**Date:** 2026-11-01
**Scope reviewed:** fix-pass delta `fa07128db..285351c94`. The only product commit is `285351c94` (test: match AddPasskey to the Mediator 15 activation contract). Three files changed, +40/−31: `spa-mediator-send-analyzer.cs`, `spa-mediator-send-analyzer-tests.cs`, and `catalog-agent-tests.cs`.

## Summary

The fix pass closes both round-2 findings. M1: the on-Settings `Credentials.AddPasskey` call now asserts the Mediator 15 path. The dispatcher returns `completed: true` with no `navigateTo`, there is no pending approval, and there is exactly one new `NotificationState` message, with intent `Error` and a Title that contains `IWebServerApiService`. The test still proves the call executed and did not offer navigation: the off-Settings half asserts no new message, and the on-Settings half asserts the error bar. `WebMcpDispatcher` emits `WebMcpCompleted(..., Completed: true, ...)` at `web-mcp-dispatcher.cs:238`, so the JSON shape matches. The product is unchanged, as the goal restatement requires. M2: the analyzer Design region and the test stub now describe Mediator 15 (`ValueTask` Send and Publish, no `CreateStream` or `IStreamRequest`). The `TimeWarp.Mediator` 15.0.0-beta.4 assembly in the local NuGet cache has no `CreateStream`, which matches. The method-group test's expected column moved from 52–65 to 57–70 because `ValueTask` is five characters longer than `Task`.

One new nit: the sentences added to the catalog test Design region sit between the feedback-receipt sentences and "That test drives the real path…", so "That test" now reads as if it meant AddPasskey.

Gates re-run by this reviewer: `dev build` 0 warnings / 0 errors; `timewarp-architecture-analyzers-tests` 259/259; `web-spa-integration-tests` 578/578; `ganda repo audit` exit 0 (34 pass; the only failure is the advisory `kanban-landed`, which names other tasks' claims).

## Issues

### Issue 1 — Severity: nit
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs:18
- Description: The three new Design lines (18–20, "AddPasskey on Settings uses that same closed box…") were inserted in the middle of the feedback-receipt paragraph. The next sentence, "That test drives the real path — dispatcher, approval, ActionSet handler, AgentCallOutcome, and the dispatcher's own result serialization — and asserts the JSON the agent receives." (line 21), describes the FeedbackSpa submit-receipt test. Its nearest antecedent is now the AddPasskey sentence, so an agent reading the region would attach the receipt description to the wrong test. Purpose and Design regions are the record in this repo (TWA0004, `tw-agent-context-regions`).
- Suggestion: Move lines 18–20 to after line 22, so they follow "…asserts the JSON the agent receives." Or keep them where they are and change "That test" to "The feedback receipt test".
- Status: open
