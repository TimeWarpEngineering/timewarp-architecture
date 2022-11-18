namespace TimeWarp.Architecture.Pages;

using System.Threading.Tasks;
using TimeWarp.Architecture.Features.Applications;
using TimeWarp.Architecture.Features.Base;

public partial class Index : BaseComponent
{
  private const string RouteTemplate = "/";

  public static string GetRoute() => RouteTemplate;

  private async Task FiveSecondTaskButtonClick() =>
    await Send(new ApplicationState.FiveSecondTaskAction());

  private async Task TwoSecondTaskButtonClick() =>
    await Send(new ApplicationState.TwoSecondTaskAction());
}
