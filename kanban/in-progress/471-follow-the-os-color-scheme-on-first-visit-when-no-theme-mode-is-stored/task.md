# Follow the OS color scheme on first visit when no theme mode is stored

## Description

Follow-up to task 465 (dark theme fix). Today a first visit with no stored Fluent mode starts
light even when the OS prefers dark, because `web-server/components/App.razor` renders
`<body data-theme="light">` and Fluent only flips to dark if the OS setting changes during the
session. A stored choice from the shell's theme button (ApplicationState.ToggleTheme, stored under
`fluentui-blazor:theme-settings`) already survives reloads and must keep winning.

Goal: on first load with no stored mode, the page starts in the OS color scheme with no light
flash before Fluent and Blazor start. Precedence: stored mode, else OS preference.

## Requirements

- Decide the initial `data-theme` before Blazor starts. The natural place is the classic
  pre-Blazor seed script (`web-spa/configuration/brand-theme-script.cs`, rendered by App.razor):
  if the stored settings carry a mode, apply it; else read `prefers-color-scheme: dark` and set
  `body[data-theme]` to match. Alternative is an inline attribute script in App.razor; pick one,
  record it in the Design region of the file that owns it, and update the App.razor comment.
- Do not store the OS preference as a user choice. With nothing stored, Fluent's system-change
  listener must still follow the OS during the session (task 465 behavior).
- The theme button still toggles and stores the mode; a stored light choice on a dark OS stays light.
- `tokens.css` dark overrides already key on `body[data-theme="dark"]`; no token changes expected.
- Follow `tw-blazor-css-strategy` and `tw-blazor`: no `<style>` elements, no TS patch of Fluent
  internals, no page-local interop. Check Fluent v5's own mode handling with the fluentui-mcp tool
  before writing script, in case `SwitchThemeAsync` or the startup script already offers a
  "system" initial mode that only needs enabling.
- Tests: extend `tests/container-apps/web/web-spa-playwright-tests/dark-theme-playwright-tests.cs`
  with a scenario that emulates a dark color scheme (Playwright `colorScheme: dark`) with cleared
  storage and asserts the body attribute and a dark `--twe-*` surface on first paint, and a
  scenario that a stored light mode on a dark OS stays light.
- Gates: `dev build` 0 warnings, Playwright tests pass, `ganda repo audit` clean. Screenshots of
  first load on a dark OS before and after go in `kitchen/`.

## Goal (implementer restatement)

A first visit with nothing stored under `fluentui-blazor:theme-settings` must paint in the OS
color scheme from the first frame: a dark OS gets `body[data-theme="dark"]` (and so the dark
`--twe-*` tokens) before Fluent or Blazor run, with no light flash. Precedence is the stored mode
(light or dark from the theme button) first, then the OS preference. The OS preference is never
written to storage, so with nothing stored Fluent's own system-change listener keeps following
the OS during the session, and the theme button still flips and stores a choice that wins over
the OS on later loads.

Acceptance:

- The pre-Blazor seed script (BrandThemeScript) sets `body[data-theme]` from the stored mode,
  else from `prefers-color-scheme: dark`, and runs before the body content can paint. It still
  merges the seed and does not write a mode.
- App.razor no longer forces `data-theme="light"`; its comment and the script's Design region
  say who decides the initial mode.
- Playwright: a dark-OS context with cleared storage shows `data-theme="dark"` and a dark
  `--twe-*` surface with blazor.web.js blocked (first paint) and after interactive; a stored
  light mode on a dark OS stays light.
- `dev build` 0 warnings, the dark-theme Playwright class and the brand unit test pass,
  `ganda repo audit` clean, before/after first-load screenshots in `kitchen/`.

## Checklist

- [x] Initial theme follows stored mode, else OS preference, with no light flash
- [x] Playwright scenarios for first visit on a dark OS and for stored-light precedence
- [x] Design region and App.razor comment updated
- [x] Gates green

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-04)
- 2026-11-04 implementer (ganda task work): seed script decides the initial mode; tests and gates
  in ## Results.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 0c486098-fef5-428b-8802-4163a806cce7, max-turns 500, Done — 2026-11-04T14:47:52Z

