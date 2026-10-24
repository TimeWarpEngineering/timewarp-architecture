# Extend StyleGuidePage contract for upstreamed portal components

## Description

Each 379 port added its own style-guide card and its own component render test, and left the
page-level contract alone. This task pins that page. A static render of `StyleGuidePage` must
show every upstreamed part, in the page's order, with the sample text and `data-qa` markers
those ports named. The form fields are the only stored values. Filter, tabs, segmented control,
role cards, and the other samples do not store a choice. The drawer follows `ApplicationState`.
The confirm sample is the only style-guide action.

## Checklist

- [x] Restate the goal before product edits
- [x] Render contract for the composed style-guide page
- [x] Inert-sample rule: only the form fields bind a value
- [x] `./bin/dev build`, the contract test, `./bin/dev template-smoke`, and `ganda repo audit`
- [x] Results, commit, and push
- [x] Review fix: every `@bind`, no `Changed="`, confirm wired to `ConfirmSample` (M1)
- [x] Review fix: one static-render host for both page contracts (M2)
- [x] Review fix: Owner is the only checked role card (M3)
- [x] Review fix: Expect uses the rendered pill-card title (M4)

## Session

- Created: ses_ebc1f6587ffeC5oxDhO3SRDeMu (2026-10-16)
- Implementation: implementer-grok (2026-10-24)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-24T02:27:00Z
- Review round 1: review oracle (Claude Code, claude-opus-5-5, headless), effort 2, roster general, session id not exposed — 2026-10-24
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-24T02:29:49Z
- Fix pass: implement-fix-2 (grok, model grok-4.7), session not reported — 2026-10-24

## Review

- Round 1: Blocked — open M1, M2, M3, M4
- Fix pass: M1–M4 addressed on this branch. Round 2 has not re-verified them.

## Notes

- Sibling ports (379-001 through 379-016) each say the StyleGuidePage contract is 379-017.
- Component render tests stay where those ports put them. This task does not re-test a leaf in
  isolation.
- Generated copy still carries no client name.

## Goal (implementer restatement)

The upstreamed parts are already on `StyleGuidePage`. This task pins that page, and the review
fix makes the pin hard to satisfy by accident.

A static `HtmlRenderer` render goes through one shared host, `StaticRenderHost`, the same host
`TimeWarpPage_Should_` uses. The render shows one h1, the description and actions, and the
cards in page order: Brand, Color Tokens, Type Scale, Forms, the loading Section, Section
Header, Filter Bar, Tabs, Segmented Control, Avatars, User card, Sign-in, Empty States,
Drawers, KPI Cards, Chart frame, Trend, SLA, and class pills, Integration Cards, Grouped Table,
Count Chip, Confirmations, Buttons, Status Badges, Role badges, Role cards, Permission grid,
Dev note, Message bars, and Notifications. The render also shows the sample facts each port
named: the checked-in brand, the form value, the section header, the filter and switcher
samples, the user-card link, the sign-in mode, the empty states, the closed drawer opener, the
KPI tiles, the chart frame and pills, the integration rows, the grouped table and count chips,
the confirm button, the danger outline, the Lost badge, the role samples, the permission grid,
the dev note, and the notification buttons.

Inside the role-card region, Owner is the checked card (`aria-checked="true"`, `tabindex="0"`).
Admin and Staff are unchecked (`aria-checked="false"`, `tabindex="-1"`). Exactly one card is
checked.

The page stores a choice only on the two form fields. `StyleGuidePage.razor` contains exactly
two `@bind` occurrences, `FormDemoName` and `FormDemoEmail`, and no `Changed="` callback. The
`data-qa="ConfirmSample"` button uses `OnClick="ConfirmSampleAsync"`, and that method calls
`NoSubStyleGuideState.ConfirmSample()`. Opening and closing the sample drawer go through
`ApplicationState`.

Acceptance: `./bin/dev build` is clean, `StyleGuidePage_Should_` and `TimeWarpPage_Should_`
pass, `ganda repo audit` passes, and the product diff does not introduce the source product's
name. Expect names the pill card "Trend, SLA, and class pills".

## Results

`StyleGuidePage_Should_` is the page contract the sibling ports left for this task.
`Show_Every_Upstreamed_Part_In_Page_Order` renders the page through `StaticRenderHost`, the
same host `TimeWarpPage_Should_` uses, and walks card titles and sample markers in page order.
The role-card region has exactly one `aria-checked="true"`, on the Owner card (`tabindex="0"`),
before Admin. Admin and Staff are `aria-checked="false"` and `tabindex="-1"`.
`Keep_Stored_Choices_On_The_Form_Fields_Only` reads `StyleGuidePage.razor` and requires exactly
two `@bind` occurrences (`FormDemoName` and `FormDemoEmail`), no `Changed="` callback, the
`ApplicationState` drawer calls, `data-qa="ConfirmSample"` on the button whose `OnClick` is
`ConfirmSampleAsync`, and that method calling `NoSubStyleGuideState.ConfirmSample()`. The page
code-behind Design note names that contract. The absent-name check composes its token so the
test file does not spell it.

