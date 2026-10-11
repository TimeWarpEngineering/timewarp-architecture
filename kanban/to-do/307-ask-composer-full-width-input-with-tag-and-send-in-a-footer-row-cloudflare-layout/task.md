# Ask composer: full-width input with Tag and send in a footer row (Cloudflare layout)

## Description

Steven, 2026-10-11 (with DevTools screenshots in `assets/`). The Ask panel's composer
(placeholder "Tell the app what you want", a `Tag` control, a round send button) lays out as
**one horizontal row of three**: the Tag column on the left, the textarea in the middle, send on
the right. The Tag column takes width, so the placeholder and caret start far to the right, and
the `Tag` label sits low (flex-end) and looks broken (`assets/before-ours.png`,
`assets/before-ours-devtools.png`). Earlier Steven also said this input is ugly and asked what
`Tag` is supposed to do.

**Target:** Cloudflare's Ask composer (`assets/target-cloudflare.png`,
`assets/target-cloudflare-devtools.png`): a rounded container with the multi-line textarea
spanning the **full width on top** (placeholder starting at the left padding), and a **footer row
underneath**: a chip/pill button on the left (ours: Tag) and the send button on the right, with
room for an options icon beside send.

## What Tag does (document this in Results)

`AgentAsk.razor`: `Tag` (`data-qa=AskTagResource`) calls `OpenResources()`, which sets
`ResourceMenuOpen = true`; typing `@` anywhere in the panel does the same (`OnKeyDown`). That
shows `AskResourceMenu`, a list of the typed resources on the current page (from
`page_context` via `AskResourceReferences.FromPageContext`), or "Nothing on this page to tag."
Choosing one inserts its `@` token into the textarea at the caret
(`AskAiJsModule.InsertReferenceAsync` → `source/features/ask-ai.ts` `InsertReference`, which
replaces a just-typed `@` instead of doubling it) and closes the menu. So Tag = "reference
something on this page in your message." Give it a clear label and tooltip, e.g. a pill with an
`@`/tag icon labelled "Tag" (or "Reference") and `Title`/`Tooltip`/`aria-label`
"Tag a resource on this page (@)". Optionally change the placeholder to hint at it, e.g.
"Tell the app what you want. Type @ to tag something on this page."

## Requirements

- Fluent UI Blazor + scoped CSS per the `tw-blazor` and `tw-blazor-css-strategy` skills: all CSS in
  `AgentAsk.razor.css`, anchored `::deep` for library/Fluent light-DOM children, `::part()` for
  Fluent shadow internals, `--twe-*` tokens. No Tailwind, no `<style>` element, no inline `style=`.
- Keep every existing behavior: Tag button and `@` opening the resource menu, token insertion at
  the caret, send, Enter to send / Shift+Enter newline (whatever the library `MessageInput` does
  today; verify and keep), disabled state while a turn streams, WebMCP / agent-surface state,
  all existing `data-qa` hooks (`AskTagResource`, `AskResourceMenu`, `AskResourceOption`, …) and
  the `.sc-ai-input__textarea` selector that `ask-ai.ts` and tests rely on.
- The textarea auto-grows vertically with its content up to a cap (CSS `field-sizing: content`
  with min/max height is preferred over JS; if JS is needed it must be presentational and follow
  TWA0026 rules).
- Tests (Playwright, `tests/container-apps/web/web-spa-playwright-tests/ask-surface-playwright-tests.cs`):
  add or adjust a test that measures the real rendered layout with the panel open:
  - the **inner `textarea.sc-ai-input__textarea`** (not a host/wrapper element; lesson from
    PR #459) has its left edge within the composer container's left padding (≈16px of the
    container's left edge), and its width is ≥ 90% of the container's width;
  - the Tag button and the send button both sit **below** the textarea (their top ≥ textarea bottom);
    Tag is left-aligned, send is right-aligned;
  - the textarea grows taller when several lines are typed (Shift+Enter or long text) and Enter
    still sends.
  Capture before/after screenshots of the composer in the test run if practical.
- Keep the existing Tag-menu and insert tests green.

## Implementation notes (grounding, read-only, master 2026-10-11)

- Markup: `source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor`,
  `<div class="sc-ai-chat-page__input-container"><MessageInput Placeholder="Tell the app what you want"><LeadingActions><FluentButton … Class="twe-agent-ask__tag" data-qa="AskTagResource" OnClick="OpenResources">Tag</FluentButton></LeadingActions></MessageInput>`.
- Library: `Microsoft.AspNetCore.Components.AI` 0.1.0-preview.1.26466.103. `MessageInput` has
  `Placeholder`, `LeadingActions`, `TrailingActions`. Its CSS (`staticwebassets/ai-chat.css`):
  `.sc-ai-input { display:flex; align-items:flex-end; gap; padding; border-radius:lg }`,
  `.sc-ai-input__body { flex:1; flex-direction:column }`, `.sc-ai-input__textarea { min-height:3rem; max-height:10rem; resize:none }`,
  `.sc-ai-input__send` 2.25rem circle. That single flex row is the cause. Verify the rendered DOM
  order (leading actions / body / trailing actions / send) with the browser or
  `tw-dotnet-inspect` / ILSpy before writing selectors.
- Likely fix: anchor on `.twe-agent-ask` and turn `::deep .sc-ai-input` into a 2-row grid
  (`grid-template-areas: "body body body" "lead . send"` or similar), with the body spanning full
  width on top and leading actions / trailing actions + send in the footer row; pill styling for
  the Tag button via `::part(control)`. Keep the library markup (no fork of MessageInput).
- `.twe-agent-ask__tag` currently only sets `cursor: pointer`.

## Checklist

- [ ] Verify `MessageInput` rendered DOM and Enter/Shift+Enter behavior
- [ ] Composer: full-width textarea on top, footer row with Tag pill (icon + tooltip) left, send right
- [ ] Textarea auto-grows to a cap
- [ ] Tag has a clear label/tooltip/aria-label; document what Tag does in Results
- [ ] Playwright layout test measuring the inner textarea (left edge, ≥90% width, Tag/send below), auto-grow, Enter sends
- [ ] Existing Ask Playwright tests green; before/after screenshots captured if practical

## Session

- Created: 742019 (2026-10-11)

## Notes

- Screenshots from Steven: `assets/before-ours.png`, `assets/before-ours-devtools.png` (ours),
  `assets/target-cloudflare.png`, `assets/target-cloudflare-devtools.png` (Cloudflare target).

## Results
