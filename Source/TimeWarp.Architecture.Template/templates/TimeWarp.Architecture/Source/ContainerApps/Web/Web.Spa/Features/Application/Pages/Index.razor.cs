namespace TimeWarp.Architecture.Pages;

using TimeWarp.Architecture.Features;

public partial class Index : BaseComponent
{
  private const string RouteTemplate = "/";

  public static string GetRoute() => RouteTemplate;

}
