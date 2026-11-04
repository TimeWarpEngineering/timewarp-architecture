# Fix Dark theme broken

## Description

Admin triage for feedback filing "Dark theme broken" (live host https://arch.timewarp.work).
FeedbackItemId: `01a12f40-fd5d-77ca-beff-b33d0d65c964`.
Kind: BugReport. Filer body: "Ah, my eyes!" plus one PNG screenshot attachment
(`01a12f40-f348-76b7-a1c5-9e0702feeb0e`). Status on server is already Planned
(New → UnderReview → Planned, Oct 15); this task tracks the actual fix.

## Requirements

- Reproduce locally: run the app (`dev run`) and switch to the dark theme. Capture a before
  screenshot with Playwright (`tw-playwright`) into `kitchen/` on this task. The filer's PNG lives
  on the live host's feedback store and may be unreachable from a worker; do not block on it.
- Fix follows `tw-blazor-css-strategy`: design tokens and CSS isolation, FluentUI v5 theme tokens,
  no `<style>` elements in components, no Tailwind, no TypeScript patches of library internals.
  Fix the root cause (hard-coded light colors or missing dark token overrides), not one page.
- Walk every top-level page and the Ask AI modal in dark theme and capture after screenshots.
  Light theme must be unchanged.
- Gates: `dev build` 0 warnings, Playwright tests pass, `ganda repo audit` clean. `dev template-smoke`
  is not required unless a `#if` region changes.
- Do not change the feedback item's status on the live server; that is the admin's step after the
  release ships. Note the item id in Results.

## Goal (implementer restatement)

When the app is in dark mode, every app surface must be dark and legible. Dark mode comes from
the OS preference (Fluent's default mode is "system") or from the shell's theme button. Today
Fluent paints its own components dark while the `--twe-*` tokens stay light.
The fix is at the token layer (`tokens.css` dark overrides keyed on Fluent's
`body[data-theme="dark"]`, no per-page patch, no `<style>`, no Tailwind, no TS patch of Fluent),
and the shell's theme button must actually switch Fluent's mode through a State action.

Acceptance:
- Before screenshots (dark, unfixed) and after screenshots (dark, fixed) of every navigable
  top-level page and the Ask AI modal are in `kitchen/` on this task.
- Light theme renders the same as before (same token values when `data-theme` is not dark).
- Hard-coded light colors that bypass tokens (StatusBadge tints, inverse hint surface) are tokenized.
- A Playwright test proves dark mode darkens the shell surfaces and the theme button toggles.
- `dev build` 0 warnings, Playwright suite passes, `ganda repo audit` clean.
- Feedback item `01a12f40-fd5d-77ca-beff-b33d0d65c964` is not touched on the live server; it is noted in Results.

## Checklist

- [x] Reproduce dark-theme break (the filer's PNG was not reachable; reproduced locally, `kitchen/before/`)
- [x] Fix and verify in dark theme (`kitchen/after/`, `DarkTheme_Given_Shell`)
- [ ] Confirm feedback item 01a12f40-fd5d-77ca-beff-b33d0d65c964 stays Planned / move to Completed when shipped (admin step after release)

## Session

- Created: ses_e644b7498ffeR8qWbSTjbeTMvu (2026-11-02)
- 2026-11-04 implementer (ganda task work, Claude Opus 5.5): reproduced, fixed, gates run. See Results.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 4b054e7f-dbc1-4ac5-b8ea-d51ae99544c6, max-turns 500, Done — 2026-11-04T12:58:33Z
- Review oracle round 1 (ganda task work, Claude Opus 5.5, tw-implementation-review): effort 3,
  roster general, turn cap 200. 1 open suggestion (M1, dark-mode focus rings); fix pass requested. 2026-11-04
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 27e16840-c6a2-4f79-a5ba-64f389769084, max-turns 200, artifacts general.md, merged.md — 2026-11-04T13:04:57Z

## Results

Feedback item `01a12f40-fd5d-77ca-beff-b33d0d65c964` (BugReport "Dark theme broken") was not
touched on the live server. It stays Planned until the admin moves it after the release ships.

### Root cause

- Fluent marks dark mode with `body[data-theme="dark"]` and paints its own components from its
  dark ramp. The `--twe-*` tokens had only light values, so app paper (cards, app bar, nav column,
  filter bar, footer), page titles (dark ink on dark), and status badges (hard-coded light hex)
  stayed light over Fluent's dark surfaces. Repro: `kitchen/before/dark-home.png`. The full list
  of light surfaces the detector found is in `kitchen/before-light-surfaces.txt`.
- The app-bar theme button called `ThemeState.Update(Dark)` (TimeWarp.State.Plus). Nothing read
  that state, so the button did nothing. Users reached dark through a stored Fluent mode or an
  OS switch to dark during the session. With no stored mode, Fluent's system-change listener
  follows the OS.

### Fix

- `wwwroot/css/tokens.css`: a `body[data-theme="dark"]` block overrides neutrals (Fluent 2 dark
  neutrals: page #292929, paper #303030, subtle #1f1f1f), semantic colors (lighter steps that clear
  4.5:1 on dark paper and on a 12% tint), shadows, and `color-scheme`. FluentNav's own
  `--nav-bg-color` hooks keep nav items on the column fill. New role tokens have light values equal
  to what components used before: `--twe-link`, `--twe-link-hover`, `--twe-primary-text`,
  `--twe-accent-text`, `--twe-on-accent-tint`, `--twe-on-info`, `--twe-positive-tint`,
  `--twe-warning-tint`, `--twe-info-tint`, `--twe-inverse`, and `--twe-on-inverse`. The dark block
  overrides no brand key, so `/brand.css` stays the one brand source.
- Components read the role tokens instead of light hex or deep brand steps: StatusBadge, Tabs,
  DevNote, RoleCard, 403/404 pages, shell breadcrumb, `a` links, LinkDisplay, and the ask-tag hint.
- `ApplicationState.ToggleTheme` (new action) calls `IThemeService.SwitchThemeAsync`. The app-bar
  button dispatches it (`data-qa=ThemeToggle`, with an aria-label). The unused ThemeState accessors
  were removed from BaseComponent.
- The design-system Color Tokens swatches now label the custom property (`--twe-ink`). Their hex
  copy disagreed with the dark values.
- `skills/tw-blazor-css-strategy`: rule 9 (dark mode) and a quick-reference row.
- Test: `DarkTheme_Given_Shell` (Playwright). (1) With dark mode stored, it signs in (passkey plus
  the Developer role), walks all 19 PageRegistry pages and the Ask panel, and fails on any visible
  near-white light-DOM box or on a light body. (2) The theme button toggles to dark, dark
  survives a reload, a second click restores the light token surfaces, and an OS switch to dark
  does not override a stored light choice.
  `DesignSystemCatalog_Given_Wasm.GrantDeveloperAsync` became internal so the walk can reuse it.

### Evidence (kitchen/)

- `before/`: dark mode on the unfixed code, all 19 pages plus `dark-ask.png`.
- `after/`: the same walk on the fix, plus `light-home.png`.
- `light-compare/`: a light-mode walk on both commits, pixel-diffed. Only per-run data changed
  (random avatar, ids, timestamps) plus the intended swatch label. See `pixel-diff.md`.

### Gates

- `dev build`: 0 warnings, 0 errors.
- Playwright suite (`web-spa-playwright-tests`): 35/35 passed, including the 2 new tests.
- `dev test --group fast`: 14/14 projects passed. `web-spa-integration-tests`: 578/578 passed.
- `dev lint-skills`: 9/9 skills passed.
- `ganda repo audit`: the only error was `kanban-landed` (kitchen edits not yet on master).
  `ganda kanban reconcile` cleared it by landing this kitchen; publish refuses a branch with
  product code. The `source-file-line-cap` warnings were already there, in files this diff
  does not touch.
- `dev template-smoke`: not run. No `#if` region or template symbol changed.

### Follow-ups (not in this task)

- The default logo's wordmark is a purple-to-blue gradient that is weak on the dark app bar.
  A dark-mode logo needs a new BrandOptions field, for example `LogoSrcDark`.
- Some links render in Fluent's own dark brand ramp (FluentAnchor and the default link color),
  which is a muted purple on dark paper. That ramp is Fluent's; changing it means retuning the
  seed for dark mode.
- On a first visit with no stored mode, Fluent starts light even when the OS is dark, because
  App.razor renders `data-theme="light"`, and turns dark only if the OS changes during the session.
  Following the OS on first load is a product decision.

### Review

- Round 1 (effort 3, roster: general): bug 0, suggestion 1 open (M1), nit 0. No disposition yet.
- M1: focus rings and focused-field borders still read `--twe-primary`, about 1.6:1 on dark paper
  (WCAG 1.4.11 needs 3:1). A fix pass is requested. Round 2 re-reviews it.
- The reviewer re-ran `dev build` (0 warnings) and `DarkTheme_Given_Shell` (2/2 passed).
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.
- M1 fix pass (2026-11-04): new role token `--twe-focus-ring` (light `var(--twe-primary)`, so light
  mode is unchanged; dark `var(--twe-purple-soft)`, about 8:1 on dark paper). Tabs,
  SegmentedControl, PeriodStepper, AskReferencePicker, and the ask-tag outline (`app.css`) read it
  for their focus outlines; FilterSelect, FilterBar, and the Ask input read it for the focused border
  and halo. The active-tab underline reads `--twe-primary-text`, the tab's own text color.
  RoleCard keeps `--twe-on-accent-tint` (purple-deep light, purple-soft dark, already 3:1 in both).
  The breadcrumb and command-palette rings use `--twe-blue` (about 3.1:1 on dark paper) and were
  left as they are. `DarkTheme_Given_Shell` now checks `--twe-focus-ring` against `--twe-paper` for
  3:1 on every dark page and in light. `tw-blazor-css-strategy` rule 9 names focus and selection
  indicators.
- Fix-pass gates: `dev build` 0 warnings; Playwright suite 35/35 passed (including
  `DarkTheme_Given_Shell` 2/2); `dev lint-skills` passed; `ganda repo audit` has no errors (warnings
  only: `kanban-landed` for other tasks' claims and the pre-existing `source-file-line-cap` files).

### How to validate

Smoke:

```bash
cd tests/container-apps/web/web-spa-playwright-tests
dotnet test -c Release -- --filter-class DarkTheme_Given_Shell
```

Expect: 2 tests pass, including the 3:1 `--twe-focus-ring` check on every dark page. `bin/Release/net11.0/playwright-screenshots/DarkTheme_Given_Shell/` holds
`dark-*.png` for every page and Ask (dark cards, nav, app bar, and footer, with light text) and
`light-home.png`, which matches the light shell.

Manual: `dev run`, sign in, and click the half-moon button in the app bar. The whole app turns dark
(cards, nav, app bar, badges, Ask panel). Reload: it stays dark. Tab through a tab strip or focus the filter search: the ring is light purple
and clearly visible. Click the button again: light, unchanged.

## Review

- Round 1: M1 fixed in the fix pass; round 2 re-reviews.

## Notes

Feedback ID: 01a12f40-fd5d-77ca-beff-b33d0d65c964. Owner has no email (FilerEmailed false).
