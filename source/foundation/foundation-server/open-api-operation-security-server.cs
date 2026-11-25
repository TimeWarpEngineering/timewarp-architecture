#region Purpose
// Writes each OpenAPI operation's security from the endpoint's authentication schemes: session cookie, agent bearer, both, or none.
#endregion

#region Design
// One operation transformer owns operation.Security so the requirement list cannot drift from
// EndpointDefinition.AuthSchemeNames, the list ASP.NET authorizes with.
// - An anonymous verb (AllowAnonymous) gets security: [] so a reader sees it is public on
//   purpose. Microsoft.OpenApi writes an empty list for OpenAPI 3.x.
// - The agent scheme listed → JWTBearerAuth with the scopes OpenApiBearerSecurity derives, plus
//   its 401/403 problem responses.
// - The session scheme listed, or no scheme listed when the session is the host's
//   DefaultScheme → IdentitySessionCookie with no scopes.
// - Both → two requirement objects, bearer first. OpenAPI reads them as alternatives.
// - A scheme this host did not describe (for example mock-identity-session alone, or Entra) is
//   left without security; the document test fails when such an operation documents 401.
// The operation description gets one sentence naming what is accepted.
// The schemes are added to components by a document transformer here, not FastEndpoints
// AddAuth: FastEndpoints' document pass runs after operation transformers and writes every
// AddAuth scheme onto an endpoint whose AuthSchemeNames is null, which would replace the
// session-only requirement with bearer-or-session.
// The session scheme is apiKey in a cookie. Its description says the cookie comes only from the
// browser passkey or Entra ceremony, so an agent does not try to obtain it.
#endregion

namespace TimeWarp.Foundation;

using Microsoft.OpenApi;

/// <summary>
/// Documents which credentials each OpenAPI operation accepts.
/// </summary>
internal static class OpenApiOperationSecurity
{
  internal const string SessionSchemeName = "IdentitySessionCookie";

  internal static OpenApiSecurityScheme CreateSessionScheme(OpenApiSessionCookie sessionCookie)
  {
    ArgumentNullException.ThrowIfNull(sessionCookie);
    return new OpenApiSecurityScheme
    {
      Type = SecuritySchemeType.ApiKey,
      In = ParameterLocation.Cookie,
      Name = sessionCookie.CookieName,
      Description =
        "Browser session cookie. It is issued only by the browser sign-in ceremony (passkey or Entra) "
        + "and is HttpOnly, so an agent or a generated client cannot obtain it. "
        + "Operations that also accept an agent token list JWTBearerAuth as an alternative requirement. "
        + "A missing or expired session answers 401 with an empty body and no WWW-Authenticate header."
    };
  }

  internal static void Register
  (
    OpenApiOptions openApiOptions,
    OpenApiAgentBearer? agentBearer,
    OpenApiSessionCookie? sessionCookie
  )
  {
    ArgumentNullException.ThrowIfNull(openApiOptions);
    openApiOptions.AddDocumentTransformer((document, context, cancellationToken) =>
    {
      _ = context;
      _ = cancellationToken;
      AddSchemes(document, agentBearer, sessionCookie);
      return Task.CompletedTask;
    });
    openApiOptions.AddOperationTransformer((operation, context, cancellationToken) =>
      TransformAsync(operation, context, agentBearer, sessionCookie, cancellationToken));
  }

  private static void AddSchemes(OpenApiDocument document, OpenApiAgentBearer? agentBearer, OpenApiSessionCookie? sessionCookie)
  {
    if (agentBearer is null && sessionCookie is null)
    {
      return;
    }

    document.Components ??= new OpenApiComponents();
    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
    if (agentBearer is not null)
    {
      document.Components.SecuritySchemes[OpenApiBearerSecurity.SchemeName] =
        OpenApiBearerSecurity.CreateScheme(agentBearer.PolicyScopes.Values.ToList());
    }

    if (sessionCookie is not null)
    {
      document.Components.SecuritySchemes[SessionSchemeName] = CreateSessionScheme(sessionCookie);
    }
  }

