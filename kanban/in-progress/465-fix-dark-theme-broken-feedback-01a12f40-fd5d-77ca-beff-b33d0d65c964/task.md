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

- [ ] Reproduce dark-theme break with attached screenshot
- [ ] Fix and verify in dark theme
- [ ] Confirm feedback item 01a12f40-fd5d-77ca-beff-b33d0d65c964 stays Planned / move to Completed when shipped

## Session

- Created: ses_e644b7498ffeR8qWbSTjbeTMvu (2026-11-02)

## Notes

Feedback ID: 01a12f40-fd5d-77ca-beff-b33d0d65c964. Owner has no email (FilerEmailed false).
