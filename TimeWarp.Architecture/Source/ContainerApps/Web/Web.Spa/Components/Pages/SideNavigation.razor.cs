namespace TimeWarp.Architecture.Components;

public partial class SideNavigation : BaseComponent
{
  [Parameter] public RenderFragment? SideNavigationContent { get; set; }
}
