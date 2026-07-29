#region Purpose
// Verifies HttpApiService transport: success, 204, problem, cancel, Head, Stream, bearer header.
#endregion

namespace TimeWarp.Architecture.Foundation.Contracts.Tests;

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OneOf;

public class HttpApiService_GetResponse
{
  private sealed class GetRequest : IApiRequest
  {
    public string GetRoute() => "/api/sample";
    public HttpVerb GetHttpVerb() => HttpVerb.Get;
  }

  private sealed class HeadRequest : IApiRequest
  {
    public string GetRoute() => "/api/sample";
    public HttpVerb GetHttpVerb() => HttpVerb.Head;
  }

  private sealed class GetWithQueryRequest : IApiRequest, IQueryStringRouteProvider
  {
    public string GetRoute() => "/api/items";
    public HttpVerb GetHttpVerb() => HttpVerb.Get;
    public string GetRouteWithQueryString() => "/api/items?q=alpha";
  }

  private sealed class SampleDto
  {
    public string Name { get; set; } = "";
  }

  private sealed class RecordingHandler : HttpMessageHandler
  {
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Responder;

    public HttpRequestMessage? LastRequest { get; private set; }

    public RecordingHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
    {
      Responder = responder;
    }

    public RecordingHandler(HttpResponseMessage response)
      : this((_, _) => Task.FromResult(response))
    {
    }

    protected override Task<HttpResponseMessage> SendAsync
    (
      HttpRequestMessage request,
      CancellationToken cancellationToken
    )
    {
      LastRequest = request;
      return Responder(request, cancellationToken);
    }
  }

  private static HttpApiService CreateService
  (
    HttpMessageHandler handler,
    Func<CancellationToken, Task<string?>>? acquireBearerTokenAsync = null
  )
  {
    HttpClient client = new(handler) { BaseAddress = new Uri("https://example.test/") };
    return new HttpApiService(client, ContractSerializationDefaults.Options, acquireBearerTokenAsync);
  }

  private static HttpResponseMessage JsonResponse(HttpStatusCode status, object body)
  {
    string json = JsonSerializer.Serialize(body, ContractSerializationDefaults.Options);
    return new HttpResponseMessage(status)
    {
      Content = new StringContent(json, Encoding.UTF8, "application/json")
    };
  }

  public async Task Returns_typed_response_on_success()
  {
    RecordingHandler handler = new(JsonResponse(HttpStatusCode.OK, new SampleDto { Name = "ok" }));
    HttpApiService service = CreateService(handler);

    OneOf<SampleDto, FileResponse, SharedProblemDetails> result =
      await service.GetResponse<SampleDto>(new GetRequest(), CancellationToken.None);

    result.IsT0.ShouldBeTrue();
    result.AsT0.Name.ShouldBe("ok");
    handler.LastRequest.ShouldNotBeNull();
    handler.LastRequest!.Method.ShouldBe(HttpMethod.Get);
    handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/api/sample");
  }

  public async Task Uses_query_string_route_for_get()
  {
    RecordingHandler handler = new(JsonResponse(HttpStatusCode.OK, new SampleDto { Name = "q" }));
    HttpApiService service = CreateService(handler);

    await service.GetResponse<SampleDto>(new GetWithQueryRequest(), CancellationToken.None);

    handler.LastRequest.ShouldNotBeNull();
    handler.LastRequest!.RequestUri!.PathAndQuery.ShouldBe("/api/items?q=alpha");
  }

  public async Task Maps_204_to_shared_problem_details()
  {
    RecordingHandler handler = new(new HttpResponseMessage(HttpStatusCode.NoContent));
    HttpApiService service = CreateService(handler);

    OneOf<SampleDto, FileResponse, SharedProblemDetails> result =
      await service.GetResponse<SampleDto>(new GetRequest(), CancellationToken.None);

    result.IsT2.ShouldBeTrue();
    SharedProblemDetails problem = result.AsT2;
    problem.Status.ShouldBe((int)HttpStatusCode.NoContent);
    problem.Title.ShouldBe("No Content");
  }

  public async Task Maps_problem_json_to_shared_problem_details()
  {
    SharedProblemDetails body = new()
    {
      Title = "Bad Request",
      Status = 400,
      Detail = "invalid"
    };
    RecordingHandler handler = new(JsonResponse(HttpStatusCode.BadRequest, body));
    HttpApiService service = CreateService(handler);

    OneOf<SampleDto, FileResponse, SharedProblemDetails> result =
      await service.GetResponse<SampleDto>(new GetRequest(), CancellationToken.None);

    result.IsT2.ShouldBeTrue();
    SharedProblemDetails problem = result.AsT2;
    problem.Status.ShouldBe(400);
    problem.Title.ShouldBe("Bad Request");
    problem.Detail.ShouldBe("invalid");
  }

