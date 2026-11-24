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

## Checklist

- [ ] Icon map + fallback; tooltip and aria-label copy
- [ ] Fade on mode change under Auto
- [ ] `data-qa`/`data-mode` attributes; existing footer tests updated
- [ ] Six screenshots plus one tooltip screenshot in `evidence/`
- [ ] `dev build` 0/0, web-spa integration tests, `ganda repo audit` green

## Session

- Created: 2026-11-24 from Steve: icon with tooltip instead of the enum word, no colour coding.
