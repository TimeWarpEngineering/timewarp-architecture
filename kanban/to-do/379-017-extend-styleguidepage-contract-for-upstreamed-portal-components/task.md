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

## Session

- Created: ses_ebc1f6587ffeC5oxDhO3SRDeMu (2026-10-16)
- Implementation: implementer-grok (2026-10-24)

## Notes

- Sibling ports (379-001 through 379-016) each say the StyleGuidePage contract is 379-017.
- Component render tests stay where those ports put them. This task does not re-test a leaf in
  isolation.
- Generated copy still carries no client name.

## Goal (implementer restatement)

The upstreamed parts are already on `StyleGuidePage`. This task adds the page contract those
ports were not allowed to extend.

A static `HtmlRenderer` render of the page shows one h1, the description and actions, and the
cards in page order: Brand, Color Tokens, Type Scale, Forms, the loading Section, Section
Header, Filter Bar, Tabs, Segmented Control, Avatars, User card, Sign-in, Empty States,
Drawers, KPI Cards, Chart frame, Trend / SLA / class pills, Integration Cards, Grouped Table,
Count Chip, Confirmations, Buttons, Status Badges, Role badges, Role cards, Permission grid,
Dev note, Message bars, and Notifications. The render also shows the sample facts each port
named: the checked-in brand, the form value, the section header, the filter and switcher
samples, the user-card link, the sign-in mode, the empty states, the closed drawer opener, the
KPI tiles, the chart frame and pills, the integration rows, the grouped table and count chips,
the confirm button, the danger outline, the Lost badge, the role samples, the permission grid,
the dev note, and the notification buttons.

The page stores a choice only on the two form fields. Nothing else uses `@bind` or
`ValueChanged`. Opening and closing the sample drawer go through `ApplicationState`. The
confirm button dispatches `StyleGuideState.ConfirmSample` and does not keep a local flag.

Acceptance: `./bin/dev build` is clean, the new contract test passes, `./bin/dev template-smoke`
passes, `ganda repo audit` passes, and the product diff does not introduce the source product's
name.

## Results

`StyleGuidePage_Should_` is the page contract the sibling ports left for this task.
`Show_Every_Upstreamed_Part_In_Page_Order` renders the page through the same static host as
`TimeWarpPage_Should_` and walks card titles and sample markers in page order.
`Keep_Stored_Choices_On_The_Form_Fields_Only` reads `StyleGuidePage.razor` and requires the two
form `@bind-Value` uses, no `ValueChanged`, the `ApplicationState` drawer calls, and
`ConfirmSampleAsync`. The page code-behind Design note names that contract. The absent-name
check composes its token so the test file does not spell it.

### How to validate

Smoke:

- `./bin/dev build` — Build succeeded. 0 Warning(s). 0 Error(s). Time Elapsed 00:00:05.96.
- `dotnet run -c Release --project tests/container-apps/web/web-spa-integration-tests -- --filter-class StyleGuidePage_Should_` — Passed. total 2, failed 0, succeeded 2, duration 399ms.
- `./bin/dev template-smoke` — exit 0 in 266s. SmokeDefault OK. Generated app build 0 warnings, 0 errors, 00:00:11.96. Jaribu aggregators: web 405/405, api 9/9, timewarp-testing 14/14. Template smoke SUCCEEDED.
- `ganda repo audit` — Passed 33, Failed 0, Skipped 1. Repository passes all audit checks.
- `git diff origin/master -- source tests | rg -i crunchit` — no match. The new test file composes the token, so it is not a match either.

Expect:

- One h1, "Style Guide", plus the description and the action row.
- Card titles in page order: Brand, Color Tokens, Type Scale, Forms, Section, Section Header, Filter Bar, Tabs, Segmented Control, Avatars, User card, Sign-in, Empty States, Drawers, KPI Cards, Chart frame, Trend / SLA / class pills, Integration Cards, Grouped Table, Count Chip, Confirmations, Buttons (FluentUI, themed), Status Badges, Role badges, Role cards, Permission grid, Dev note, Message bars (FluentUI), Notifications (message bars).
- Sample facts in that same order: TimeWarp.Architecture, seed `#55409c`, Harmonized, form value Ada Lovelace on `StyleGuideFormField`, Reports and "Summary for the current period.", the filter and the two switchers, the `/Profile` user card, Passkey and "Staff only.", the three empty states, `DrawerOpen`, the KPI figures, the chart series, the pills, the integration rows, Alpha / In progress, `ConfirmSample`, `DangerOutline`, Lost, the role samples, the permission grid, the dev note, and the five notification buttons.
- The closed drawer does not render "Sample drawer". The role-card slice shows Owner checked (`aria-checked="true"`, `tabindex="0"`) and Admin and Staff unchecked (`tabindex="-1"`).
- The razor has exactly two `@bind-Value` attributes, `FormDemoName` and `FormDemoEmail`. It has no `ValueChanged`. The drawer calls `ApplicationState.SetActiveModal(StyleGuideDrawerId)` and `ApplicationState.CloseModal()`. Confirm goes through `ConfirmSampleAsync`.

A live browser pass was not done. `HtmlRenderer` does not run `OnAfterRenderAsync`, so the drawer script stays dormant and the closed title stays absent. The static render covers the catalog markup.
