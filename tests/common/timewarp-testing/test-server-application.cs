#nullable enable
namespace TimeWarp.Architecture.Testing;

/// <summary>
/// An abstract class that adds test functionality for the passed in WebApplication.
/// </summary>
/// <example><see cref="WebTestServerApplication"/></example>
/// <remarks>This allows for registering a WebApplication as a dependency and DI can fire it up and shut it down. </remarks>
/// <typeparam name="TProgram"></typeparam>
[NotTest]
public abstract class TestServerApplication<TProgram> : IAsyncDisposable, IWebApiTestService, ISender
  where TProgram : IAspNetProgram
{
  private readonly ISender ScopedSender;
  private IWebApiTestService WebApiTestService { get; }

  public readonly WebApplicationHost<TProgram> WebApplicationHost;
  public HttpClient HttpClient { get; }

  protected TestServerApplication(WebApplicationHost<TProgram> webApplicationHost)
  {
    WebApplicationHost = webApplicationHost;

    ScopedSender = new ScopedSender(webApplicationHost.ServiceProvider);

    HttpClient = new HttpClient
    {
      BaseAddress = new Uri(WebApplicationHost.Urls.First())
    };

    WebApiTestService = CreateWebApiTestService(webApplicationHost);
  }

  public Task ConfirmEndpointValidationError<TResponse>(IApiRequest apiRequest, string attributeName) =>
    WebApiTestService.ConfirmEndpointValidationError<TResponse>(apiRequest, attributeName);

  #region IAyncDisposable
  public async ValueTask DisposeAsync()
  {
    Console.WriteLine("==== TestApplication.DisposeAsync ====");
    await DisposeAsyncCore().ConfigureAwait(false);
    GC.SuppressFinalize(this);
  }

  protected virtual ValueTask DisposeAsyncCore()
  {
    Console.WriteLine("==== TestApplication.DisposeAsyncCore ====");
    return WebApplicationHost.DisposeAsync();
  }

  #endregion

  #region IWebApiTestService

  public async Task<OneOf.OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
  (
    IApiRequest apiRequest,
    CancellationToken cancellationToken
  ) where TResponse : class =>
    await WebApiTestService.GetResponse<TResponse>(apiRequest, cancellationToken);

  #endregion

  #region ISender
  public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
    ScopedSender.Send(request, cancellationToken);

  public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
    where TRequest : IRequest => ScopedSender.Send(request, cancellationToken);

  public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
    ScopedSender.Send(request, cancellationToken);

  public IAsyncEnumerable<TResponse> CreateStream<TResponse>
  (
    IStreamRequest<TResponse> request,
    CancellationToken cancellationToken = default
  ) => ScopedSender.CreateStream(request, cancellationToken);

  public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => ScopedSender.CreateStream(request, cancellationToken);

  #endregion

  protected abstract IWebApiTestService CreateWebApiTestService(WebApplicationHost<TProgram> webApplicationHost);
}

public class TestServerApplication : TestServerApplication<Api.Server.Program>
{
  public TestServerApplication(WebApplicationHost<Api.Server.Program> webApplicationHost) : base(webApplicationHost)
  {
  }

  protected override IWebApiTestService CreateWebApiTestService(WebApplicationHost<Api.Server.Program> webApplicationHost)
  {
    IServiceProvider serviceProvider = webApplicationHost.ServiceProvider;

    IHttpClientFactory httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    IAccessTokenProvider accessTokenProvider = serviceProvider.GetRequiredService<IAccessTokenProvider>();

    var jsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    IOptions<JsonSerializerOptions> jsonSerializerOptionsAccessor = Options.Create(jsonSerializerOptions);

    var apiService = new ApiServerApiService(httpClientFactory, accessTokenProvider, jsonSerializerOptionsAccessor);
    return new WebApiTestService(apiService);
  }
}
