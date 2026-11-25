#region Purpose
// Writes a SharedProblemDetails body as application/problem+json with the problem's own status.
#endregion

#region Design
// HttpResponse.WriteAsJsonAsync sets Content-Type itself. The overloads without a contentType
// argument write application/json and replace a Content-Type set on the response beforehand, so a
// problem body went out as application/json (task 458-004). This writer passes the media type to
// WriteAsJsonAsync, so the header and the body are written by one call.
// Options null resolves the host's HTTP JSON options, which CommonServerModule sets to the
// contract serialization defaults. A caller outside an endpoint (an authentication event) may
// pass ContractSerializationDefaults.Options itself.
// The status is the problem's Status, 400 when the problem sets none. The response and the
// OpenAPI document (OpenApiProblemDetails) use this one media type constant.
#endregion

namespace TimeWarp.Foundation.Features;

/// <summary>
/// Writes <see cref="SharedProblemDetails"/> responses as <c>application/problem+json</c>.
/// </summary>
public static class SharedProblemDetailsHttp
{
  /// <summary>
  /// The problem details media type, without parameters.
  /// </summary>
  public const string MediaType = "application/problem+json";

  /// <summary>
  /// The Content-Type header value written with a problem body.
  /// </summary>
  public const string ContentType = MediaType + "; charset=utf-8";

  /// <summary>
  /// Writes <paramref name="problem"/> with its own status (400 when it sets none) and the problem media type.
  /// </summary>
  public static Task WriteAsync
  (
    HttpResponse response,
    SharedProblemDetails problem,
    CancellationToken cancellationToken
  ) => WriteAsync(response, problem, options: null, cancellationToken);

  /// <summary>
  /// Writes <paramref name="problem"/> with <paramref name="options"/>, or the host's HTTP JSON options when null.
  /// </summary>
  public static Task WriteAsync
  (
    HttpResponse response,
    SharedProblemDetails problem,
    System.Text.Json.JsonSerializerOptions? options,
    CancellationToken cancellationToken
  )
  {
    ArgumentNullException.ThrowIfNull(response);
    ArgumentNullException.ThrowIfNull(problem);
    response.StatusCode = problem.Status ?? StatusCodes.Status400BadRequest;
    return response.WriteAsJsonAsync(problem, options, ContentType, cancellationToken);
  }
}
