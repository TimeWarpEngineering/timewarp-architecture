namespace eShopOnBlazorWasm.Pages.Catalog
{
  using BlazorState.Features.Routing;
  using eShopOnBlazorWasm.Features.Bases;
  using static eShopOnBlazorWasm.Features.CatalogItems.CatalogItemState;
  using System.Threading.Tasks;

  public partial class Index: BaseComponent
  {
    public const string Route = "/Catalog";

    protected async Task CreateClick() =>
      _ = await Mediator.Send(new RouteState.ChangeRouteAction { NewRoute = Create.Route });

    protected override async Task OnAfterRenderAsync(bool aFirstRender)
    {
      _ = await Mediator.Send(new FetchCatalogItemsAction());
    }
  }
}
