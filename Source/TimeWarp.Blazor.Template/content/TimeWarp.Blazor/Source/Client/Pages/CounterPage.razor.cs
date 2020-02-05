namespace TimeWarp.Blazor.Client.Pages
{
  using BlazorState.Features.Routing;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Client.BaseFeature;

  public partial class CounterPage: BaseComponent
  {
    public const string Route = "/counter";

    protected async Task ButtonClick() =>
      _ = await Mediator.Send(new RouteState.ChangeRouteAction { NewRoute = "/" });
  }
}
