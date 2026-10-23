# Upstream ChartCard frame and Trend Sla Class pills extend StatusBadge Lost Error

## Description

A generated app should be able to frame a chart, mark a period-over-period move, mark an on-time tier, and mark a classification, without each page inventing that chrome. The plot itself stays with the page. The source never shipped a chart component or a sparkline (its chart card is still a backlog item), so this task ports the frame and the pills, not a charting library.

`ChartCard` is the paper frame: a title, an optional header slot (a period control belongs here), an optional filter row, a plot slot, and an optional legend slot. `ChartLegendItem` is one legend row: a token-coloured dot and a label. `TrendPill` is the up, down, or flat move. `SlaChip` is the good, warning, or behind tier, with the tier name available to assistive tech. `ClassPill` is the mapped or unmapped classification. `StatusBadge` gains `BadgeStatus.Error`, the red lost/error status. Callers pass pre-formatted labels. None of these components format numbers, choose a period, or wire an event.

The style guide is the only consumer. There is no product page in the template that shows a series, a trend, an on-time tier, or a classification pill.

## Checklist

- [x] `BadgeStatus.Error` and a `twe-badge--error` modifier. The style guide shows the default label and a `Lost` label.
- [x] `ChartCard` frame with title, `HeadingLevel`, header, filter, plot, and legend slots. Ask Tag marks it as a card. No plot is drawn by the component.
- [x] `ChartLegendItem` with `ChartSeriesTone` (`Ink`, `Primary`, `Positive`).
- [x] `TrendPill` with `TrendDirection` (`Up`, `Down`, `Flat`). The direction word is in the accessible name. A blank label leaves only the glyph.
- [x] `SlaChip` with `SlaTone` (`Good`, `Warn`, `Bad`). The tone word is in the accessible name. The dot is decorative.
- [x] `ClassPill` with an `Unmapped` modifier. Mapped is an ink wash. Unmapped is a dashed rule.
- [x] Isolated CSS, `--twe-*` tokens, no `<style>`, no client name. Text on a tint clears 4.5:1. `--twe-on-positive` and `--twe-on-warning` exist because `--twe-positive` and `--twe-warning` do not.
- [x] Purpose and Design regions. A row in `components/overview.md`. Style-guide cards. HtmlRenderer tests, including an encoded `<script>` label.
- [x] `./bin/dev build` is 0 warnings and 0 errors. `./bin/dev template-smoke` passes. `ganda repo audit` passes. `git diff origin/master -- source tests` has no case-insensitive client-name match.

## Goal (implementer restatement)

Bring the chart frame and three pills into the template, and add the red status the badge is missing. Generated code, including copy, comments, sample data, and class names, carries no source-product name.

`ChartCard` is a native `section.twe-chart-card`. The title is a heading. `HeadingLevel` 2 through 6 selects h2–h6, and any other value stays h2, the same rule `EmptyState` uses. `Header`, `Filter`, `ChildContent`, and `Legend` render only when the caller passes them. The component does not draw a series, a sparkline, or a period control. A caller puts `SegmentedControl` in `Header` and its own plot in `ChildContent`. The root is pickable by Ask Tag as a card, using the title as the label. A caller class is appended. Other unmatched attributes splat onto the root and can override the Ask Tag mark. `ChartLegendItem` is a native span: a decorative dot in `Ink`, `Primary`, or `Positive`, plus the label.

`TrendPill` is a native span. `Up`, `Down`, and `Flat` set the modifier and a visible glyph (↑, ↓, —). The direction word is visually hidden and part of the accessible name. `Label` renders only when it has text. The default direction is `Flat`.

`SlaChip` is a native span with a decorative dot. `Good`, `Warn`, and `Bad` set the modifier. The accessible name leads with Good, Warning, or Behind, then the caller's label. The default tone is `Good`.

`ClassPill` is a native span. The default look is a mapped classification (ink text on a light ink wash). `Unmapped` switches to a dashed rule and quieter text. The caller supplies the words, including "Not mapped".

