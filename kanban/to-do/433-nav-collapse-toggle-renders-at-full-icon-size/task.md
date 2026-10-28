# Nav collapse toggle renders at full icon size

## Description

Task 426 (PR #588) added the nav width toggle at the bottom of the nav column, below the user
card (`components/TimeWarpPage.razor`, `twe-nav-column__toggle`, two `FluentButton`s with
`Appearance="ButtonAppearance.Subtle"`, `IconOnly="true"`, and `Icons.Regular.Size20.PanelLeftContract` /
`PanelLeftExpand`). In the running app (Steve, 2026-10-28, 1440px wide, full nav) the toggle
renders as a tiny glyph, a few pixels wide. It should be a normal 20px icon in a normal-size
icon button, like the other icon-only buttons in the app (for example in
`features/application/modals/agent-ask/AgentAsk.razor`).

The existing Playwright test (`shell-nav-playwright-tests.cs`) finds the button by its accessible
name and clicks it, so it passes with an icon too small to see.

## Requirements

- Find the root cause of the shrunken icon. Start from `TimeWarpPage.razor.css`
  (`.twe-nav-column__toggle` and the `::deep .twe-nav-toggle--*` rules) and compare with how
  the working icon-only `FluentButton`s render. Check the FluentUI v5 docs
  (fluentui-mcp) for how `IconOnly` and `IconStart` size the icon. Fix it in markup or CSS
  (`tw-blazor-css-strategy`). Do not patch FluentUI internals with script.
- The fix must hold in both states: the full column (collapse button) and the 68px rail
  (expand button, centered).
- Add a Playwright assertion that the visible toggle's icon is at least 16px wide and tall, and
  that the button is at least 24px, at 1440px (full) and 1100px (rail). It must fail on today's
  code.
- If the cause is shared with other buttons in the nav column, fix it there too and say so.

## Out of scope

- Changing the toggle's position, behavior, persistence, or icons.
- Any change outside this repository. If something here seems to need one, stop and report.

## Checklist

- [ ] Root cause recorded in Results
- [ ] Toggle icon renders at 20px in the full column and the rail
- [ ] Playwright size assertion at 1440px and 1100px (fails before, passes after)
- [ ] `dev build` 0/0; `dev test --group playwright` passes
- [ ] No client name in source, tests, or skills

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-28)

## Notes

- Steve's screenshot: the bottom of the nav column shows the "Member" user card, then a divider,
  then a tiny two-dot glyph where the toggle should be.
