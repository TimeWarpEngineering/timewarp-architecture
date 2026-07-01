# Add a Notifications demo to the Style Guide (and plan when to split it into a nav section)

## Why

There is currently **no UI affordance that deliberately triggers a toast/notification** in the app.
The toast handlers in `source/container-apps/web/web-spa/features/toast-notification/` only fire on
the **error path**:

- `ExceptionNotificationHandler` — on any unhandled action exception (via `StateTransactionBehavior`'s
  `ExceptionNotification`).
- `AddProblemDetailsActionSet` — on an API `SharedProblemDetails`.

So the only way to see a toast in the running app is to *cause an error*. After the FluentUI
**rc.4** migration (`IToastService` → `INotificationService`, `ToastOptions.Timeout` → `Lifetime`)
there is no quick way to **visually verify** that toasts still render. The provider
(`<FluentUIRequiredFeatures />` in `MainLayout.razor:21`) is wired but otherwise dormant.

Related dead/stub surfaces to clean up while we're here:
- `components/pages/NotificationBanner.razor` — an **empty stub** (`@namespace` only); renders nothing.
- `features/notification/` (`NotificationState` banner/list) — state + actions exist but nothing
  renders or dispatches it (the banner above is the empty stub).
- `features/counter/notification/IncrementCountNotificationHandler` — only `Logger.LogDebug(...)`;
  no visible notification.

The StyleGuide is exactly the right home for a live demo — it already showcases tokens, type scale,
buttons, and status badges.

## What

### 1. Add a "Notifications" card to the Style Guide
File: `source/container-apps/web/web-spa/features/style-guide/pages/StyleGuidePage.razor`
- Add a `<Card Title="Notifications">` with buttons that dispatch a toast for each intent
  (Success / Info / Warning / Error) via the existing `ToastNotificationState` actions
  (`AddNotificationActionSet` — confirm the action's public surface; add an intent/title overload
  if the only entry point today is the error-only `AddProblemDetailsActionSet`).
- Add one button that throws inside an action to exercise the
  `ExceptionNotification` → `ExceptionNotificationHandler` → toast path end-to-end.
- Dispatch through the page's state/sender the same way other pages do (BaseComponent/`Sender`),
  not by calling `INotificationService` directly — the point is to test *our* pipeline.

### 2. Decide the fate of the banner / `features/notification`
- Either render `NotificationState` in `NotificationBanner.razor` and demo it from the StyleGuide,
  **or** delete the empty stub + unused `features/notification` feature if the toast system fully
  supersedes it. Pick one; don't leave both half-wired.

### 3. Structure decision — single page now, nav category later (DECIDED)
Keep the Style Guide as a **single page now** (5 cards is still scannable; a menu section for 5 small
sections would be worse UX). Each `<Card>` is already self-contained, so a later split is cheap.

**Promotion trigger:** when the page exceeds ~6–8 sections (or any section needs heavy interaction),
promote `StyleGuidePage` from a single `TimeWarpNavLink` under the **Developer**
`FluentNavCategory` (`components/NavMenu.razor`) into its **own `FluentNavCategory` "Style Guide"**
with one child page per area (Tokens, Type, Components, Notifications, Forms…). This mirrors Fluent's
own demo-site layout. Do NOT split as part of this task — just add the Notifications card.

## Done when
- [x] A developer can open the Style Guide in a running app and fire a Success/Info/Warning/Error
  toast and an exception-path toast with one click each — visually confirming the rc.4 migration.
- [x] The empty `NotificationBanner.razor` / unused `features/notification` is either wired up or
  removed (no half-wired stub left).
- [x] `dev build` green; no new warnings (warnings-as-errors is enforced, task 074).

## Outcome (done 2026-07-01)
- **Notifications card** added to `StyleGuidePage` (`features/style-guide/pages/StyleGuidePage.razor`
  + `.razor.cs`): Info/Success/Warning/Error buttons dispatch through the app's own
  `ToastNotificationState.AddNotification(intent, title)` pipeline; a "Throw exception" button
  dispatches `CounterState.ThrowException.Action` to exercise the
  exception → `StateTransactionBehavior` → `ExceptionNotification` → `ExceptionNotificationHandler`
  → toast path end-to-end.
  - Verified against the decompiled `StateTransactionBehavior.Handle`: it catches, rolls back,
    publishes `ExceptionNotification`, and returns `default` (no rethrow) — so the throw button
    shows an error toast **without crashing the circuit**.
- **Renamed** toast `AddNotification` → `AddNotificationActionSet` so the TimeWarp.State
  `ActionSetMethodSourceGenerator` emits the strongly-typed `ToastNotificationState.AddNotification`
  dispatcher (matching `AddProblemDetailsActionSet`); confirmed generated. No external references.
- **Deleted dead code:** `features/notification/` (`NotificationState` banner/list — never rendered
  or dispatched), the empty `components/pages/NotificationBanner.razor` stub, and the stray
  `global using TimeWarp.Architecture.Features.Notifications;`.
- **Structure decision (single page, kept):** did NOT split into a nav category — 5 cards is still
  scannable. Promotion trigger recorded above (~6–8 sections).
- `dev build` = 0 warnings / 0 errors. Web-spa Fixie suite: the non-Docker tests pass; the
  `AspireSpaTestApplication` tests fail **locally only** because Docker isn't running in this WSL
  session (Aspire needs a container runtime) — they pass in CI. Manual/E2E toast verification is
  still a follow-up (see note re: [[060-write-real-e2e-tests-for-sunny-day-money-paths-primary-use-cases--payment-flow]]).

## Notes
- rc.4 toast call shape (post-migration) is `INotificationService.ShowToastAsync(options => { options.Intent = ToastIntent.X; options.Title = ...; })`
  — see the three handlers in `features/toast-notification/toast-notification-state/`.
- Headless tests drop the toast handler (`spa-test-application.cs` removes the
  `INotificationHandler<ExceptionNotification>`) because there's no rendered `<FluentToastProvider>`;
  any StyleGuide demo is a manual/E2E check, not a Fixie unit test. An E2E click test could live
  alongside [[060-write-real-e2e-tests-for-sunny-day-money-paths-primary-use-cases--payment-flow]].
- Verify FluentUI APIs against the `fluent-ui-blazor` MCP (project is on `5.0.0-rc.4-26180.1`).
