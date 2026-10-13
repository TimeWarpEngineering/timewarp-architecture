# Ask Tag: pick an element on the page for the AI to consider

## Why (Steve + cockpit, 2026-10-13)

"Tag" implies **selecting an element on the screen** that the AI should consider, not a resource
reference. Resource references are task **345** (`@prefix:id`, site-wide, backed by list actions).
This task gives the Ask panel's **Tag** button that meaning.

**Depends on 345**, which removes or parks the old Tag menu.

## Behaviour

- **Tag** enters **pick mode**: an overlay that highlights the element under the pointer (outline
  plus label) and also works by keyboard (Tab / arrow to move between candidates, ↵ to choose, esc to
  cancel). Choosing an element adds a **context chip** to the composer ("Feedback row: Dark theme
  broken", "Error bar: OriginMismatch", "Card: Change status") that can be removed before sending.
- **What is attached:** a compact, semantic description, never raw DOM dumps:
  - the component or region kind and its visible text, trimmed and capped;
  - its `data-qa` / landmark;
  - the page route;
  - **if the element represents a referenceable resource**, the matching `@prefix:id` from 345 (for
    example a feedback grid row resolves to its `FeedbackItemId`), so a picked row is as precise as
    an `@` reference.

  Optionally attach a cropped screenshot of the element. Coordinate with task 303-001 (web-app
  self-screenshot) and reuse its capture rather than adding a second one.
- **What is pickable:** elements the app marks as meaningful (cards, grid rows, notification bars,
  form fields, sections), not every `<div>`. Decide the marking mechanism and record it. Options:
  - existing `data-qa`;
  - a small attribute or component parameter on shared components (Card, Section, MessageBars,
    FluentDataGrid rows);
  - generated from the page agent context.

  Prefer something enforced or generated over convention by memory.
- **Privacy and safety:** only what is visible to the user on their own page. Hidden or redacted
  fields, and anything behind `AuthorizeView` the user can't see, are never attached. The chip shows
  exactly what will be sent.
- Every interaction is a TimeWarp.State action. Any JS (pointer tracking, overlay) lives in a typed
  `[SideEffectService]` module invoked from handlers (TWA0026). Styling follows
  `tw-blazor-css-strategy`.

## Tests

- State: enter and cancel pick mode; choose; chip added and removed; payload contents.
- Resource mapping: picking a feedback row yields its `@feedback:<id>` (345 prefix).
- Privacy: an element inside an unauthorized region is not pickable or attachable.
- Playwright: Tag → hover highlights → click a row → chip shows the row label → send includes the
  semantic payload.

## Checklist

- [x] Pickable-element marking mechanism (recorded; enforced or generated)
- [x] Pick mode overlay (pointer + keyboard), chip in composer
- [x] Semantic payload incl. 345 resource mapping; optional screenshot via 303-001 (deferred: 303-001 not landed)
- [x] Privacy rules + tests
- [x] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`

## Session

- Created: 2026-10-13 (cockpit, from a design conversation with Steve; after 345)
- 2026-10-13 implementer (ganda task work, Claude): implemented; decisions and validation below.

## Decisions

- **Marking mechanism: emitted by shared components, prefixes from the contract.** An element is
  pickable only when it carries `data-ask-tag` (`AskTagMark`, `components/ask-tag-mark.cs`).
  Pages do not write it by hand: `Card` marks itself (so every `Section` is pickable) with its
  title, `MessageBars` marks each bar ("Error bar", "Warning bar", …) with its title, and a
  labelled `FormField` marks itself as "Field". Rows that stand for a referenceable resource use
  `AskTagMark.Resource(kind, prefix, id, label)`, and the prefix is the contract's own constant
  (`ListFeedback.ReferencePrefix`, `ListMyFeedback.ReferencePrefix`), which the
  `[ReferenceableResource]` attribute now also uses, so the mark and the 345 registry cannot drift.
  The state re-checks every reference against the generated `ResourceReferenceRegistry`.
- **Sending:** the library `MessageInput` has no attachment hook, so `AskTagChatClient` (a
  `DelegatingChatClient` in front of the tool-invoking client) appends `AskTagPayload.Message` to a
  copy of the outgoing user message and dispatches `MarkSent`. The transcript keeps what the person
  typed; `page_context.tags` lists sent tags for later turns.
- **JS:** `ask-tag.ts` through the `[SideEffectService]` `AskTagJsModule`, called only from the
  `StartPick` / `CancelPick` handlers (TWA0026). `StartPick` is `[TrackAction]`; the panel reads
  ActionTracking to show pick mode and Cancel.
- **Privacy:** only marked elements inside `.twe-page__body` that are rendered and visible;
  `data-ask-tag-private`, `[hidden]`, `[aria-hidden=true]`, `[inert]` and the Ask panel are never
  candidates; text is rendered text nodes only (input values are never read). AuthorizeView
  content the user cannot see is not rendered, so it has no mark. A reference to a type the user
  cannot list is dropped. Chips are per conversation and per principal (SessionChanged).
- **Screenshot:** not added. 303-001 (self-screenshot) has not landed; when it does, its capture
  can fill the reserved `screenshot` slot. No second capture path was built here.

## Depends on

- 345

## Results

Tag in the Ask composer's lead slot enters pick mode: an outline plus chip-text badge follows the
pointer over marked elements; Tab / arrows move, Enter chooses, Esc cancels; a click picks without
following links. The chosen element becomes a removable chip above the composer whose disclosure
lists every field that will be sent (kind, label, text, page, landmark, data-qa, reference). The
next message carries the semantic JSON payload; the chips then move to `page_context.tags`. A
feedback row resolves to its `@feedback:<id>` (admin grid) or `@my-feedback:<id>` (own filings).

New: `AskTagMark`, `AskTagState` (StartPick, CancelPick, Choose, Remove, MarkSent, SessionChanged),
`AskTag`, `AskTagCandidate`, `AskTagPayload`, `AskTagChatClient`, `AskTagJsModule`, `ask-tag.ts`,
`AskTagChips`. Changed: Card, FormField, MessageBars, both feedback list pages, AgentAsk,
page_context (`tags`), Ask instructions, the two feedback list contracts (prefix constants),
AuthenticationStateListener, app.css (overlay styles).

Tests: `tests/container-apps/web/web-spa-integration-tests/features/application/ask-tag-tests.cs`
(7: pick + cancel, chips add/remove/cap/per-conversation, payload + MarkSent + page_context.tags,
feedback-row resource mapping, privacy for references and principals, AuthorizeView render
privacy). Playwright: `ask-sign-in-playwright-tests.cs` (file feedback → Tag → Esc → Tag → hover
row → click → chip → send → page_context.tags) and `ask-surface-playwright-tests.cs` (Tag button
placement).

Gates (2026-10-13, this worktree): `dev build` 0 warnings / 0 errors; `dev test`: every project
passed except the new web-spa-playwright Tag step on its first run (the filings list loads on page
open, so the test now waits for the receipt and reloads /Feedback); after that fix
web-spa-playwright-tests 11/11 and web-spa-integration-tests Unit 114/114; `dev template-smoke`
succeeded; `dev check-version` safe (2.0.0-beta.20 > nuget beta.19); `ganda repo audit` 32/32.

### How to validate

**Smoke:**

```bash
dev build
cd tests/container-apps/web/web-spa-integration-tests && dotnet run -- --filter-class AskTag_
cd tests/container-apps/web/web-spa-playwright-tests && dotnet run -- --filter-class AskSignIn_Given_Wasm
```

**Expect:** `dev build` reports 0 warnings and 0 errors. `AskTag_` reports 7 passed, 0 failed.
The Playwright sign-in run passes and writes `ask-tag-hover-row.png` (outline + "Feedback row:
Dark theme broken" badge over the filing) and `ask-tag-chip.png` (chip in the composer) to test
output. Manually: `dev run`, sign in, file a feedback item on /Feedback, open Ask, click **Tag**,
hover the filing (purple outline + badge), click it: a chip "Feedback row: <title>" appears and
opening it shows `@my-feedback:<id>`; send "What is this?" and the chip clears.
