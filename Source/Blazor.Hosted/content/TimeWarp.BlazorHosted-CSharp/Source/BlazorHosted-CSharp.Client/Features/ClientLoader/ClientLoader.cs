namespace BlazorState.Features.ClientLoader
{
  using System;
  using System.Threading.Tasks;
  using Microsoft.Extensions.Logging;
  using Microsoft.JSInterop;

  public class ClientLoader
  {
    private IJSRuntime JSRuntime { get; }

    private ILogger Logger { get; }

    public ClientLoader
    (
      ILogger<ClientLoader> aLogger,
      IJSRuntime aJSRuntime
    )
    {
      Logger = aLogger;
      Logger.LogDebug($"{GetType().Name}: constructor");
      JSRuntime = aJSRuntime;
    }

    public async Task LoadClient()
    {
      const string LoadClientInteropName = "TimeWarp.loadClient";
      Logger.LogDebug(LoadClientInteropName);
      await JSRuntime.InvokeAsync<object>(LoadClientInteropName);
    }

    public async Task InitAsync(TimeSpan aDelayTimeSpan)
    {
      await Task.Delay(aDelayTimeSpan);
      await LoadClient();
    }
  }
}