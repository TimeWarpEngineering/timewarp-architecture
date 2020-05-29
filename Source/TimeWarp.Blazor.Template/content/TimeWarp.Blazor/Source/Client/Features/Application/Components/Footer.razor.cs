namespace TimeWarp.Blazor.Features.Applications.Components
{
  using TimeWarp.Blazor.Features.Bases;

  public partial class Footer: BaseComponent
  {
    protected string Version => ApplicationState.Version;
  }
}
