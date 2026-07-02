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
