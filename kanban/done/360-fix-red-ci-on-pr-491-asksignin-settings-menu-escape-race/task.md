# Fix red CI on PR 491: AskSignIn settings-menu Escape race

## Why (2026-10-14)

PR #491 (feature/ask-composer -> master, head 54c8b6b, tasks 355/356/357) is red. Both `ci` runs
fail the same test: `AskSignIn_Given_Wasm.SignIn_Should_ReprobeAsk_And_NeverShowA401AsNotConfigured`
(`tests/container-apps/web/web-spa-playwright-tests/ask-sign-in-playwright-tests.cs`, the task 356
block around line 143-149). It times out waiting for `[data-qa=AskAgentAccessExpiry]` to be hidden
after pressing Escape; the settings menu stays open. Logs:
https://github.com/TimeWarpEngineering/timewarp-architecture/actions/runs/38506505769/job/115669162164

Task PRs into the feature branch get no CI, so this test first ran in CI on #491.

## Root cause (verified locally on 54c8b6b, before any change)

Reproduced locally: 1 failure in 6 runs normally, and on the first instrumented run. An in-page
event log of the failing run shows:

```
8  keydown Enter  -> fluent-menu toggleMenu() -> showPopover (beforetoggle closed->open)
16 popover is open, menu content visible, aria-expanded still "false"
24 keydown Escape, target=FLUENT-BUTTON; defaultPrevented=true at bubble, popover stays open
33 toggle closed->open fires -> aria-expanded="true", _open=true
```

`fluent-menu` (Microsoft.FluentUI.AspNetCore.Components 5.0.0 web component) sets its `_open`
flag and the trigger's `aria-expanded` only in the async `toggle` event handler. Its
`menuKeydownHandler` always calls `preventDefault()` on Escape but only closes when `_open` is
true. An Escape that lands between `showPopover()` and the `toggle` event is swallowed: the
library does not close the menu and its `preventDefault` also blocks the browser's native popover
Escape dismissal. The menu then settles open.

The test waits for the expiry text to be *visible* (true as soon as the popover shows) and presses
Escape immediately, so on a slower CI runner it hits that gap every time. A person cannot press
Escape within that window, and with the menu settled (aria-expanded="true") Escape closes it and
returns focus to the button. The app behaves as specified. The test's synchronization is wrong.
The sibling tests in `ask-surface-playwright-tests.cs` (edit-mode pill and the settings menu)
already wait for `aria-expanded="true"` before Escape and pass; the sign-in test is the outlier.

## Requirements

- In the AskSignIn task 356 block, after opening the menu with Enter, wait for the trigger
  `[data-qa=AskSettings]` to report `aria-expanded="true"` before pressing Escape, and assert
  `aria-expanded="false"` after it, then keep the existing wait for the expiry to be hidden. Use
  the same approach as the `ask-surface-playwright-tests.cs` menu tests. Add a short comment naming
  the fluent-menu toggle gap.
- No app change, no retries or sleeps, no skipping or disabling the test. The Escape-closes-menu
  assertion stays.
- Proof: run `web-spa-playwright-tests --filter-class AskSignIn_Given_Wasm` repeatedly (at least
  10 runs) before (show the failure) and after the change (all pass) and record the output in the
  Results section and the PR body.

## Checklist

- [x] Test waits for the menu's open state before Escape and asserts it closed
- [x] Repro before / pass after recorded (repeated runs)
- [x] Gates: `dev build` 0/0, the web-spa Playwright AskSignIn and AskSurface classes, `ganda repo audit`

## Resolution (2026-10-14, cockpit)

Already fixed; closed without new work. The cockpit's subagent fixed this race on
`feature/ask-composer` in commit **`ef27f338f`** ("fix(ask): Escape closes the Ask settings menu
(356 follow-up)"):
- **Root cause:** the test pressed Escape in the gap where fluent-menu had shown the popover but
  not yet set `aria-expanded=true`, so the keypress was ignored. The product was fine; Escape closes
  the menu once it is armed.
- **Fix:** the test waits for `aria-expanded=true`, presses Escape, then expects
  `aria-expanded=false` before checking that the expiry line is hidden.
- **Proof:** 10/10 in a loop, and 10/10 under CPU load. PR #491 CI went green on `ef27f338f`, and
  #491 merged to master (`ae03c2774`).
