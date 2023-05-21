namespace TimeWarp.Architecture.Components;

public partial class App : ComponentBase
{
  [Inject] private ClientLoader ClientLoader { get; set; }
  [Inject] private JsonRequestHandler JsonRequestHandler { get; set; }
  #if DEBUG
  [Inject] private ReduxDevToolsInterop ReduxDevToolsInterop { get; set; }
  #endif

  [Inject] private RouteManager RouteManager { get; set; }

  protected override async Task OnAfterRenderAsync(bool aFirstRender)
  {
    #if DEBUG
    await ReduxDevToolsInterop.InitAsync().ConfigureAwait(false);
    #endif
    await JsonRequestHandler.InitAsync().ConfigureAwait(false);
    await ClientLoader.LoadClient().ConfigureAwait(false);
  }
}
