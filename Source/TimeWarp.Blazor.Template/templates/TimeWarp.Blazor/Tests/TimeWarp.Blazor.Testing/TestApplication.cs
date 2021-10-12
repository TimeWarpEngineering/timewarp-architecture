namespace TimeWarp.Blazor.Testing
{
  using MediatR;
  using System;
  using System.Linq;
  using System.Net.Http;
  using System.Text.Json;
  using System.Threading.Tasks;

  [NotTest]
  public class TestApplication<TStartup> : IDisposable, IAsyncDisposable
    where TStartup : class
  {
    public readonly Application<TStartup> Application;
    private readonly MediationTestService MediationTestService;
    private bool Disposed;
    public HttpClient HttpClient { get; }
    public IServiceProvider ServiceProvider { get; }
    public WebApiTestService WebApiTestService { get; }

    public TestApplication(Application<TStartup> aApplication)
    {
      Application = aApplication;
      ServiceProvider = Application.Host.Services;
      MediationTestService = new MediationTestService(ServiceProvider);
      HttpClient = new HttpClient
      {
        BaseAddress = new Uri(Application.Urls.First())
      };

      var jsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
      WebApiTestService = new WebApiTestService(HttpClient, jsonSerializerOptions);
    }

    public Task Send(IRequest aRequest) => MediationTestService.Send(aRequest);
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> aRequest) =>
      MediationTestService.Send(aRequest);

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
      Console.WriteLine("==== TestApplication.DisposeAsync ====");
      await DisposeAsyncCore().ConfigureAwait(false);
      Dispose(false);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool aIsDisposing)
    {
      if (Disposed) return;

      if (aIsDisposing)
      {
        Application?.Dispose();
      }

      Disposed = true;
    }

    protected virtual ValueTask DisposeAsyncCore()
    {
      Console.WriteLine("==== TestApplication.DisposeAsyncCore ====");
      return Application.DisposeAsync();
    }
  }
}
