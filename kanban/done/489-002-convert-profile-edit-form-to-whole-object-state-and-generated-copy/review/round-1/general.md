# Round 1 — general
**Date:** 2026-11-23
**Scope reviewed:** commit ce52debcd (product and test files; kanban excluded), plus call sites of
`ProfileState` in `source/container-apps/web/projects/web-spa`, `skills/`, and `documentation/`.

## Summary

`ProfileState` now holds one `GetProfile.Response? Profile`. Only the fetch and update action-set
handlers write it, and `Initialize` clears it on sign-out. The update handler rebuilds a
`GetProfile.Response` from `UpdateProfile.Response` and keeps the snapshot avatar, or
`ProfileState.FallbackAvatar` when there is no snapshot. `ProfilePage` replaces the field-by-field
`SyncCommandFromState` with `UpdateProfile.Command.From(profile)` after the interactive load, on
Cancel, and after Save. It does not copy on render. The `en-US` / `US` / `system` defaults moved to
`ProfilePreferenceDefaults`, which the GET and PUT handlers use for create-if-missing and to fill a
null or whitespace stored preference in the response. The stored row is not rewritten. Header
(`Profile.razor`), `HomePage`, `ShellUserCard`, and the agent page facts read `Profile`, and the
avatar falls back to the person icon. `StateJsonContext` registers `GetProfile.Response` under its
own `TypeInfoPropertyName` so it does not collide with the other nested `Response` types.

Checks made:

- No reference to the removed fields (`ProfileState.DisplayName|Email|Avatar|Language|Region|Theme|Notifications`)
  or to `SyncCommandFromState` remains outside `kanban/` (`rg`). No skill or documentation page names them.
- The web-spa no longer substitutes `en-US` / `US` / `system`. `GetProfile.Response` and
  `UpdateProfile.Response` guard language, region, and theme with `Guard.Against.NullOrEmpty`, so
  before this change a null or empty stored preference failed the response. The new
  `Or*` helpers close that gap. Whitespace-only values are also filled.
- The goal restatement matches the task description and acceptance list. No misread.
- Tests cover each acceptance line: fetch stores the whole response and a `Command.From` copy does
  not change the store (`profile-state-tests.cs`). Update keeps the fetched avatar, and with no
  snapshot it uses the fallback. Sign-out clears `Profile` (`sign-out-state-tests.cs`). The server
  fills blank preferences without rewriting the row (`Stored_blank_preferences_Should_ReturnResponseDefaults`).
  The Playwright test covers load, then Cancel, then Save, then reload.
- Recorded gates in `## Results` cover the change: build, both handler runfiles, the three
  integration classes, the Playwright class, and `ganda repo audit`. No gate was re-run here, and no
  targeted command was needed.

Observation (not a finding): replacing `Command` gives `EditForm` a new `Model`, so the form subtree
is rebuilt on load, Cancel, and Save. The Playwright test shows that the Fluent combobox text and
the validation still work after that rebuild. A save that comes back as a problem response copies
the snapshot over the draft. The removed `SyncCommandFromState` call after `UpdateProfile` did the
same, and `## Results` records this behavior, so it is not a regression.

## Issues

None.
