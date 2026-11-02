#region Purpose
// Writes the agent reviewer guide onto the JWTBearerAuth scheme when the OpenAPI document is built.
#endregion

#region Design
// An outside reviewer is given only this document. The scheme description names the four
// ceremony operations in order, the lifetime from AgentTokenOptions at document build, that
// there is no refresh token, and when to reissue. Foundation does not know those product
// operation names or AgentTokenOptions, so this host appends the paragraph. The scheme object
// is reused across document builds, so a second build replaces the paragraph instead of
// appending it again. What the client signs lives on the ceremony contracts, and the
// processed marker lives on list-feedback, so those are not written here.
#endregion

namespace TimeWarp.Architecture.Web.Server;

using System.Globalization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using TimeWarp.Architecture.Features.Identity.Application;

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
      + " start-agent-key-registration, then complete-agent-key-registration,"
      + " then start-agent-token-issuance, then complete-agent-token-issuance."
      + " The token lasts "
      + lifetimeMinutes.ToString(CultureInfo.InvariantCulture)
      + " minutes. There is no refresh token."
      + " Reissue with the agent key before the token expires, or after a 401 invalid_token.";

    return string.IsNullOrEmpty(basis) ? guide : basis.TrimEnd() + " " + guide;
  }
}
