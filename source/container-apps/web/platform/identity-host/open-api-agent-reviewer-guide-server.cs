#region Purpose
// Writes the agent reviewer guide onto the JWTBearerAuth scheme when the OpenAPI document is built.
#endregion

#region Design
// An outside reviewer is given only this document. The scheme description names the four
// ceremony operations in order, the lifetime from AgentTokenOptions at document build, that
// there is no refresh token, when to reissue, and that a token may name only admin-approved
// scopes (identity:read is always allowed). Each step also names its HTTP method and
// path, taken from that contract's generated route, because the document does not set
// operationId to the kebab id. Foundation does not know those product operation names or
// AgentTokenOptions, so this host appends the paragraph. The scheme object is reused across
// document builds, so a second build replaces the paragraph instead of appending it again.
// What the client signs lives on the ceremony contracts, and the processed marker lives on
// list-feedback, so those are not written here.
#endregion

namespace TimeWarp.Architecture.Web.Server;

using System.Globalization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using TimeWarp.Architecture.Features.Identity.Application;
using TimeWarp.Foundation.Features;
using Contracts = TimeWarp.Architecture.Features.Identity;

/// <summary>Appends the agent-token reviewer guide to the OpenAPI bearer scheme.</summary>
internal static class OpenApiAgentReviewerGuide
{
  internal const string SchemeName = "JWTBearerAuth";

  private const string GuideMarker = "To obtain a token, call these operations in order:";

  internal static void Register(OpenApiOptions openApiOptions)
  {
    ArgumentNullException.ThrowIfNull(openApiOptions);
    openApiOptions.AddDocumentTransformer(DescribeAsync);
  }

  private static Task DescribeAsync
  (
    OpenApiDocument document,
    OpenApiDocumentTransformerContext context,
    CancellationToken cancellationToken
  )
  {
    _ = cancellationToken;
    int lifetimeMinutes = context.ApplicationServices
      .GetRequiredService<IOptions<AgentTokenOptions>>()
      .Value
      .TokenLifetimeMinutes;

    if (document.Components?.SecuritySchemes is not { } schemes
      || !schemes.TryGetValue(SchemeName, out IOpenApiSecurityScheme? scheme)
      || scheme is not OpenApiSecurityScheme concrete)
    {
      throw new InvalidOperationException
      (
        "JWTBearerAuth is missing from the OpenAPI document, so the agent reviewer guide cannot be written."
      );
    }

    concrete.Description = WithGuide(concrete.Description, lifetimeMinutes);
    return Task.CompletedTask;
  }

  private static string WithGuide(string? existing, int lifetimeMinutes)
  {
    string basis = existing ?? string.Empty;
    int marker = basis.IndexOf(GuideMarker, StringComparison.Ordinal);
    if (marker >= 0)
    {
      basis = basis[..marker].TrimEnd();
    }

    string guide =
      GuideMarker
      + " " + Step<Contracts.StartAgentKeyRegistration.Command>("start-agent-key-registration")
      + ", then " + Step<Contracts.CompleteAgentKeyRegistration.Command>("complete-agent-key-registration")
      + ", then " + Step<Contracts.StartAgentTokenIssuance.Command>("start-agent-token-issuance")
      + ", then " + Step<Contracts.CompleteAgentTokenIssuance.Command>("complete-agent-token-issuance")
      + ". The token lasts "
      + lifetimeMinutes.ToString(CultureInfo.InvariantCulture)
      + " minutes. There is no refresh token."
      + " Reissue with the agent key before the token expires, or after a 401 invalid_token."
      + " The agent may request only scopes an admin approved (identity:read is always allowed); an unapproved scope returns 403 naming those scopes, and removing an approval applies at the next issuance rather than revoking a token already issued.";

    return string.IsNullOrEmpty(basis) ? guide : basis.TrimEnd() + " " + guide;
  }

  private static string Step<TRequest>(string operationId) where TRequest : IApiRequest, new()
  {
    TRequest request = new();
    string method = request.GetHttpVerb().ToString().ToUpperInvariant();
    string route = request.GetRoute();
    string path = route.StartsWith('/', StringComparison.Ordinal) ? route : "/" + route;
    return operationId + " (" + method + " " + path + ")";
  }
}
