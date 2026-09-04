#region Purpose
// State caching the signed-in user's display profile for the app chrome and Profile page.
#endregion

#region Design
// Avatar defaults to a FluentUI Person icon encoded as a data URI so the UI always has an
// image to render — no network fetch and no broken-image flash before profile data loads.
// A null Alias is the field value (or no snapshot yet), not loading UI — in-flight fetch
// is [TrackAction] on FetchProfileData. Population and reset happen only through the
// Fetch/Clear/Update action-set partials. Task 148 D7 / 205: Language/Region/Theme/Notifications/Email
// mirror GetProfile.Response; Initialize clears them so sign-out does not leave stale prefs.
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

[StateAccess]
public sealed partial class ProfileState: State<ProfileState>
{
  public string? Alias { get; private set; }
  public string? Email { get; private set; }
  public string? Avatar { get; private set; }
  public string? Language { get; private set; }
  public string? Region { get; private set; }
  public string? Theme { get; private set; }
  public bool? Notifications { get; private set; }

  public override void Initialize()
  {
    Alias = null;
    Email = null;
    Avatar = new Icons.Regular.Size48.Person().ToDataUri(size: "25px", color: "white");
    Language = null;
    Region = null;
    Theme = null;
    Notifications = null;
  }
}
