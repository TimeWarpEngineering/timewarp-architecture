# Round 2 — merged findings
**Date:** 2026-11-01
**Sources:** general

Round 1 (`review/round-1/merged.md`) closed with 0 findings. Its scope was the extraction commit `3cfa29bae`. This round covers the package-roll commit `97ff4440a`. IDs restart at M1 because round 1 raised none.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs:1066
- Description: `CatalogAgent_Should.AddPasskey_Off_Settings_Offers_Navigation_And_On_Settings_Runs` fails deterministically (2/2 runs; suite 577/578). The cause is Mediator 15: the generator resolves the handler inside the pipeline chain. `StateTransactionBehavior` (unchanged between State beta.15 and beta.16, and `RethrowHandlerExceptions` defaults to false) catches the `IWebServerApiService` activation failure. It publishes `ExceptionNotification` instead of letting the exception propagate. `WebMcpDispatcher.InvokeTool` then returns `Completed: true`. CI's `--phase test --group integration` runs this suite.
- Suggestion: Assert the new contract while keeping the "executed, not offered" proof: one new error message in `NotificationState` whose Title contains `IWebServerApiService`, and no pending approval. Fix the comment at lines 1064–1065. If activation failures should still reach the agent as errors, change the product instead and record that decision. Re-run `web-spa-integration-tests` as part of this walk's gates.
- Source: general
- Disposition notes:

### M2 — Severity: nit — Status: open
- File: source/analyzers/timewarp-architecture-convention-analyzers/spa-mediator-send-analyzer.cs:26
- Description: The analyzer Design region ("TimeWarp.Mediator 14 also has ISender<TScope>", a `Task` method-group example) and the analyzer test stub comment ("Mirrors TimeWarp.Mediator 14.0.0-beta.1", with `CreateStream`) describe the Mediator 14 API. The analyzer still works, because it keys on names.
- Suggestion: Update the Design text and the stub comment and members to Mediator 15 (`ValueTask`, no `CreateStream`).
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
