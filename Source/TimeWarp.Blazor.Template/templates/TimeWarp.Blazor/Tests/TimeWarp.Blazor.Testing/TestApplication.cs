namespace TimeWarp.Blazor.Testing
{
  using MediatR;
  using System;
  using System.Linq;
  using System.Net.Http;
  using System.Text.Json;
  using System.Threading;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Features.Bases;

  /// <summary>
  /// An abstract class that adds test functionality for the passed in WebApplication.
  /// </summary>
  /// <example><see cref="TimeWarpBlazorServerApplication"/></example>
  /// <remarks>This allows for registering a WebApplication as a dependency and DI can fire it up and shut it down. </remarks>
  /// <typeparam name="TStartup"></typeparam>
  [NotTest]
  public abstract class TestApplication<TStartup> : IDisposable, IAsyncDisposable, ISender, IWebApiTestService
    where TStartup : class
  {
    public readonly WebApplication<TStartup> WebApplication;
    private readonly ISender ScopedSender;
    private IWebApiTestService WebApiTestService { get; }

    private bool Disposed;
    public HttpClient HttpClient { get; }
    public IServiceProvider ServiceProvider { get; }

    public TestApplication(WebApplication<TStartup> aWebApplication)
    {
      WebApplication = aWebApplication;
      ServiceProvider = WebApplication.Host.Services;
      ScopedSender = new ScopedSender(ServiceProvider);
      HttpClient = new HttpClient
      {
        BaseAddress = new Uri(WebApplication.Urls.First())
      };

      var jsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
      WebApiTestService = new WebApiTestService(HttpClient, jsonSerializerOptions);
    }

    public Task<TResponse> Send<TResponse>
    (
      IRequest<TResponse> aRequest,
      CancellationToken aCancellationToken = default
    ) =>
      ScopedSender.Send(aRequest, aCancellationToken);

    public Task<object> Send(object aRequest, CancellationToken aCancellationToken = default) =>
      ScopedSender.Send(aRequest, aCancellationToken);

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
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
      GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
    }

    protected virtual void Dispose(bool aIsDisposing)
    {
      if (Disposed) return;

      if (aIsDisposing)
      {
        WebApplication?.Dispose();
      }

      Disposed = true;
    }

    protected virtual ValueTask DisposeAsyncCore()
    {
      Console.WriteLine("==== TestApplication.DisposeAsyncCore ====");
      return WebApplication.DisposeAsync();
    }

    public Task ConfirmEndpointValidationError<TResponse>(IApiRequest aRequest, string aAttributeName) =>
      WebApiTestService.ConfirmEndpointValidationError<TResponse>(aRequest, aAttributeName);

    public Task<TResponse> GetResponse<TResponse>(IApiRequest aRequest) => WebApiTestService.GetResponse<TResponse>(aRequest);
  }
}
