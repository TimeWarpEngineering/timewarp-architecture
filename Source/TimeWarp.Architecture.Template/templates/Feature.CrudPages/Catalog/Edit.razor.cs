namespace eShopOnBlazorWasm.Pages.Catalog
{
  using BlazorState.Features.Routing;
  using System.Threading.Tasks;
  using eShopOnBlazorWasm.Features.Bases;
  using Microsoft.AspNetCore.Components;

  public partial class Edit: BaseComponent
  {
    public const string Route = "/Catalog/Edit/{EntityId}";

    public static string RouteFactory(int aEntityId) =>
      Route.Replace($"{{{nameof(EntityId)}}}", aEntityId.ToString(), System.StringComparison.OrdinalIgnoreCase);

    [Parameter] public int EntityId { get; set; }

    protected async Task ButtonClick() =>
      _ = await Mediator.Send(new RouteState.ChangeRouteAction { NewRoute = "/" });
  }
}
