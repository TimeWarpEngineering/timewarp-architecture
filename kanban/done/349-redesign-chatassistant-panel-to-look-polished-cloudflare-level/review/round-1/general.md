# Round 1 — general
**Date:** 2026-10-13
**Scope reviewed:** commit 4228d4886

## Summary
The redesign is sound overall. Every color is a `--twe-*` token that exists in `tokens.css`. `::deep` is anchored on native roots, and no component has a `<style>` element. The AskAnswerBar subscription is correct. The `@key="Conversation"` on the chat-page div recreates the bar for each new conversation, `Dispose` releases the subscription, and the bar's only parameter is a string, so the status re-render is needed. I found two real defects. First, the new `::part(control)` styling targets a shadow part that Fluent v5 `fluent-button` does not have, so the Tag pill and segment styling are no-ops (the after screenshots confirm the Tag button is not a pill). Second, the new Playwright helper `WaitForBoxAsync` never fails when its condition is unmet, so the viewport-resize steps prove nothing. The remaining items are suggestions and nits.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor.css:369
- Description: `.twe-agent-ask__lead ::deep fluent-button::part(control)` (pill radius `999px`, border color, ink-2 color) and the segment rules at lines 414-430 (`width`, `justify-content`, `border-radius: 0`, `font-size`, first/last-child radii) all target `::part(control)`. In Microsoft.FluentUI.AspNetCore.Components 5.0.0 the bundled web components expose `part="control"` only on the textarea and input templates, not on `fluent-button`. The button's shadow parts are `content` plus the start/end slots. The commit's own comment in `AskStarterPrompts.razor.css` says the same thing: "Fluent v5 draws the button's border, radius and fill on the fluent-button host". In `assets/after-ask-empty-state-1280.png` and `-800.png` the Tag button is a small rounded rectangle, not the pill the task Results describe ("Tag as a small outline pill"). The segment radius and font-size rules were partly carried over from before, and they are also dead.
- Suggestion: Style the host instead, as the starter cards now do. For example, `.twe-agent-ask__lead ::deep fluent-button { border-radius: 999px; border-color: …; color: … }`, and the same for `.twe-agent-ask__segments ::deep > fluent-button` and its `:first-child` / `:last-child` radii. Or use the Fluent CSS custom properties. Then remove the `::part(control)` selectors.
- Status: open

### Issue 2 — Severity: bug
- File: tests/container-apps/web/web-spa-playwright-tests/ask-panel-design-playwright-tests.cs:135
- Description: `WaitForBoxAsync` polls 50 times. If the `ready` predicate never holds, it falls through to `last.ShouldNotBeNull(selector)` and returns the stale box. A layout that never reaches the expected geometry passes silently. Two calls depend on it. At line 93 (full-viewport at 800 wide), a regression in the 800px layout would still take the "800" screenshot of the wrong layout. At line 100 (docked 450 wide at 1280), the layout assertions that follow may run against the wrong layout. Neither wait proves its claim.
- Suggestion: After the loop, assert the predicate. For example, `ready(last).ShouldBeTrue($"{selector} never reached the expected box: {last.X},{last.Width}")`. Alternatively, have the helper return a bool and assert it at the call site.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor.css:441
- Description: The empty state does not fit at either captured size. In `after-ask-empty-state-1280.png` (docked panel at 800 high) and `after-ask-empty-state-800.png` (800x700), the fourth starter card ("Check approvals") is cut off behind the notice and composer. Its subtitle cannot be seen until the user scrolls, and nothing in the UI suggests more cards below. The hero (80px plus margin), the greeting, the tagline and the 52px-minimum cards take up the height. The test checks that every card has an icon, a title and a subtitle, but not that the cards are within the scroll viewport, so this goes unflagged.
- Suggestion: Make the empty state shorter on short viewports, for example with `@media (max-height: …)`: shrink or hide the hero, tighten the card `min-height` and the gaps. Or accept scrolling, but add a scroll affordance such as a fade at the bottom. Optionally, assert in the test that the last card's bottom is at or above the top of the notice at 1280x800.
- Status: open

### Issue 4 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AskStarterPrompts.razor.css:77
- Description: The redesign moves more small text onto `--twe-muted` (`#767a86`): the card subtitle (previously `--twe-ink-2`), the new tagline (AgentAsk.razor.css:268), the conversation name (line 89), the edit-behavior label, and the answer-bar icons. `#767a86` on `#ffffff` is about 4.3:1. On the `--twe-background` (`#fafafb`) dotted canvas behind the tagline it is lower. Both are below WCAG AA's 4.5:1 for normal text, and most of this text is 12px (`--twe-text-helper`).
- Suggestion: Use `--twe-ink-2` (`#3b3f4d`) for secondary text that has to be read: the card subtitles and the tagline. Or darken `--twe-muted` in `tokens.css` so it passes 4.5:1.
- Status: open

### Issue 5 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AskStarterPrompts.razor.css:10
- Description: There are two consecutive comment blocks above `.twe-ask-starters ::deep > fluent-button`. The first one ("…not from its shadow control, so the host carries them") is an earlier draft that the second one supersedes.
- Suggestion: Delete the first comment block.
- Status: open

### Issue 6 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AskAnswerBar.razor:34
- Description: `HasAnswer` compares `LastAnswer()` against the fallback string `"Ask AI answer"`. If a real answer's raw text is exactly that string, the bar hides. Each render also walks the turns twice: once for `HasAnswer`, and again when Copy or the thumbs run.
- Suggestion: Split it into a `string? LastAnswerText()` that returns null when there is no answer. Render on `is not null`, and fall back to the label only in the feedback and copy paths.
- Status: open

### Issue 7 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor.css:83
- Description: `.twe-agent-ask__title` uses a literal `font-size: 15px` that falls between the `--twe-text-body` (14px) and `--twe-text-section-title` (18px) tokens. Other literals in the same commit (the 24px mark, `999px` radius, 16px dot grid, orb sizes) are decorative geometry and acceptable. The type size, however, falls outside the type scale.
- Suggestion: Use `--twe-text-body` with weight 600, or add a token if 15px is the intended size.
- Status: open

### Issue 8 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:471
- Description: `role="status"` sits on a `<p>` that is rendered conditionally, so the live region and its text are inserted together. Screen readers often skip live regions that are inserted with their content already in them, so the added role may not cause an announcement.
- Suggestion: Keep an always-present empty `role="status"` container and render the expiry text inside it. Or drop the role if no announcement is intended.
- Status: open
