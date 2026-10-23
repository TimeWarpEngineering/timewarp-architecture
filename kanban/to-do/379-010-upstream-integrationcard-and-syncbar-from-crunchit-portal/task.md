# Upstream IntegrationCard and SyncBar from Crunchit portal

## Description

Port Crunchit's `IntegrationCard` and `SyncBar` into the template as one native, brand-neutral
component with two variants, so a generated app can show "this external system, its connection state, and what
you can do about it" without each page hand-rolling the row.

Crunchit's `IntegrationCard` (`components/IntegrationCard.razor`, `.razor.css`) works like this:

- Parameters: `Title` (required string), `Meta` (string), `Icon` (`RenderFragment`), `Status`
  (`BadgeStatus?`), `StatusLabel` (string), `Actions` (`RenderFragment`). It inherits a
  `DisplayComponent` base for class and attribute merging.
- Markup: one `<div>` root, flex row that wraps. An optional 40px icon tile, a body with the
  title (14px, 600) and the meta line (12.5px, muted), then, when `Status` is set, a
  `StatusBadge`, then an optional actions container (gap 8px).
- It has no events and no state. Pages put `Button`s in `Actions` and wire them to their own
  `OnClick` methods.
- Uses: the Clients list, the Dashboard financials block and the Staff page, all for one
  accounting system ("Last data pull 2 hours ago" and a "Sync now" button), plus Microsoft 365 on
  Staff (a multi-colour vendor logo `<svg>` with hex fills in the icon slot, a "Connected" badge
  when connected, "Sync now" when connected and "Connect" when not). The style guide has one
  sample.
- Defects: hex literals in the Staff logo, `crunchit-*` variables, pixel literals, no heading or
  list semantics, and the page-local `SyncQboAsync` / `SyncM365Async` handlers call services
  directly.

Crunchit's `SyncBar` (`components/SyncBar.razor`, `.razor.css`) works like this:

- Parameters: `Title`, `Meta` (strings), `Icon` and `Actions` (`RenderFragment`s), and
  `ChildContent`. It inherits `ParentComponent`.
- Markup: a thinner flex row (12.5px muted text, 10px 16px padding, 28px icon tile). The body
  renders `<strong>Title</strong> · Meta` and then `ChildContent`, so a page can write a rich
  sentence ("Pulled from X's QuickBooks · last data pull ... · 12 months shown"). Actions sit at
  the right.
- Its header comment says what separates it from the card: `IntegrationCard` is the identity
  banner (connection title, status badge, connect or disconnect), `SyncBar` is the pull-status
  row above data.
- Uses: one place, the client detail revenue view (rich `ChildContent` plus a "Sync now" ghost
  button). The style guide has two samples. The other sync rows in Crunchit use
  `IntegrationCard`, so in practice the two components overlap heavily.

Template today: there is neither. The nearest pieces are `StatusBadge`
(`components/elements/StatusBadge.razor`, `BadgeStatus` = Active, Inactive, Warning, Info; no
Error value) and `Card`. The admin AI page (`features/admin/ai/pages/AiPage.razor`) shows an
external integration's state as a plain `<p data-qa="XaiApiKeyStatus">` line ("Configured, ends in
...1234. Set ... by ...", "No key stored.", "The stored key cannot be read. Paste a new one.").
There is no sync, refresh or last-synced affordance anywhere in the template. COPIC (FluentUI
v4) has no integration card or sync bar; there is nothing to borrow there.

What we take: the icon tile, title, meta, status badge, actions-slot layout for the full banner,
and the thin pull-status row for `SyncBar`, folded in as the Compact variant.

What we drop:

- The Staff Microsoft 365 logo, its hex fills, and every vendor or client name.
- `DisplayComponent`, `ParentComponent`, `CssBuilder`, `crunchit-*` variables, pixel literals.
- The page-local `OnClick` handlers that call services. Both components stay leaves; the page
  puts its own `FluentButton` in the slot and the handler dispatches a State action.
- `SyncBar` as a separate component, and its rich body `ChildContent` (see Decision).

Follow the conventions of 379-001 through 379-009:

- A native root with a `twe-` class (`twe-integration-card`) and a variant modifier, as
  `EmptyState` does with `Variant` Full / Inline.
- A `Class` parameter and `[Parameter(CaptureUnmatchedValues = true)] AdditionalAttributes`
  splatted on the root, as `EmptyState` and `KpiCard` do.
- Isolated `.razor.css` with `--twe-*` tokens. No hex values, no `<style>`.
- Purpose and Design regions (TWA0004).
- Static `HtmlRenderer` Jaribu tests in
  `tests/container-apps/web/web-spa-integration-tests/components/`, modelled on
  `kpi-card-render-tests.cs`.
