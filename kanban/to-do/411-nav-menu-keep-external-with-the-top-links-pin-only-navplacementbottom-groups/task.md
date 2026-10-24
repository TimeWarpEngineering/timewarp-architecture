# Nav menu: keep External with the top links; pin only NavPlacement.Bottom groups

## Goal (implementer restatement)

The side menu is one continuous list again: top-level links, then categories in NavOrder, then External immediately after those links. The empty gap and the scrollbar inside the menu come from always parking External and the Developer group in a second nav that is pushed to the bottom, while FluentNav scrolls and shrinks on its own.

Acceptance:

- `UserClaimsPage`, `ServicesPage`, `TestPage`, and `StyleGuidePage` use the default Top placement. No template page sets `NavPlacement.Bottom`.
- External renders in the primary nav, after the top groups.
- The pinned nav renders only when a permitted page uses `NavPlacement.Bottom`. The pin stays available for an app that opts in.
- `.twe-nav-menu` is the one scroll container. Each `FluentNav` inside it is `flex: none` and does not scroll itself. The signed-in user card stays fixed under the menu.
- The icon rail (880–1279px) and the mobile drawer stay as they are.
- Purpose and Design notes in `NavMenu.razor`, `NavMenu.razor.css`, and `TimeWarpPage.razor` match this layout.
- Tests that expected Developer or External in the pinned nav expect this shape, including External in the primary nav and no pinned nav when no page is Bottom.
- `dev build` is 0/0, the affected tests pass, and `ganda repo audit` is clean.

## Description

Task 379-015 (pinned user card and nav group) regressed the side menu in two ways:

1. **Big empty gap.** `NavMenu.razor` renders a second `FluentNav` (`twe-nav--pin`) with
   `margin-top: auto` (`NavMenu.razor.css`). It always holds the External category, and it holds
   the Developer group because the Developer and debugger pages set `NavPlacement = NavPlacement.Bottom`.
   With few links (signed out shows only Home), Home sits at the top, External sits at the bottom
   of the column, and the space between is empty.
2. **A scroll box inside the menu.** When the list is tall (signed in as an admin), the top
   `FluentNav` scrolls on its own with its own scrollbar, while Developer and External stay fixed
   below it. The menu should scroll as one list.

## Required outcome

- The default template shows one continuous menu, as before 015: top-level links, then the
  categories in NavOrder, then External last, directly after the other links. No gap.
- Developer pages (`UserClaimsPage`, `ServicesPage`, `TestPage`, `StyleGuidePage`) go back to the
  default Top placement. No template page pins to the bottom.
- External renders in the main nav after the top groups, not in the pinned nav.
- The pinned nav renders only when at least one page uses `NavPlacement.Bottom`. Keep the feature;
  the default template no longer uses it.
- There is exactly one scroll container for the menu (`.twe-nav-menu`). The `FluentNav` elements
  inside it do not scroll or shrink on their own (`flex: none` or equivalent, via the existing
  `::deep` anchor). The pinned user card stays fixed below the menu.
- The icon rail (880–1279px) and the mobile drawer behave as they do now.
- Update the Purpose/Design comments in `NavMenu.razor`, `NavMenu.razor.css`, and the
  `TimeWarpPage.razor` note about NavPlacement.Bottom so they match.
- Adjust existing tests that assert Developer or External is in the pinned nav. If a test checks
  nav placement, add one: External is in the primary nav, and the pinned nav is absent when no
  page is Bottom.

## Checklist

- [x] External moved back into the primary nav
- [x] Developer pages back to Top placement
- [x] Pinned nav rendered only when Bottom groups exist
- [x] Single scroll container; inner FluentNav does not scroll
- [x] Comments and tests updated
- [x] `dev build` 0/0, tests pass, `ganda repo audit` clean

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-24)
- Implementation: 01a16d1e-d936-7d21-840c-d5beb8e4279d (2026-10-24)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-24T09:05:55Z
- Review oracle: tw-implementation-review, general reviewer (claude-opus-5-5), effort 1, round 1, disposition clean, session not reported (2026-10-24)

## Results

External follows the top groups in the primary nav. `UserClaimsPage`, `ServicesPage`, `TestPage`, and `StyleGuidePage` use the default Top placement, so this template renders no pinned nav. The pinned nav still renders when a permitted page is `NavPlacement.Bottom`. `.twe-nav-menu` is the one scroll container. Each `FluentNav` is `flex: none` and `overflow: visible`, which overrides Fluent's `flex: 1 1 0%` and `overflow-y: auto`. The signed-in user card stays fixed under the menu.

### How to validate

Smoke: Sign out and open Home. Then sign in as an administrator and scroll the side menu. Resize through the icon rail (about 1024px wide) and below 880px.

Expect: Signed out, Home is followed immediately by External (The Freeze Team, then GitHub). There is no empty band between them and no Pinned nav. Signed in, Developer sits with the other categories and External follows the last group. The menu has one scrollbar, and Developer and External move with that scroll. The user card stays at the bottom of the column. The rail still clips labels. Below 880px the drawer is unchanged.

Checked: `dev build` 0 warnings, 0 errors. `web-spa-integration-tests` 497 passed, 0 failed. `dev lint-skills` 9/9. `ganda repo audit` 33 passed, 0 failed. Headless Chromium loaded the compiled scoped rules plus Fluent's `.fluent-nav` rule: a short list leaves External directly under Home (292px of column left below the list), a tall list scrolls `.twe-nav-menu` and not the inner nav, and the user card stays at the column bottom while the menu scrolls. A Bottom group still sits on the menu's bottom edge. The live Aspire shell was not opened.

### Review

- Rounds: 1. Roster: general. Effort: 1 (by diff, 186 lines).
- Final counts: bug 0, suggestion 0, nit 0. All are 0 open, 0 fixed, 0 wontfix.
- Disposition: **clean**. No findings. The reviewer re-ran `web-spa-integration-tests`: 497 passed, 0 failed.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Notes

Steve reported both with screenshots on 2026-10-24 ("look at all the white space"; "this wierd
shit of scrool in a menu").
