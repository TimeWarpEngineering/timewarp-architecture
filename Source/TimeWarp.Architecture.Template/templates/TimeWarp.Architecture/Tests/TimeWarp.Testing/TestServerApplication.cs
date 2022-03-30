namespace TimeWarp.Architecture.Testing;

using MediatR;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TimeWarp.Architecture.Features;

/// <summary>
/// An abstract class that adds test functionality for the passed in WebApplication.
/// </summary>
/// <example><see cref="WebServerApplication"/></example>
/// <remarks>This allows for registering a WebApplication as a dependency and DI can fire it up and shut it down. </remarks>
/// <typeparam name="TProgram"></typeparam>
[NotTest]
public abstract class TestServerApplication<TProgram> : IDisposable, IAsyncDisposable, ISender, IWebApiTestService
  where TProgram : IProgram
{
  //[Delegate]
  private readonly ISender ScopedSender;
  private IWebApiTestService WebApiTestService { get; }
  private bool Disposed;

  public readonly WebApplicationHost<TProgram> WebApplicationHost;
  public HttpClient HttpClient { get; }

  public TestServerApplication(WebApplicationHost<TProgram> aWebApplication) : base()
  {
    WebApplicationHost = aWebApplication;

    // ISender Delegate
    ScopedSender = new ScopedSender(aWebApplication.Host.Services);

    HttpClient = new HttpClient
    {
      BaseAddress = new Uri(WebApplicationHost.Urls.First())
    };

    var jsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    IOptions<JsonSerializerOptions> jsonSerializerOptionsAccesor = Options.Create(jsonSerializerOptions);
    var webApiService = new WebApiService(HttpClient, jsonSerializerOptionsAccesor);
    WebApiTestService = new WebApiTestService(webApiService);
  }

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
      WebApplicationHost?.Dispose();
    }

    Disposed = true;
  }

  protected virtual ValueTask DisposeAsyncCore()
  {
    Console.WriteLine("==== TestApplication.DisposeAsyncCore ====");
    return WebApplicationHost.DisposeAsync();
  }

  public Task ConfirmEndpointValidationError<TResponse>(IApiRequest aRequest, string aAttributeName) =>
    WebApiTestService.ConfirmEndpointValidationError<TResponse>(aRequest, aAttributeName);

  #region IWebApiTestService
  public Task<TResponse> GetResponse<TResponse>(IApiRequest aRequest) => WebApiTestService.GetResponse<TResponse>(aRequest);
  #endregion

  #region ISender
  public Task<TResponse> Send<TResponse>
  (
    IRequest<TResponse> aRequest,
    CancellationToken aCancellationToken = default
  ) =>
    ScopedSender.Send(aRequest, aCancellationToken);

  public Task<object> Send(object aRequest, CancellationToken aCancellationToken = default) =>
    ScopedSender.Send(aRequest, aCancellationToken);

  public IAsyncEnumerable<TResponse> CreateStream<TResponse>
  (
    IStreamRequest<TResponse> aStreamRequest,
    CancellationToken aCancellationToken = default
  ) => throw new NotImplementedException();

  public IAsyncEnumerable<object> CreateStream
  (
    object aRequest,
    CancellationToken aCancellationToken = default
  ) => throw new NotImplementedException();

  #endregion

}
