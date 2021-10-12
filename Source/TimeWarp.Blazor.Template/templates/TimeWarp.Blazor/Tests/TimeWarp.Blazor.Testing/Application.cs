namespace TimeWarp.Blazor.Testing
{
  using Microsoft.AspNetCore.Hosting;
  using Microsoft.Extensions.DependencyInjection;
  using Microsoft.Extensions.Hosting;
  using System;
  using System.Threading.Tasks;

  [NotTest]
  public class Application<TStartup> : IDisposable, IAsyncDisposable
    where TStartup : class
  {
    private bool Disposed;
    public bool Started;
    private readonly IHostBuilder HostBuilder;
    public readonly string[] Urls;

    public IHost Host { get; }

    public Application
    (
      string aEnvironmentName,
      string[] aUrls,
      string aApplicationName,
      Action<HostBuilderContext, IServiceCollection> aConfigureServicesDelegate = null
    )
    {
      Urls = aUrls;
      HostBuilder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
        .ConfigureWebHostDefaults
        (
          aWebHostBuilder =>
          {
            aWebHostBuilder.UseStaticWebAssets();
            aWebHostBuilder.UseUrls(aUrls);
            aWebHostBuilder.UseStartup<TStartup>();
            aWebHostBuilder.UseEnvironment(aEnvironmentName);
            aWebHostBuilder.UseShutdownTimeout(TimeSpan.FromSeconds(30));
          }
        );
      // Allow for changes to the configuration
      if (aConfigureServicesDelegate != null) HostBuilder.ConfigureServices(aConfigureServicesDelegate);
      Host = HostBuilder.Build();
      try
      {
        Host.StartAsync().GetAwaiter().GetResult();
        Started = true;
      }
      catch (Exception)
      {
        Console.WriteLine("======= Failed to Start Application Disposing Host ======");
        Host.Dispose();
        Console.WriteLine("======= Host.Disposed ======");
        throw;
      }
    }

    protected virtual void Dispose(bool aIsDisposing)
    {
      Console.WriteLine($"==== Application.Dispose({aIsDisposing}) ====");
      if (Disposed) return;

      if (aIsDisposing)
      {
        if (Started)
        {
          Console.WriteLine("==== Wait till Host Stops ====");
          Host?.StopAsync().GetAwaiter().GetResult();
          Started = false;
        }
        Console.WriteLine("==== Now dispose of Host ====");
        Host?.Dispose();
      }

      Disposed = true;
    }

    public void Dispose()
    {
      Console.WriteLine("==== Application.Dispose ====");
      Dispose(true);
      GC.SuppressFinalize(this);
    }


    protected virtual async ValueTask DisposeAsyncCore()
    {
      Console.WriteLine("==== Application.DisposeAsyncCore ====");
      if (Started)
      {
        Console.WriteLine("==== Wait till Host Stops ====");
        await Host.StopAsync().ConfigureAwait(false);
        Started = false;
      }
      Console.WriteLine("==== Now dispose of Host ====");
      Host?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
      Console.WriteLine("==== Application.DisposeAsync ====");
      await DisposeAsyncCore().ConfigureAwait(false);
      Dispose(false);
      GC.SuppressFinalize(this);
    }
  }
}
