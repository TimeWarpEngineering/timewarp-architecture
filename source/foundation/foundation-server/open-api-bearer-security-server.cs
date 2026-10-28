#region Purpose
// Attaches JWTBearerAuth to OpenAPI operations that accept the agent-token scheme, including scopes and bearer failure responses.
#endregion

#region Design
// FastEndpoints.OpenApi inserts a scheme named JWTBearerAuth and then copies it onto an
// operation only when EndpointDefinition.AuthSchemeNames contains that same string.
// Product endpoints call AuthSchemes("agent-token"), so the scheme stayed unused and
// Scalar had nothing to authorize. EnableJWTBearerAuth stays off and AddAuth registers
// the same scheme name with a description of the opaque bearer.
// Scopes are not Roles and not runtime Scopes() checks. They are the OpenAPI reading of
// the policy name already on the endpoint: PermissionIds and the api-server
// agent-scope:* policies. foundation-server cannot reference those catalogs, so the
// strings live here and the integration test pins them.
// identity:read is the scope for agent self-lookup and for agent-link.manage.self
// (AgentScopePermissionSeed expands that permission from identity:read).
// 401/403 bodies match AgentTokenAuthenticationHandler: application/problem+json and
// the RFC 6750 WWW-Authenticate values. Cookie-only and anonymous operations are left
// alone so a public ceremony is not documented as requiring a bearer.
#endregion

namespace TimeWarp.Foundation;

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

/// <summary>
/// Documents agent bearer auth on the OpenAPI operations that accept it.
/// </summary>
internal static class OpenApiBearerSecurity
{
  internal const string SchemeName = "JWTBearerAuth";
  internal const string AgentTokenSchemeName = "agent-token";

  private const string ProblemJson = "application/problem+json";
  private const string WwwAuthenticate = "WWW-Authenticate";

  private static readonly Dictionary<string, string> PolicyScopes = new(StringComparer.Ordinal)
  {
    ["identity.read"] = AgentScopes.IdentityRead,
    ["agent-scope:identity:read"] = AgentScopes.IdentityRead,
    ["credential.manage.self"] = AgentScopes.CredentialManage,
    ["demo.invoke"] = AgentScopes.DemoInvoke,
    ["agent-scope:demo:invoke"] = AgentScopes.DemoInvoke,
    ["agent-link.manage.self"] = AgentScopes.IdentityRead,
    ["feedback.read"] = AgentScopes.FeedbackRead,
    ["feedback.note"] = AgentScopes.FeedbackNote,
    ["feedback.severity"] = AgentScopes.FeedbackSeverity,
    ["feedback.status"] = AgentScopes.FeedbackStatus,
  };

  internal static OpenApiSecurityScheme CreateScheme()
  {
    return new OpenApiSecurityScheme
    {
      Type = SecuritySchemeType.Http,
      Scheme = "bearer",
      Description =
        "Opaque agent access token from POST /api/identity/agent/token. "
        + "Send it as Authorization: Bearer. "
        + "Operations name the required scope: identity:read (agent self and human-link calls), "
        + "credential:manage (credential list, add, and revoke), "
        + "demo:invoke (metered capability), "
        + "or a feedback scope (feedback:read, feedback:note, feedback:severity, feedback:status). "
        + "401 and 403 bodies are application/problem+json. "
        + "401 WWW-Authenticate is Bearer when no credential was presented, "
        + "or Bearer error=\"invalid_token\" when a bearer was rejected. "
        + "403 WWW-Authenticate is Bearer error=\"insufficient_scope\"."
    };
  }

  internal static void Register(OpenApiOptions openApiOptions)
  {
    ArgumentNullException.ThrowIfNull(openApiOptions);
    openApiOptions.AddOperationTransformer(TransformAsync);
  }

  private static Task TransformAsync
  (
    OpenApiOperation operation,
    OpenApiOperationTransformerContext context,
    CancellationToken cancellationToken
  )
  {
    _ = cancellationToken;
    EndpointDefinition? endpoint = context.Description.ActionDescriptor.EndpointMetadata
      .OfType<EndpointDefinition>()
      .SingleOrDefault();
    if (endpoint is null || context.Document is null || !AcceptsAgentToken(endpoint, context.Description.HttpMethod))
    {
      return Task.CompletedTask;
    }

    List<string> scopes = ScopesFor(endpoint);
    operation.Security =
    [
      new OpenApiSecurityRequirement
      {
        [new OpenApiSecuritySchemeReference(SchemeName, context.Document, null)] = scopes
      }
    ];
    NoteRequiredScopes(operation, scopes);
    DocumentBearerFailure
    (
      operation,
      "401",
      "Missing or rejected agent bearer. The body is application/problem+json with title Unauthorized.",
      "RFC 6750 challenge. Bearer when no credential was presented; Bearer error=\"invalid_token\" when a presented bearer was rejected."
    );
    DocumentBearerFailure
    (
      operation,
      "403",
      "The bearer authenticated but does not carry the required scope. The body is application/problem+json with title Forbidden.",
      "RFC 6750. The value is Bearer error=\"insufficient_scope\"."
    );
    return Task.CompletedTask;
  }

  private static bool AcceptsAgentToken(EndpointDefinition endpoint, string? httpMethod)
  {
    if (endpoint.AuthSchemeNames?.Contains(AgentTokenSchemeName, StringComparer.Ordinal) != true)
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

  private static List<string> ScopesFor(EndpointDefinition endpoint)
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
        if (PolicyScopes.TryGetValue(policy, out string? scope))
        {
          scopes.Add(scope);
        }
      }
    }

    var ordered = scopes.ToList();
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

    if (string.IsNullOrEmpty(operation.Description))
    {
      operation.Description = note;
      return;
    }

    if (operation.Description.Contains(note, StringComparison.Ordinal))
    {
      return;
    }

    operation.Description = operation.Description.TrimEnd() + " " + note;
  }

  private static void DocumentBearerFailure
  (
    OpenApiOperation operation,
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

    response.Description = description;
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
    if (!response.Headers.ContainsKey(WwwAuthenticate))
    {
      response.Headers[WwwAuthenticate] = new OpenApiHeader
      {
        Required = true,
        Description = headerDescription,
        Schema = new OpenApiSchema { Type = JsonSchemaType.String }
      };
    }
  }

  /// <summary>Scope strings carried by an agent access token. Kept beside the policy map so the document does not reference the identity library.</summary>
  private static class AgentScopes
  {
    internal const string IdentityRead = "identity:read";
    internal const string DemoInvoke = "demo:invoke";
    internal const string CredentialManage = "credential:manage";
    internal const string FeedbackRead = "feedback:read";
    internal const string FeedbackNote = "feedback:note";
    internal const string FeedbackSeverity = "feedback:severity";
    internal const string FeedbackStatus = "feedback:status";
  }
}
