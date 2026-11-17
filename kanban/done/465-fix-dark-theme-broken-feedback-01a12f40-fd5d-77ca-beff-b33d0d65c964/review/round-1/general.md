# Round 1 — general
**Date:** 2026-11-04
**Scope reviewed:** same as `review/review-framework.md` (branch vs merge-base with master).

## Summary

The change fixes dark mode at the token layer. A `body[data-theme="dark"]` block in `tokens.css`
overrides the neutral, semantic, and new role/tint tokens. Components that hard-coded light hex or
deep brand steps as text now read role tokens. The app-bar button dispatches a new
`ApplicationState.ToggleTheme` action that calls Fluent's `IThemeService.SwitchThemeAsync`. The
light `:root` values equal what components used before, so light mode is unchanged. The kitchen
pixel diff supports that. The shape follows `tw-blazor-css-strategy`: no `<style>`, no Tailwind, no
TS patch, and brand keys stay owned by `/brand.css`. Risk is low. One theme: focus indicators that
read `--twe-primary` were not moved to a role token, so they nearly vanish on dark paper.

Re-verified in this worktree:

- `./bin/dev build`: 0 warnings, 0 errors.
- `dotnet test -c Release --no-build -- --filter-class DarkTheme_Given_Shell`: 2/2 passed (1m 15s).
- Screenshots `kitchen/after/dark-home.png` and `dark-admin-roles.png` were inspected: dark surfaces,
  light text, badges legible.
- Dark tint contrast was computed by hand. Info badge text `#00ccff` on the 24% info tint is
  about 5.5:1. Positive badge text `#9fd89f` on the 18% positive tint is about 6.0:1.
- No remaining `color: var(--twe-<brand>)` text rule outside tokens.css except via role tokens.
  `#blazor-error-ui` keeps its own self-contained light banner, which is fine in either mode.

Goal restatement vs task: consistent. The restatement adds the theme-button fix and the
OS-preference path, which serve the task's "fix the root cause" requirement. No misread.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/components/composites/Tabs.razor.css:35
  (same pattern in `SegmentedControl.razor.css:35`, `PeriodStepper.razor.css:29`,
  `FilterSelect.razor.css:58`, `FilterBar.razor.css:56`,
  `features/application/modals/agent-ask/AskReferencePicker.razor.css:33`,
  `features/application/modals/agent-ask/AgentAsk.razor.css:372`, and the `.twe-ask-tag-outline`
  rule in `wwwroot/css/app.css:113`)
- Description: Keyboard focus rings and focused-field borders read `--twe-primary` (`#55409c`). The
  dark block does not override that brand key, by design. On dark paper `#303030` that purple is
  about 1.6:1, under the WCAG 1.4.11 3:1 bar for non-text indicators, so in dark mode keyboard
  focus on tabs, the segmented control, the period stepper, filter inputs, and the Ask inputs is
  close to invisible. In light mode it is about 8:1. The diff already moved one focus ring,
  `RoleCard.razor.css:30`, to the role token `--twe-on-accent-tint`, so the shell is now
  inconsistent. The new skill rule 9 covers text and fills but not indicators, so the next
  component will repeat the miss.
- Suggestion: Add a role token, for example `--twe-focus-ring: var(--twe-primary)` on `:root`,
  with a dark value that clears 3:1 on dark paper (for example `var(--twe-purple-soft)`, about
  9:1). Point the listed focus outlines and focus borders at it. Light mode stays pixel-identical.
  Extend skill rule 9 / the quick-reference row to name focus and selection indicators. The
  active-tab underline `Tabs.razor.css:31` can use the same token. Optionally, have the dark
  Playwright walk focus one tab and assert the outline color is not `--twe-primary`.
- Status: open
