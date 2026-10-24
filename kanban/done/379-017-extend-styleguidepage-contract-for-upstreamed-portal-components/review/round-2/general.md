# Round 2 — general
**Date:** 2026-10-24
**Scope reviewed:** `aa69106be..HEAD` (fix commit e9374c26d plus kanban commit fd31d8a59).
Product paths: `tests/.../infrastructure/static-render-host.cs` (new),
`tests/.../features/style-guide/style-guide-page-contract-tests.cs`,
`tests/.../components/time-warp-page-render-tests.cs`.

## Summary

The fix pass moves the static-render service host, the headless JS runtime, the fixed
authentication state, and the `Title` and `CountOpen` helpers into one `StaticRenderHost`. Both
page contracts now use it, which closes M2. The style-guide source scan now counts every `@bind`,
bans `Changed="`, ties the `ConfirmSample` tag to `OnClick="ConfirmSampleAsync"`, and checks that
the method body calls `NoSubStyleGuideState.ConfirmSample()` (M1). Each role card is sliced and
checked on its own, so Owner is the only checked card (M3). Expect now uses the rendered title
(M4). No new defects were found in the delta.

## Verification

- M1: `StyleGuidePage.razor:76` is `private Task ConfirmSampleAsync() => NoSubStyleGuideState.ConfirmSample();`.
  That is the first `ConfirmSampleAsync()` in the file, so the substring check reads the right
  statement. Line 378 is the only `data-qa="ConfirmSample"` tag, and it carries
  `OnClick="ConfirmSampleAsync"`. Lines 136 and 139 are the only two `@bind` occurrences.
- M2: neither test class still declares `CreateServices`, `HeadlessJsRuntime`,
  `FixedAuthenticationStateProvider`, `Title`, or `CountOpen`. `global-usings.cs:25` brings the
  `Infrastructure` namespace in. `TimeWarpPage_Should_` now also gets `TimeProvider.System`. That
  registration is harmless.
- M3: `RoleCard` finds a card by its `twe-role-card__title` text and slices from `<button` to
  `</button>`. The test requires exactly one `aria-checked="true"` and two `aria-checked="false"`,
  checks each card's state and `tabindex`, and requires the checked state to come before `>Admin<`.
- M4: `task.md` Expect reads "Trend, SLA, and class pills".
- Re-ran: `./bin/dev build` gave 0 warnings and 0 errors. `--filter-class StyleGuidePage_Should_`
  passed 2/2. `--filter-class TimeWarpPage_Should_` passed 14/14.

## Goal alignment

`## Goal (implementer restatement)` matches the task Description and the round-1 fix brief. No
misread.

## Issues

None.
