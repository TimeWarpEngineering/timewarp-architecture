#region Purpose
// Builds the JWTBearerAuth scheme, the scopes an agent-token operation requires, and its bearer failure responses.
#endregion

#region Design
// JWTBearerAuth is the scheme name Scalar and the reviewer guide look up. FastEndpoints'
// automatic scheme and AddAuth stay off (see CommonServerModule); OpenApiOperationSecurity
// adds this scheme to the components.
// The host passes OpenApiAgentBearer: the scheme name and the policy → scope map. This
// file has no product policy names, scope strings, or scheme name.
// Scopes are the OpenAPI reading of the policy already on the endpoint, not Roles and
// not runtime Scopes() checks. An unmapped policy still requires the bearer and an empty
// scope list.
// 401 and 403 bodies are application/problem+json. WWW-Authenticate follows RFC 6750 when
// the bearer handler challenges, and is omitted on a handler-level 401 or 403. An existing
// response description is kept and the bearer note is appended. The header is not required.
// OpenApiOperationSecurity decides which operations accept the bearer and writes
// operation.Security; this file supplies the scheme, the scopes, and the failure responses.
// How to obtain a token, and the configured lifetime, are not written here. The web host
// appends that paragraph when the document is built (OpenApiAgentReviewerGuide), because
// this file does not know the ceremony operation names or AgentTokenOptions.
#endregion

namespace TimeWarp.Foundation;

using Microsoft.OpenApi;

/// <summary>
/// Agent bearer scheme, scopes, and failure responses for the OpenAPI document.
/// </summary>
internal static class OpenApiBearerSecurity
{
  internal const string SchemeName = "JWTBearerAuth";

  private const string ProblemJson = "application/problem+json";
  private const string WwwAuthenticate = "WWW-Authenticate";

  private const string UnauthorizedDescription =
    "Missing or rejected agent bearer, or a handler-level 401. The body is application/problem+json. "
    + "WWW-Authenticate is present for a bearer challenge and absent on a handler-level 401.";

  private const string ForbiddenDescription =
    "Insufficient scope from the bearer handler, or a handler-level 403. The body is application/problem+json. "
    + "WWW-Authenticate is Bearer error=\"insufficient_scope\" only when the scope is missing, "
    + "and is absent on a handler-level 403.";

  private const string UnauthorizedHeader =
    "Present for an RFC 6750 challenge only. Bearer when no credential was presented; "
    + "Bearer error=\"invalid_token\" when a presented bearer was rejected. Absent on a handler-level 401.";

  private const string ForbiddenHeader =
    "Present only when the bearer lacks the required scope. The value is Bearer error=\"insufficient_scope\". "
    + "Absent on a handler-level 403.";

  internal static OpenApiSecurityScheme CreateScheme(IReadOnlyCollection<string> scopes)
  {
    ArgumentNullException.ThrowIfNull(scopes);
    List<string> ordered = [];
    HashSet<string> seen = new(StringComparer.Ordinal);
    foreach (string scope in scopes)
    {
      if (!string.IsNullOrWhiteSpace(scope) && seen.Add(scope))
      {
        ordered.Add(scope);
      }
    }

    ordered.Sort(StringComparer.Ordinal);
    string scopeSentence = ordered.Count == 0
      ? "Operations name the required scope."
      : "Known scopes: " + string.Join(", ", ordered) + ".";

    return new OpenApiSecurityScheme
    {
      Type = SecuritySchemeType.Http,
      Scheme = "bearer",
      Description =
        "Opaque agent access token. Send it as Authorization: Bearer. "
        + scopeSentence + " "
        + "401 and 403 bodies are application/problem+json. "
        + "401 WWW-Authenticate is Bearer when no credential was presented, "
        + "or Bearer error=\"invalid_token\" when a presented bearer was rejected. "
        + "403 WWW-Authenticate is Bearer error=\"insufficient_scope\" only when the scope is missing, "
        + "and is absent on a handler-level 403."
    };
  }

  internal static List<string> ScopesFor(EndpointDefinition endpoint, OpenApiAgentBearer agentBearer)
  {
    HashSet<string> scopes = new(StringComparer.Ordinal);
    if (endpoint.AllowedScopes is not null)
    {
      foreach (string scope in endpoint.AllowedScopes)
      {
        if (!string.IsNullOrWhiteSpace(scope))
        {
          scopes.Add(scope);
        }
      }
    }

    if (endpoint.PreBuiltUserPolicies is not null)
    {
      foreach (string policy in endpoint.PreBuiltUserPolicies)
      {
        if (agentBearer.PolicyScopes.TryGetValue(policy, out string? scope))
        {
          scopes.Add(scope);
        }
      }
    }

    List<string> ordered = [.. scopes];
    ordered.Sort(StringComparer.Ordinal);
    return ordered;
  }

  internal static void DocumentBearerFailures(OpenApiOperation operation, EndpointDefinition endpoint)
  {
    ArgumentNullException.ThrowIfNull(operation);
    ArgumentNullException.ThrowIfNull(endpoint);
    DocumentBearerFailure(operation, endpoint, "401", UnauthorizedDescription, UnauthorizedHeader);
    DocumentBearerFailure(operation, endpoint, "403", ForbiddenDescription, ForbiddenHeader);
  }

  private static void DocumentBearerFailure
  (
    OpenApiOperation operation,
    EndpointDefinition endpoint,
    string statusCode,
    string description,
    string headerDescription
  )
  {
    operation.Responses ??= new OpenApiResponses();
    OpenApiResponse response;
    if (operation.Responses.TryGetValue(statusCode, out IOpenApiResponse? existing) && existing is OpenApiResponse concrete)
    {
      response = concrete;
    }
    else
    {
      response = new OpenApiResponse();
      operation.Responses[statusCode] = response;
    }

    string? described = response.Description;
    if (string.IsNullOrEmpty(described)
      && int.TryParse(statusCode, out int status)
      && endpoint.EndpointSummary?.Responses.TryGetValue(status, out string? summaryDescription) == true)
    {
      described = summaryDescription;
    }

    response.Description = AppendNote(described, description);
    response.Content ??= new Dictionary<string, IOpenApiMediaType>();
    if (!response.Content.ContainsKey(ProblemJson))
    {
      response.Content[ProblemJson] = new OpenApiMediaType
      {
        Schema = new OpenApiSchema
        {
          Type = JsonSchemaType.Object,
          Properties = new Dictionary<string, IOpenApiSchema>
          {
            ["title"] = new OpenApiSchema { Type = JsonSchemaType.String },
            ["status"] = new OpenApiSchema { Type = JsonSchemaType.Integer },
            ["detail"] = new OpenApiSchema { Type = JsonSchemaType.String }
          }
        }
      };
    }

    response.Headers ??= new Dictionary<string, IOpenApiHeader>();
    if (response.Headers.TryGetValue(WwwAuthenticate, out IOpenApiHeader? existingHeader) && existingHeader is OpenApiHeader header)
    {
      header.Required = false;
      header.Description = AppendNote(header.Description, headerDescription);
      return;
    }

    response.Headers[WwwAuthenticate] = new OpenApiHeader
    {
      Required = false,
      Description = headerDescription,
      Schema = new OpenApiSchema { Type = JsonSchemaType.String }
    };
  }

  internal static string AppendNote(string? existing, string note)
  {
    if (string.IsNullOrEmpty(existing))
    {
      return note;
    }

    if (existing.Contains(note, StringComparison.Ordinal))
    {
      return existing;
    }

    return existing.TrimEnd() + " " + note;
  }
}
