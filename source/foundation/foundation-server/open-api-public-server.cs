#region Purpose
// Replaces the OpenAPI servers entry with the caller's public origin when a proxy forwarded it.
#endregion

#region Design
// Microsoft.AspNetCore.OpenApi sets servers from Request.Scheme and Request.Host. Web.Server does
// not call UseForwardedHeaders (that would rewrite Host for the whole app; identity reads
// X-Forwarded-Host itself). Behind the YARP ingress, Host is the internal hop — the dev http
// address http://localhost:63621 — while "X-Forwarded": "Set" puts the browser origin on
// X-Forwarded-Proto and X-Forwarded-Host. This transformer runs after the FastEndpoints document
// transformer and, only when one of those headers is present, replaces servers with that origin.
// A direct request keeps the framework server, so local Scalar still targets the listening address.
// The document is built per request, so a forged header changes only the caller's own document.
// Scalar leaves its Servers list empty and reads this document, so the UI base URL follows it.
// Same first-value rule as the mail public-origin accessor: first header value, then the first
// comma-separated entry; proto is honored only for http and https.
#endregion

namespace TimeWarp.Foundation;

using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi;

/// <summary>
/// Points the OpenAPI <c>servers</c> entry at the forwarded public origin.
/// </summary>
internal static class OpenApiPublicServer
{
  internal static void Register(OpenApiOptions openApiOptions)
  {
    ArgumentNullException.ThrowIfNull(openApiOptions);
    openApiOptions.AddDocumentTransformer(ReplaceServerAsync);
  }

  private static Task ReplaceServerAsync
  (
    OpenApiDocument document,
    OpenApiDocumentTransformerContext context,
    CancellationToken cancellationToken
  )
  {
    _ = cancellationToken;
    string? publicServerUrl = TryResolve(context.ApplicationServices);
    if (publicServerUrl is null)
    {
      return Task.CompletedTask;
    }

    document.Servers = [new OpenApiServer { Url = publicServerUrl }];
    return Task.CompletedTask;
  }

  private static string? TryResolve(IServiceProvider services)
  {
    IHttpContextAccessor? httpContextAccessor = services.GetService<IHttpContextAccessor>();
    HttpRequest? request = httpContextAccessor?.HttpContext?.Request;
    if (request is null)
    {
      return null;
    }

    string? forwardedHost = FirstValue(request.Headers[ForwardedHeadersDefaults.XForwardedHostHeaderName]);
    string? forwardedProto = FirstValue(request.Headers[ForwardedHeadersDefaults.XForwardedProtoHeaderName]);
    bool protoIsHttp = forwardedProto is not null
      && (string.Equals(forwardedProto, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
        || string.Equals(forwardedProto, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase));

    if (forwardedHost is null && !protoIsHttp)
    {
      return null;
    }

    if (forwardedHost is not null && !IsHost(forwardedHost))
    {
      return null;
    }

    string scheme = protoIsHttp ? forwardedProto!.ToLowerInvariant() : request.Scheme;
    HostString host = forwardedHost is not null ? new HostString(forwardedHost) : request.Host;
    if (!host.HasValue)
    {
      return null;
    }

    string serverUrl = UriHelper.BuildAbsolute(scheme, host, request.PathBase);
    return Uri.TryCreate(serverUrl, UriKind.Absolute, out Uri? uri)
      && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
      ? serverUrl
      : null;
  }

  private static bool IsHost(string host)
  {
    if (host.Contains("://", StringComparison.Ordinal)
      || host.Contains('/', StringComparison.Ordinal)
      || host.Contains('\\', StringComparison.Ordinal)
      || host.Contains('@', StringComparison.Ordinal))
    {
      return false;
    }

    foreach (char character in host)
    {
      if (char.IsWhiteSpace(character) || char.IsControl(character))
      {
        return false;
      }
    }

    return true;
  }

  private static string? FirstValue(StringValues values)
  {
    string? first = values.FirstOrDefault()?.Split(',')[0].Trim();
    return string.IsNullOrEmpty(first) ? null : first;
  }
}
