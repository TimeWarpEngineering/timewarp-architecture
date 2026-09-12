# Profile menu transitions and NotifyLossOfInterest

## Description

Decide the fate of `ProfileMenuState` (Opening/Closing transitions and
TimeWarp.State `NotifyLossOfInterest`). Spun out of 210-006 / 210 round-1
finding M36 so the hygiene task does not own the UX work.

## Requirements

`ProfileMenuState` has four phases (`Closed`, `Opening`, `Open`, `Closing`) and a
Close action that moves `Open → Closing`. Toggle still jumps `Closed ↔ Open`
because the transition path and `NotifyLossOfInterest` were never wired.

The **live** header menu is `features/profiles/components/Profile.razor`, which
uses FluentUI `FluentMenu` (click-to-open, light-DOM popup, outside-dismiss are
the web component's job). Nothing in the SPA reads `ProfileMenuState`. There
are no tests for it.

Pick one:

1. **Delete** the unused `features/profile-menu/` state if FluentMenu remains
   the product menu (likely). Confirm no generated ActionSet consumers, no
   Redux DevTools-only workflow that needs it, then remove the folder.
2. **Wire it** only if a consumer is restored: Toggle `Closed → Opening → Open`
   and `Open → Closing → Closed`; Closing must complete to Closed; outside-click
   / `NotifyLossOfInterest` must dismiss via Close.

Files:

- `source/container-apps/web/projects/web-spa/features/profile-menu/` (state)
- `source/container-apps/web/projects/web-spa/features/profiles/components/Profile.razor` (live UI)

## Checklist

- [ ] Either delete unused `ProfileMenuState` or wire transitions + loss-of-interest
- [ ] Live Profile FluentMenu still opens/closes and stays in the viewport
- [ ] `dev build` 0/0
- [ ] `dev test`

## Notes

- Opened from 210-006 (M36). Linked from the Toggle file's Design region.
- Parent epic: 210 round-1 ledger
  `kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md`.

## Session

- Created: 397564 (2026-09-12)
