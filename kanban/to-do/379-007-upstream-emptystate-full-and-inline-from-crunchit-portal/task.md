# Upstream EmptyState, full and inline, from Crunchit portal

## Goal (implementer restatement)

Every generated app should say "nothing here" the same way. One native `EmptyState` element, brand-neutral, has a full paper card and a compact inline form for use inside a card or grid. The full form is a centred column: an optional FluentUI icon (hidden from assistive tech, omitted entirely when inline), a heading whose level follows the page, an optional description, and an optional actions slot the page fills with its own button. The inline form drops the paper, shadow, and icon, and uses a paragraph for the title so it does not add a second heading inside a section. There is no phase chip; a page that wants one puts it in the actions slot.

The root keeps `role="status"`. The Design region says that inserting that region together with its text is announced inconsistently, and that a page which needs a guaranteed announcement keeps its own persistent live region (for example an always-rendered result count). It does not claim that a filter-to-empty transition is announced.

Wire the inline form into the admin feedback grid's empty content (same `data-qa`, still inside `EmptyContent`) and the "Your filings" empty branch. Those titles have no trailing period. Leave the principals grid and the admin item activity line for a later task on this same pattern. Show both forms on the style guide. The two full samples use heading level 3, because the card title is already an `h2`. The sample button dispatches an existing notification action. Render tests pin the root class, variant modifier, optional description and actions, attribute passthrough, heading level, and the hidden icon. `dev build` is 0 warnings and 0 errors, `dev template-smoke` passes, `ganda repo audit` passes, and the source and test diff contains no client name.

## Description

Port Crunchit's `EmptyState` into the template as a native, brand-neutral element, so every
list or panel that has nothing to show says so the same way.

Crunchit has one component, not two. It is a centred paper card (`crunchit-empty`) with an icon
tile, an `<h2>` title, an optional description `<p>`, and an optional "phase" chip. Parameters:
`Title` (required), `Description`, `Phase`. The icon is a hard-coded Blazicon. There is no
action slot and no inline form. Crunchit uses it on seven pages in three ways:

- Load failure: `Title="Could not load …" Description="@LoadError"` (dashboard, clients,
  staff, client detail).
- Stand-alone empty list: "No staff yet", "No clients match these filters".
- Nested inside a `Card` next to a button: "No locations selected" with a "Select all
  locations" button placed by the page in a `cd-empty-actions` row.

The "inline" form named in this task is the nested case. Crunchit draws the full card chrome
(shadow, 56px padding) inside another card, which is why this task adds a compact variant
with no chrome. The page-owned button rows (`cd-empty-actions`, `clients-empty-actions`) show
that an action slot is wanted.

Defects to fix in the port:

- `<h2>` is fixed, so the heading level cannot follow the page outline.
- The icon is hard-coded and uses Blazicons, which the template does not use.
- The "phase" chip is a one-off for a "coming soon" screen.
- The card is empty text with no live-region or role semantics. An empty list that appears
  after a filter changes should be announced.

The template has no `EmptyState` today. Empty lists are hand-written `<p>` text:

- `features/admin/feedback/pages/FeedbackAdminListPage.razor`: `<EmptyContent>` of the grid,
  `<p data-qa="AdminFeedbackEmpty">No feedback matches this filter.</p>`.
- `features/feedback/pages/FeedbackListPage.razor`: `<p>No feedback filed yet.</p>` inside the
  "Your filings" `Section`.
- `features/admin/principals/pages/PrincipalsPage.razor`: grid `EmptyContent`,
  `data-qa="AdminPrincipalsEmpty"`.
- `features/admin/feedback/pages/FeedbackAdminItemPage.razor`: `<p>No activity yet.</p>`.

The generated code contains no client name.

Follow the conventions of 379-001 through 379-006:

- A native root with a `twe-` class.
- A `Class` parameter and `CaptureUnmatchedValues` `AdditionalAttributes`, as `Card.razor` does.
- No `DisplayComponent` or `CssBuilder`.
- Isolated `.razor.css` with `--twe-*` tokens from `wwwroot/css/tokens.css`. No hex values, no
  `<style>`.
- Purpose and Design regions (TWA0004).
- Static `HtmlRenderer` Jaribu tests in
  `tests/container-apps/web/web-spa-integration-tests/components/`, modelled on
  `avatar-render-tests.cs` and `kpi-card-render-tests.cs`.
- The component exposes at most `EventCallback`s. A page handler only dispatches
  TimeWarp.State actions (`tw-blazor`).

## Decision

