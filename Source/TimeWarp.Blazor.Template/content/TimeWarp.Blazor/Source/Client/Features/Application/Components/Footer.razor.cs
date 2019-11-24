namespace TimeWarp.Blazor.Client.ApplicationFeature
{
  using TimeWarp.Blazor.Client.BaseFeature;

  public partial class Footer
  {
    protected string Version => ApplicationState.Version;
  }
}
