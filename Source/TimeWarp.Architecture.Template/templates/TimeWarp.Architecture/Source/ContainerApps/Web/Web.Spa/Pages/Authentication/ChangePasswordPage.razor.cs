namespace TimeWarp.Architecture.Pages;

using TimeWarp.Architecture.Features.Base;

public partial class ChangePasswordPage : BaseComponent
{
  private const string RouteTemplate = "/changePassword";

  public static string GetRoute() => RouteTemplate;
}
