# Round 1 — general
**Date:** 2026-10-24
**Scope reviewed:** same as framework (3d5e22546 vs 907b056ab, `source/` and `tests/`)

## Summary

The change adds five native-root leaves under `components/elements` (`ChartCard`, `ChartLegendItem`, `TrendPill`, `SlaChip`, `ClassPill`), three enums, `BadgeStatus.Error` with a `twe-badge--error` modifier, two `--twe-on-*` tokens, two style-guide cards, and six HtmlRenderer test classes. Every component is presentation-only: no events, no number formatting, no plot. Risk is low. The implementation matches both the original task and the implementer restatement. No defects were found.

## Verification performed

- `./bin/dev build`: 0 warnings, 0 errors (17.76 s).
- Render tests, re-run in this worktree: `ChartCard_Should_` 5/5, `ChartLegendItem_Should_` 4/4, `TrendPill_Should_` 4/4, `SlaChip_Should_` 5/5, `ClassPill_Should_` 4/4, `StatusBadge_Should_` 3/3. Counts match the Results claims.
- `git diff master -- source tests | grep -ci crunchit` printed 0.
- WCAG contrast, computed from `tokens.css` values:
  - error badge `--twe-danger` on 12% danger into paper: 4.60:1 (the comment's claim). The same mix into paper-2 gives 4.24:1, so the comment's warning about paper-2 is correct.
  - `--twe-on-positive` on 12% positive into paper: 4.75:1.
  - `--twe-on-warning` on 12% warning into paper: 6.25:1.
  - Flat trend `--twe-ink-2` on paper-2: 9.62:1.
  - Mapped class pill `--twe-ink` on 6% ink into paper: 14.88:1.
  - `--twe-positive` on white is 4.44:1, `--twe-warning` on white is 3.19:1, and `--twe-muted` on white is 4.29:1. The token and CSS comments state these values correctly.
  - The legend dot `--twe-positive` on white (4.44:1) clears the 3:1 non-text bar.
- `ChartCard` attribute merge: Ask Tag `Card` + title is seeded first, and `AdditionalAttributes` overwrite it. This matches the Design region and the restatement. A lower-case `class` in the test binds to the `Class` parameter, because Blazor matches parameters case-insensitively. So the test proves that the class is appended. It does not prove that a raw class splat replaces the class, and no caller can reach that path.
- `HeadingLevel` fallback: `HeadingLevel is >= 2 and <= 6 ? HeadingLevel : 2` is the rule `EmptyState` uses, as the task requires.
- Accessible names: the `TrendPill` glyph and the `SlaChip` dot are `aria-hidden`. The direction or tier word is a visually hidden sibling that comes before the label, so a screen reader reads "Up 4.2%" or "Warning 90%".
- `SegmentedControl` in the `ChartCard` header uses the style guide's existing demo pattern (line 142): no `ValueChanged`. That is consistent with the page, and the StyleGuidePage contract is unchanged, as 379-017 requires.
- The same visually hidden rule appears in both `TrendPill.razor.css` and `SlaChip.razor.css`. This is not raised: `tw-blazor-css-strategy` prefers co-located isolated CSS over a global utility file, and `app.css` holds only the reset and genuinely global element rules.

## Goal restatement vs task

The restatement is faithful. It adds two specifics the task leaves implicit: the glyph set (↑ ↓ —), and `Flat` and `Good` as the defaults. Both are consistent with the task. Nothing was misread.

## Issues

None.
