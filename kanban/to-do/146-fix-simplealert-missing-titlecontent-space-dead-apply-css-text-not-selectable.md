# Fix SimpleAlert: missing title/content space, dead @apply CSS, text not selectable

## Description

Bug report from the Login page (`/account/login`): when a danger `SimpleAlert` fires (e.g. a
failed passkey registration), it renders two UX defects, both visible in the rendered DOM:

```html
<div class="fluent-card simple-alert__card" role="alert">
  <fluent-label class="simple-alert__label" typo="Typography.Body">
    <span class="simple-alert__title">Error</span>
    <span class="simple-alert__content">Passkey registration verification failed: Verification failed: OriginMismatch.</span>
  </fluent-label>
</div>
```

1. **Missing space between title and content** — renders as
   `ErrorPasskey registration verification failed: …` instead of `Error: Passkey registration…`.
2. **Alert text cannot be selected; hover shows a pointer cursor** — users cannot copy the error
   message out of the alert.

## Repro

- `dev run`, open the Login page, trigger a passkey registration failure (e.g. origin mismatch)
  → the danger alert shows the concatenated text; hovering shows a pointer cursor and
  drag-to-select does not work.
- Also visible anywhere `SimpleAlert` renders with `Title` + content:
  - `web-spa/features/account/pages/login-page/LoginPage.razor` (lines 48, 53)
  - `web-spa/features/identity/pages/passkeys-page/PasskeysPage.razor` (lines 37, 42)
  - `web-spa/features/event-stream/pages/EventStreamPage.razor` (line 14)
  - `web-spa/features/developer/pages/AlertExamplePage.razor` (all variants)

## Root-cause leads

- **Spacing:** `source/container-apps/web/projects/web-spa/components/elements/SimpleAlert.razor`
  lines 7–8 render `<span class="simple-alert__title">` and
  `<span class="simple-alert__content">` on adjacent lines; Blazor collapses the inter-element
  newline, so no whitespace is emitted. Needs an explicit separator (space/colon), a CSS
  `margin-inline-start` on `__content`, or a block layout for content.
- **Dead CSS:** `SimpleAlert.razor.css` is written entirely with Tailwind `@apply` directives,
  but the repo has **no Tailwind/PostCSS pipeline** (see `blazor-css-strategy` skill: FluentUI v5
  + plain CSS tokens, Tailwind removed). Every rule in the file is invalid and silently dropped
  by the browser — including the intended `block sm:inline` on `__content` and all the
  color/padding/border styles. Rewrite the file in plain CSS (design tokens from
  `wwwroot/css/tokens.css` where applicable).
- **Pointer cursor / no selection:** investigate `cursor` / `user-select` on the `<fluent-label>`
  custom element (Fluent UI web-component shadow styles) and ancestors. Alert content should be
  selectable (`user-select: text`, `cursor: text` or `default`) — error text must be copyable.

## Checklist

- [ ] Title and content visually separated on all SimpleAlert variants and consumers
- [ ] Alert text selectable with the mouse and copyable
- [ ] `SimpleAlert.razor.css` rewritten in plain CSS (no `@apply`); variant colors/padding/borders
      visually verified against AlertExamplePage (success/danger/warning/info/custom)
- [ ] Check `AlertExamplePage.razor.css` and other component CSS files for the same dead-`@apply`
      problem; fix or file follow-ups
- [ ] `dev build` 0/0

## Notes

- The example error message originates from
  `source/container-apps/web/features/identity/identity-problems-application.cs` (line 57).
- `AlertExamplePage.razor.css` also uses Tailwind-style classes in selectors (`::deep`) — verify
  whether it has the same dead-CSS issue.

## Session

- Created: opencode (2026-08-04)
