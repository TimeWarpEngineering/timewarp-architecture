# Round 2 — merged findings
**Date:** 2026-10-30
**Sources:** general (sub-agent report in `general.md`), review oracle verification (gates re-run, claims re-checked against the tree)
**Scope:** fix-pass delta, commit 1a7797596 (round-1 base ecfc574f0 → HEAD): 8 product/test files, 171 insertions, 80 deletions. The whole-task review is round 1; this round re-verifies M1-M4 and scans the delta for new defects.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 2 | 0 |

Zero open. No new finding was raised on the delta.

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/analyzers/timewarp-architecture-convention-analyzers/direct-fluent-message-bar-analyzer.cs:17-25 (Design), :89-94 (`ResolveHostNamespace`), :159-168 (`IsHostType`); tests/analyzers/timewarp-architecture-analyzers-tests/direct-fluent-message-bar-analyzer-tests.cs
- Description: (round 1) host exemption was a path-suffix match that exempted any `…/components/MessageBar.razor`, while the Design region said otherwise.
- Source: general
- Disposition notes: Fixed by option (a). The exemption is now by type identity: the outermost containing type of the `OpenComponent<FluentMessageBar>` call is named `MessageBar` or `MessageBars` and its namespace equals `{build_property.RootNamespace}.Components`; the path is not consulted, and no `RootNamespace` means no exemption. Verified by the review oracle: both hosts declare `@namespace TimeWarp.Architecture.Components`; `RootNamespace` is `TimeWarp.Architecture` from `source/container-apps/Directory.Build.props:6` (not overridden in `web-spa.csproj`), and the template `sourceName` rewrite moves that value and the `@namespace` lines together. Tests replaced: feature `components/` folder with the default namespace flags, feature `@namespace` flags, non-host type in a host-named file flags, renamed root namespace exempts, host code-behind `.cs` exempts, no `RootNamespace` flags; the `#line`-mapped path test was dropped because the path no longer decides anything, and `ReportLocation` keeps its own mapped-parameter test. Design regions in the analyzer, the test file, and `MessageBar.razor` now describe the code. Real-tree proof: `dev build` 0/0 (the control and host are exempt at the real namespace) and `dev template-smoke` succeeded (renamed root namespace, generated app builds with the analyzer on). Analyzer `MessageBar` class 16/16.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/notification/notification-state/notification-state.cs:157-166; tests/container-apps/web/web-spa-integration-tests/features/notification/message-bar-copy-tests.cs:81-112, :205-213
- Description: (round 1) the rewritten `RemoveExpired` `copiedRowExpired` branch had no test reaching it.
- Source: general
- Disposition notes: Fixed. `Clear_Confirmation_When_The_Copied_Row_Expires_First` copies a Success row backdated (internal `AddMessage` with `now = UtcNow - SuccessAutoDismissInterval + 1s`, reachable through `InternalsVisibleTo web-spa-integration-tests`) so it auto-dismisses before `CopiedUntil`, then asserts the row is gone and `CopiedMessageId` is null; only the `copiedRowExpired` clause can clear it there, so deleting the clause fails this test. `Keep_Page_Bar_Confirmation_When_An_Unrelated_Row_Expires` copies `bar-1`, expires an unrelated Success row, and asserts `CopiedMessageId` stays `bar-1`; the old "any expiry clears" behavior would fail it. Each test guards `AutoDismissAt < CopiedUntil` so the ordering cannot drift. Copy classes 16/16.

### M3 — Severity: nit — Status: fixed
- File: source/analyzers/timewarp-architecture-convention-analyzers/AnalyzerReleases.Unshipped.md:5-6
- Description: (round 1) reservation note named a kanban column and task id in packed template content.
- Source: general
- Disposition notes: Fixed. Reads "TWA0036 is reserved for the ISO date rendering rule." No task id or board state remains; `git grep` over the tree outside `kanban/` finds no "to-do task 452".

### M4 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/design-system/samples/NotificationsSample.razor:13; samples/MessageBarsSample.razor:4; features/design-system/design-system-parts.cs:438
- Description: (round 1) stale "records a FluentMessageBar" comment and "(FluentUI)" sample/part title.
- Source: general
- Disposition notes: Fixed. The comment says "records a NotificationState row"; the card and part titles are "Message bars". Nothing under `tests/` or `skills/` asserts the old strings (`git grep`); `DesignSystemPage_Should_` 6/6 after the rename. The new title does not collide with "Notifications (message bars)".

## New findings on the delta

None. The general reviewer and the review oracle each read the full delta (analyzer, analyzer tests, `MessageBar.razor` comment, three design-system wording changes, copy tests) and found no new defect. Conventions hold: Purpose/Design regions accurate (TWA0004), 2-space indentation, Jaribu/Shouldly only, no new `.cs` files.

## Duplicates / conflicts

- Single reviewer; nothing collapsed. The review oracle re-verified every "fixed" claim against the tree and re-ran the gates (see `review-framework.md` round 2 table) before merging; the sub-agent ran no builds.

## Next

0 open → `review/disposition.md` (clean). No further fix pass.