- The generated code contains no client name.

## Decision

- **One component with a Variant** (Steve, 2026-10-23): a single `IntegrationCard` with
  `Variant` of `IntegrationCardVariant.Full` (default) or `Compact`, matching how `EmptyState`
  uses `EmptyStateVariant` Full / Inline (`components/elements/EmptyState.razor`, its
  `empty-state-variant.cs`). There is no separate `SyncBar` component.
  - Full is the identity banner: paper card, icon tile (about 40px, from the spacing scale),
    title on its own line, meta below it, badge, actions.
  - Compact is the thin pull-status row that replaces `SyncBar`: smaller padding, a smaller
    icon tile, helper-size muted text, and title and meta on one line with an `aria-hidden`
    separator when both are set. It adds `twe-integration-card--compact` to the same root.
- **Parameters** (default chosen for this task): `Title` (required), `Meta`, `Variant`,
  `Icon` (FluentUI `Icon`, like `EmptyState`), `Status` (`BadgeStatus?`, reusing the existing
  enum; null shows no badge), `StatusLabel`, `ChildContent` (the actions slot, rendered only
  when passed), `Class`, `AdditionalAttributes`. No named `Actions` fragment: `ChildContent`
  matches `EmptyState`. Crunchit's rich `SyncBar` body `ChildContent` is dropped; a page that
  needs a sentence passes it as `Meta`.
- **Enum file** (default chosen): `components/elements/integration-card-variant.cs`, kebab
  name (TW0001), beside `empty-state-variant.cs`.
- **No refresh action inside the component** (default chosen for this task): it does not own a
  "Sync now" button or an `OnSync` callback. The page passes its own `FluentButton` whose
  handler dispatches a State action. The component formats no dates; callers pass a string.
- **Status reuses `BadgeStatus`** (default chosen for this task): no new enum, no `Error`
  value added. The AI page maps "configured" to Active, "needs replacement" to Warning, "no key"
  to Inactive.
- **Icon is a FluentUI `Icon`** (default chosen for this task), not a `RenderFragment`.
- **No JavaScript, no events, no `[DirectComponentSideEffect]`.**

## Adoption site

`IntegrationCard` Full: the admin AI page (`features/admin/ai/pages/AiPage.razor`). It is a real
external integration with a connection state and a destructive action. In the editable branch,
replace the `<p data-qa="XaiApiKeyStatus">@StatusLine</p>` line with an `IntegrationCard`
(`Title="xAI"`, `Meta` = the existing `StatusLine` text, `Status` from `XaiApiKeyState`:
Active when `Configured`, Warning when `NeedsReplacement`, Inactive otherwise;
`StatusLabel` "Configured" / "Needs a new key" / "No key"). Keep `data-qa="XaiApiKeyStatus"` on
the card root, and keep `XaiApiKeySave` and `XaiApiKeyClear` where they are so the existing
tests (`clear-xai-api-key-playwright-tests.cs`, `clear-xai-api-key-confirmation-tests.cs`) keep
passing. The "Managed by the host" and "host only" branches could use the card too, with Info
and Inactive, but leave them as they are to keep the page diff small.

`IntegrationCard` Compact: style guide only. Nothing in the template pulls data on a schedule or shows a
last-synced time, so any page adoption would be invented. A later task adds one where a real
sync appears.

## Checklist

- [ ] `components/elements/IntegrationCard.razor` and `.razor.css`.
  - Root `<div class="twe-integration-card @VariantClass @Class" @attributes="AdditionalAttributes">`.
    Icon tile (`aria-hidden`, `--twe-radius-sm`, `--twe-paper-2`, `--twe-muted`), body, then
    `StatusBadge` when `Status` is set, then the actions container when `ChildContent` is set.
  - Full: title (`--twe-text-body`, 600, `--twe-ink`) and meta (`--twe-text-helper`,
    `--twe-muted`) stacked. Title is a `<p>` or `<div>`, not a heading: the card sits under a
    page or section heading that owns the outline. Surface: `--twe-paper`, `--twe-radius`,
    `--twe-shadow-sm`, spacing from `--twe-space-*`, flex row with wrap and a body `min-width`.
  - Compact (`twe-integration-card--compact`): thinner padding, smaller tile, helper-size muted
    text, title in `--twe-ink-2` at 600 followed by an `aria-hidden` separator and the meta on
    one line, actions right-aligned and not shrinking.
  - Blank `Meta` is omitted. No hex values and no `<style>`.