  private static async Task TransformAsync
  (
    OpenApiOperation operation,
    OpenApiOperationTransformerContext context,
    OpenApiAgentBearer? agentBearer,
    OpenApiSessionCookie? sessionCookie,
    CancellationToken cancellationToken
  )
  {
    EndpointDefinition? endpoint = context.Description.ActionDescriptor.EndpointMetadata
      .OfType<EndpointDefinition>()
      .SingleOrDefault();
    if (endpoint is null || context.Document is null)
    {
      return;
    }

    if (IsAnonymous(endpoint, context.Description.HttpMethod))
    {
      operation.Security = [];
      return;
    }

    bool acceptsAgent = agentBearer is not null && Lists(endpoint, agentBearer.SchemeName);
    bool acceptsSession = sessionCookie is not null
      && (Lists(endpoint, sessionCookie.SchemeName) || (sessionCookie.IsDefaultScheme && ListsNone(endpoint)));
    if (!acceptsAgent && !acceptsSession)
    {
      return;
    }

    List<OpenApiSecurityRequirement> requirements = [];
    List<string> scopes = [];
    if (acceptsAgent)
    {
      scopes = OpenApiBearerSecurity.ScopesFor(endpoint, agentBearer!);
      requirements.Add(new OpenApiSecurityRequirement
      {
        [new OpenApiSecuritySchemeReference(OpenApiBearerSecurity.SchemeName, context.Document, null)] = scopes
      });
    }

    if (acceptsSession)
    {
      requirements.Add(new OpenApiSecurityRequirement
      {
        [new OpenApiSecuritySchemeReference(SessionSchemeName, context.Document, null)] = []
      });
    }

    operation.Security = requirements;
    operation.Description = OpenApiBearerSecurity.AppendNote
    (
      operation.Description,
      Note(acceptsAgent, acceptsSession, agentBearer is not null, scopes)
    );
    if (acceptsAgent)
    {
      IOpenApiSchema problem = await OpenApiProblemDetails.ReferenceAsync(context, cancellationToken).ConfigureAwait(false);
      OpenApiBearerSecurity.DocumentBearerFailures(operation, endpoint, problem);
    }
  }

  private static bool IsAnonymous(EndpointDefinition endpoint, string? httpMethod)
  {
    if (endpoint.AnonymousVerbs is null || string.IsNullOrEmpty(httpMethod))
    {
      return false;
    }

    foreach (string verb in endpoint.AnonymousVerbs)
    {
      if (string.Equals(verb, httpMethod, StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }
    }

    return false;
  }

  private static bool Lists(EndpointDefinition endpoint, string schemeName) =>
    endpoint.AuthSchemeNames?.Contains(schemeName, StringComparer.Ordinal) == true;

  private static bool ListsNone(EndpointDefinition endpoint) =>
    endpoint.AuthSchemeNames is null || endpoint.AuthSchemeNames.Count == 0;

  private static string Note(bool acceptsAgent, bool acceptsSession, bool hostHasAgentBearer, List<string> scopes)
  {
    string session = $"the browser session cookie ({SessionSchemeName})";
    if (!acceptsAgent)
    {
      return hostHasAgentBearer
        ? $"Requires {session}. An agent bearer token is not accepted."
        : $"Requires {session}.";
    }

    string bearer = scopes.Count switch
    {
      0 => $"an agent bearer token ({OpenApiBearerSecurity.SchemeName})",
      1 => $"an agent bearer with scope {scopes[0]} ({OpenApiBearerSecurity.SchemeName})",
      _ => $"an agent bearer with scopes {string.Join(", ", scopes)} ({OpenApiBearerSecurity.SchemeName})"
    };
    return acceptsSession
      ? $"Accepts {session} or {bearer}."
      : $"Requires {bearer}.";
  }
}
