#region Purpose
// Attaches JWTBearerAuth to OpenAPI operations that accept the host's agent bearer scheme, including scopes and bearer failure responses.
#endregion

#region Design
// FastEndpoints.OpenApi inserts a scheme named JWTBearerAuth and then copies it onto an
// operation only when EndpointDefinition.AuthSchemeNames contains that same string.
// Product endpoints list the host's agent scheme, so EnableJWTBearerAuth stays off and
// AddAuth registers JWTBearerAuth for the opaque bearer.
// The host passes OpenApiAgentBearer: the scheme name and the policy → scope map. This
// file has no product policy names, scope strings, or scheme name.
// Scopes are the OpenAPI reading of the policy already on the endpoint, not Roles and
// not runtime Scopes() checks. An unmapped policy still requires the bearer and an empty
// scope list.
// 401 and 403 bodies are application/problem+json. WWW-Authenticate follows RFC 6750 when
// the bearer handler challenges, and is omitted on a handler-level 401 or 403. An existing
// response description is kept and the bearer note is appended. The header is not required.
// Cookie-only and anonymous operations are left alone so a public ceremony is not
// documented as requiring a bearer.
#endregion

namespace TimeWarp.Foundation;

using Microsoft.OpenApi;

/// <summary>
/// Documents agent bearer auth on the OpenAPI operations that accept it.
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

  internal static void Register(OpenApiOptions openApiOptions, OpenApiAgentBearer agentBearer)
  {
    ArgumentNullException.ThrowIfNull(openApiOptions);
    ArgumentNullException.ThrowIfNull(agentBearer);
    openApiOptions.AddOperationTransformer((operation, context, cancellationToken) =>
      TransformAsync(operation, context, agentBearer, cancellationToken));
  }

  private static Task TransformAsync
  (
    OpenApiOperation operation,
    OpenApiOperationTransformerContext context,
    OpenApiAgentBearer agentBearer,
    CancellationToken cancellationToken
  )
  {
    _ = cancellationToken;
    EndpointDefinition? endpoint = context.Description.ActionDescriptor.EndpointMetadata
      .OfType<EndpointDefinition>()
      .SingleOrDefault();
    if (endpoint is null || context.Document is null || !AcceptsAgentToken(endpoint, context.Description.HttpMethod, agentBearer.SchemeName))
    {
      return Task.CompletedTask;
    }

    List<string> scopes = ScopesFor(endpoint, agentBearer);
    operation.Security =
    [
      new OpenApiSecurityRequirement
      {
        [new OpenApiSecuritySchemeReference(SchemeName, context.Document, null)] = scopes
      }
    ];
    NoteRequiredScopes(operation, scopes);
    DocumentBearerFailure(operation, endpoint, "401", UnauthorizedDescription, UnauthorizedHeader);
    DocumentBearerFailure(operation, endpoint, "403", ForbiddenDescription, ForbiddenHeader);
    return Task.CompletedTask;
  }

  private static bool AcceptsAgentToken(EndpointDefinition endpoint, string? httpMethod, string schemeName)
  {
    if (endpoint.AuthSchemeNames?.Contains(schemeName, StringComparer.Ordinal) != true)
    {
      return false;
    }

    if (endpoint.AnonymousVerbs is null || string.IsNullOrEmpty(httpMethod))
    {
      return true;
    }

    foreach (string verb in endpoint.AnonymousVerbs)
    {
      if (string.Equals(verb, httpMethod, StringComparison.OrdinalIgnoreCase))
      {
        return false;
      }
    }

    return true;
  }

  private static List<string> ScopesFor(EndpointDefinition endpoint, OpenApiAgentBearer agentBearer)
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

  private static void NoteRequiredScopes(OpenApiOperation operation, List<string> scopes)
  {
    string note = scopes.Count switch
    {
      0 => "Requires an agent bearer token (JWTBearerAuth).",
      1 => $"Requires bearer scope {scopes[0]} (JWTBearerAuth).",
      _ => $"Requires bearer scopes {string.Join(", ", scopes)} (JWTBearerAuth)."
    };

    operation.Description = AppendNote(operation.Description, note);
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
    response.Content ??= new Dictionary<string, OpenApiMediaType>();
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

  private static string AppendNote(string? existing, string note)
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