- [ ] `components/elements/integration-card-variant.cs` with `Full` and `Compact`.
- [ ] Purpose and Design regions (TWA0004) stating the two variants, that neither wires events,
      and that the Compact row replaces a separate sync bar.
- [ ] AI page adoption (Full) as above, with `data-qa="XaiApiKeyStatus"` on the card root.
- [ ] Style guide: add an "Integration Cards" card (after "KPI Cards") with: Full connected with
      a status badge and two action buttons, Full warning with one action, Full with no status,
      Compact with title, meta and one button, and Compact with meta only. Buttons are plain
      `FluentButton`s. They carry no handler (the samples change nothing) or post an info note
      through the existing `ShowMessage`. Do not extend the StyleGuidePage contract (379-017).
- [ ] Add a row to `components/overview.md`.
- [ ] Render tests in `components/integration-card-render-tests.cs`. They pin only what the
      static render shows:
  - Title and meta render; blank meta is omitted.
  - No badge when `Status` is null; the badge and its label render when set.
  - The actions container appears only when `ChildContent` is passed.
  - The icon tile is `aria-hidden` and appears only when `Icon` is set.
  - Full is the default and has no compact modifier; Compact adds
    `twe-integration-card--compact`.
  - Compact shows the `aria-hidden` separator only when both title and meta are set.
  - Root carries `twe-integration-card`, `Class` merges and does not replace it, `data-qa`
    passes through, no `<style` in the output, and a script-bearing title stays encoded (raw
    markup, as `kpi-card-render-tests.cs` does).
- [ ] No new Playwright test: the component has no interaction of its own.
- [ ] Gates:
  - `./bin/dev build` reports 0 warnings and 0 errors.
  - `./bin/dev template-smoke` passes.
  - `ganda repo audit` passes.
  - `git diff origin/master -- source tests | rg -i "crunchit|copic"` prints nothing.

## Accessibility

- The component adds no landmark or heading, in either variant. The title is not a heading,
  so the outline stays with the page and section headings.
- The status badge carries its meaning as text (the label), not only as colour. The dot is
  decorative. `Status` without a `StatusLabel` shows the enum name, so pages should pass a label
  that says what the state means ("Needs a new key").
- The icon tile is decorative (`aria-hidden`). Meaning is in the title and meta.
- The Compact separator is `aria-hidden` so a screen reader does not read "middle dot".
- Outcome of an action in the slot ("Synced", "Sync failed") goes through the shell's
  notification region (`NotificationState`, TWA0025), not a live region inside these components.
  The component is static; a change of `Meta` text is not announced.
- Action buttons are `FluentButton`s supplied by the page, so focus, keyboard and disabled
  state are the library's. Wrapped layout must keep reading order (icon, text, badge, actions).
- Contrast: the badge colours come from `StatusBadge`. This task does not change them; note
  in the Results if the warning pair fails 4.5:1.
- Render tests pin markup only (roles, attributes, slot presence). They cannot see layout,
  wrap behaviour, or contrast.

## Session

- Created: ses_ebc1f6587ffeC5oxDhO3SRDeMu (2026-10-16)
- Spec drafted: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-23)

## Notes

- Crunchit source is under
  `/home/steve/worktrees/github.com/Crunchitfs/crunchit/master/source/container-apps/web/projects/web-spa/`:
  - `components/IntegrationCard.razor(.css)` and `components/SyncBar.razor(.css)` (the latter
    becomes the Compact variant).
  - Usages: `features/clients/pages/ClientsPage.razor`, `features/dashboard/pages/DashboardPage.razor`,
    `features/staff/pages/StaffPage.razor` (card); `features/clients/pages/ClientDetailPage.razor`
    (the bar); both in `features/style-guide/pages/StyleGuidePage.razor`.
- COPIC has no equivalent; checked, nothing to port.
- Template targets:
  - `source/container-apps/web/projects/web-spa/components/elements/` (new files, plus
    `StatusBadge.razor` and `badge-status.cs` for reuse).
  - `features/admin/ai/pages/AiPage.razor`
  - `features/style-guide/pages/StyleGuidePage.razor`
  - `components/overview.md`
- FluentUI v5 (5.0.1): the only library types used are `Icon`, `FluentIcon` (as in
  `EmptyState`) and `FluentButton` in samples. No new library component is needed; the
  implementer confirms the icon names against the Icons package.
- Boundaries: do not touch `StatusBadge` styles, the shell (379-015), or the StyleGuidePage
  contract (379-017). Adding `BadgeStatus.Error` is out of scope; open a follow-up if a page
  needs it.
- Parent 379 stays in-progress until all its children are done.
