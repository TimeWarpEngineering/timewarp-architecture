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

- [x] `KpiCard.razor` with a `.razor.css`, root `div.twe-kpi`.
  - Takes `AdditionalAttributes`.
  - Does not use Crunchit's `DisplayComponent` or `CssBuilder`.
- [x] `KpiCard` parameters, all strings and pre-formatted:
  - `Label` and `Value` (`EditorRequired`).
  - `Unit`, `SecondaryValue`, `SecondaryUnit`, `Caption`. These render only when they are not
    blank.
  - No `EventCallback`. Any interaction from a caller dispatches a TimeWarp.State action.
- [x] Tone:
  - Add a `KpiTone` enum (`None`, `Positive`, `Danger`) as the `Tone` parameter.
  - It renders the modifier class `twe-kpi--positive` or `twe-kpi--danger`, styled in the card's
    own CSS.
  - It replaces Crunchit's caller `::deep` box-shadow.
  - It is a border or accent only, not a pill.
- [x] `KpiStrip.razor` with a `.razor.css`.
  - Root `div.twe-kpi-strip`, with `ChildContent` and `AdditionalAttributes`.
  - The grid is `repeat(auto-fit, minmax(var(--twe-kpi-min, 160px), 1fr))`, with
    `gap: var(--twe-space-3)` and `min-width: 0` on the children.
  - One 160px minimum replaces Crunchit's 140–180px spread. This is a deliberate normalisation.
  - It reflows to fewer columns on narrow screens without media queries.
  - Spacing comes from `--twe-space-*`, not page-local margins.
- [x] Use CSS isolation only: no `<style>` element and no Tailwind (`tw-blazor-css-strategy`).
      There are no FluentUI children, so no `::deep` is needed.
- [x] Tokens in `wwwroot/css/tokens.css`:
  - Map Crunchit tokens to their existing `--twe-*` equivalents: paper, rule, radius, shadow-sm,
    muted, ink, text-eyebrow, text-helper. Success maps to `--twe-positive` and danger to
    `--twe-danger`.
  - Add `--twe-text-kpi-value` (30px) and `--twe-kpi-pad` only if nothing existing fits. Check
    `tokens.css` first, and add a spacing step only if the padding needs one.
- [x] Add Purpose and Design regions (TWA0004) to both `.razor` files, in the style of
      `SectionHeader.razor`.
- [x] Style guide (`features/style-guide/pages/StyleGuidePage.razor`):
  - Replace the hand-rolled `.sg-kpis` demo with a "KPI Cards" `Card`.
  - The card shows a `KpiStrip` of four cards: single, dual-metric, caption, and tone.
  - Do not extend the StyleGuidePage contract; that is 379-017.
- [x] Render tests in `tests/container-apps/web/web-spa-integration-tests/components/`, in
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
- [x] Gates:
  - `./bin/dev build` reports 0 warnings and 0 errors.
  - `./bin/dev template-smoke` passes.
  - `ganda repo audit` passes.
  - `git diff origin/master -- source tests | rg -i crunchit` prints nothing.

## Session

- Created: ses_ebc1f6587ffeC5oxDhO3SRDeMu (2026-10-16)
- Spec written: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-22)
- Implement oracle: implemented by implementer-cursor (cursor, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 500), Done — 2026-10-22T05:03:49Z
- Review oracle round 1: claude-opus-5-5, effort 2, roster general (inline), session not reported — 2026-10-22. Fix pass requested for M1, M2.
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-22T05:06:33Z
- Fix pass (implement-fix-2): implementer-cursor (cursor, grok-4.7), 2026-10-22. Addressed M1 and M2.

## Review

- Round 1: Blocked — open M1, M2
- Fix pass: M1 and M2 are addressed on this branch. Round 2 is the host review node.

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

## Goal (implementer restatement)

Bring the stat tile and its row layout into the template as two components. Generated code,
including copy, comments, sample data, and class names, carries no source-product name.

`KpiCard` is an element: a native `div.twe-kpi` with an uppercase label, a pre-formatted value,
and an optional unit, second metric, and caption. Those optional strings render only when they
are not blank. Callers pass the strings already formatted. The card does no number formatting
and has no delta, trend, or event callback. `Tone` is `KpiTone` (`None`, `Positive`, `Danger`).
`None` adds no class. The other two add `twe-kpi--positive` or `twe-kpi--danger`, drawn as a
border in the card's own stylesheet. A caller class binds to a `Class` parameter and is rendered
on the root the way `Card` does, so it is appended and does not replace `twe-kpi` or
`twe-kpi-strip`. Other unmatched attributes, including `data-testid`, splat onto the root.

