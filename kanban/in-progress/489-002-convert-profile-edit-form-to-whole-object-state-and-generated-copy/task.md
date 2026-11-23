# Convert Profile edit form to whole-object state and generated copy

## Goal (implementer restatement)

The signed-in profile screen should edit a copy of the profile the server just returned, and the store should keep that response as one object. `ProfileState` drops the separate display name, email, avatar, language, region, theme, and notifications fields. It keeps `GetProfile.Response? Profile`, and only the fetch and update handlers assign it. The header, the home strip, the shell user card, and the agent page facts read that object. Until a response exists, the avatar is still the person icon.

`ProfilePage` stops copying those fields one by one. When the page loads and when Cancel is clicked, it builds `UpdateProfile.Command` with `UpdateProfile.Command.From(ProfileState.Profile)`. That copy does not run on every render. After Save, the page copies again so the form shows what the server stored. Language, region, and theme defaults (`en-US`, `US`, `system`) are filled when the server builds a response, including when a stored profile has those preferences blank. The page does not invent them.

Acceptance:

- Fetch stores the `GetProfile.Response`. Update stores a new `GetProfile.Response` with the saved fields and the avatar already on the snapshot.
- Cancel puts the form back to that snapshot. Save sends the command and the reloaded page shows the saved name.
- A stored profile with a blank language, region, or theme still produces a response that carries `en-US`, `US`, and `system`.
- Sign-out clears `Profile`. Chrome that used the old fields still shows the name, email, and avatar.

## Description

Reference conversion.

- `ProfileState`: replace `DisplayName`, `Email`, `Avatar`, `Language`, `Region`, `Theme`,
  `Notifications` with `GetProfile.Response? Profile`, set only in
  `profile-state.fetch-profile-data.cs` / `profile-state.update-profile.cs`. Update chrome
  consumers (header avatar / display name) and `profile-state.agent-page-facts.cs`.
- `ProfilePage.razor`: replace `SyncCommandFromState()` with
  `UpdateProfile.Command.From(ProfileState.Profile)` on load and Cancel. Copy only on entering
  edit, not every render.
- The `?? "en-US"` / `?? "US"` / `?? "system"` defaults move server-side (Response always
  carries them) — they are the non-mechanical part.
- Keep the Avatar fallback icon behaviour.

## Depends on

- 489-001

## Checklist

- [x] State + handlers
- [x] Page
- [x] Consumers of the removed properties
- [x] Tests (integration / playwright profile save + cancel)

## Results

`ProfileState` stores `GetProfile.Response? Profile`. Fetch assigns the response. Update builds a new response from the saved fields and keeps the snapshot avatar, or `ProfileState.FallbackAvatar` when there is no snapshot. Header, home, the shell user card, and the profile page read `Profile`. Agent page facts treat a null profile as no snapshot and still emit display name, email, language, region, theme, and notifications.

`ProfilePage` builds `UpdateProfile.Command` with `UpdateProfile.Command.From` after the interactive load, on Cancel, and after Save. It does not copy on every render. A failed save that does not throw copies the last snapshot back into the form.

Language, region, and theme blanks are filled in the GET and PUT response mapping by `ProfilePreferenceDefaults` (`en-US`, `US`, `system`). Create-if-missing uses the same constants, including display name `Member`. A real stored code is kept, and the stored row is not rewritten on read. Display name is not coalesced.

### How to validate

Smoke: sign in, open `/Profile`, and wait until the display name is filled. Type another name and click Cancel. Type `Ada Lovelace` and click Save. Reload `/Profile`.

Expect: the form opens on the server profile (`Member` for a new profile). Cancel restores that name. Save shows `Ada Lovelace` on the shell user card and on the profile form, and both still show it after reload. The avatar stays the person icon when no image has been stored.

Gates:

- `./bin/dev build` — succeeded, 0 warnings, 0 errors.
- `dotnet run --file source/container-apps/web/features/profile/get-profile/get-profile-tests.cs` — passed, 16 tests, including `Stored_blank_preferences_Should_ReturnResponseDefaults`.
- `dotnet run --file source/container-apps/web/features/profile/update-profile/update-profile-tests.cs` — passed, 16 tests.
- `tests/container-apps/web/web-spa-integration-tests/features/profiles/profile-state-tests.cs` — not a standalone runfile (no `#if !JARIBU_MULTI`). `./bin/dev test` does not take a file. CI runs the project. `dotnet run --project tests/container-apps/web/web-spa-integration-tests -c Release -- --filter-class FetchAndUpdate_Should_` — passed, 3 tests.
- `tests/container-apps/web/web-spa-integration-tests/features/profiles/sign-out-state-tests.cs` — not a standalone runfile (no `#if !JARIBU_MULTI`). CI runs the project. `dotnet run --project tests/container-apps/web/web-spa-integration-tests -c Release --no-build -- --filter-class SignOut_Should_` — passed, 2 tests.
- `tests/container-apps/web/web-spa-integration-tests/components/time-warp-page-render-tests.cs` — not a standalone runfile (no `#if !JARIBU_MULTI`). CI runs the project. `dotnet run --project tests/container-apps/web/web-spa-integration-tests -c Release --no-build -- --filter-method Show_The_Profile_Alias_When_It_Arrives_After_The_First_Render` — passed, 1 test.
- `tests/container-apps/web/web-spa-playwright-tests/profile-edit-playwright-tests.cs` — not a standalone runfile (no `#if !JARIBU_MULTI`). CI runs the project. `dotnet run --project tests/container-apps/web/web-spa-playwright-tests -c Release --no-build -- --filter-class ProfileEdit_Given_Wasm` — passed, 1 test (save and cancel).
- `ganda repo audit` — exit 0. 43 passed, 2 skipped, 1 advisory warning: `vscode-window-icon` (`.vscode/settings.json` is missing; gitignored local state, pre-existing). `analyzer-suppressions` passed: 843 suppressions, none above the baseline.

## Session

- Implementation: Grok 4.7, 2026-11-23. The profile store keeps one `GetProfile.Response`. The page copies it with `UpdateProfile.Command.From` on load, Cancel, and after Save. Blank language, region, and theme are filled when the server builds the response.

## Parent

- 489
