# Shared TextArea component that fills its field and resizes, so pages cannot repeat the 302 narrow-textarea bug

## Problem

On 2026-10-12, Steve found the admin feedback detail page (`/Admin/Feedback/{id}`, task 314)
showing tiny, narrow "Reply to filer" and "Internal note" textareas. This is the same bug fixed for
`/Feedback` in task 302. The filer's permalink reply box (`FeedbackItemPage.razor`) has it too.

**Why it came back:**
- 302 fixed it **in page-scoped CSS** (`features/feedback/pages/FeedbackListPage.razor.css`,
  `.feedback-details ::deep fluent-textarea { --inline-size: 100%; --min-block-size: 9rem; … }`).
- The visible box is FluentUI's shadow `part="root"`. Its `--inline-size` defaults to 18rem, so
  `Width="100%"` and `block` on the host do not size it.
- Every new page that uses `FluentTextArea` has to remember that CSS. That is agreement by memory.

Affected uses today:
- `features/feedback/pages/FeedbackListPage.razor` (fixed by page CSS);
- `features/feedback/pages/FeedbackItemPage.razor` (broken);
- `features/admin/feedback/pages/FeedbackAdminItemPage.razor`, two of them (broken).

## Requirements

- **One shared component** in `web-spa/components/forms/`, for example `TextArea.razor` +
  `TextArea.razor.css`. It wraps `FluentTextArea` and always:
  - fills its container (`--inline-size: 100%` on the host, `display: block`, `min-width: 0`);
  - has a sensible default height via `--min-block-size` (9rem default, overridable by a
    parameter);
  - resizes vertically, with no fixed host height, because that fights the grip as 302 found.

  Pass-through parameters: `@bind-Value`, `Name`, `AriaLabel`, `Placeholder`, `data-qa` /
  additional attributes, and `Disabled`. CSS lives in the component's isolated `.razor.css`
  (`tw-blazor-css-strategy`; no `<style>` element). Purpose and Design regions explain the
  shadow-root sizing in one place.
- **Replace all four uses** with the component. Remove the now-redundant `fluent-textarea` rules
  from `FeedbackListPage.razor.css`. Keep `.feedback-details` as the paste host (295 / 303-001)
  wrapping the new component, and check paste still works.
- **Guard so it cannot recur.** Raw `<FluentTextArea` in web-spa razor outside the shared
  component fails the build or tests. Prefer a TWA analyzer rule in the same family as TWA0025
  (FluentMessageBar is allowed only in `MessageBars.razor`), with an opt-out attribute carrying a
  reason. A source-scan test is acceptable if an analyzer is disproportionate; record the choice.
  If it's an analyzer, give it the next free TWA id and add `AnalyzerReleases.Unshipped.md` and
  the AGENTS-style table entry where diagnostics are listed.
- **Playwright proof.** Generalize 302's `VisibleDetailsBox_Should_MatchTitle_And_Resize`
  measurement, which compares the shadow root box with the field width and drags the grip. Cover
  the admin reply and note boxes and the filer reply box at desktop and narrow widths. Screenshots
  go to test output (316).
- **Docs:** `skills/tw-blazor` (or `tw-blazor-css-strategy`) gets one line: use the shared
  TextArea; never use raw `FluentTextArea`.

## Checklist

- [ ] `components/forms/TextArea.razor` (+ isolated CSS) with fill/resize behaviour
- [ ] Four call sites migrated; page CSS cleaned; paste host still works
- [ ] Guard (analyzer preferred, or source-scan test) with recorded choice
- [ ] Playwright measurements for admin reply/note and filer reply
- [ ] Skill line
- [ ] Gates: `dev build` 0/0, `dev test` (Playwright), `dev template-smoke`, `ganda repo audit`

## Session

- Created: 2026-10-12 (cockpit, per Steve's screenshot of `/Admin/Feedback/{id}`)
