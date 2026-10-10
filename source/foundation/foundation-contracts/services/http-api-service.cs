#region Purpose
// Shared HTTP transport core for IApiService: contract → request → OneOf outcome.
#endregion

#region Design
// Single home for the verb/route/body/problem mapping that SPA BaseApiService and test
// TestApiService previously mirrored. Both composers pass an HttpClient + seam
// JsonSerializerOptions; the SPA supplies a per-request bearer acquire delegate (MSAL
// IAccessTokenProvider adapter), tests pin Authorization on the client and pass null.
//
// Verbs: Get/Delete/Post/Put/Patch only — Head/Options throw NotSupportedException (no
// real client for them on either side). GET/DELETE take data via query string
// (IQueryStringRouteProvider). POST/PUT/PATCH send JSON bodies with the seam options,
// except IFileUploadRequest, which sends the stream as the body and the file name in
// X-File-Name. The media type is the file's type, with no charset.
// 204 is checked before IsSuccessStatusCode (it is 2xx but has no body to deserialize) and
// maps to SharedProblemDetails; other non-success map to problem; cancellation maps to 499.
// Stream TResponse becomes FileResponse without EnsureSuccessStatusCode (already on the
// success branch). Problem-body deserialization catches only JsonException/
// InvalidOperationException so unexpected failures surface rather than becoming a synthetic
// problem. Empty or non-JSON error bodies synthesize SharedProblemDetails from the status:
// 401 Unauthorized, 403 Forbidden, otherwise Unhandled Error (cookie challenges return
// empty 401/403; mapping them as Unhandled Error made Profile Save look like a crash).
#endregion

namespace TimeWarp.Foundation;

using System.Net.Http.Headers;

/// <summary>
/// HTTP transport implementation of <see cref="IApiService"/>.
/// Compose from SPA and test clients rather than duplicating verb/route/problem logic.
/// </summary>
public sealed class HttpApiService : IApiService
{
  /// <summary>Title of the problem synthesized for a non-problem failure body other than 401/403.</summary>
  public const string UnhandledErrorTitle = "Unhandled Error";

  private readonly HttpClient HttpClient;
  private readonly JsonSerializerOptions JsonSerializerOptions;
  private readonly Func<CancellationToken, Task<string?>>? AcquireBearerTokenAsync;

  /// <summary>Constructs an HTTP-backed <see cref="IApiService"/> for the contract seam.</summary>
  /// <param name="httpClient">Client used for all requests (caller owns lifetime).</param>
  /// <param name="jsonSerializerOptions">Contract-seam serializer options.</param>
  /// <param name="acquireBearerTokenAsync">
  /// Optional per-request bearer acquisition. When non-null, invoked before each send;
  /// a non-null returned string is set as the Bearer header on the client.
  /// </param>
  public HttpApiService
  (
    HttpClient httpClient,
    JsonSerializerOptions jsonSerializerOptions,
    Func<CancellationToken, Task<string?>>? acquireBearerTokenAsync = null
  )
  {
    HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    JsonSerializerOptions = jsonSerializerOptions ?? throw new ArgumentNullException(nameof(jsonSerializerOptions));
    AcquireBearerTokenAsync = acquireBearerTokenAsync;
  }

  /// <inheritdoc />
  public async Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
  (
    IApiRequest request,
    CancellationToken cancellationToken
  ) where TResponse : class
  {
    try
    {
      HttpResponseMessage httpResponseMessage =
        await GetHttpResponseMessage(request, cancellationToken).ConfigureAwait(false);

      // 204 is a 2xx success, so check it before IsSuccessStatusCode — empty body is not a DTO.
      if (httpResponseMessage.StatusCode == HttpStatusCode.NoContent)
      {
        return new SharedProblemDetails
        {
          Title = "No Content",
          Status = (int)HttpStatusCode.NoContent,
          Detail = "The response content is empty."
        };
      }

      return httpResponseMessage.IsSuccessStatusCode
        ? await HandleSuccessResponse<TResponse>(httpResponseMessage, cancellationToken).ConfigureAwait(false)
        : await HandleProblemResponse(httpResponseMessage, cancellationToken).ConfigureAwait(false);
    }
    catch (OperationCanceledException)
    {
      return new SharedProblemDetails
      {
        Title = "Operation Cancelled",
        Status = 499, // "Client Closed Request"
        Detail = "The request was cancelled."
      };
    }
  }

  /// <summary>Send the contract's request and return the raw response (status/body assertions, SPA internals).</summary>
  public async Task<HttpResponseMessage> GetHttpResponseMessage
  (
    IApiRequest apiRequest,
    CancellationToken cancellationToken
  )
  {
    // Relative-or-absolute so contract routes ("/api/…") resolve against HttpClient.BaseAddress.
    Uri route = new(PrepareRoute(apiRequest), UriKind.RelativeOrAbsolute);
    await ApplyBearerTokenAsync(cancellationToken).ConfigureAwait(false);
    if (apiRequest is IFileUploadRequest fileUpload
      && apiRequest.GetHttpVerb() is HttpVerb.Post or HttpVerb.Put or HttpVerb.Patch)
    {
      return await SendFileAsync(route, fileUpload, apiRequest.GetHttpVerb(), cancellationToken)
        .ConfigureAwait(false);
    }

    using StringContent? httpContent = PrepareContent(apiRequest);
    return apiRequest.GetHttpVerb() switch
    {
      HttpVerb.Get => await HttpClient.GetAsync(route, cancellationToken).ConfigureAwait(false),
      HttpVerb.Delete => await HttpClient.DeleteAsync(route, cancellationToken).ConfigureAwait(false),
      HttpVerb.Post => await HttpClient.PostAsync(route, httpContent, cancellationToken).ConfigureAwait(false),
      HttpVerb.Put => await HttpClient.PutAsync(route, httpContent, cancellationToken).ConfigureAwait(false),
      HttpVerb.Patch => await HttpClient.PatchAsync(route, httpContent, cancellationToken).ConfigureAwait(false),
      // Head and Options have no sender here. NotSupportedException matches the discard, which is
      // what these verbs threw before IDE0072 required them to be named.
      HttpVerb.Head or HttpVerb.Options => throw new NotSupportedException($"HttpVerb: {apiRequest.GetHttpVerb()} is not supported."),
      var verb => throw new NotSupportedException($"HttpVerb: {verb} is not supported.")
    };
  }

