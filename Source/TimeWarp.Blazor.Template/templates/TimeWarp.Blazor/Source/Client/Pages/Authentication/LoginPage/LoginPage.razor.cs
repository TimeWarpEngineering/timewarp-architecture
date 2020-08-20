namespace TimeWarp.Blazor.Pages
{
  using TimeWarp.Blazor.Features.Bases;

  public partial class LoginPage : BaseComponent
  {
    private const string RouteTemplate = "/Login";

    public static string GetRoute() => RouteTemplate;
  }
}
