namespace __RootspaceName__.Pages
{
  using BlazorState.Features.Routing;
  using __RootspaceName__.Features.__FeatureName__s;
  using __RootspaceName__.Features.Bases;
  using System.Threading.Tasks;
  using static BlazorState.Features.Routing.RouteState;
  using static __RootspaceName__.Features.__FeatureName__s.__FeatureName__State;

  public partial class CreateItem : BaseComponent
  {
    private const string RouteTemplate = "/__FeatureName__/Create";
    public static string GetRoute() => RouteTemplate;
    public __FeatureName__CreateRequest __FeatureName__CreateRequest { get; set; }

    protected async Task CancelClick() =>
    _ = await Mediator.Send(new ChangeRouteAction { NewRoute = Index.Route });

    protected async Task HandleValidSubmit()
    {
      _ = await Mediator.Send(new __FeatureName__CreateAction { __FeatureName__CreateRequest = __FeatureName__CreateRequest });
      _ = await Mediator.Send(new ChangeRouteAction { NewRoute = Index.Route });
    }




  }
}