Review round 1 findings M1–M4 are addressed on this branch. Round 2 has not re-verified them.

### How to validate

Smoke:

- `./bin/dev build` — Build succeeded. 0 Warning(s). 0 Error(s). Time Elapsed 00:00:05.79.
- `dotnet run -c Release --project tests/container-apps/web/web-spa-integration-tests -- --filter-class StyleGuidePage_Should_` — Passed. total 2, failed 0, succeeded 2, duration 381ms.
- `dotnet run -c Release --project tests/container-apps/web/web-spa-integration-tests -- --filter-class TimeWarpPage_Should_` — Passed. total 14, failed 0, succeeded 14, duration 484ms.
- `./bin/dev template-smoke` — prior run on this branch, exit 0 in 266s. SmokeDefault OK. Generated app build 0 warnings, 0 errors, 00:00:11.96. Jaribu aggregators: web 405/405, api 9/9, timewarp-testing 14/14. Template smoke SUCCEEDED. This fix pass left the page and the template unchanged, so template-smoke was not re-run.
- `ganda repo audit` — Passed 33, Failed 0, Skipped 1. Repository passes all audit checks.
- `git diff origin/master -- source tests | rg -i crunchit` — no match. The new test file composes the token, so it is not a match either.

Expect:

- One h1, "Style Guide", plus the description and the action row.
- Card titles in page order: Brand, Color Tokens, Type Scale, Forms, Section, Section Header, Filter Bar, Tabs, Segmented Control, Avatars, User card, Sign-in, Empty States, Drawers, KPI Cards, Chart frame, Trend, SLA, and class pills, Integration Cards, Grouped Table, Count Chip, Confirmations, Buttons (FluentUI, themed), Status Badges, Role badges, Role cards, Permission grid, Dev note, Message bars (FluentUI), Notifications (message bars).
- Sample facts in that same order: TimeWarp.Architecture, seed `#55409c`, Harmonized, form value Ada Lovelace on `StyleGuideFormField`, Reports and "Summary for the current period.", the filter and the two switchers, the `/Profile` user card, Passkey and "Staff only.", the three empty states, `DrawerOpen`, the KPI figures, the chart series, the pills, the integration rows, Alpha / In progress, `ConfirmSample`, `DangerOutline`, Lost, the role samples, the permission grid, the dev note, and the five notification buttons.
- The closed drawer leaves "Sample drawer" out of the render. The role-card slice has exactly one `aria-checked="true"`, on Owner, with `tabindex="0"`, and that checked state appears before Admin. Admin and Staff are `aria-checked="false"` with `tabindex="-1"`.
- The razor has exactly two `@bind` occurrences, `FormDemoName` and `FormDemoEmail`. It has no `Changed="` and no `ValueChanged`. The drawer calls `ApplicationState.SetActiveModal(StyleGuideDrawerId)` and `ApplicationState.CloseModal()`. The `data-qa="ConfirmSample"` tag uses `OnClick="ConfirmSampleAsync"`, and `ConfirmSampleAsync()` calls `NoSubStyleGuideState.ConfirmSample()`.

A live browser pass was not done. `HtmlRenderer` does not run `OnAfterRenderAsync`, so the drawer script stays dormant and the closed title stays absent. The static render covers the catalog markup.

### Review

- Round 1 (effort 2, roster general): 0 bug, 2 suggestion, 2 nit. M1–M4 were open.
  The reviewer re-ran `--filter-class StyleGuidePage_Should_`: 2/2 passed.
- Disposition: fixed on this branch (implement-fix-2, 2026-10-24). Round 2 has not re-verified them.
  - M1: the source scan counts every `@bind`, bans `Changed="`, and ties `data-qa="ConfirmSample"` to `OnClick="ConfirmSampleAsync"` and that method to `NoSubStyleGuideState.ConfirmSample()`.
  - M2: `StaticRenderHost` in `infrastructure/static-render-host.cs` is the service host, headless JS runtime, fixed auth state, and h1 helpers for both page contracts.
  - M3: each role card is sliced. Owner is the one checked card. Admin and Staff are unchecked.
  - M4: Expect uses the rendered title "Trend, SLA, and class pills".
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.
