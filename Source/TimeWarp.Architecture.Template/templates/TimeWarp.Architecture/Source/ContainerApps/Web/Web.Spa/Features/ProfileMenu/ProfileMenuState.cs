namespace TimeWarp.Architecture.Features.ProfileMenus;

[StateAccessMixin]
internal sealed partial class ProfileMenuState: State<ProfileMenuState>
{
  public bool IsOpen { get; private set; }

  public override void Initialize()
  {
    IsOpen = false;
  }
}
