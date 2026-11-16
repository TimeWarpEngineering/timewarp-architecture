# Convert Profile edit form to whole-object state and generated copy

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

- [ ] State + handlers
- [ ] Page
- [ ] Consumers of the removed properties
- [ ] Tests (integration / playwright profile save + cancel)

## Results

## Parent

- 489
