namespace TimeWarp.Architecture.Features.Sidebars;

[StateAccessMixin]
internal sealed partial class SidebarState: State<SidebarState>
{
  public bool IsOpen { get; private set; }

  public override void Initialize()
  {
    IsOpen = false;
  }
}
