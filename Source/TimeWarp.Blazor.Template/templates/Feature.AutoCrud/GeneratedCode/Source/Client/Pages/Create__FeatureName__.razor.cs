namespace __RootNamespace__.Pages
{
  using BlazorState.Features.Routing;
  using __RootNamespace__.Features.__FeatureName__s;
  using __RootNamespace__.Features.Bases;
  using System.Threading.Tasks;
  using static BlazorState.Features.Routing.RouteState;
  using static __RootNamespace__.Features.__FeatureName__s.__FeatureName__State;

  public partial class CreateItem : BaseComponent
  {
    private const string RouteTemplate = "/__FeatureName__/Create";
    public static string GetRoute() => RouteTemplate;
    __FeatureName__CreateRequest __FeatureName__Model = new __FeatureName__CreateRequest();

    protected async Task HandleValidSubmit() => await Send(new __FeatureName__CreateAction() { __FeatureName__CreateRequest = __FeatureName__Model });
  }
}