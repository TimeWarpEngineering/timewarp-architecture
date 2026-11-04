# Round 1 — general
**Date:** 2026-11-04
**Scope reviewed:** branch vs master (product files: App.razor, brand-theme-script.cs, brand-options.cs,
application-state.toggle-theme.cs, tokens.css, brand-options-tests.cs, dark-theme-playwright-tests.cs),
plus repo call sites and shipped skills that describe the first-paint theme contract.

## Summary

The change moves BrandThemeScript to the first child of `<body>` and has it set `body[data-theme]` from
the stored `light`/`dark` mode, else `prefers-color-scheme: dark`, without writing a mode. The server
default becomes `data-theme="system"`. I checked this against Fluent 5.0.1 `lib.module.js`. Its
`ir()` accepts `dark`/`light`/`system` from the body attribute, and its system-change listener
re-applies only when no `dark`/`light` mode is stored. So the implementer's analysis holds, and
the session-follow behavior from task 465 is kept. The code matches both the task and the
restatement. Risk is low.

Re-ran: `DarkTheme_Given_Shell` 4/4 passed (Release, 1m 23s), and `BrandOptions_Should_` 7/7 passed.
`dev build`, `ganda repo audit`, and template-smoke were not re-run. The test run compiled the
product projects with no errors.

The one issue is a shipped skill that still states the old contract.

## Issues

### Issue 1 — Severity: suggestion
- File: skills/tw-blazor-css-strategy/SKILL.md:70
- Description: Rule 8 still says `App.razor` renders `<body data-theme-color="#55409c" data-theme="light">`.
  Rule 9 (line 75) still says "Fluent sets `body[data-theme="dark"]` when its mode is dark". After this
  change the server renders `data-theme="system"`, and BrandThemeScript, the first child of body, sets
  `dark`/`light` from the stored mode, else the OS preference, before first paint. Skills ship in every
  generated app and are where a live rule belongs (AGENTS.md). An agent that follows rule 8 as written
  would restore `data-theme="light"` and bring back the light flash on a dark OS. The implementer
  updated the Design regions and code comments but missed this skill. The task asked for the work to
  follow `tw-blazor-css-strategy`, so the skill's first-paint rule should describe the new behavior.
- Suggestion: Update rule 8 to say the body starts as `data-theme="system"` and that BrandThemeScript
  (the first child of body, a classic script) sets the initial `light`/`dark` mode from the stored mode,
  else `prefers-color-scheme`, and never stores a mode. Update rule 9 so it names who sets the attribute
  (BrandThemeScript at first paint, then Fluent and the theme button). Run `dev lint-skills`.
  `skills/tw-blazor-layout/SKILL.md:34` only mentions `data-theme` generically and needs at most a
  pointer to rule 8.
- Status: open