`StatusBadge` accepts `BadgeStatus.Error`. The modifier is `twe-badge--error`. The default label is "Error". A caller passes "Lost" or any other word. The other four statuses stay as they are.

`KpiCard` does not grow a trend parameter. The page places a `TrendPill` beside a value when it wants one.

There is no sparkline and no chart renderer. The source left both unbuilt. The plot slot is the extension point.

Tokens: `--twe-on-positive` (`#14784a`) and `--twe-on-warning` (`#92400e`). `--twe-positive` is 4.44:1 on white and `--twe-warning` is 3.19:1 on white, so neither can be body text on a tint. Soft fills are a 12% mix of the semantic color into `--twe-paper`. Flat and unmapped text use `--twe-ink-2`, because `--twe-muted` is under 4.5:1 on paper.

The style guide shows the frame (period control in the header, a filter line, a decorative sample plot, three legend items), the three pills, and the error badge with a Lost label. The page contract is unchanged. Render tests pin the slots, the modifiers, attribute passthrough, heading fallback, the Ask Tag mark, and an encoded `<script>` label.

Acceptance: `./bin/dev build` is clean, `./bin/dev template-smoke` passes, `ganda repo audit` passes, and `git diff origin/master -- source tests` contains no case-insensitive match for the source product name.

## Session

- Created: ses_ebc1f6587ffeC5oxDhO3SRDeMu (2026-10-16)
- Implemented: chart frame, legend item, trend pill, SLA chip, class pill, and `BadgeStatus.Error` (2026-10-24). Style guide is the only consumer. A live browser pass was not done.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-23T19:59:27Z
- Review oracle: general reviewer (claude, Claude Opus 5.5, headless print mode, session not exposed), effort 3, 1 round, disposition clean — 2026-10-24
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-23T20:03:05Z

## Results

`ChartCard`, `ChartLegendItem`, `TrendPill`, `SlaChip`, and `ClassPill` are native leaves under `components/elements`. `ChartCard` is a paper frame (`twe-chart-card`) with a title heading. `HeadingLevel` 2–6 selects h2–h6; any other value stays h2. `Header`, `Filter`, `ChildContent`, and `Legend` render only when the caller passes them. The component draws no series. Ask Tag marks the root as a card with the title as the label, and a caller attribute can override that mark. `ChartLegendItem` is a decorative dot in `Ink`, `Primary`, or `Positive`, plus the label.

`TrendPill` shows ↑, ↓, or — for `Up`, `Down`, and `Flat`. The direction word is visually hidden and stays in the accessible name. A blank label leaves the glyph. `SlaChip` is a decorative dot plus the caller's label. The accessible name leads with Good, Warning, or Behind. `ClassPill` defaults to a mapped ink wash. `Unmapped` switches to a dashed rule and `--twe-ink-2`. Callers pass the words, including "Not mapped".

`StatusBadge` accepts `BadgeStatus.Error`. The modifier is `twe-badge--error`. The default label is "Error". A caller passes "Lost" or any other word. The other four statuses are unchanged. `KpiCard` has no trend parameter. There is no sparkline and no chart renderer.

New tokens: `--twe-on-positive` (`#14784a`) and `--twe-on-warning` (`#92400e`). Soft fills are a 12% mix of the semantic color into `--twe-paper`. The error badge uses that mix with `--twe-danger`. Flat and unmapped text use `--twe-ink-2`. The four existing badge modifiers stay as they were.

The style guide, after KPI Cards, shows a chart frame titled "Sample series" at heading level 3 (`data-qa="StyleGuideChartCard"`): a period control (Day / Week / Month, value Week, label "Sample period"), the filter "All locations", a decorative three-line sample, and legend items Series A, Series B, and Series C. The next card shows trend pills Up 4.2%, Down 1.8%, and Flat; SLA chips 98%, 90%, and 78%; and class pills "Operations" and "Not mapped". The status-badge row adds Error and Lost. `components/overview.md` has a row for each new leaf and names the error status. The StyleGuidePage contract is unchanged.

