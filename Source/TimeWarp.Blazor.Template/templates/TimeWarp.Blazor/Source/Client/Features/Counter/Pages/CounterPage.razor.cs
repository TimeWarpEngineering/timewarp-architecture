namespace TimeWarp.Blazor.Pages
{
  using BlazorState.Features.Routing;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Features.Bases;

  public partial class CounterPage: BaseComponent
  {
    private const string RouteTemplate = "/counter";

    public static string GetRoute() => RouteTemplate;

    protected async Task ButtonClick() =>
      _ = await Mediator.Send(new RouteState.ChangeRouteAction { NewRoute = "/" });
  }
}