  private async Task<HttpResponseMessage> SendFileAsync(
    Uri route,
    IFileUploadRequest fileUpload,
    HttpVerb verb,
    CancellationToken cancellationToken)
  {
    HttpMethod method = verb switch
    {
      HttpVerb.Post => HttpMethod.Post,
      HttpVerb.Put => HttpMethod.Put,
      HttpVerb.Patch => HttpMethod.Patch,
      HttpVerb.Get or HttpVerb.Delete or HttpVerb.Head or HttpVerb.Options =>
        throw new NotSupportedException($"HttpVerb: {verb} is not supported for a file upload."),
      var unsupported => throw new NotSupportedException($"HttpVerb: {unsupported} is not supported for a file upload."),
    };

    using HttpRequestMessage message = new(method, route);
    StreamContent content = new(fileUpload.Content);
    content.Headers.ContentType = new MediaTypeHeaderValue(fileUpload.ContentType);
    message.Content = content;
    message.Headers.TryAddWithoutValidation(
      FileUploadHeaders.FileName,
      Uri.EscapeDataString(fileUpload.FileName));
    return await HttpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
  }

  private async Task ApplyBearerTokenAsync(CancellationToken cancellationToken)
  {
    if (AcquireBearerTokenAsync is null)
    {
      return;
    }

    string? token = await AcquireBearerTokenAsync(cancellationToken).ConfigureAwait(false);
    if (token is not null)
    {
      HttpClient.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue(scheme: "Bearer", token);
    }
  }

  private async Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> HandleSuccessResponse<TResponse>
  (
    HttpResponseMessage httpResponseMessage,
    CancellationToken cancellationToken
  ) where TResponse : class
  {
    if (typeof(TResponse) == typeof(Stream))
    {
      Stream fileStream = await httpResponseMessage.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
      return new FileResponse(fileStream: fileStream)
      {
        FileName = httpResponseMessage.Content.Headers.ContentDisposition?.FileName,
        ContentType = httpResponseMessage.Content.Headers.ContentType?.MediaType
      };
    }

    return await ReadFromJson<TResponse>(httpResponseMessage, cancellationToken).ConfigureAwait(false);
  }

  private async Task<SharedProblemDetails> HandleProblemResponse
  (
    HttpResponseMessage httpResponseMessage,
    CancellationToken cancellationToken
  )
  {
    try
    {
      return await ReadFromJson<SharedProblemDetails>(httpResponseMessage, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception exception) when (exception is JsonException or InvalidOperationException)
    {
      // Body was not RFC 7807 JSON — synthesize a problem from the status code.
      return SynthesizeProblemFromStatus(httpResponseMessage.StatusCode);
    }
  }

  private static SharedProblemDetails SynthesizeProblemFromStatus(HttpStatusCode statusCode)
  {
    int status = (int)statusCode;
    // Numeric switch: HttpStatusCode names every IANA code, and undefined values exist too.
    // Only 401 and 403 have their own problem; every other code shares the discard body.
    // IDE0072's fixer would throw NotImplementedException for those codes and break clients.
    return status switch
    {
      (int)HttpStatusCode.Unauthorized => new SharedProblemDetails
      {
        Title = "Unauthorized",
        Status = status,
        Detail = "Authentication is required."
      },
      (int)HttpStatusCode.Forbidden => new SharedProblemDetails
      {
        Title = "Forbidden",
        Status = status,
        Detail = "You do not have permission to perform this action."
      },
      _ => new SharedProblemDetails
      {
        Title = UnhandledErrorTitle,
        Status = status,
        Detail = "An unhandled error occurred while processing the request."
      }
    };
  }

  private static string PrepareRoute(IApiRequest apiRequest) =>
    apiRequest.GetHttpVerb() switch
    {
      // GET and DELETE carry no body, so the query string is their only data channel
      // besides route parameters.
      HttpVerb.Get or HttpVerb.Delete =>
        (apiRequest as IQueryStringRouteProvider)?.GetRouteWithQueryString() ?? apiRequest.GetRoute(),
      HttpVerb.Post or HttpVerb.Put or HttpVerb.Patch or HttpVerb.Head or HttpVerb.Options =>
        apiRequest.GetRoute(),
      _ => apiRequest.GetRoute()
    };

  private StringContent? PrepareContent(IApiRequest apiRequest) =>
    apiRequest.GetHttpVerb() switch
    {
      HttpVerb.Post or HttpVerb.Put or HttpVerb.Patch =>
        new StringContent
        (
          JsonSerializer.Serialize(apiRequest, apiRequest.GetType(), JsonSerializerOptions),
          Encoding.UTF8,
          MediaTypeNames.Application.Json
        ),
      HttpVerb.Get or HttpVerb.Delete or HttpVerb.Head or HttpVerb.Options => null,
      _ => null
    };

  private async Task<TResponse> ReadFromJson<TResponse>
  (
    HttpResponseMessage httpResponseMessage,
    CancellationToken cancellationToken
  )
  {
    string json = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

    return JsonSerializer.Deserialize<TResponse>(json, JsonSerializerOptions) ??
      throw new InvalidOperationException("The response is null.");
  }
}
