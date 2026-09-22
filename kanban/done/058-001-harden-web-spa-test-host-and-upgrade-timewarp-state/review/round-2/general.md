# Round 2 — general
**Date:** 2026-09-22
**Scope reviewed:** Product diff `12cfb442` vs `origin/master` (`source/` and `tests/`). Kanban commits after that diff were not reviewed as product changes. Round 1 files were left unchanged.

## Summary

Re-checked the same web-spa workaround removal. Exception and problem-details handlers append a `FluentMessageBar` row and call `ReRenderSubscribers`; they do not `Send` and do not use `IToastService`. TimeWarp.State 12.0.0-beta.3 `StateTransactionBehavior` calls `Store.SetState(originalState)` before it publishes `ExceptionNotification`, so the error bar lands on the restored state. `TestCaller.Ensure` still throws `FieldAccessException` unless the calling assembly name contains `test` (ordinal-ignore-case). The weather fact is not skipped, the named api-server client uses the ingress `http` endpoint, and `MockAuthenticationRegistration` supplies `MockAccessTokenProvider`. No new defect on this delta.

## Issues

None.
