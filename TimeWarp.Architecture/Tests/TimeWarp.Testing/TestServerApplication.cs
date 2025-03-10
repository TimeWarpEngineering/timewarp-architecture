﻿#nullable enable
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

  protected TestServerApplication(WebApplicationHost<TProgram> aWebApplicationHost)
  {
    WebApplicationHost = aWebApplicationHost;

    // ISender Delegate
    ScopedSender = new ScopedSender(aWebApplicationHost.ServiceProvider);

    HttpClient = new HttpClient
    {
      BaseAddress = new Uri(WebApplicationHost.Urls.First())
    };

    var jsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    IOptions<JsonSerializerOptions> jsonSerializerOptionsAccessor = Options.Create(jsonSerializerOptions);

    // I need Ihttpclientfactory to create the WebApiService.  Where can I get it?
    IHttpClientFactory httpClientFactory = aWebApplicationHost.ServiceProvider.GetRequiredService<IHttpClientFactory>();

    // I need IAccessTokenProvider to create the WebApiService.  Where can I get it?
    // TODO:
    // Will it be registered in the WebApplicationHost.ServiceProvider? Will I need a Mock? I think I will need a Mock.
    IAccessTokenProvider accessTokenProvider = aWebApplicationHost.ServiceProvider.GetRequiredService<IAccessTokenProvider>();

    var apiService = new WebServerApiService( accessTokenProvider, httpClientFactory, jsonSerializerOptionsAccessor);
    WebApiTestService = new WebApiTestService(apiService);
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

}
