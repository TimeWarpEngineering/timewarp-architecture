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

- [x] Ask rendered in `FluentLayoutItem Area="LayoutArea.Aside" Width="450px"` when open
- [x] Coexistence with TimeWarpPage `Aside` parameter decided and documented
- [x] `.twe-shell--ask` flex wrapper rules removed; `.twe-shell` stays `display: contents`
- [x] `MobileBreakdownWidth` = 880; anchored full-screen rule for narrow + Expanded
- [x] No `<style>`, no `style=`, no Fluent internal id selectors (tw-blazor-css-strategy)
- [x] Header icon buttons (New, Expand/Collapse, Close) with tooltips + aria-labels
- [x] Message icon buttons (Copy, Thumbs up, Thumbs down) with tooltips + aria-labels
- [x] Styled suggestion cards; edit-behavior segmented control
- [x] Playwright: 1280 open (x ≈ 830, right edge = viewport, layout > 800, no `mobile`)
- [x] Playwright: closed (layout full width)
- [x] Playwright: < 880 and Expanded (full-screen)
- [x] Weak assertion at `ask-surface-playwright-tests.cs:241-244` replaced
- [x] Screenshots of each state committed beside this task for the PR
- [ ] Full ganda walk (implement, review, audit, done-move, PR); CI green

## Session

- Created: 3183331 (2026-10-10, filed at Steven's request after root-cause investigation)
- Implementation: 01a123c6-0b7a-7a02-b748-b976f5f7bdc1 (2026-10-10, Grok 4.7 implementer)

## Notes

- Don't run the app on TWE-001; browser proof comes from Playwright runs (box/CI).
- Merge only via `ganda pr merge <pr> --task-id <id>` after Steven approves.
- Aside coexistence: FluentLayout has one aside column (`nav content aside` / `auto 1fr auto`).
  While Ask is open it owns that column at 450px and the page `Aside` RenderFragment is not
  rendered. The page aside (300px) returns when Ask closes. Two aside items would share one
  grid area and paint on top of each other. No page passes `Aside` today. Recorded on
  `TimeWarpPage`, in `skills/tw-blazor-layout` (the shell reference), and in `skills/tw-blazor`
  (the Ask sentence). There is no README that describes the shell.
- Full-screen is one treatment written as two rulesets. A media query cannot share a selector
  list with a class that lives outside it, so `(max-width: 879px)` and
  `.twe-shell--ask-expanded` each carry the same `position: fixed; inset: 0` declarations.
  The anchor is `.twe-shell.twe-shell--ask ::deep .fluent-layout-item[area=aside]`. The shell
  class is only that hook. `.twe-shell` stays `display: contents`. `z-index: 200` sits above
  Fluent chrome (85) and below the shell modal and `#blazor-error-ui` (1000). `width` and
  `height` are `auto !important` because `FluentLayoutItem` writes an inline width.
- `twe-shell--ask` must not set `display`. The old flex row made Fluent's `{id}-container`
  the flex item. That wrapper keeps `flex: 0 1 auto` and `container-type: inline-size`, so
  its natural width was 0 and the page collapsed.

## Results

Ask is the FluentLayout aside while it is open, so the page reflows beside a 450px panel
instead of collapsing to 0px. `.twe-shell` stays `display: contents`. The flex dock rules are
gone. Below 880px, and when expanded, the aside covers the viewport.

Header actions are icon-only FluentButtons (New conversation, Expand/Collapse, Close). Message
actions are Copy, Thumbs up, and Thumbs down, each with `Title`, `Tooltip`, and `aria-label`.
Suggestion buttons are Outline cards. Edit behavior is a `role="group"` segmented control
(Primary for the selected segment, Outline for the other).

### Files

- `source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor`
- `source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor.css`
- `source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor`
- `source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor.css`
- `source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AskAnswerBar.razor`
- `source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AskAnswerBar.razor.css`
- `source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AskSuggestions.razor`
- `source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AskSuggestions.razor.css`
- `tests/container-apps/web/web-spa-playwright-tests/ask-surface-playwright-tests.cs`
- `skills/tw-blazor-layout/SKILL.md`
- `skills/tw-blazor/SKILL.md`
- Shots beside this task: `ask-docked-1280.png`, `ask-expanded-1280.png`, `ask-narrow-800.png`,
  `ask-closed-1280.png`

### Decisions

- One aside column. Ask owns it while open. The page `Aside` returns when Ask closes.
- Full-screen is one treatment in two rulesets (media query and expanded class). See Notes.
- The old assertion (panel width 450 and app bar + panel <= viewport) is replaced. A 0px page
  no longer passes: layout width must be > 800, content width > 400, panel x ≈ viewport − 450,
  and the panel's right edge must be the viewport. `.fluent-layout` must have no `mobile`
  attribute at 1280.

### Tests

`ganda repo audit`: Passed 31, Failed 0 (2026-10-10).

Ask surface Playwright, from `tests/container-apps/web/web-spa-playwright-tests` (the project
`global.json` selects Microsoft.Testing.Platform; the repo root does not):

```text
web-spa-playwright-tests.dll (net11.0|x64) passed [+1/x0/?0] (10s 823ms)
Test run summary: Passed! total: 1 failed: 0 succeeded: 1 skipped: 0 duration: 10s 941ms
```

Playwright logged that chromium has no ubuntu26.04 build, then used the ubuntu24.04 fallback.
The class still passed. Review, done-move, PR, and CI are later host nodes.

### How to validate

**Smoke:**

```bash
cd tests/container-apps/web/web-spa-playwright-tests
dotnet test -c Release --nologo -- --filter-class AskSurface_Given_Wasm
```

The class opens real WASM at `https://localhost:7000`, asks at 1280×800, expands, shrinks to
800×700, closes, and reopens from Ask AI. Shots land beside this task's `task.md`.

**Expect:**

- Exit 0. Summary `Passed!` with `failed: 0` and one succeeded test (configured and
  not-configured passes run inside that one class).
- At 1280×800 with Ask open: `[data-qa=AgentAsk]` x is within 8px of viewport width − 450, its
  right edge is the viewport (±2px), width is 450 (±2px). `.twe-shell__layout` width > 800.
  `.fluent-layout-item[area=content]` width > 400. `.fluent-layout` has no `mobile` attribute.
- Header actions sit to the right of the title. `AskBeforeEditing` and `AskAutomaticallyEdit`
  share a row (±4px). Accessible name and `title` are New conversation, Expand, and Close.
- After Expand: accessible name is Collapse, and the panel box is the viewport (x and y ≤ 2,
  width and height match). Collapse returns the docked x.
- At 800×700 the panel box is 800×700 and covers the viewport.
- After Close, `[data-qa=AgentAsk]` is hidden and `.twe-shell__layout` width equals the viewport.
- `ask-docked-1280.png` shows the home page beside a right Ask panel. `ask-expanded-1280.png`
  and `ask-narrow-800.png` are Ask full-screen. `ask-closed-1280.png` is the full home page.

**Depends on:** Playwright chromium (the test installs it; ubuntu26.04 falls back to the
ubuntu24.04 build). The project sets `UseMock` and, on the configured pass, `UseFakeUpstream`.
No xAI key.

**Not in scope:** running the app on TWE-001. Review, `ganda kanban done`, and `gh pr create`
are later host nodes. Merge only via `ganda pr merge` after Steven approves.
