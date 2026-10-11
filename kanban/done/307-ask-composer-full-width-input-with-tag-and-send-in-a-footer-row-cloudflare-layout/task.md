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

- [x] Verify `MessageInput` rendered DOM and Enter/Shift+Enter behavior
- [x] Composer: full-width textarea on top, footer row with Tag pill (icon + tooltip) left, send right
- [x] Textarea auto-grows to a cap
- [x] Tag has a clear label/tooltip/aria-label; document what Tag does in Results
- [x] Playwright layout test measuring the inner textarea (left edge, ≥90% width, Tag/send below), auto-grow, Enter sends
- [x] Existing Ask Playwright tests green; before/after screenshots captured if practical

## Session

- Created: 742019 (2026-10-11)
- Review oracle: claude-opus-5-5 (headless ganda task work), effort 2, roster general
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-11T15:25:22Z

## Notes

- Screenshots from Steven: `assets/before-ours.png`, `assets/before-ours-devtools.png` (ours),
  `assets/target-cloudflare.png`, `assets/target-cloudflare-devtools.png` (Cloudflare target).

- Steering 2026-10-11 22:12 (Steven via parent): (1) Playwright runs rewrite screenshots under
  `kanban/done/295-*`, `296-*` and `302-*`. Those must NOT be in the PR: `git restore` them before
  every commit and check `git status` before pushing. (2) In the after screenshot the Tag chip is
  indented about 30px from the textarea's text. Align the chip's left edge with the textarea's left
  padding / text start, like Cloudflare's Ask chip; add a Playwright assertion that the Tag chip's
  left edge is within a few px of the textarea's text start (textarea left + its padding-left).

## Results

The Ask composer still uses the library `MessageInput`. Scoped CSS in `AgentAsk.razor.css` turns `.sc-ai-input` from one flex row into a two-row grid: the textarea spans the full width on the first row, and Tag and send sit on the footer. The middle footer column stays empty so a later options control can sit beside send. `MessageInput` was not forked. There is no options button.

### What Tag does

Tag (`data-qa=AskTagResource`) calls `OpenResources()`, which sets `ResourceMenuOpen = true`. Typing `@` anywhere in the panel does the same (`OnKeyDown`). That shows `AskResourceMenu`: the typed resources on the current page (`page_context` via `AskResourceReferences.FromPageContext`), or "Nothing on this page to tag." Choosing a row inserts that resource's `@` token at the caret (`AskAiJsModule.InsertReferenceAsync` → `source/features/ask-ai.ts` `InsertReference`, which replaces a just-typed `@` instead of doubling it) and closes the menu. Tag means "reference something on this page in your message."

The control is a bordered pill: a tag icon plus the label "Tag". Its left edge sits on the textarea's text start. Fluent v5 `fluent-button` has no `control` part, so the pill is styled on the host (`min-width: 0`, `justify-content: flex-start`). `Title`, `Tooltip`, and `aria-label` are "Tag a resource on this page (@)". The placeholder is "Tell the app what you want. Type @ to tag something on this page."

### Layout

Decompiled `Microsoft.AspNetCore.Components.AI` 0.1.0-preview.1.26466.103 `MessageInput`. Direct children of `.sc-ai-input` are the leading actions (no wrapper), `.sc-ai-input__body` (the `textarea.sc-ai-input__textarea`), then the default `.sc-ai-input__send` button. Enter without Shift calls `SubmitAsync`. Shift+Enter does not submit; the browser inserts a newline. The textarea stays disabled while `ConversationStatus` is Streaming or AwaitingInput.

Playwright measured the inner `textarea.sc-ai-input__textarea` against `.twe-agent-ask .sc-ai-input` with the panel open:

- Left inset (`textarea.X − composer.X`) is in [4, 20] px. Container padding is `--twe-space-3` (12px). The textarea's `padding-inline` is 0, so the placeholder starts at that padding.
- Textarea width is at least 90% of the composer width.
- Tag's top and send's top are at or below the textarea bottom (2px tolerance).
- Tag's left inset is in [0, 24] px, and its left edge is within 4px of the textarea text start (textarea left + computed padding-left). Send's right inset is in [0, 24] px. Send sits to the right of Tag.
- The Tag icon is at least 12px wide.
- Three Shift+Enter lines grew the textarea by at least 8px (`field-sizing: content`, min-height 3rem, max-height 10rem).
- Enter sent. A user bubble containing "one" and a `page_context:` answer appeared, then the textarea enabled again.

Shots in this folder: `ask-composer-before.png` (placeholder, Tag pill, send) and `ask-composer-after.png` (`one` / `two` / `three` on three lines, Tag and send still on the footer).

The same test still opens the empty resource menu on the home page, inserts a reference without doubling `@`, copies the last answer, toggles edit mode, and checks the docked panel. The layout check's Enter is the first answer, so the later click-send waits on the second `page_context:` match.

`./bin/dev build` completed with 0 warnings and 0 errors. `ganda repo audit` passed 31 checks and failed 0.

### How to validate

**Smoke:**

```bash
cd tests/container-apps/web/web-spa-playwright-tests && dotnet test -c Release
```

Run that from the project directory. Its `global.json` selects Microsoft.Testing.Platform. `dotnet test` with a project path from the repo root uses the VSTest target and fails.

**Expect:**

- 7 passed, 0 failed: Ask surface, both ask-sign-in tests, both feedback-attachment tests, and FirstPaint.
- `AskSurface_Given_Wasm.CtrlK_Should_ShowAsk_And_TheKeyState` measures the inner textarea (left inset in [4, 20] px, width at least 90% of `.sc-ai-input`), Tag and send below it (Tag left, send right), growth after Shift+Enter, and Enter sending.
- `ask-composer-before.png` shows the placeholder starting at the left padding, the Tag pill on the footer left, and send on the footer right.
- `ask-composer-after.png` shows three lines of text with Tag and send still on the footer.
- Tag still opens `AskResourceEmpty` on the home page, and `@` insertion still replaces a typed `@`.

### Review disposition

- Effort 2, roster `general`, 2 rounds. Framework: `review/review-framework.md`. Last merged: `review/round-2/merged.md`. Disposition: `review/disposition.md`.
- Final counts: bug 2 fixed, suggestion 0, nit 1 fixed. 0 open, 0 wontfix. Outcome: **clean**.
- M1: the Tag pill CSS targeted `::part(control)`, which Fluent v5 `fluent-button` does not have. The host's 96px centered minimum indented the chip about 25px (Steven's steering note). Fixed by styling the host.
- M2: added the steering assertion that the chip is within 4px of the textarea text start.
- M3: the comment said 16px. The padding is 12px.
- After the fixes, `./bin/dev build` reported 0 warnings and 0 errors, and the Playwright project ran 7 passed and 0 failed. The `kanban/done/295-*`, `296-*`, and `302-*` screenshots the run rewrote were restored, not committed.
