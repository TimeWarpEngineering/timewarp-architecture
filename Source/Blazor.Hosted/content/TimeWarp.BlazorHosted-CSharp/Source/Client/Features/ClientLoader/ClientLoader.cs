namespace BlazorHostedCSharp.Client.Features.ClientLoader
{
  using Microsoft.Extensions.Logging;
  using Microsoft.JSInterop;
  using System;
  using System.Threading.Tasks;

  public class ClientLoader
  {
    private IClientLoaderConfiguration ClientLoaderConfiguration { get; }

    private IJSRuntime JSRuntime { get; }

    private ILogger Logger { get; }

    public ClientLoader
    (
      ILogger<ClientLoader> aLogger,
      IJSRuntime aJSRuntime,
      IClientLoaderConfiguration aClientLoaderConfiguration
    )
    {
      Logger = aLogger;
      Logger.LogDebug($"{GetType().Name}: constructor");
      JSRuntime = aJSRuntime;
      ClientLoaderConfiguration = aClientLoaderConfiguration;
    }

    public async Task InitAsync()
    {
      
      await Task.Delay(TimeSpan.FromSeconds(10));
      //await Task.Delay(ClientLoaderConfiguration.DelayTimeSpan);
      await LoadClient();
    }

    public async Task LoadClient()
    {
      const string LoadClientInteropName = "TimeWarp.loadClient";
      Logger.LogDebug(LoadClientInteropName);
      await JSRuntime.InvokeAsync<object>(LoadClientInteropName);
    }
  }
}