#region Purpose
// State caching the signed-in user's display alias and avatar for the app chrome.
#endregion

#region Design
// Avatar defaults to a FluentUI Person icon encoded as a data URI so the UI always has an
// image to render — no network fetch and no broken-image flash before profile data loads.
// A null Alias signals "no profile loaded"; population and reset happen only through the
// Fetch/Clear action-set partials.
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

[StateAccess]
public sealed partial class ProfileState: State<ProfileState>
{
  public string? Alias { get; private set; }
  public string? Avatar { get; private set; }

  public override void Initialize()
  {
    Alias = null;
    Avatar = new Icons.Regular.Size48.Person().ToDataUri(size: "25px", color: "white");
  }
}
