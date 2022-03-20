namespace TimeWarp.Architecture.Pages
{
  using TimeWarp.Architecture.Features.Bases;

  public partial class SettingsPage : BaseComponent
  {
    private const string RouteTemplate = "/Settings";

    public static string GetRoute() => RouteTemplate;
  }
}
