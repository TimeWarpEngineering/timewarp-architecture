# Round 1 — general
**Date:** 2026-09-22
**Scope reviewed:** Product diff `12cfb442` vs `origin/master` (`source/` and `tests/`). Kanban rescope `4672e102` not reviewed.

## Summary

The change drops the `web-spa-integration-Tests` assembly rename, records pipeline failures as `FluentMessageBar` rows on `ToastNotificationState`, and un-skips the closed-box weather fetch against Aspire ingress HTTP. `TestCaller.Ensure` is case-insensitive and still rejects `Web.Spa`; debug seeders pass `Assembly.GetCallingAssembly()` from `web-spa-integration-tests`. Exception and problem-details handlers mutate the live toast state and do not send actions. `StateTransactionBehavior` in TimeWarp.State 12.0.0-beta.3 restores the failing action's state and only then publishes `ExceptionNotification`, so the error bar is applied after rollback rather than on the discarded clone. The weather client base address is the ingress `http` endpoint, the contract route is anonymous `api/weatherforecast`, and `Days=5` matches the five-row sample. No live `IToastService`, `FluentToast`, or `web-spa-integration-Tests` reference remains in the product diff.
