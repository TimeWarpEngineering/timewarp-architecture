namespace TimeWarp.Architecture.Components;

public partial class HeaderBar
{
  private async Task OnClickHandlerAsync() => await Send(new SidebarState.OpenSideBar.Action());
}