- **One component with a variant** (Steve, 2026-10-22): a single `EmptyState` with `Variant`
  (`EmptyStateVariant.Full` default, `Inline`). The modifier class `twe-empty-state--inline`
  drops the paper, shadow and large padding. Not two components.
- **Action slot** (default chosen for this task): `ChildContent` `RenderFragment`, rendered in an
  actions row under the text. The page passes its own `FluentButton`, so label, intent,
  `data-qa` and any `AuthorizeView` stay with the page (the `FilterBar` precedent).
- **Icon** (default chosen for this task): an optional `Icon` parameter of the FluentUI icon
  type, defaulting to a neutral regular-size icon picked with the fluent-ui-blazor MCP, rendered
  `aria-hidden`. `Inline` never shows the icon.
- **Phase chip** (default chosen for this task): dropped. A page that wants a chip passes it in
  `ChildContent`. Say so in Design.

## Checklist

- [x] `components/elements/EmptyState.razor` and `.razor.css`. It goes under `elements/` because it
      holds no child components of its own and no state, as `Card` and `Avatar` do.
  - Root: `<div class="twe-empty-state @VariantClass @Class" role="status">`, with
    unmatched attributes splatted on it. Confirm in Design whether `role="status"` is right
    for a list that is empty at first paint; if it is noisy, use no role and leave the live
    region to the page.
  - Parameters: `Title` (required),
    `Description`, `Variant` (default `Full`), `Icon`, `HeadingLevel` (2 to 6, default 2) or a
    non-heading `<p class="twe-empty-state__title">` for `Inline`; `ChildContent`; `Class`;
    `AdditionalAttributes`.
  - Description and actions render only when set.
- [x] `components/elements/empty-state-variant.cs` for the enum (kebab file name, TW0001).
- [x] CSS in the isolated stylesheet:
  - Full: centred column, `--twe-paper`, `--twe-radius`, `--twe-shadow-sm`, padding from
    `--twe-space-*`, title in `--twe-ink` at 600, description in `--twe-muted` and
    `--twe-text-body`, max width on the description.
  - Inline: no background or shadow, smaller padding, left-aligned or centred as chosen, text
    sizes from `--twe-text-*`.
  - Add tokens to `tokens.css` only if needed (for example an icon-tile size). No hex values.
- [x] Adopt in `features/admin/feedback/pages/FeedbackAdminListPage.razor`: replace the grid's
      `<EmptyContent>` paragraph with `<EmptyState Variant="Inline" Title="No feedback matches
      this filter" Description="…" data-qa="AdminFeedbackEmpty" />`. The `data-qa` value and
      its place inside `EmptyContent` stay, so any existing test selector still resolves.
- [x] Adopt in `features/feedback/pages/FeedbackListPage.razor`: replace "No feedback filed
      yet." in the "Your filings" `Section` with the inline form (the Section is already a
      Card). Keep the `Items.Count == 0` branch.
- [x] Leave `PrincipalsPage` and `FeedbackAdminItemPage` alone in this task, and say in Notes
      that they are follow-ups on the same pattern. (Choosing one adopter is acceptable if
      the second grows the diff; the first is required.)
- [x] Style guide: add an "Empty States" card (after "Avatars") showing the full form with
      description, the full form with an action, and the inline form, using callbacks that do
      not write local fields. Do not extend the StyleGuidePage contract (379-017).
- [x] Add a row to `components/overview.md`.
- [x] Render tests in `components/empty-state-render-tests.cs`:
  - The root carries `twe-empty-state` and the title text.
  - The default variant has no inline modifier; `Inline` adds it.
  - Description and the actions row are omitted when unset, and present when set.
  - `Class` and the additional attributes are merged on the root.
  - The heading level follows `HeadingLevel`, if kept.
  - The icon, when shown, is `aria-hidden`.
- [x] Gates:
  - `./bin/dev build` reports 0 warnings and 0 errors.
  - `./bin/dev template-smoke` passes.
  - `ganda repo audit` passes.
  - `git diff origin/master -- source tests | rg -i crunchit` prints nothing.

## Session

- Spec drafted: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-22)
- Implement oracle: implemented by implementer-cursor (cursor, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 500), Done — 2026-10-22T17:32:22Z
- Review oracle round 1: general reviewer (claude-opus-5-5), effort 2, session not reported. 3 open (1 suggestion, 2 nits), 0 bugs; fix pass requested for M1–M3 (2026-10-23)
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-22T17:38:02Z
- Fix pass (implement-fix-2): review round 1 M1–M3. The Design region no longer claims that inserting the status region, or a filter that empties a list, is announced. The two style-guide Full samples use heading level 3. The adopter titles drop the trailing period. grok-4.7, 2026-10-23.

