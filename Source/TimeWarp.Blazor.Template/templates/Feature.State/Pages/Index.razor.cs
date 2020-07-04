namespace __RootNamespace__.Features.__FeatureName__s.Pages
{
  using BlazorState.Features.Routing;
  using __RootNamespace__.Features.Bases;
  using static __RootNamespace__.Features.__FeatureName__s.__FeatureName__State;
  using System.Threading.Tasks;

  public partial class Index: BaseComponent
  {
    public const string Route = "/__FeatureName__s";

    protected async Task CreateClick() =>
      _ = await Mediator.Send(new RouteState.ChangeRouteAction { NewRoute = Create.Route });

    protected override async Task OnAfterRenderAsync(bool aFirstRender)
    {
      _ = await Mediator.Send(new Fetch__FeatureName__sAction());
    }
  }
}