`KpiStrip` is a composite: a native `div.twe-kpi-strip` whose isolated CSS is the one responsive
grid, `repeat(auto-fit, minmax(var(--twe-kpi-min, 160px), 1fr))`, with `--twe-space-3` and
`min-width: 0` on the children. Pages do not repeat that grid or add a page-local margin. There
is no `<style>` element and no Tailwind. Tokens reuse the existing paper, rule, radius, shadow,
muted, ink, eyebrow, and helper variables. The positive and danger tones use `--twe-positive`
and `--twe-danger`. `--twe-text-kpi-value` (30px) and `--twe-kpi-pad` are added only because the
type scale and spacing steps do not already cover 30px and the tile padding.

The style guide is the only consumer: a "KPI Cards" card with a strip of four tiles (single,
dual-metric, caption, tone). The page contract stays as it is. Render tests cover label and
value, optional unit, one or two metric blocks, optional caption, each tone, attribute
passthrough, strip child content, and an HTML-encoded `<script>` label.

Acceptance: `./bin/dev build` is clean, `./bin/dev template-smoke` passes, `ganda repo audit`
passes, and `git diff origin/master -- source tests` contains no case-insensitive match for the
source product name.

## Results

`KpiCard` lives in `components/elements/` (`TimeWarp.Architecture.Components`). The root is
`div.twe-kpi`. Label and value are required pre-formatted strings. Unit, secondary value,
secondary unit, and caption render only when they are not blank. `KpiTone` is `None`,
`Positive`, or `Danger`. `None` adds no modifier. The other two add `twe-kpi--positive` or
`twe-kpi--danger`, which set the border color to `--twe-positive` or `--twe-danger`. There is
no delta, trend, or event callback.

A caller class binds to `Class` and is rendered on the root the way `Card` does
(`class="twe-kpi @ToneClass @Class"` and `class="twe-kpi-strip @Class"`). Other unmatched
attributes, including `data-testid`, splat directly. There is no class-merge helper.

`KpiStrip` lives in `components/composites/`. Its grid is
`repeat(auto-fit, minmax(var(--twe-kpi-min, 160px), 1fr))` with `gap: var(--twe-space-3)` and
`min-width: 0` on the children. The strip has no margin. `--twe-text-kpi-value` is 30px.
`--twe-kpi-pad` is `18px 20px` because the spacing scale is 8, 12, 24, and 48. The 6px gap
between a value and its unit stays a hairline under that scale. Paper, rule, radius, shadow,
muted, ink, eyebrow, and helper reuse the existing tokens.

The style guide's KPI Cards card is a strip of four tiles: a single value, a dual metric, a
caption, and a danger tone. The page contract is unchanged. No other page adopts the tile.

### How to validate

Smoke:

- `./bin/dev build` — 0 warnings, 0 errors (Release, this fix pass).
- `./bin/dev template-smoke` — SmokeDefault OK; generated Jaribu aggregators 405/405, 9/9, and 14/14. Template smoke succeeded.
- `ganda repo audit` — 32 passed, 0 failed, 1 skipped.
- From `tests/container-apps/web/web-spa-integration-tests`: `dotnet test -c Debug -- --filter-class KpiCard_Should_` (7 passed) and `--filter-class KpiStrip_Should_` (2 passed).
- `git diff origin/master -- source tests | rg -i crunchit` — no match.

Expect:

- Style Guide → KPI Cards shows four tiles in one strip: "Open items" / 12, "Coverage" / 4 sites and 2 regions, "On time" / 49.5% with "Last 12 months", and "Exceptions" / 3 with the danger border.
- A narrow viewport reflows the strip to fewer columns without a breakpoint.
- A tile with no unit, second metric, or caption omits those blocks. Tone None has no modifier class.
- Passing `class` and `data-testid` keeps `twe-kpi` (or `twe-kpi-strip`) on the root and adds the caller class and test id. The strip's child sits between the root's opening tag and the closing `</div>` that ends the markup.

A live browser pass of the running app was not done in this walk. The static renderer covers the tile and strip markup, including the caller class on the root.

### Review

- Round 1 (effort 2, roster general): 0 bug, 1 suggestion, 1 nit. Fix pass addressed both.
- M1 (suggestion): `splatted-class.cs` is gone. `KpiCard` and `KpiStrip` take `Class` and render it on the root the way `Card` does. Unmatched attributes splat directly. The passthrough tests still pass.
- M2 (nit): the strip child must sit between the end of the root's opening tag and the closing `</div>` that ends the markup.
- Round 2 has not run. The host review node does that.
- Files: `review/review-framework.md` and `review/round-1/{general,merged}.md`.

## Learnings

- No misread: the implementer restatement matches the task.
- The spec's "replace the hand-rolled `.sg-kpis` demo" names a class that is not on master. The
  implementer added the KPI Cards card instead, which was the right call.
