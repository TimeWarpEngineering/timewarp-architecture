namespace TimeWarp.Architecture.Pages;

using TimeWarp.Architecture.Features.Base;

public partial class LoginPage : BaseComponent
{
  private const string RouteTemplate = "/Login";

  public static string GetRoute() => RouteTemplate;
}
