namespace __RootspaceName__.Pages
{
  using BlazorState.Features.Routing;
  using __RootspaceName__.Features.__FeatureName__s;
  using __RootspaceName__.Features.Bases;
  using __RootspaceName__.Models;
  using System.Threading.Tasks;
  using static __RootspaceName__.Features.__FeatureName__s.__FeatureName__State;

  public partial class CreateItem : BaseComponent
  {
    private const string RouteTemplate = "/__FeatureName__/Create";
    public static string GetRoute() => RouteTemplate;


  }
}