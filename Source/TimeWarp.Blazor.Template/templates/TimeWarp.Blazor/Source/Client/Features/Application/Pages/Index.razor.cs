namespace TimeWarp.Blazor.Pages
{
  using TimeWarp.Blazor.Features.Bases;

  public partial class Index : BaseComponent
  {
    private const string RouteTemplate = "/";

    public static string GetRoute() => RouteTemplate;
  }
}