## Results

- Fluent 5.0.1 check (its `lib.module.js`): `initializeThemeSettings` takes the stored mode, else
  `body[data-theme]` if it is `dark`, `light`, or `system`, else `system`. Its system-change
  listener re-applies only when no mode is stored. Fluent has no option to apply the system mode
  before its own startup, and that startup is a module that blazor.web.js imports, so it runs after
  the prerendered page can paint. A pre-Blazor script is still needed.
- Picked the seed script, not an inline attribute script. `BrandThemeScript` now reads the stored
  mode (`light` or `dark` only), else `prefers-color-scheme: dark`, and sets `body[data-theme]` to
  `dark` or `light`. It still merges color, exact, and base and never writes a mode, so the OS
  preference is not stored. The Design region records the choice.
- `App.razor` renders the script as the first child of `<body>`, so no body content is parsed or
  painted before the attribute is set. The old spot (end of body) could paint first, for example
  while the parser waited on `browser-log-forwarder.js`. The server default is now
  `data-theme="system"` (Fluent's follow-the-OS value; a page without script keeps light tokens),
  and the comment says the script sets the real mode.
- Updated the comments that described the old behavior: `tokens.css` dark block,
  `ApplicationState.ToggleTheme` Design, and `BrandOptions` Design. Tokens are unchanged.
- Tests: `BrandOptions_Should_` checks that the script reads the stored mode and the OS preference
  and still writes no mode. `DarkTheme_Given_Shell` adds two scenarios:
  `FirstVisit_On_DarkOs_Should_Paint_Dark_Without_Storing_A_Mode` (dark OS, empty storage; with
  blazor.web.js aborted the body is `dark` and the app bar is `--twe-paper` dark `#303030`; then
  interactive with every dark-surface check passing; storage holds no mode; an OS switch to light
  turns the page light) and `StoredLight_On_DarkOs_Should_Stay_Light` (stored light on a dark OS:
  light at first paint and when interactive). Against the old product files the first scenario
  failed (body was `light`); with the change it passes.
- Screenshots: `kitchen/before/os-dark-first-paint.png` (dark OS, old code: light shell) and
  `kitchen/after/` (`os-dark-first-paint.png`, `os-dark-interactive.png`,
  `stored-light-first-paint.png`).
- Gates: `dev build` 0 warnings 0 errors; `BrandOptions_Should_` 7/7; full Playwright suite 37/37
  (including `DarkTheme_Given_Shell` 4/4); `dev template-smoke` (default variant) succeeded;
  `ganda repo audit` passes with 2 advisory warnings that were already there (`kanban-landed` for
  other tasks' claims, `source-file-line-cap` on generated migrations and a done task's research file).
- Not in scope, seen in the first-paint shot: before Fluent starts, the outcome-bar dismiss icons
  are dark on dark. They are the same with a stored dark mode today (task 465 behavior).

### How to validate

Smoke:

```bash
cd tests/container-apps/web/web-spa-playwright-tests
dotnet test -c Release -- --filter-class DarkTheme_Given_Shell
```

Expect: 4 tests pass. `bin/Release/net11.0/playwright-screenshots/DarkTheme_Given_Shell/` holds
`os-dark-first-paint.png` (dark shell with blazor.web.js blocked), `os-dark-interactive.png`
(dark shell, interactive), and `stored-light-first-paint.png` (light shell on a dark OS).

Manual: set the OS (or browser DevTools "Emulate CSS prefers-color-scheme") to dark, clear site data,
and open the app: the first paint is dark with no light flash. Click the half-moon button: light,
and a reload stays light on the dark OS. Clear site data again: it follows the OS once more.

## Notes

Steve asked for this on 2026-11-04 after 465 shipped ("does darkmode support system setting?").
The remaining 465 follow-ups (dark-mode logo, Fluent's muted link purple) are not in this task.
