# Ask side panel: dock in FluentLayout aside instead of collapsing the page

## Description

Steven (2026-10-10, screenshot from TWE-001): Ask is supposed to be a Cloudflare-style side panel
docked on the right, but it shows as the whole screen. Root cause confirmed and reproduced on the
Grok box at master `ee336d56c` (2026-10-10). Evidence is in `evidence/` beside this file.

### Root cause

- Task 292 (PR #455, merge `ee336d56c`, commit `ebfaecd`) docked Ask by wrapping the shell in a flex
  row: `TimeWarpPage.razor.css:16-39` `.twe-shell.twe-shell--ask { display: flex }` and line 25
  `::deep .twe-shell__layout { flex: 1 1 auto }`. `TimeWarpPage.razor:165-168` renders `<AgentAsk />`
  beside `<FluentLayout>` inside `<div class=@ShellClass>` (lines 58-62, 78).
- FluentUI v5.0.0 `FluentLayout` wraps `.twe-shell__layout` in `<div id="{id}-container">` with inline
  `container-type: inline-size`. That wrapper is the actual flex item. It keeps `flex: 0 1 auto`, and
  with inline-size containment its natural width is 0, so the whole app collapses to 0px. Measured at
  1280x800: layout 0x800, app bar 32px, `.twe-agent-ask` at x=0, 450x800. FluentLayout then flips to
  its mobile layout. Result: panel on the LEFT and a blank page, which looks full-screen.
- Experiment: injecting `.twe-shell--ask > div[id$='-container'] { flex: 1 1 0; min-width: 0 }` gives
  page 830px and the panel docked at x=830. This proves the cause; it is NOT the fix (it relies on
  Fluent internal ids).
- CI missed it: `ask-surface-playwright-tests.cs:241-244` only asserts panel width 450 and
  app bar + panel <= viewport, which a 0px page passes. Task 292's own proof screenshot
  `ask-panel-docked.png` shows the bug and the review missed it.

### Intended design (task 292)

Cloudflare-style right dock: 450px at 1280 wide, the page reflows beside it, no scrim. Full-screen
below 880px or when Expanded.

### Evidence (`evidence/`)

- `steven-twe001-ask-fullscreen.png`: Steven's report from TWE-001.
- `probe-open-1280.png`: master `ee336d56c`, Ask open at 1280x800 (panel left, page 0px).
- `probe-experiment-container-flex-1280.png`: same with the diagnostic container rule injected (panel
  docked right at x=830).
- `probe-dump.txt`: measured geometry and DOM of the probe.
- `ask-panel-docked.png`: task 292's own proof screenshot, already showing the bug.

## Requirements

1. **Render Ask via FluentLayout's built-in aside.** When open:
   `<FluentLayoutItem Area="LayoutArea.Aside" Width="450px"><AgentAsk /></FluentLayoutItem>`, so the
   grid (`nav content aside`, `auto 1fr auto`) reflows the page. Define how this coexists with
   TimeWarpPage's existing `Aside` parameter, and document the decision (task notes + skill/README
   where the shell is described).
2. **Remove the `.twe-shell--ask` flex wrapper rules.** `.twe-shell` stays `display: contents`.
3. **Mobile breakpoint and Expand.** Set FluentLayout `MobileBreakdownWidth` to 880. Below it, and
   when Expanded, Ask is full-screen via one rule anchored on
   `.twe-shell ::deep .fluent-layout-item[area=aside]` (`position: fixed; inset: 0`) with specificity
   above Fluent's `.fluent-layout .fluent-layout-item[area="aside"]`. Follow tw-blazor-css-strategy:
   no `<style>`, no `style=`, no targeting Fluent internal ids. Do NOT use the DialogService drawer
   (it overlays instead of reflowing and is unreachable by `.razor.css`).
4. **Icon buttons for the panel.** New / Expand (Collapse) / Close in the header and Copy / Thumbs up /
   Thumbs down on messages as FluentUI icon buttons with tooltips and aria-labels (nothing wraps under
   the title at 450px). Styled suggestion cards. Edit-behavior as a segmented control.
5. **Playwright tests (real WASM)** in the web-spa Playwright tests:
   - 1280x800, Ask open: panel x ≈ 830 and its right edge = viewport; page/layout width > 800;
     FluentLayout has no `mobile` attribute.
   - Closed: layout is full width.
   - Below 880 wide, and when Expanded: panel is full-screen.
   - Screenshots of each state.
   - Fix the weak assertion at `ask-surface-playwright-tests.cs:241-244`.
6. **Acceptance:** full ganda walk (implement, review, audit, done-move, PR), CI green, PR includes the
   screenshots. Merge only via `ganda pr merge` after Steven approves.

## Checklist

- [ ] Ask rendered in `FluentLayoutItem Area="LayoutArea.Aside" Width="450px"` when open
- [ ] Coexistence with TimeWarpPage `Aside` parameter decided and documented
- [ ] `.twe-shell--ask` flex wrapper rules removed; `.twe-shell` stays `display: contents`
- [ ] `MobileBreakdownWidth` = 880; anchored full-screen rule for narrow + Expanded
- [ ] No `<style>`, no `style=`, no Fluent internal id selectors (tw-blazor-css-strategy)
- [ ] Header icon buttons (New, Expand/Collapse, Close) with tooltips + aria-labels
- [ ] Message icon buttons (Copy, Thumbs up, Thumbs down) with tooltips + aria-labels
- [ ] Styled suggestion cards; edit-behavior segmented control
- [ ] Playwright: 1280 open (x ≈ 830, right edge = viewport, layout > 800, no `mobile`)
- [ ] Playwright: closed (layout full width)
- [ ] Playwright: < 880 and Expanded (full-screen)
- [ ] Weak assertion at `ask-surface-playwright-tests.cs:241-244` replaced
- [ ] Screenshots of each state in the PR
- [ ] Full ganda walk (implement, review, audit, done-move, PR); CI green

## Session

- Created: 3183331 (2026-10-10, filed at Steven's request after root-cause investigation)

## Notes

- Don't run the app on TWE-001; browser proof comes from Playwright runs (box/CI).
- Merge only via `ganda pr merge <pr> --task-id <id>` after Steven approves.
