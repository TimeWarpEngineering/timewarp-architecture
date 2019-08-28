namespace TimeWarp.Blazor.Client
{
  using TimeWarp.Blazor.Client.Features.ClientLoaderFeature;
  using BlazorState.Features.JavaScriptInterop;
  using BlazorState.Features.Routing;
  using BlazorState.Pipeline.ReduxDevTools;
  using Microsoft.AspNetCore.Components;
  using System.Threading.Tasks;

  public class AppBase : ComponentBase
  {
    [Inject] private ClientLoader ClientLoader { get; set; }
    [Inject] private JsonRequestHandler JsonRequestHandler { get; set; }
    [Inject] private ReduxDevToolsInterop ReduxDevToolsInterop { get; set; }

    /// <remarks>
    /// Injected so it is created by the container. Even though the IDE says it is not used it is.
    /// </remarks>
    [Inject] private RouteManager RouteManager { get; set; }

    protected override async Task OnAfterRenderAsync()
    {
      await ReduxDevToolsInterop.InitAsync();
      await JsonRequestHandler.InitAsync();
      await ClientLoader.InitAsync();
    }
  }
}