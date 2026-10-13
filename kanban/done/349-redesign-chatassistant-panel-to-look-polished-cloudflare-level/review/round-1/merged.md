# Round 1 — merged findings
**Date:** 2026-10-13
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 4 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor.css:369, 414-430
- Description: The Tag pill and edit-behavior segment rules used `fluent-button::part(control)`. Fluent v5 `fluent-button` exposes no `control` part (verified: in the 5.0.0 lib.module.js, `part="control"` is only on the input and textarea templates), so these rules did nothing.
- Suggestion: Style the fluent-button host.
- Source: general
- Disposition notes: The rules now target the host. The Tag button renders as a pill and the segments get their radius and font size (screenshots re-taken).

### M2 — Severity: bug — Status: fixed
- File: tests/container-apps/web/web-spa-playwright-tests/ask-panel-design-playwright-tests.cs:135
- Description: `WaitForBoxAsync` returned a stale box when the predicate never held, so the resize checks could pass on the wrong layout.
- Suggestion: Assert the predicate after the loop.
- Source: general
- Disposition notes: The helper now asserts `ready(last)` with the last x and width.

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor.css:221
- Description: At 1280x800 (docked) and 800x700 the 4th starter card was clipped behind the notice and composer.
- Suggestion: Make the empty state shorter on short viewports.
- Source: general
- Disposition notes: `@media (max-height: 900px)` replaces the hero's scale transform with `zoom: 0.5`, which also shrinks the layout box. All four cards are now visible at both captured sizes (assets re-taken).

### M4 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AskStarterPrompts.razor.css:77
- Description: Small secondary text on `--twe-muted` (#767a86) is about 4.28:1 on white, below WCAG AA's 4.5:1.
- Suggestion: Use `--twe-ink-2` for text that has to be read.
- Source: general
- Disposition notes: The card subtitle, tagline, conversation name and edit-behavior label now use `--twe-ink-2`. The `--twe-muted` token itself is unchanged (template-wide; out of scope).

### M5 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AskStarterPrompts.razor.css:10
- Description: A superseded duplicate comment block.
- Source: general
- Disposition notes: Removed. Also added a `prefers-reduced-motion` guard for the card transition and press transform.

### M6 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AskAnswerBar.razor:34
- Description: `HasAnswer` compared the answer text with the fallback label, so an answer whose text was exactly that label would hide the bar.
- Source: general
- Disposition notes: `LastAnswer()` returns `string?`. The bar renders on `is not null`; Copy and feedback fall back to the label.

### M7 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor.css:83
- Description: A literal `15px` title size, off the type scale.
- Source: general
- Disposition notes: Now `var(--twe-text-body)` at weight 600.

### M8 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:471
- Description: `role="status"` was inserted together with its text, so it may not be announced.
- Source: general
- Disposition notes: An always-present `div.twe-agent-ask__status[role=status]` (flex: none) wraps the conditional expired text.

## Duplicates / conflicts

- None.
