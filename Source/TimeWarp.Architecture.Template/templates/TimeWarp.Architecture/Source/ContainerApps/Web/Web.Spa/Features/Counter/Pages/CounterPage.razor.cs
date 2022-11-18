namespace TimeWarp.Architecture.Pages;

using BlazorState.Features.Routing;
using System.Threading.Tasks;
using TimeWarp.Architecture.Features.Base;
using static TimeWarp.Architecture.Features.Applications.ApplicationState;

public partial class CounterPage : BaseComponent
{
  private const string RouteTemplate = "/Counter";

  public static string GetRoute() => RouteTemplate;

  private async Task ButtonClick() =>
    await Send(new RouteState.ChangeRouteAction { NewRoute = "/" }).ConfigureAwait(false);

  private async Task ResetButtonClick() => await Send(new ResetStoreAction()).ConfigureAwait(false);
}
