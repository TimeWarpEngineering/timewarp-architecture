using TimeWarp.Blazor.Features.Bases.Client;

namespace TimeWarp.Blazor.Features.Applications.Components
{
  public partial class Footer: BaseComponent
  {
    protected string Version => ApplicationState.Version;
  }
}