HtmlRenderer tests pin the slots, the modifiers, attribute passthrough, heading fallback, the Ask Tag mark, and an encoded `<script>` label. They do not see color or contrast. A signed-in Style Guide was not opened in a browser.

### How to validate

**Smoke**

```bash
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Debug -- --filter-class ChartCard_Should_
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Debug --no-build -- --filter-class ChartLegendItem_Should_
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Debug --no-build -- --filter-class TrendPill_Should_
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Debug --no-build -- --filter-class SlaChip_Should_
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Debug --no-build -- --filter-class ClassPill_Should_
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Debug --no-build -- --filter-class StatusBadge_Should_
```

**Expect:** `ChartCard_Should_` 5 passed, 0 failed. A title-only card is an h2, marked Ask Tag Card with that title, and omits header, filter, plot, and legend. Passing all four slots renders each mark. `HeadingLevel` 3 yields an h3. `HeadingLevel` 1 stays h2. A caller `data-ask-tag` overrides Card. A title of `<script>` stays encoded. `ChartLegendItem_Should_` 4 passed, 0 failed. The default dot is ink and `aria-hidden`. Primary and Positive set their modifiers. `TrendPill_Should_` 4 passed, 0 failed. A blank label is flat, shows the em dash and the word Flat, and omits the label element. Up 4.2% and Down 1.8% set their modifiers. `SlaChip_Should_` 5 passed, 0 failed. The default is good, names Good, and shows 98%. Warn names Warning and shows 90%. Bad names Behind and shows 78%. A blank label omits the label element and keeps Warning. `ClassPill_Should_` 4 passed, 0 failed. "Operations" stays mapped. "Not mapped" with `Unmapped` uses that modifier. `StatusBadge_Should_` 3 passed, 0 failed. Error renders `twe-badge--error` and the word Error. Active still renders Active. Label "Lost" with class `caller-mark` shows Lost and that class, and the word Error is absent under a case-sensitive check. Each class passes a caller attribute through, and a `<script>` label stays encoded.

**Automated gate**

```bash
./bin/dev build
./bin/dev template-smoke
ganda repo audit
git diff origin/master -- source tests | rg -i crunchit || echo NO_CLIENT_NAME
```

`dev build`: 0 warnings, 0 errors (00:00:18.43). `dev template-smoke`: `Template smoke SUCCEEDED`. SmokeDefault OK. create-role 5/5, weather 5/5, hello 2/2, web-jaribu-tests 405/405, api-jaribu-tests 9/9, timewarp-testing-tests 14/14. `ganda repo audit`: Passed 33, Failed 0, Skipped 1 (self-hosted, public repository). The client-name search prints `NO_CLIENT_NAME`.

### Review disposition

- **Outcome:** clean. 1 round. Roster: general only, at effort 3 (by-diff budget, 1316 lines).
- **Final counts:** bug 0, suggestion 0, nit 0. Open 0, fixed 0, wontfix 0.
- **Reviewer re-ran these in this worktree:** `./bin/dev build` (0 warnings, 0 errors), the six render-test classes (25/25 passed, counts match above), and the client-name search (0 matches). Every contrast ratio in the CSS and token comments was recomputed from `tokens.css` and holds. `dev template-smoke` and `ganda repo audit` were not re-run by the reviewer. Their results above are implementer-reported.
- **Artifacts:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Review

- Round 1: Done

## Notes

- Source (read-only): the portal mockup's `.chart-card`, `.trend-chip`, `.sla-chip`, and `.class-pill`, plus `BadgeStatus.Error` on the portal `StatusBadge`. The portal chart component is backlog and is not ported as a plot.
- Boundary from 379-003: trend and delta pills, SLA class pills, and the chart frame belong here. `KpiCard` stays free of a trend parameter. Sparklines and a line renderer were named in that boundary and do not exist in the source, so they are not invented here.
- Boundary from 379-010: `BadgeStatus.Error` was left for a follow-up. This task is that follow-up. One enum value covers lost and error. The label carries the word.
- Do not extend the StyleGuidePage contract. That is 379-017.
