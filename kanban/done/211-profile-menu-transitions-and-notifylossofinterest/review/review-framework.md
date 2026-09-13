# Review framework — task 211

**Date:** 2026-09-13
**Host task:** kanban/in-progress/211-profile-menu-transitions-and-notifylossofinterest/
**Diff scope:** branch `task/211-profile-menu-transitions-and-notifylossofinterest` vs `origin/master` (product commit `40d3c3bb`; kanban brief/results excluded from product review)
**Plan / brief:** Steve decided option 1 — delete unused `ProfileMenuState` leftover. Remove `source/container-apps/web/projects/web-spa/features/profile-menu/` (state, ActionSets, debug hydrate). Remove leftover `_ContentIncludedByDefault` excludes in `web-spa.csproj` for already-gone ProfileDropDown / ProfileMenuNavLink / ProfileMenuPanel. Do not wire Opening/Closing + `NotifyLossOfInterest`. Live header menu remains FluentMenu in `features/profiles/components/Profile.razor`. Confirm no remaining product consumers (generated ActionSets, `_Imports.razor`, Redux DevTools registrations, tests, skills).
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok review oracle 01a098df-5cee-7a72-8a52-82effe95a7b7 (2026-09-13)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
