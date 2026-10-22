# Upstream EmptyState, full and inline, from Crunchit portal

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

- [ ] `components/elements/EmptyState.razor` and `.razor.css`. It goes under `elements/` because it
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
- [ ] `components/elements/empty-state-variant.cs` for the enum (kebab file name, TW0001).
- [ ] CSS in the isolated stylesheet:
  - Full: centred column, `--twe-paper`, `--twe-radius`, `--twe-shadow-sm`, padding from
    `--twe-space-*`, title in `--twe-ink` at 600, description in `--twe-muted` and
    `--twe-text-body`, max width on the description.
  - Inline: no background or shadow, smaller padding, left-aligned or centred as chosen, text
    sizes from `--twe-text-*`.
  - Add tokens to `tokens.css` only if needed (for example an icon-tile size). No hex values.
- [ ] Adopt in `features/admin/feedback/pages/FeedbackAdminListPage.razor`: replace the grid's
      `<EmptyContent>` paragraph with `<EmptyState Variant="Inline" Title="No feedback matches
      this filter" Description="…" data-qa="AdminFeedbackEmpty" />`. The `data-qa` value and
      its place inside `EmptyContent` stay, so any existing test selector still resolves.
- [ ] Adopt in `features/feedback/pages/FeedbackListPage.razor`: replace "No feedback filed
      yet." in the "Your filings" `Section` with the inline form (the Section is already a
      Card). Keep the `Items.Count == 0` branch.
- [ ] Leave `PrincipalsPage` and `FeedbackAdminItemPage` alone in this task, and say in Notes
      that they are follow-ups on the same pattern. (Choosing one adopter is acceptable if
      the second grows the diff; the first is required.)
- [ ] Style guide: add an "Empty States" card (after "Avatars") showing the full form with
      description, the full form with an action, and the inline form, using callbacks that do
      not write local fields. Do not extend the StyleGuidePage contract (379-017).
- [ ] Add a row to `components/overview.md`.
- [ ] Render tests in `components/empty-state-render-tests.cs`:
  - The root carries `twe-empty-state` and the title text.
  - The default variant has no inline modifier; `Inline` adds it.
  - Description and the actions row are omitted when unset, and present when set.
  - `Class` and the additional attributes are merged on the root.
  - The heading level follows `HeadingLevel`, if kept.
  - The icon, when shown, is `aria-hidden`.
- [ ] Gates:
  - `./bin/dev build` reports 0 warnings and 0 errors.
  - `./bin/dev template-smoke` passes.
  - `ganda repo audit` passes.
  - `git diff origin/master -- source tests | rg -i crunchit` prints nothing.

## Session

- Spec drafted: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-22)

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
