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

- [x] Root cause recorded in Results
- [x] Toggle icon renders at 20px in the full column and the rail
- [x] Playwright size assertion at 1440px and 1100px (fails before, passes after)
- [x] `dev build` 0/0; `dev test --group playwright` passes
- [x] No client name in source, tests, or skills

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-28)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session 01a183c5-e0e5-7011-b1f1-f6638b017ff1, max-turns 500, Blocked — 2026-10-28T18:29:18Z

## Notes

- Steve's screenshot: the bottom of the nav column shows the "Member" user card, then a divider,
  then a tiny two-dot glyph where the toggle should be.

## Blocked

2026-10-28: (no reason given)

## Results

- **Root cause (recorded).** `wwwroot/css/app.css` global reset `img, svg { display: block;
  max-width: 100%; }` collapses the slotted svg inside `<fluent-button icon-only>`. The svg is
  a flex item of the component's shadow `span.content` (`display: inherit`), whose containing
  block resolves from the icon itself, so `max-width: 100%` clamps the icon to 6px while the
  32px button frame stays intact (measured live: button 32x32, svg 6x20). FluentButton's own
  shadow CSS sizes the svg correctly (`::slotted(svg) { font-size: 20px; height: 20px;
  width: 20px; }`). Not specific to `.twe-nav-toggle`: a class-stripped clone of the button
  still rendered 6px. The repo already carried four per-slice patches of this same defect
  (`.twe-agent-ask__header-actions`, `.twe-agent-ask__answer-bar`, `.twe-ask-edit-mode`,
  `.twe-ask-privacy` — all `fluent-button > svg { max-width: none; }`); the toggle is the
  fifth instance.
- **Fix.** One global accommodation in `app.css` next to the reset it qualifies:
  `fluent-button[icon-only] > svg { max-width: none; }` (labeled Exception B). This covers the
  nav toggle AND every other present/future icon-only button; the four Ask patches become
  redundant but harmless and are left in place. No markup change, no script, no FluentUI
  internals touched — `tw-blazor-css-strategy` compliant.
- **Fail-first proof.** New `Toggle_Icon_Should_Render_At_Full_Size` (shell-nav suite)
  asserts button >= 24px and icon >= 16px at 1440px (full: Collapse) and 1100px (rail:
  Expand). Ran before the fix: failed with `iconWidth should be >= 16 but was 6`. Ran after:
  green, and the original `Toggle_Should_Collapse_Expand_And_Remember` still passes (both
  states hold; the rail keeps the expand button centered).

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-28)
- Implemented: ses_e7d6c83cfffebh1UD5myX7NDeE (2026-10-28). The implementer-grok oracle walk
  returned Blocked with no detail after one turn (prompt conflict in its own log); implementation
  taken over manually in the claimed worktree. Gates: dev build 0/0, dev test -g playwright
  33/33 pass, ganda repo audit clean, dev template-smoke SUCCEEDED.
