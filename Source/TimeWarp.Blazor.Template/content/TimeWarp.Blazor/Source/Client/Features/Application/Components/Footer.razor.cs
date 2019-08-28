namespace TimeWarp.Blazor.Client.Features.Application.Components
{
  using TimeWarp.Blazor.Client.Features.Base.Components;

  public class FooterBase: BaseComponent
  {
    protected string Version => ApplicationState.Version;
  }
}
