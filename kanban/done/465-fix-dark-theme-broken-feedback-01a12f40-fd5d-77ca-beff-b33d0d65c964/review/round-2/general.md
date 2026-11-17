# Round 2 — general
**Date:** 2026-11-04
**Scope reviewed:** the fix delta since round 1. Base `3f2c4ea5a` (`review/round-1/base.sha`) to
`016def93c` "fix(web-spa): dark-mode focus rings read --twe-focus-ring (task 465 M1)". Product
paths: `wwwroot/css/tokens.css`, `wwwroot/css/app.css`, `Tabs`, `SegmentedControl`,
`PeriodStepper`, `FilterSelect`, `FilterBar`, `AgentAsk`, `AskReferencePicker` (`*.razor.css`),
`skills/tw-blazor-css-strategy/SKILL.md` rule 9, and
`tests/container-apps/web/web-spa-playwright-tests/dark-theme-playwright-tests.cs`.
**Effort:** 2 (by-diff budget, 216 lines), roster general.

## Summary

The delta fixes M1 the way round 1 suggested. A new role token, `--twe-focus-ring`, is
`var(--twe-primary)` on `:root`, so light mode is unchanged, and `var(--twe-purple-soft)` in the
`body[data-theme="dark"]` block. All eight focus outlines and focused-field borders/halos that
round 1 listed now read it. The active-tab underline reads `--twe-primary-text`, which is
`var(--twe-primary)` in light, so light is unchanged there too. Skill rule 9 now names focus rings,
focused-field borders, and selection indicators. The dark Playwright walk resolves
`--twe-focus-ring` and `--twe-paper` on a probe element and requires 3:1 on every dark page and in
light. Risk is low. No new defects.

## M1 re-verification — fixed

- `grep 'var(--twe-primary)'` over web-spa CSS (excluding `obj/`) finds no remaining focus,
  outline, or focused-border rule. What is left are fills and strokes (Avatar, CountChip, chart
  line/dot/legend, app.css:128 background), the Ask accent variable, and the `AskTagChips` dashed
  picking border. None is a focus or selection indicator, so none is in M1's scope.
- Light mode: every changed rule resolves to `#55409c` in light, the same value as before.
- Dark contrast, computed by hand: `#c5b4ff` on `#303030` is about 7.1:1, well over 3:1.
  (task.md Results says "about 8:1" and round 1 said "about 9:1"; the test checks the real value,
  so the prose difference does not matter.)
- Remaining rings not moved, each checked: RoleCard reads `--twe-on-accent-tint`
  (`#352078` light, about 12:1; `#c5b4ff` dark, about 7:1). The breadcrumb and command-palette rings
  read `--twe-blue` `#0085b2`, about 3.1:1 on dark paper, which passes. RoleCard's `.is-selected`
  border reads `--twe-purple` (about 1.6:1 on dark), but the selected card also shows a checkmark in
  `--twe-on-accent-tint`, so selection is not carried by the border alone. Not raised.
- The probe script parses `getComputedStyle(...).color`. Both tokens are plain hex values, so
  the browser returns `rgb(r, g, b)` and the regex parse is correct.

## Gates re-run in this worktree

- `./bin/dev build`: 0 warnings, 0 errors.
- `dotnet test -c Release --no-build -- --filter-class DarkTheme_Given_Shell` in
  `tests/container-apps/web/web-spa-playwright-tests`: 2/2 passed (1m 20s).
- `./bin/dev lint-skills`: 9/9 skills passed.

## Goal restatement vs task

Consistent, as round 1 found. The restatement adds the theme-button fix and the OS-preference path.
Both serve the task's "fix the root cause" requirement. No misread.

## Issues

None.
