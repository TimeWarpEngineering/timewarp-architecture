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

- [ ] Pickable-element marking mechanism (recorded; enforced or generated)
- [ ] Pick mode overlay (pointer + keyboard), chip in composer
- [ ] Semantic payload incl. 345 resource mapping; optional screenshot via 303-001
- [ ] Privacy rules + tests
- [ ] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`

## Session

- Created: 2026-10-13 (cockpit, from a design conversation with Steve; after 345)
