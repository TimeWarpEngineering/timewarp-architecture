#nullable enable
namespace TimeWarp.Architecture.Testing;

using Microsoft.Extensions.Configuration.Json;

#region Design
// Hermeticity (task 104-031): after CreateBuilder, this host UNCONDITIONALLY strips every
// user-secrets JsonConfigurationSource (Path == "secrets.json") from the configuration before the
// app reads it. WebApplicationHost builds every integration-test host as EnvironmentName=Development
// with ApplicationName=Web.Server, which otherwise loads the DEVELOPER's Web.Server user secrets into
// the test process — so a machine-local secret (e.g. WebAuthnOptions:AllowedRpIds pinning a personal
// share hostname) would silently alter test outcomes. Stripping the secrets.json sources makes tests
// depend only on committed appsettings + the test project's own appsettings, never on whatever the
// developer has in user secrets. Environment variables are deliberately LEFT intact (CI supplies
// secrets that way); only the user-secrets file source is removed. The WebAuthnOptions binding test
// pins this by asserting no configuration source path ends with secrets.json.
#endregion

/// <summary>
/// A wrapper around WebApplication.CreateBuilder that will launch the WebApplication when constructed and
/// And properly shut down the WebApplication when disposed.
/// </summary>
/// <remarks>This allows for registering a WebApplication as a dependency and DI can fire it up and shut it down.
/// Further configuring or overriding Services can be done by passing in delegate to the constructor
/// </remarks>
/// <example><see cref="WebTestServerApplication"/></example>
/// <typeparam name="TProgram">The IProgram Implementation to use</typeparam>
[NotTest]
public class WebApplicationHost<TProgram> : IAsyncDisposable
  where TProgram : IAspNetProgram
{
  public bool Started;
  private readonly WebApplication WebApplication;
  public readonly string[] Urls;

  public IServiceProvider ServiceProvider { get; }
  public IConfiguration Configuration { get; }

  /// <summary>
  /// Construct a WebApplication
  /// </summary>
  /// <param name="urls"></param>
  /// <param name="webApplicationOptions"></param>
  /// <param name="configureServicesDelegate"></param>
  public WebApplicationHost
  (
    string[] urls,
    WebApplicationOptions webApplicationOptions,
    Action<IServiceCollection>? configureServicesDelegate = null
  )
  {
    Urls = urls;
    WebApplicationBuilder builder =
      WebApplication.CreateBuilder(webApplicationOptions);

    // Hermeticity (task 104-031): remove developer user secrets so machine-local values cannot alter
    // test outcomes — see this class's Design region. Iterate backwards because we mutate in place.
    IList<IConfigurationSource> configurationSources = ((IConfigurationBuilder)builder.Configuration).Sources;
    for (int index = configurationSources.Count - 1; index >= 0; index--)
    {
      if (configurationSources[index] is JsonConfigurationSource jsonSource && jsonSource.Path == "secrets.json")
      {
        configurationSources.RemoveAt(index);
      }
    }

    builder.WebHost
      .UseUrls(urls)
      .UseShutdownTimeout(TimeSpan.FromSeconds(30));

    Configuration = builder.Configuration;
    TProgram.ConfigureServices(builder.Services, builder.Configuration);
    configureServicesDelegate?.Invoke(builder.Services);

    WebApplication = builder.Build();
    TProgram.ConfigureMiddleware(WebApplication);
    TProgram.ConfigureEndpoints(WebApplication);

    ServiceProvider = WebApplication.Services;
    // Options validation is now wired at registration time via the Timewarp.OptionsValidation
    // package's AddFluentValidatedOptions (in each Program's ConfigureServices); the former
    // host-side ServiceProvider.ValidateOptions sweep no longer exists in the package.

    try
    {
      Task runTask = WebApplication.RunAsync();

      // Wait for the server to be ready to accept connections
      IHostApplicationLifetime lifetime = ServiceProvider.GetRequiredService<IHostApplicationLifetime>();
      TaskCompletionSource serverStartedTcs = new();
      lifetime.ApplicationStarted.Register(() => serverStartedTcs.SetResult());
      serverStartedTcs.Task.Wait(TimeSpan.FromSeconds(30));

      Console.WriteLine("======= WebApplication Started ======");
      Started = true;
    }
    catch (Exception)
    {
      Console.WriteLine("======= Failed to Start WebApplication Disposing ======");
      WebApplication.DisposeAsync().AsTask().GetAwaiter().GetResult();
      Console.WriteLine("======= WebApplication.Disposed ======");
      throw;
    }
  }

  protected virtual async ValueTask DisposeAsyncCore()
  {
    Console.WriteLine("==== Application.DisposeAsyncCore ====");
    if (Started)
    {
      Console.WriteLine("==== Wait till WebApplication Stops ====");
      await WebApplication.StopAsync();
      Started = false;
    }

    Console.WriteLine("==== Now dispose of WebApplication ====");
    await WebApplication.DisposeAsync();
  }

  public async ValueTask DisposeAsync()
  {
    Console.WriteLine("==== Application.DisposeAsync ====");
    await DisposeAsyncCore();
    GC.SuppressFinalize(this);
  }
}
