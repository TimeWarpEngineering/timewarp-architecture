namespace TimeWarp.Architecture.Components;

using BlazorState.Features.JavaScriptInterop;
using BlazorState.Features.Routing;
using BlazorState.Pipeline.ReduxDevTools;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using TimeWarp.Architecture.Features.ClientLoaders;

public partial class App : ComponentBase
{
  [Inject] private ClientLoader ClientLoader { get; set; }
  [Inject] private JsonRequestHandler JsonRequestHandler { get; set; }
#if ReduxDevToolsEnabled
  [Inject] private ReduxDevToolsInterop ReduxDevToolsInterop { get; set; }
#endif
  [Inject] private RouteManager RouteManager { get; set; }

  protected override async Task OnAfterRenderAsync(bool aFirstRender)
  {
#if ReduxDevToolsEnabled
    await ReduxDevToolsInterop.InitAsync().ConfigureAwait(false);
#endif
    await JsonRequestHandler.InitAsync().ConfigureAwait(false);
    await ClientLoader.LoadClient().ConfigureAwait(false);
  }
}
