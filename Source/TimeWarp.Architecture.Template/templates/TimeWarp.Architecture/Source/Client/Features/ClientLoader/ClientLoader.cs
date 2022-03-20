namespace TimeWarp.Architecture.Features.ClientLoaders
{
  using Microsoft.Extensions.Logging;
  using Microsoft.JSInterop;
  using System.Threading.Tasks;

  public class ClientLoader
  {
    private readonly IClientLoaderConfiguration ClientLoaderConfiguration;

    private readonly IJSRuntime JSRuntime;

    private readonly ILogger Logger;

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

    public async Task LoadClient()
    {
      await Task.Delay(ClientLoaderConfiguration.DelayTimeSpan).ConfigureAwait(false);
      const string LoadClientInteropName = "CompositionRoot.BlazorDualMode.LoadClient";
      Logger.LogDebug(LoadClientInteropName);
      await JSRuntime.InvokeAsync<object>(LoadClientInteropName).ConfigureAwait(false);
    }
  }
}
