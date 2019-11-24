namespace TimeWarp.Blazor.Client.ApplicationFeature
{
  using TimeWarp.Blazor.Client.Features.Base.Components;

  public partial class Footer
  {
    protected string Version => ApplicationState.Version;
  }
}
