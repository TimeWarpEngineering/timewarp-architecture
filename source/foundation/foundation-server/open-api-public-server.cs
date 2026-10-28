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
// The origin comes from PublicRequestOrigin, the same rule mail and link previews use: first
// header value, then the first comma-separated entry; proto is honored only for http and https;
// a forwarded host that is not a host is refused. This transformer applies that origin only when
// a forwarded value is present, so a direct request keeps the framework server.
#endregion

namespace TimeWarp.Foundation;

using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
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
    return request is null ? null : PublicRequestOrigin.ForwardedServerUrl(request);
  }
}
