namespace TimeWarp.Architecture.Components;

using BlazorState.Features.JavaScriptInterop;
using BlazorState.Features.Routing;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

public partial class App : ComponentBase
{
  [Inject] private JsonRequestHandler JsonRequestHandler { get; set; }

  [Inject] private RouteManager RouteManager { get; set; }

  protected override async Task OnAfterRenderAsync(bool aFirstRender)
  {
    Console.WriteLine("Pre JsonRequestHandler.InitAsync");
    await JsonRequestHandler.InitAsync().ConfigureAwait(false);
    Console.WriteLine("Post JsonRequestHandler.InitAsync");
  }
}
