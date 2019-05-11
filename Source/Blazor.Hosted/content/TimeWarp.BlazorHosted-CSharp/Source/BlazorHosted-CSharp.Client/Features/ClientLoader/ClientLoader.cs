namespace BlazorHostedCSharp.Client.Features.ClientLoader
{
  using Microsoft.Extensions.Logging;
  using Microsoft.JSInterop;
  using System.Threading.Tasks;

  public class ClientLoader
  {

    private IClientLoaderConfiguration ClientLoaderConfiguration { get; }

    private IJSRuntime JSRuntime { get; }

    private ILogger Logger { get; }

    public ClientLoader(IClientLoaderConfiguration aClientLoaderConfiguration)
    {
      ClientLoaderConfiguration = aClientLoaderConfiguration;
    }

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

    public async Task InitAsync()
    {
      await Task.Delay(ClientLoaderConfiguration.DelayTimeSpan);
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