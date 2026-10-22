# Upstream KpiCard and strip layout from Crunchit portal

## Description

Crunchit's `KpiCard` is a stat tile. It has:

- an uppercase eyebrow label
- a large metric with an optional unit
- an optional second metric and unit, for the dual variant ("0 CLIENTS | 0 LOCATIONS")
- an optional caption

Callers pass pre-formatted strings. The card has no formatting, delta, or trend logic.

The "strip" is not a component in Crunchit. Five page-local CSS classes each repeat a grid,
`repeat(auto-fit, minmax(Npx, 1fr))`, with `N` anywhere from 140 to 180 and a margin that varies.
One page tones cards with caller `::deep` box-shadow CSS.

This task brings both into the template:

- `KpiCard` goes in `components/elements/`.
- `KpiStrip` goes in `components/composites/` and owns the responsive grid, so no page repeats
  it.

Both use `@namespace TimeWarp.Architecture.Components`. Follow the conventions 379-001
established for `SectionHeader`:

- a native root element
- `CaptureUnmatchedValues` `AdditionalAttributes`
- isolated CSS
- `--twe-*` tokens
- static `HtmlRenderer` render tests

The generated code carries no client name. That covers copy, comments, sample data, and class
names.

There is no honest adopting page in the template today. Analytics has no page, and the feedback,
admin, counter, and todo pages show no aggregate tiles. The style guide is the only consumer.

## Checklist

- [ ] `KpiCard.razor` with a `.razor.css`, root `div.twe-kpi`.
  - Takes `AdditionalAttributes`.
  - Does not use Crunchit's `DisplayComponent` or `CssBuilder`.
- [ ] `KpiCard` parameters, all strings and pre-formatted:
  - `Label` and `Value` (`EditorRequired`).
  - `Unit`, `SecondaryValue`, `SecondaryUnit`, `Caption`. These render only when they are not
    blank.
  - No `EventCallback`. Any interaction from a caller dispatches a TimeWarp.State action.
- [ ] Tone:
  - Add a `KpiTone` enum (`None`, `Positive`, `Danger`) as the `Tone` parameter.
  - It renders the modifier class `twe-kpi--positive` or `twe-kpi--danger`, styled in the card's
    own CSS.
  - It replaces Crunchit's caller `::deep` box-shadow.
  - It is a border or accent only, not a pill.
- [ ] `KpiStrip.razor` with a `.razor.css`.
  - Root `div.twe-kpi-strip`, with `ChildContent` and `AdditionalAttributes`.
  - The grid is `repeat(auto-fit, minmax(var(--twe-kpi-min, 160px), 1fr))`, with
    `gap: var(--twe-space-3)` and `min-width: 0` on the children.
  - One 160px minimum replaces Crunchit's 140–180px spread. This is a deliberate normalisation.
  - It reflows to fewer columns on narrow screens without media queries.
  - Spacing comes from `--twe-space-*`, not page-local margins.
- [ ] Use CSS isolation only: no `<style>` element and no Tailwind (`tw-blazor-css-strategy`).
      There are no FluentUI children, so no `::deep` is needed.
- [ ] Tokens in `wwwroot/css/tokens.css`:
  - Map Crunchit tokens to their existing `--twe-*` equivalents: paper, rule, radius, shadow-sm,
    muted, ink, text-eyebrow, text-helper. Success maps to `--twe-positive` and danger to
    `--twe-danger`.
  - Add `--twe-text-kpi-value` (30px) and `--twe-kpi-pad` only if nothing existing fits. Check
    `tokens.css` first, and add a spacing step only if the padding needs one.
- [ ] Add Purpose and Design regions (TWA0004) to both `.razor` files, in the style of
      `SectionHeader.razor`.
- [ ] Style guide (`features/style-guide/pages/StyleGuidePage.razor`):
  - Replace the hand-rolled `.sg-kpis` demo with a "KPI Cards" `Card`.
  - The card shows a `KpiStrip` of four cards: single, dual-metric, caption, and tone.
  - Do not extend the StyleGuidePage contract; that is 379-017.
- [ ] Render tests in `tests/container-apps/web/web-spa-integration-tests/components/`, in
      `kpi-card-render-tests.cs` and `kpi-strip-render-tests.cs`, modelled on
      `section-header-render-tests.cs`. Cases:
  - Label and value render.
  - Unit is absent when null and present when set.
  - With no `SecondaryValue` there is no secondary block; with one there are two
    `.twe-kpi__metric` blocks.
  - Caption is optional.
  - Tone `None` adds no modifier; `Positive` and `Danger` add theirs.
  - Unmatched attributes (`data-testid`, `class`) land on the root.
  - The strip renders `ChildContent` inside `.twe-kpi-strip`.
  - A `<script>` label is HTML-encoded.
- [ ] Gates:
  - `./bin/dev build` reports 0 warnings and 0 errors.
  - `./bin/dev template-smoke` passes.
  - `ganda repo audit` passes.
  - `git diff origin/master -- source tests | rg -i crunchit` prints nothing.

## Session

- Created: ses_ebc1f6587ffeC5oxDhO3SRDeMu (2026-10-16)
- Spec written: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-22)

## Notes

- Crunchit source is under
  `/home/steve/worktrees/github.com/Crunchitfs/crunchit/master/source/container-apps/web/projects/web-spa/`:
  - `components/KpiCard.razor` and `KpiCard.razor.css`
  - The strip CSS in `features/{dashboard,staff,clients}/pages/*.razor.css`.
  - The tone in `ClientsPage.razor` (`KpiCloseTone`) and its CSS (around lines 328–336).
  - The demo in `features/style-guide/pages/StyleGuidePage.razor` (lines 56–61).
- Target folders: `source/container-apps/web/projects/web-spa/components/{elements,composites}/`,
  `wwwroot/css/tokens.css`, and the style guide page.
- Boundary with 379-012:
  - This task covers the plain value tile and its tone only.
  - Trend and delta pills, SLA class pills, sparklines, charts, and the ChartCard frame belong to
    379-012.
  - Add no delta or trend parameter here. 379-012 may add a `Trend` slot later.
- Tone as an enum parameter, rather than a strictly literal port, was the recommendation adopted
  when this spec was written (2026-10-22).
