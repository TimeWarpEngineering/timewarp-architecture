#region Purpose
// In-proc SPA ServiceProvider for analytics pipeline tests: TimeWarp.State + TrackEventBehavior
// + a recording IWebServerApiService. No Aspire host.
#endregion

#region Design
// C-create (AGENTS.md fixture-lifetime default): these facts substitute IWebServerApiService,
// which the closed-box AspireSpaTestApplication cannot do. TrackEventBehavior is compile-time
// on ClientPipeline. Recording is a singleton so SpaTestScope sees the same instance the
// TrackEvent handler resolves. Message-bar handlers record state and do not need a provider.
#endregion

namespace TimeWarp.Architecture.Web.Spa.Integration.Tests.Features.Analytics;

using Microsoft.JSInterop;

internal sealed class AnalyticsSpaTestApplication : ISpaTestApplication, IDisposable
{
  public IServiceProvider ServiceProvider { get; }
  public RecordingWebServerApiService Recording { get; }

  public AnalyticsSpaTestApplication()
  {
    Recording = new RecordingWebServerApiService();
    ServiceCollection services = new();

    services.AddLogging();
    services.AddWebSpaGeneratedMediator();
    services.AddTimeWarpState
    (
      options =>
      {
        options.Assemblies =
        [
          typeof(TimeWarp.Architecture.Web.Spa.IAssemblyMarker).Assembly
        ];
      }
    );

    // Fully qualified: TimeWarp.Architecture.Services is a global using only when the api
    // template flag is on; IWebServerApiService is the BFF client and exists without api.
    services.AddSingleton<TimeWarp.Architecture.Services.IWebServerApiService>(Recording);

    IJSRuntime fakeJsRuntime = FakeItEasy.A.Fake<IJSRuntime>();
    services.AddScoped(_ => fakeJsRuntime);

    ServiceProvider = services.BuildServiceProvider();
  }

  public void Dispose()
  {
    if (ServiceProvider is IDisposable disposable)
    {
      disposable.Dispose();
    }
  }
}
