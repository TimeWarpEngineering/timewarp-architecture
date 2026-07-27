#region Purpose
// State for the profile dropdown menu's open/closed lifecycle.
#endregion

#region Design
// Four states instead of a bool: Opening/Closing are transitional phases reserved for
// animated show/hide, letting handlers ignore input while a transition is in flight.
// Mutation happens only in the Toggle/Close action-set partials; the debug partial carries
// Redux DevTools hydration and test seeding.
#endregion

namespace TimeWarp.Architecture.Features.ProfileMenus;

[StateAccess]
public sealed partial class ProfileMenuState : State<ProfileMenuState>
{

  public enum MenuStates
  {
    Closed,
    Closing,
    Open,
    Opening
  }

  public MenuStates MenuState { get; private set; }

  public override void Initialize()
  {
    MenuState = MenuStates.Closed;
  }
}
