# Round 1 — general
**Date:** 2026-10-24
**Scope reviewed:** commit e97d8530e (new `StyleGuidePage_Should_` contract test and the page Design note)

## Summary

The commit adds `StyleGuidePage_Should_`. One test renders the page through `HtmlRenderer`
and checks the card titles and the sample markers, in page order. The other reads the razor source
and checks the stored-choice rule. Re-run here: `dotnet run -c Release --project
tests/container-apps/web/web-spa-integration-tests -- --filter-class StyleGuidePage_Should_` passed
2/2 (386ms). The render contract matches the page and the restated goal, and the goal restatement
matches the task, so there is no misread. Risk is low because only test code changed. The gaps: the
inert-sample scan is narrower than the rule it claims to enforce, and the render host is a copy of
the `TimeWarpPage_Should_` host rather than shared with it.

## Issues

### Issue 1 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/style-guide/style-guide-page-contract-tests.cs:160
- Description: `Keep_Stored_Choices_On_The_Form_Fields_Only` is meant to enforce the task's
  inert-sample rule. The restatement says "Nothing else uses `@bind` or `ValueChanged`". The scan
  only counts `@bind-Value` and bans the literal `ValueChanged`. Several edits that store a choice
  would still pass:
  - `@bind="…"`, `@bind-Checked`, `@bind-SelectedOption`, or `@bind-Open` on any sample.
  - A `SelectedChanged=` or `CheckedChanged=` callback that writes a field.
  - Replacing the confirm sample's dispatch with a local flag, because the test only needs the
    string `ConfirmSampleAsync` somewhere in the file. It does not tie that string to the
    `ConfirmSample` button.
- Suggestion: Count every `@bind` occurrence and expect 2. Ban any `Changed="` attribute (or
  `Changed=` in general), not just `ValueChanged`. Assert that the `data-qa="ConfirmSample"` button's
  `OnClick` is `ConfirmSampleAsync`, and that `ConfirmSampleAsync` dispatches
  `NoSubStyleGuideState.ConfirmSample()`.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/style-guide/style-guide-page-contract-tests.cs:328
- Description: The Design note says the test uses "the same shell host as TimeWarpPage_Should_".
  In fact it keeps its own copy of that host. `CreateServices`, `HeadlessJsRuntime`,
  `FixedAuthenticationStateProvider`, `Title`, and `CountOpen` are copied line for line from
  `components/time-warp-page-render-tests.cs:437-560`. The only intended difference is the added
  `TimeProvider.System`. When a shell dependency is added (the 379 ports added BrandOptions,
  SignInOptions, and WebMcp services one at a time), both copies must change. Nothing checks
  that they agree. The repo rule is: when two things must agree, generate one from the other or add
  a build check.
- Suggestion: Extract a static-render host (service registration, headless JS runtime, fixed
  auth provider, and the h1 / open-tag helpers) into `infrastructure/`. Both
  `TimeWarpPage_Should_` and `StyleGuidePage_Should_` should use it. Registering `TimeProvider`
  there is harmless for the shell tests.
- Status: open

### Issue 3 — Severity: nit
- File: tests/container-apps/web/web-spa-integration-tests/features/style-guide/style-guide-page-contract-tests.cs:151
- Description: The role-card slice proves only that the strings `aria-checked="true"`,
  `tabindex="0"`, and `tabindex="-1"` appear somewhere in it, along with the three names. Results
  says more than that: Owner is the checked card with `tabindex="0"`, and Admin and Staff are
  unchecked with `tabindex="-1"`. The test would still pass if Admin were the checked card, or if
  two cards were checked.
- Suggestion: Split the slice per card (Owner…Admin, Admin…Staff, Staff…grid). Assert
  `aria-checked="true"` plus `tabindex="0"` on Owner, and `aria-checked="false"` plus
  `tabindex="-1"` on Admin and on Staff. Or count `aria-checked="true"` as exactly 1, located
  before "Admin".
- Status: open

### Issue 4 — Severity: nit
- File: kanban/to-do/379-017-extend-styleguidepage-contract-for-upstreamed-portal-components/task.md (Results → Expect)
- Description: The Results "Expect" list, like the goal restatement, names the card "Trend / SLA /
  class pills". The page and the test both use "Trend, SLA, and class pills". Someone validating by
  hand from Expect would search for a title that does not exist.
- Suggestion: Use the rendered title "Trend, SLA, and class pills" in Results → Expect.
- Status: open
