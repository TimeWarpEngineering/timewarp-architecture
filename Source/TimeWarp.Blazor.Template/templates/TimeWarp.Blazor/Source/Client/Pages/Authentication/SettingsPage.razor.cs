namespace TimeWarp.Blazor.Pages
{
  using TimeWarp.Blazor.Features.Bases;

  public partial class SettingsPage : BaseComponent
  {
    private const string RouteTemplate = "/Settings";

    public static string GetRoute() => RouteTemplate;
  }
}
