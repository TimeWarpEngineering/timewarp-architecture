namespace TimeWarp.Architecture.Pages;

using BlazorState.Features.Routing;
using System.Threading.Tasks;
using static TimeWarp.Architecture.Features.Applications.ApplicationState;

public partial class EventStreamPage : BaseComponent
{
  private const string RouteTemplate = "/EventStream";

  public static string GetRoute() => RouteTemplate;

  private async Task ButtonClick() =>
    await Send(new RouteState.ChangeRouteAction { NewRoute = "/" }).ConfigureAwait(false);

  private async Task ResetButtonClick() => await Send(new ResetStoreAction()).ConfigureAwait(false);
}