## Review

- Round 1: Blocked — open M1, M2, M3
- Fix pass implemented M1–M3 on this id. Round 2 verifies.

## Notes

- Crunchit source is under
  `/home/steve/worktrees/github.com/Crunchitfs/crunchit/master/source/container-apps/web/projects/web-spa/`:
  - `components/EmptyState.razor(.css)`: the only component; no inline variant exists.
  - Usages: `features/staff/pages/StaffPage.razor`, `features/clients/pages/ClientsPage.razor`,
    `ClientDetailPage.razor`, `features/dashboard/pages/DashboardPage.razor`, and the style
    guide.
- Template targets:
  - `source/container-apps/web/projects/web-spa/components/elements/`
  - `features/style-guide/pages/StyleGuidePage.razor`
  - `wwwroot/css/tokens.css`
- The load-failure use ("Could not load …") is not ported here. The template reports failures
  through the shell notification region (TWA0025).
- Out of scope: the shell user card (379-015), the Drawer (379-008), and 379-017.
- Follow-up on the same pattern, left untouched: `PrincipalsPage` grid empty content
  (`data-qa="AdminPrincipalsEmpty"`) and `FeedbackAdminItemPage` ("No activity yet.").
- Inline is left-aligned. Full keeps `role="status"`. A status region already in the first
  paint is not announced. Inserting the region together with its text is announced
  inconsistently; a page that needs a guaranteed announcement keeps its own persistent
  live region. No token was added. The default icon is Fluent UI regular 24px
  `CollectionsEmpty` (the fluent-ui-blazor MCP was not in this session).

## Results

One `EmptyState` under `components/elements`. Full is a centred paper card (`twe-empty-state`) with `role="status"`, a regular 24px Fluent `CollectionsEmpty` icon in an `aria-hidden` tile, a heading at `HeadingLevel` (2–6, otherwise h2), an optional description, and an actions slot. Inline adds `twe-empty-state--inline`: no paper, shadow, or icon, left-aligned, and a paragraph title so a nested section does not gain a second heading. Description and actions render only when set. A page that wants a phase chip puts it in `ChildContent`. No new tokens: the tile is `--twe-space-12`, and the description measure is 32rem in the component stylesheet.

Adopted on the admin feedback grid empty content (`data-qa="AdminFeedbackEmpty"` stays inside `EmptyContent`) and the "Your filings" empty branch. Those titles have no trailing period. The style guide shows the full form, the full form with a button that dispatches the existing notification action, and the inline form. Both full samples use heading level 3 under the card's `h2`. `PrincipalsPage` and `FeedbackAdminItemPage` are unchanged.

The Design region keeps `role="status"` and records the limit: a change of text inside a live region that is already on the page is what screen readers reliably announce. Inserting the region with its text is announced inconsistently. A page that needs a guaranteed announcement keeps its own persistent live region.

Markup is pinned by seven HtmlRenderer tests. The style guide and the two feedback pages were not opened in a browser. The heading-level test already renders `h3` for `HeadingLevel` 3, and the sample markup now passes that value.

### How to validate

**Smoke**

```bash
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release --no-build -- --filter-class EmptyState_Should_
```

**Expect:** 7 passed, 0 failed. The root carries `twe-empty-state` and the title. The default variant has no inline modifier. `Inline` adds `twe-empty-state--inline`, uses a paragraph title, and renders no icon. Description and `twe-empty-state__actions` are absent until set. `Class` and `data-qa` land on the root. `HeadingLevel` 3 renders `h3`. The icon tile is `aria-hidden="true"` and the root is not.

**Automated gate**

```bash
./bin/dev build
./bin/dev template-smoke
ganda repo audit
git diff origin/master -- source tests | rg -i crunchit
```

Re-ran on 2026-10-23 after the fix pass. `dev build`: 0 warnings, 0 errors. `dev template-smoke`: `Template smoke SUCCEEDED` (SmokeDefault). `ganda repo audit`: Passed 32, Failed 0, Skipped 1. The client-name search prints nothing. `EmptyState_Should_`: 7 passed, 0 failed.

### Review

Round 1 ran with effort 2 and a single general reviewer. It found 0 bugs, 1 suggestion, and 2 nits. The fix pass implemented all three. Round 2 has not verified them yet.

- **M1:** the Design region now says insertion is announced inconsistently, and that a guaranteed announcement belongs to a persistent live region the page already renders. `role="status"` stays.
- **M2:** the two Full samples on the style guide pass `HeadingLevel="3"`.
- **M3:** the admin filter title and the "Your filings" title no longer end in a period.

Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.
