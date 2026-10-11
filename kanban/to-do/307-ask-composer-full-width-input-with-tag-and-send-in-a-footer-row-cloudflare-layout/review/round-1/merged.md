# Round 1 — merged findings
**Date:** 2026-10-11
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor.css:186
- Description: `.twe-agent-ask__tag::part(control)` is dead CSS. Fluent v5 `fluent-button` has no `control` part, and its host keeps `min-width: 96px` with `justify-content: center`. The pill never rendered, and the label sat about 25px right of the text start.
- Suggestion: Style the host directly and drop `::part(control)`.
- Source: general
- Disposition notes: Fixed. Moved the pill styles onto the anchored `::deep .sc-ai-input > .twe-agent-ask__tag` host rule, added `min-width: 0` and `justify-content: flex-start`, removed the `::part(control)` rule, and added a comment. The new `ask-composer-before.png` shows the bordered pill flush with the text start.

### M2 — Severity: bug — Status: fixed
- File: tests/container-apps/web/web-spa-playwright-tests/ask-surface-playwright-tests.cs:281
- Description: The steering-required assertion (Tag chip left edge within a few px of textarea left + padding-left) was missing.
- Suggestion: Assert |tag.X − (textarea.X + computed padding-left)| ≤ 4.
- Source: general
- Disposition notes: Fixed. `AssertComposerLayoutAsync` reads the computed `padding-left` and asserts the chip is within 4px of the text start. The test passes.

### M3 — Severity: nit — Status: fixed
- File: tests/container-apps/web/web-spa-playwright-tests/ask-surface-playwright-tests.cs:21
- Description: The Purpose comment said "about 16px". The padding is 12px.
- Suggestion: Say 12px.
- Source: general
- Disposition notes: Fixed. The comment now says 12px (`--twe-space-3`) and describes the new chip alignment check.

## Duplicates / conflicts

- M1 and M2 come from the same steering note. M1 is the product cause and M2 is the missing guard. Kept separate.
