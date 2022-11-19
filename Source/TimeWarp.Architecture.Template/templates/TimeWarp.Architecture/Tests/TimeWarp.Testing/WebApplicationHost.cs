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
  /// <param name="aUrls"></param>
  /// <param name="aWebApplicationOptions"></param>
  /// <param name="aConfigureServicesDelegate"></param>
  public WebApplicationHost
  (
    string[] aUrls,
    WebApplicationOptions aWebApplicationOptions,
    Action<IServiceCollection>? aConfigureServicesDelegate = null
  )
  {
    Urls = aUrls;
    WebApplicationBuilder builder =
      WebApplication.CreateBuilder(aWebApplicationOptions);

    builder.WebHost
      .UseUrls(aUrls)
      .UseShutdownTimeout(TimeSpan.FromSeconds(30));

    Configuration = builder.Configuration;
    TProgram.ConfigureServices(builder.Services, builder.Configuration);
    aConfigureServicesDelegate?.Invoke(builder.Services);

    WebApplication = builder.Build();
    TProgram.ConfigureMiddleware(WebApplication);
    TProgram.ConfigureEndpoints(WebApplication);

    ServiceProvider = WebApplication.Services;
    ServiceProvider.ValidateOptions(builder.Services);

    try
    {
      WebApplication.RunAsync();
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