  public async Task Synthesizes_problem_when_error_body_is_not_json()
  {
    RecordingHandler handler = new(
      new HttpResponseMessage(HttpStatusCode.InternalServerError)
      {
        Content = new StringContent("not-json", Encoding.UTF8, "text/plain")
      });
    HttpApiService service = CreateService(handler);

    OneOf<SampleDto, FileResponse, SharedProblemDetails> result =
      await service.GetResponse<SampleDto>(new GetRequest(), CancellationToken.None);

    result.IsT2.ShouldBeTrue();
    SharedProblemDetails problem = result.AsT2;
    problem.Status.ShouldBe(500);
    problem.Title.ShouldBe("Unhandled Error");
  }

  public async Task Maps_cancellation_to_499_problem()
  {
    RecordingHandler handler = new((_, ct) =>
    {
      ct.ThrowIfCancellationRequested();
      return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    });
    HttpApiService service = CreateService(handler);
    using CancellationTokenSource cts = new();
    await cts.CancelAsync();

    OneOf<SampleDto, FileResponse, SharedProblemDetails> result =
      await service.GetResponse<SampleDto>(new GetRequest(), cts.Token);

    result.IsT2.ShouldBeTrue();
    SharedProblemDetails problem = result.AsT2;
    problem.Status.ShouldBe(499);
    problem.Title.ShouldBe("Operation Cancelled");
  }

  public async Task Throws_not_supported_for_head()
  {
    RecordingHandler handler = new(new HttpResponseMessage(HttpStatusCode.OK));
    HttpApiService service = CreateService(handler);

    NotSupportedException exception = await Should.ThrowAsync<NotSupportedException>
    (
      () => service.GetResponse<SampleDto>(new HeadRequest(), CancellationToken.None)
    );

    exception.Message.ShouldContain("Head");
  }

  public async Task Returns_file_response_for_stream_tresponse()
  {
    byte[] bytes = "hello-file"u8.ToArray();
    HttpResponseMessage response = new(HttpStatusCode.OK)
    {
      Content = new ByteArrayContent(bytes)
    };
    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
    response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
    {
      FileName = "report.bin"
    };
    RecordingHandler handler = new(response);
    HttpApiService service = CreateService(handler);

    OneOf<Stream, FileResponse, SharedProblemDetails> result =
      await service.GetResponse<Stream>(new GetRequest(), CancellationToken.None);

    result.IsT1.ShouldBeTrue();
    FileResponse file = result.AsT1;
    file.FileName.ShouldBe("report.bin");
    file.ContentType.ShouldBe("application/octet-stream");
    using StreamReader reader = new(file.FileStream);
    (await reader.ReadToEndAsync()).ShouldBe("hello-file");
  }

  public async Task Sets_bearer_header_when_acquire_returns_token()
  {
    RecordingHandler handler = new(JsonResponse(HttpStatusCode.OK, new SampleDto { Name = "authed" }));
    HttpApiService service = CreateService(
      handler,
      acquireBearerTokenAsync: _ => Task.FromResult<string?>("test-token-123"));

    await service.GetResponse<SampleDto>(new GetRequest(), CancellationToken.None);

    handler.LastRequest.ShouldNotBeNull();
    handler.LastRequest!.Headers.Authorization.ShouldNotBeNull();
    handler.LastRequest.Headers.Authorization!.Scheme.ShouldBe("Bearer");
    handler.LastRequest.Headers.Authorization.Parameter.ShouldBe("test-token-123");
  }

  public async Task Does_not_set_bearer_when_acquire_returns_null()
  {
    RecordingHandler handler = new(JsonResponse(HttpStatusCode.OK, new SampleDto { Name = "anon" }));
    HttpApiService service = CreateService(
      handler,
      acquireBearerTokenAsync: _ => Task.FromResult<string?>(null));

    await service.GetResponse<SampleDto>(new GetRequest(), CancellationToken.None);

    handler.LastRequest.ShouldNotBeNull();
    handler.LastRequest!.Headers.Authorization.ShouldBeNull();
  }

  public async Task Does_not_set_bearer_when_acquire_is_null()
  {
    RecordingHandler handler = new(JsonResponse(HttpStatusCode.OK, new SampleDto { Name = "none" }));
    HttpApiService service = CreateService(handler, acquireBearerTokenAsync: null);

    await service.GetResponse<SampleDto>(new GetRequest(), CancellationToken.None);

    handler.LastRequest.ShouldNotBeNull();
    handler.LastRequest!.Headers.Authorization.ShouldBeNull();
  }
}
