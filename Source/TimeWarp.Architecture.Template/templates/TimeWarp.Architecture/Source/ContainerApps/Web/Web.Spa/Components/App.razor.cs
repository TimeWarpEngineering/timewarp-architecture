namespace TimeWarp.Architecture.Components;

public partial class App : ComponentBase
{
  [Inject] private ClientLoader ClientLoader { get; set; } = default!;
  [Inject] private JsonRequestHandler JsonRequestHandler { get; set; } = default!;
  #if DEBUG
  [Inject] private ReduxDevToolsInterop ReduxDevToolsInterop { get; set; } = default!;
  #endif

  [Inject] private TimeWarpNavigationManager TimeWarpNavigationManager { get; set; }  = default!;

  protected override async Task OnAfterRenderAsync(bool aFirstRender)
  {
    #if DEBUG
    await ReduxDevToolsInterop.InitAsync().ConfigureAwait(false);
    #endif
    await JsonRequestHandler.InitAsync().ConfigureAwait(false);
    await ClientLoader.LoadClient().ConfigureAwait(false);
  }
}
