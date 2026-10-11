# Round 1 — general
**Date:** 2026-10-11
**Scope reviewed:** branch vs master (AgentAsk.razor, AgentAsk.razor.css, ask-surface-playwright-tests.cs, task.md, screenshots)

## Summary

The change turns the library `MessageInput` flex row into a two-row grid with scoped CSS: full-width
textarea on top, Tag and send on a footer row. It adds a Tag icon, tooltip, and aria-label, and a
Playwright layout test. The grid and textarea parts are sound. The Tag pill styling is dead CSS, and the
steering requirement that the chip align with the textarea text start is neither implemented nor asserted.
`ask-composer-before.png` shows the chip with no pill border and the icon about 25px right of the text start.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor.css:186
- Description: `.twe-agent-ask__tag::part(control)` matches nothing. In Fluent UI v5 (5.0.0 `lib.module.js`), `fluent-button` exposes only `start`, `content`, and `end` parts. Its box styles sit on `:host`: `min-width: 96px`, `justify-content: center`, `padding: 0 spacingHorizontalM`, and the appearance border. So the border, radius, and padding-inline never apply. The 96px centered minimum also pushes the icon and label about 25px past the textarea text start, which is the misalignment Steven reported.
- Suggestion: Style the host directly through the anchored `::deep` rule (border, 999px radius, `min-width: 0`, `justify-content: flex-start`, padding-inline, min-height), and drop the `::part(control)` rule.
- Status: open

### Issue 2 — Severity: bug
- File: tests/container-apps/web/web-spa-playwright-tests/ask-surface-playwright-tests.cs:281
- Description: Steering 2026-10-11 22:12 asks for a Playwright assertion that the Tag chip's left edge is within a few px of the textarea's text start (textarea left + padding-left). The test checks only a 0–24px inset from the composer edge, and that range accepted the misaligned chip.
- Suggestion: Read the textarea's computed `padding-left` and assert |tag.X − (textarea.X + padding-left)| ≤ 4.
- Status: open

### Issue 3 — Severity: nit
- File: tests/container-apps/web/web-spa-playwright-tests/ask-surface-playwright-tests.cs:21
- Description: The Purpose comment says the textarea starts "about 16px" inside the box. The CSS padding is `--twe-space-3` (12px).
- Suggestion: Say 12px (`--twe-space-3`).
- Status: open
