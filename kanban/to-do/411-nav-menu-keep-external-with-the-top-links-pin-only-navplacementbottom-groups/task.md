# Nav menu: keep External with the top links; pin only NavPlacement.Bottom groups

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

- [ ] External moved back into the primary nav
- [ ] Developer pages back to Top placement
- [ ] Pinned nav rendered only when Bottom groups exist
- [ ] Single scroll container; inner FluentNav does not scroll
- [ ] Comments and tests updated
- [ ] `dev build` 0/0, tests pass, `ganda repo audit` clean

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-24)

## Notes

Steve reported both with screenshots on 2026-10-24 ("look at all the white space"; "this wierd
shit of scrool in a menu").
