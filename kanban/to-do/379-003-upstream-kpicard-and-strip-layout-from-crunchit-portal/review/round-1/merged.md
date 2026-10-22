# Round 1 — merged findings
**Date:** 2026-10-22
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/components/splatted-class.cs:13
- Description: The new `SplattedClass` helper is a second idiom for a caller `class`. `Card`,
  `FormSection`, and `FormField` already use a `[Parameter] public string? Class` that Blazor binds
  case-insensitively from `class="…"`, so no helper is needed.
- Suggestion: Delete `splatted-class.cs`. Add a `Class` parameter to `KpiCard` and `KpiStrip`, render
  it in the root class as `Card` does, splat `AdditionalAttributes` directly, keep the passthrough
  tests green, and update both Design regions.
- Source: general
- Disposition notes:

### M2 — Severity: nit — Status: open
- File: tests/container-apps/web/web-spa-integration-tests/components/kpi-strip-render-tests.cs:42
- Description: The child-inside-strip check matches any `</div>` after the child, so it passes trivially.
- Suggestion: Tie the assertion to the strip root, for example check that the child offset is past the
  end of `RootTag` and that the markup ends with the root's closing `</div>`.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
