namespace TimeWarp.Blazor.Pages
{
  using TimeWarp.Blazor.Features.Bases;

  public partial class LogoutPage : BaseComponent
  {
    private const string RouteTemplate = "/Logout";

    public static string GetRoute() => RouteTemplate;

  }
}
