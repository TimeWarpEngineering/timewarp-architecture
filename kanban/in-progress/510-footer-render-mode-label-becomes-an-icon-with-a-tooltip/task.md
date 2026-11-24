# Footer render-mode label becomes an icon with a tooltip

## Description

`TimeWarpPage.razor` line 317 prints `@RendererInfo.Name` as text in the footer's left corner
("Static", "Server", "WebAssembly"). People have argued the word has no place on a production
site. It still has value: under `InteractiveAuto` a cold load moves Static → Server →
WebAssembly, and seeing that transition at a glance is useful to the maintainer. Replace the word
with a small icon that carries the same information, with the words in a tooltip and an
accessible label. The footer ships in every generated app, so this is template chrome, not a flag.

## Requirements

- One icon per `RendererInfo.Name`, FluentUI v5 Regular, Size16 (all three verified in the icon
  catalogue):
  - `Static` → `Icons.Regular.Size16.Document`
  - `Server` → `Icons.Regular.Size16.Server`
  - `WebAssembly` → `Icons.Regular.Size16.DeveloperBoard`
  - Any other name (future modes) → fall back to the text as today.
- Colour is the footer's muted foreground token, the same as the current text. No status colours:
  Server is a normal state for the first seconds of every session, not a warning.
- `FluentTooltip` on the icon: "Rendering: WebAssembly" and one plain sentence per mode
  ("Running in your browser." / "Running on the server over a live connection." / "Pre-rendered
  page, not yet interactive."). The icon has `aria-label` with the same words and `role="img"`.
  `data-qa="RenderMode"` with `data-mode="<name>"` so tests and Playwright can read the state.
- When the name changes while the page is mounted (Server → WebAssembly under Auto), the icon
  crosses over with a short opacity fade (`--twe-duration-fast`), no toast, no text.
- Styling in `TimeWarpPage.razor.css` (`tw-blazor-css-strategy`); keep the `twe-footer__mode`
  class so existing CSS and tests find the slot.
- Keep the layout: the spinner and the version button do not move.

### Evidence

- Screenshots in `evidence/` of the footer in all three states, light and dark, taken from the
  running app: Static (prerender, interactivity disabled or captured before the circuit),
  Server, and WebAssembly. One showing the tooltip open.

## Goal (implementer restatement)

Swap the footer's render-mode word ("Static" / "Server" / "WebAssembly") for a 16px FluentUI
Regular icon in the same muted colour, so production pages carry no stray word but the
maintainer can still watch Auto move Static → Server → WebAssembly.

Acceptance:

- Static → Document, Server → Server, WebAssembly → DeveloperBoard; any other name prints the
  name as text, as today.
- A FluentTooltip on the icon reads "Rendering: <name>" plus one plain sentence for that mode.
  The slot has `role="img"` and an `aria-label` with the same words.
- The slot keeps `twe-footer__mode` and adds `data-qa="RenderMode"` and `data-mode="<name>"`;
  tests read `data-mode` instead of the text.
- A changed name fades the new icon in over `--twe-duration-fast`. No toast, no status colour.
- CSS lives in `TimeWarpPage.razor.css`; spinner and version button stay where they are.
- Evidence screenshots in `evidence/`; `dev build` 0/0, the TimeWarpPage render tests, and
  `ganda repo audit` pass.

## Checklist

- [x] Icon map + fallback; tooltip and aria-label copy
- [x] Fade on mode change under Auto
- [x] `data-qa`/`data-mode` attributes; existing footer tests updated
- [x] Six screenshots plus one tooltip screenshot in `evidence/`
- [x] `dev build` 0/0, web-spa integration tests, `ganda repo audit` green

## Session

- Created: 2026-11-24 from Steve: icon with tooltip instead of the enum word, no colour coding.
- 2026-11-24 implementer: merged origin/master (kanban lands only). Added `RenderModeBadge`
  (`components/render-mode-badge.cs`), the icon slot and inline `FluentTooltip` in
  `TimeWarpPage.razor`, footer CSS, and the `--twe-duration-fast` token (150ms) in `tokens.css`
  (the token did not exist yet). First Static shot showed the tooltip words inline because
  `fluent-tooltip` is not defined before Fluent's script loads; CSS now hides
  `fluent-tooltip:not(:defined)`, and the Playwright test checks for it.

## Results

What changed:

- `source/container-apps/web/projects/web-spa/components/render-mode-badge.cs` (new): name to
  icon / heading / sentence / aria-label map. Static → `Document`, Server → `Server`,
  WebAssembly → `DeveloperBoard` (Regular Size16). Any other name has no icon and prints as text.
- `TimeWarpPage.razor`: the footer's left slot is `<span class="twe-footer__mode" role="img"
  aria-label="Rendering: <name>. <sentence>" data-qa="RenderMode" data-mode="<name>">` holding a
  `FluentIcon` in `var(--twe-muted)`, plus an inline `FluentTooltip` (`UseTooltipService=false`,
  so it works on the prerendered page too) with "Rendering: <name>" over the sentence. `@key` on
  the name swaps the slot on a mode change; on interactive renders the slot carries
  `twe-footer__mode--fade`, which fades it in over `--twe-duration-fast` (off under
  `prefers-reduced-motion`). Static first paint does not animate. Spinner and version button
  unchanged.
- `TimeWarpPage.razor.css`: slot, fade, hidden un-upgraded tooltip, tooltip layout.
- `wwwroot/css/tokens.css`: `--twe-duration-fast: 150ms`.
- Playwright tests that waited on the footer text leaving "Static" now read
  `[data-qa=RenderMode]`'s `data-mode` (first-paint, shell-nav, dark-theme, state-dev-tools-wasm).
- New tests: `render-mode-badge-tests.cs` (map + fallback), one footer case in
  `time-warp-page-render-tests.cs` (static render), `render-mode-footer-playwright-tests.cs`
  (Static / Server / WebAssembly, light and dark, tooltip hover; writes the evidence shots).
- Evidence: `evidence/static-{light,dark}.png`, `server-{light,dark}.png`,
  `webassembly-{light,dark}.png`, `server-tooltip-light.png`.

Gates (the test files are projects, not standalone runfiles, and `dev test` takes no file
argument; each file was run as its class through the built test host with `--filter-class`):

- `./bin/dev build`: 0 warnings, 0 errors.
- `web-spa-integration-tests --filter-class RenderModeBadge_Should_` (render-mode-badge-tests.cs): 2/2 passed.
- `web-spa-integration-tests --filter-class TimeWarpPage_Should_` (time-warp-page-render-tests.cs): 17/17 passed.
- `web-spa-playwright-tests --filter-class RenderModeFooter` (render-mode-footer-playwright-tests.cs): 2/2 passed.
- `web-spa-playwright-tests --filter-class FirstPaint_Given` (first-paint-playwright-tests.cs): 3/3 passed.
- `web-spa-playwright-tests --filter-class ShellNav` (shell-nav-playwright-tests.cs): 2/2 passed.
- `web-spa-playwright-tests --filter-class DarkTheme_Given` (dark-theme-playwright-tests.cs): 4/4 passed.
- `web-spa-playwright-tests --filter-class StateDevToolsWasm_Given` (state-dev-tools-wasm-playwright-tests.cs): 1/1 passed.
- `ganda repo audit`: first run failed only on `kanban-landed` (task 511, another live walk's
  kitchen differed from origin-home) and the `vscode-window-icon` warning; neither came from
  this change. `ganda repo audit --fix` landed 511's kitchen and wrote the ignored
  `.vscode/settings.json`. Result: "Repository passes — 1 advisory (non-blocking) warning"
  (`kanban-landed`: claim 505 has no usable worktree).

### How to validate

Smoke: `./bin/dev build`, then
`tests/container-apps/web/web-spa-playwright-tests/bin/Release/net11.0/web-spa-playwright-tests --filter-class RenderModeFooter`
and open `playwright-screenshots/RenderModeFooter_Given_Shell/` in that bin folder (or the
copies in this task's `evidence/`).

Expect: both tests pass. Each footer shot shows a small muted icon at the left (page for
Static, server for Server, chip for WebAssembly), no mode word, the version at the right, in
light and dark. `server-tooltip-light.png` shows the tooltip "Rendering: Server" over "Running on
the server over a live connection." In the browser, the slot has `data-mode` set to the mode
name and an `aria-label` with the same words.
