namespace TimeWarp.Architecture.Features.ProfileMenus.Components;

public partial class ProfileMenuNavLink
{
  [Parameter] public RenderFragment ChildContent { get; set; }

  int? GetTabIndex() => ProfileMenuState.MenuState == ProfileMenuState.MenuStates.Open ? null : -1;
}
