namespace TimeWarp.Architecture.Features.Applications.Components.NavBars.Dark;

public partial class ProfileMenuNavLink
{
  [Parameter] public RenderFragment ChildContent { get; set; }

  int? GetTabIndex() => ProfileMenuState.IsOpen ? null : -1;
}
