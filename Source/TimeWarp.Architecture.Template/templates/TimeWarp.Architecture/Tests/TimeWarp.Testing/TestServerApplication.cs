#nullable enable
namespace TimeWarp.Architecture.Testing;

using MediatR;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TimeWarp.Architecture.Features;

/// <summary>
/// An abstract class that adds test functionality for the passed in WebApplication.
/// </summary>
/// <example><see cref="WebTestServerApplication"/></example>
/// <remarks>This allows for registering a WebApplication as a dependency and DI can fire it up and shut it down. </remarks>
/// <typeparam name="TProgram"></typeparam>
[NotTest]
public abstract partial class TestServerApplication<TProgram> : IAsyncDisposable, IWebApiTestService, ISender
  where TProgram : IProgram
{
  [Delegate]
  private readonly ISender ScopedSender;
  private IWebApiTestService WebApiTestService { get; }

  public readonly WebApplicationHost<TProgram> WebApplicationHost;
  public HttpClient HttpClient { get; }

  public TestServerApplication(WebApplicationHost<TProgram> aWebApplicationHost) : base()
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
    var webApiService = new WebApiService(HttpClient, jsonSerializerOptionsAccessor);
    WebApiTestService = new WebApiTestService(webApiService);
  }

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

  public Task ConfirmEndpointValidationError<TResponse>(IApiRequest aRequest, string aAttributeName) =>
    WebApiTestService.ConfirmEndpointValidationError<TResponse>(aRequest, aAttributeName);

  #region IWebApiTestService
  public Task<TResponse> GetResponse<TResponse>(IApiRequest aRequest) => WebApiTestService.GetResponse<TResponse>(aRequest);
  #endregion

  //#region ISender
  //public Task<TResponse> Send<TResponse>
  //(
  //  IRequest<TResponse> aRequest,
  //  CancellationToken aCancellationToken = default
  //) =>
  //  ScopedSender.Send(aRequest, aCancellationToken);

  //public Task<object> Send(object aRequest, System.Threading.CancellationToken aCancellationToken = default) =>
  //  ScopedSender.Send(aRequest, aCancellationToken);

  //public IAsyncEnumerable<TResponse> CreateStream<TResponse>
  //(
  //  IStreamRequest<TResponse> aStreamRequest,
  //  CancellationToken aCancellationToken = default
  //) => throw new NotImplementedException();

  //public IAsyncEnumerable<object> CreateStream
  //(
  //  object aRequest,
  //  CancellationToken aCancellationToken = default
  //) => throw new NotImplementedException();

  //#endregion

}
