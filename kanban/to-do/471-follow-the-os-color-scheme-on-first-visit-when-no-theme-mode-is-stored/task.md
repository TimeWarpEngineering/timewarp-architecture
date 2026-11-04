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

## Checklist

- [ ] Initial theme follows stored mode, else OS preference, with no light flash
- [ ] Playwright scenarios for first visit on a dark OS and for stored-light precedence
- [ ] Design region and App.razor comment updated
- [ ] Gates green

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-04)

## Notes

Steve asked for this on 2026-11-04 after 465 shipped ("does darkmode support system setting?").
The remaining 465 follow-ups (dark-mode logo, Fluent's muted link purple) are not in this task.
