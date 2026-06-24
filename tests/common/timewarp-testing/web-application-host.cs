#nullable enable
namespace TimeWarp.Architecture.Testing;

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
      WebApplication.DisposeAsync().GetAwaiter().GetResult();
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
    WebApplication?.DisposeAsync();
  }

  public async ValueTask DisposeAsync()
  {
    Console.WriteLine("==== Application.DisposeAsync ====");
    await DisposeAsyncCore();
    GC.SuppressFinalize(this);
  }
}
