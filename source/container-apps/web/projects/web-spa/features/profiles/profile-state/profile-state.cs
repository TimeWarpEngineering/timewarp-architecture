#region Purpose
// State caching the signed-in user's display profile for the app chrome and Profile page.
#endregion

#region Design
// The store holds the fetched GetProfile.Response, not a flattened copy of its fields.
// FetchProfileData and UpdateProfile write that snapshot. A prefill stores an immutable JSON
// suggestion, an id, and whether the person accepted it over unsaved edits. Initialize clears
// the snapshot and the suggestion so sign-out does not leave the previous person on screen.
// A null Profile is "not loaded yet", not a loading flag — in-flight fetch is [TrackAction]
// on FetchProfileData.
// FallbackAvatar is the FluentUI Person icon, a data URI, so chrome has an image before the
// response arrives and when an update runs with no snapshot. It is not stored on Profile.
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

[StateAccess]
public sealed partial class ProfileState : State<ProfileState>
{
  public static string FallbackAvatar { get; } =
    new Icons.Regular.Size48.Person().ToDataUri(size: "25px", color: "white");

  public GetProfile.Response? Profile { get; private set; }

  public string? ProfileSuggestionJson { get; private set; }

  public string? ProfileSuggestionId { get; private set; }

  public bool ProfileSuggestionAccepted { get; private set; }

  public override void Initialize()
  {
    Profile = null;
    ProfileSuggestionJson = null;
    ProfileSuggestionId = null;
    ProfileSuggestionAccepted = false;
  }
}
