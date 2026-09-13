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

**Decided (Steve, 2026-09-13): option 1 — delete.** FluentMenu in `Profile.razor` is the product menu; the hand-rolled state is a pre-FluentUI leftover. Do not wire option 2.

Options considered:

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

- [x] Delete `source/container-apps/web/projects/web-spa/features/profile-menu/` (state, ActionSets, any `.razor`/`.css` in the folder)
- [x] Confirm no remaining references: generated ActionSet consumers, `_Imports.razor`, Redux DevTools registrations, tests, skills
- [x] Reconcile any Design region that pointed at task 211 or `ProfileMenuState`
- [x] Live Profile FluentMenu still opens/closes and stays in the viewport
- [x] `dev build` 0/0
- [x] `dev test`
- [x] Implementation review (effort 1) — disposition clean

## Notes

- Opened from 210-006 (M36). Linked from the Toggle file's Design region.
- Parent epic: 210 round-1 ledger
  `kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md`.
- Historical 210 / 210-006 / 152 / 149 kanban text still names the deleted path; those are closed-task records, not product consumers.

## Session

- Decision recorded: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-13)
- Implementer: grok session 01a098d4-4e5a-74b0-9539-88aab9ab9447 (2026-09-13)
- Review oracle: grok session 01a098df-5cee-7a72-8a52-82effe95a7b7 (2026-09-13)
- Created: 397564 (2026-09-12)

## Results

Deleted unused `ProfileMenuState` (option 1). FluentMenu in `Profile.razor` remains the product header menu. Opening/Closing + `NotifyLossOfInterest` were not wired.

**What changed**

- Removed `source/container-apps/web/projects/web-spa/features/profile-menu/` (`profile-menu-state.cs`, `.toggle.cs`, `.close.cs`, `.debug.cs`). No `.razor`/`.css` remained in that folder.
- Removed leftover `_ContentIncludedByDefault` excludes in `web-spa.csproj` for already-gone `ProfileDropDown` / `ProfileMenuNavLink` / `ProfileMenuPanel`.
- `Profile.razor` and `TimeWarpPage.razor` (`<Profile />` in the appbar) were not edited. Task-152 right-align CSS on `fluent-menu-list` is unchanged.

**References checked (no product consumers)**

- `_Imports.razor` never imported `TimeWarp.Architecture.Features.ProfileMenus`.
- No tests, skills, or Redux DevTools registrations referenced `ProfileMenuState`. TimeWarp.State auto-registers `IState` types; deleting the class is enough.
- `[StateAccess]` generated no remaining ActionSet consumers after the folder delete (build 0/0).
- Source Design region that pointed at task 211 lived only on the deleted Toggle file.
- `NavMenu.razor` comment "Login is profile-menu" describes the live FluentMenu placement, not the deleted state.

**Test outcomes:** `dev build` → 0 Warning(s) 0 Error(s). `dev test` → Tests completed successfully! (skipped: `RunForever`, weather SPA quarantine from task 058).

**Review** (effort 1, general only; 1 round)

- Roster: general (`review/round-1/general.md`)
- Final counts: bug 0 / suggestion 0 / nit 0 (all open/fixed/wontfix = 0)
- Disposition: **clean** (`review/disposition.md`) — no issues raised; no wontfix; no escalation
- Paths: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`

### How to validate

**Smoke**

```bash
test ! -d source/container-apps/web/projects/web-spa/features/profile-menu && echo GONE
# expect: GONE

rg -n "ProfileMenuState|Features\\.ProfileMenus" source tests
# expect: no matches

rg -n "profile-menu" source/container-apps/web/projects/web-spa --glob '!**/obj/**'
# expect: only NavMenu.razor comment "Login is profile-menu"

rg -n "FluentMenu|inset-inline-end: anchor\\(end\\)" source/container-apps/web/projects/web-spa/features/profiles/components/Profile.razor
# expect: FluentMenu with slot=trigger, FluentMenuList items (Profile/Settings/Sign out and Sign-in), and the task-152 right-align override
```

**Expect**

- `web-spa.csproj` has no `_ContentIncludedByDefault Remove` for `features\profile-menu\...`.
- Header still composes `<Profile />` from `source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor`.
- Manual UI (optional; markup unchanged): `dev run`, click the header avatar — menu opens, items stay in the viewport, outside-click dismisses via FluentMenu.

**Automated gate**

```bash
dotnet run tools/dev-cli/dev.cs -- build
# expect: Build succeeded. 0 Warning(s) 0 Error(s)

dotnet run tools/dev-cli/dev.cs -- test
# expect: Tests completed successfully!
```

**Not in scope:** option 2 (wire Opening/Closing + `NotifyLossOfInterest`). Historical kanban text under `kanban/done/` that still names the deleted Toggle path.
