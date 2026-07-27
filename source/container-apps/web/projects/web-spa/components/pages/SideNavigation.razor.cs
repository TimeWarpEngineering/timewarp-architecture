#region Purpose
// Code-behind for the side navigation container: exposes the RenderFragment slot the shell fills with nav links.
#endregion

namespace TimeWarp.Architecture.Components;

partial class SideNavigation
{
  [Parameter] public RenderFragment? SideNavigationContent { get; set; }
}
