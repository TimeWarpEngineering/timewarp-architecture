# Round 1 — general
**Date:** 2026-10-10
**Scope reviewed:** same as framework; source, tests, skills, and the committed state screenshots.

## Summary

The dock fix is correct: Ask is the FluentLayout aside item, `.twe-shell` stays `display: contents`,
and `ask-docked-1280.png` shows the page reflowed beside a 450px right panel. The full-screen rule
anchors on the aside area without Fluent internal ids. The Playwright geometry assertions now fail
a 0px page. The screenshots show one visible defect the tests do not catch: the new icon-only
buttons render as specks.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/wwwroot/css/app.css:28 (hit by AgentAsk.razor header actions and AskAnswerBar.razor)
- Description: The header (New, Expand, Close) and message (Copy, Thumbs up, Thumbs down) icon-only
  FluentButtons render their icons at a few pixels (see `ask-docked-1280.png`, `ask-narrow-800.png`).
  The global reset `img, svg { display: block; max-width: 100% }` resolves against the button's
  shrink-to-fit start slot, so the slotted svg collapses. Requirement 4 (usable icon buttons) is not met
  visually; `AssertIconButtonAsync` only checks `aria-label` and `title`.
- Suggestion: Exempt slotted Fluent icons from the percentage max-width in the Ask CSS (`::deep svg
  { max-width: none }` under the action containers) and assert a minimum rendered icon size in the
  Playwright test.
- Status: open

### Issue 2 — Severity: nit
- File: tests/container-apps/web/web-spa-playwright-tests/ask-surface-playwright-tests.cs:243
- Description: Screenshot output is hard-wired to the task folder glob `296-*` (was `292-*`). Each
  new Ask task must re-point it, and older shots stop refreshing.
- Suggestion: Leave as is; this is the established per-task evidence pattern in this test project
  (`ask-sign-in`, `first-paint` do the same).
- Status: open
