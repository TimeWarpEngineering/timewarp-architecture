#region Purpose
// Tells OpenAPI readers which public web routes exist outside the generated FastEndpoints operations.
#endregion

#region Design
// ExcludeNonFastEndpoints stays on, so minimal APIs stay out of the document. Web-server opts in. Api-server does not: it does not serve these routes.
// GET /api/health is MapHealthChecks. The body is the status name as text/plain. The Postgres
// check, when registered, opens a connection, so this is not the liveness probe.
// GET and POST /api are TipDiscoveryAlias rewrites of /api/tip. The alias is its own path so a
// missing entry is not read as a missing route. Its responses (content and headers), parameters,
// and request body are shallow copies of the /api/tip operation of the same verb, taken after the
// operation transformers ran (a host's payment 402/503 included), so the alias cannot document
// less than the route it rewrites to (task 458-009).
// Operation ids follow the contracts' <resource>.<action> style (task 458-005): health.get, and
// each alias is its /api/tip operation's id plus -alias (tip.get-alias, tip.submit-alias), so a
// renamed tip operation renames its alias.
// /.well-known discovery documents are excluded. info.description is what Scalar renders, and
// x-timewarp-excluded-routes is the same list for a reader that does not parse markdown.
// Both point at /llms.txt.
// Liveness (task 458-007) is listed, not an operation: it reports the process, not this API. Its
// entry and the health description both read IngressPaths.WebServerAlivePath, so they cannot name
// different probes. The host adds its own non-operation routes (OpenApiExcludedRoute) from the
// constants its Map calls use. info.description names the families that are not listed (Razor
// pages, static files, Blazor framework routes, this document, Scalar, and Development-only
// routes), so "a missing path is an exclusion" stays true. The web-server discovery-coverage test
// skips exactly those families, enumerates the served routes and methods, and fails on an
// unlisted one. MapDefaultEndpoints also serves HealthEndpointPaths.Alive in every environment,
// and the ingress catch-all forwards a public /alive to web-server, so that path is listed too.
#endregion

namespace TimeWarp.Foundation;

using System.Net.Http;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

/// <summary>
/// Adds the public web routes that are not generated FastEndpoints operations.
/// </summary>
internal static class OpenApiDiscoveryCoverage
{
  internal const string ExtensionName = "x-timewarp-excluded-routes";

  internal const string HealthPath = TimeWarp.Foundation.Configuration.IngressPaths.WebServerHealthPath;

  internal const string HealthOperationId = "health.get";

  internal const string AlivePath = TimeWarp.Foundation.Configuration.IngressPaths.WebServerAlivePath;

  internal const string LivenessSentence = "Liveness is GET " + AlivePath + ".";

  internal const string TipAliasPath = TimeWarp.Foundation.Configuration.IngressPaths.TipDiscoveryAliasPath;

  internal const string TipPath = "/api/tip";

  internal const string OutsideHeading = "## Routes outside this document";

  internal const string AliasSentence =
    "GET and POST /api and /api/ rewrite to /api/tip before routing.";

  internal const string UnlistedFamilies =
    "Razor pages, static files (such as `/llms.txt`), Blazor framework routes (`/_framework`, `/_blazor`), "
    + "this document (`/openapi/v1.json`), Scalar (`/scalar/v1`), and Development-only routes";

  private const string LlmsPath = "/llms.txt";

  private static readonly OpenApiExcludedRoute[] SharedRoutes =
  [
    new("GET", "/.well-known/mcp/server-card.json", "Static MCP server card. capabilities.tools is false.", LlmsPath),
    new("GET", "/.well-known/mcp.json", "Alias of /.well-known/mcp/server-card.json.", LlmsPath),
    new("GET", "/.well-known/agent-skills/index.json", "Static Agent Skills index.", LlmsPath),
    new("GET", "/.well-known/agent-card.json", "Unsigned A2A agent card. No JSON-RPC task endpoint.", LlmsPath),
    new("GET", "/.well-known/agent.json", "Alias of /.well-known/agent-card.json.", LlmsPath),
    new("GET", "/.well-known/webmcp.json", "Generated WebMCP document for the browser, not this REST API.", LlmsPath),
    new
    (
      "GET",
      AlivePath,
      "Liveness probe. The ingress forwards it to web-server "
      + TimeWarp.Foundation.Configuration.HealthEndpointPaths.Alive
      + ", which runs only the self check and opens no dependency. The body is text/plain: Healthy (200) "
      + "or Unhealthy (503). Readiness is GET " + HealthPath + ".",
      LlmsPath
    ),
    new
    (
      "GET",
      TimeWarp.Foundation.Configuration.HealthEndpointPaths.Alive,
      "The same self check as GET " + AlivePath + ", served by MapDefaultEndpoints in every environment. "
      + "The ingress forwards an unmatched public path to web-server, so this path answers too. "
      + "The body is text/plain: Healthy (200) or Unhealthy (503).",
      LlmsPath
    ),
  ];

  internal static void Register(OpenApiOptions openApiOptions, IReadOnlyList<OpenApiExcludedRoute>? hostRoutes)
  {
    ArgumentNullException.ThrowIfNull(openApiOptions);
    OpenApiExcludedRoute[] routes = [.. SharedRoutes, .. hostRoutes ?? []];
    openApiOptions.AddDocumentTransformer
    (
      (document, context, cancellationToken) => AddCoverageAsync(document, routes, context, cancellationToken)
    );
  }

  private static Task AddCoverageAsync
  (
    OpenApiDocument document,
    OpenApiExcludedRoute[] routes,
    OpenApiDocumentTransformerContext context,
    CancellationToken cancellationToken
  )
  {
    _ = context;
    _ = cancellationToken;
    if (document.Extensions?.ContainsKey(ExtensionName) == true)
    {
      return Task.CompletedTask;
    }

    document.Paths ??= new OpenApiPaths();
    AddHealth(document);
    AddTipAlias(document);
    NoteCanonicalTip(document);
    NoteExclusions(document, routes);
    document.AddExtension(ExtensionName, new JsonNodeExtension(ExcludedArray(routes)));
    return Task.CompletedTask;
  }

  private static void AddHealth(OpenApiDocument document)
  {
    if (document.Paths.ContainsKey(HealthPath))
    {
      return;
    }

    OpenApiPathItem pathItem = new();
    pathItem.AddOperation
    (
      HttpMethod.Get,
      AnonymousOperation
      (
        document,
        HealthOperationId,
        "Web server health",
        "Anonymous ASP.NET health check. The body is text/plain and is one status name: "
        + "Healthy, Degraded, or Unhealthy. 200 is Healthy or Degraded. 503 is Unhealthy. "
        + "When the PostgresDbContext check is registered, this route opens a database connection. "
        + "Do not use it as an uptime probe. " + LivenessSentence,
        "Health",
        ("200", PlainText("Healthy or Degraded. The body is that status name.")),
        ("503", PlainText("Unhealthy. The body is Unhealthy."))
      )
    );
    document.Paths.Add(HealthPath, pathItem);
  }

  private static void AddTipAlias(OpenApiDocument document)
  {
    if (document.Paths.ContainsKey(TipAliasPath))
    {
      return;
    }

    const string description =
      AliasSentence + " "
      + "The responses, parameters, and request body are copied from the /api/tip operation, "
      + "including 402 PAYMENT-REQUIRED and 503 when payment is disabled or misconfigured. Anonymous.";

    document.Paths.TryGetValue(TipPath, out IOpenApiPathItem? tipPathItem);
    OpenApiPathItem pathItem = new();
    pathItem.AddOperation
    (
      HttpMethod.Get,
      AliasOperation(document, tipPathItem, HttpMethod.Get, description)
    );
    pathItem.AddOperation
    (
      HttpMethod.Post,
      AliasOperation(document, tipPathItem, HttpMethod.Post, description)
    );
    document.Paths.Add(TipAliasPath, pathItem);
  }

  /// <summary>
  /// The alias operation for one verb. Responses, parameters, and the request body are copies of
  /// the /api/tip operation of that verb, so a transformer that documents /api/tip documents the
  /// alias too. Without that operation the alias says only that it follows /api/tip.
  /// </summary>
  private static OpenApiOperation AliasOperation
  (
    OpenApiDocument document,
    IOpenApiPathItem? tipPathItem,
    HttpMethod method,
    string description
  )
  {
    string verb = method.Method.ToUpperInvariant();
    OpenApiOperation? tip = null;
    _ = tipPathItem?.Operations?.TryGetValue(method, out tip);
    OpenApiOperation operation = AnonymousOperation
    (
      document,
      AliasOperationId(tip, method),
      "Discovery alias of " + verb + " " + TipPath,
      description,
      "Tip"
    );

    if (tip?.Responses is null || tip.Responses.Count == 0)
    {
      operation.Responses!.Add("200", new OpenApiResponse { Description = "Same responses as " + verb + " " + TipPath + "." });
      return operation;
    }

    foreach (KeyValuePair<string, IOpenApiResponse> entry in tip.Responses)
    {
      operation.Responses!.Add(entry.Key, entry.Value.CreateShallowCopy());
    }

    if (tip.Parameters is not null)
    {
      operation.Parameters = [.. tip.Parameters.Select(static parameter => parameter.CreateShallowCopy())];
    }

    operation.RequestBody = tip.RequestBody?.CreateShallowCopy();
    return operation;
  }

  /// <summary>
  /// The alias of the /api/tip operation <c>tip.submit</c> is <c>tip.submit-alias</c>. Without
  /// a named tip operation it is <c>tip.&lt;verb&gt;-alias</c>.
  /// </summary>
  private static string AliasOperationId(OpenApiOperation? tip, HttpMethod method) =>
    OpenApiOperationNames.IsOperationId(tip?.OperationId)
      ? tip!.OperationId + "-alias"
      : "tip." + method.Method.ToLowerInvariant() + "-alias";

  private static void NoteCanonicalTip(OpenApiDocument document)
  {
    if (!document.Paths.TryGetValue(TipPath, out IOpenApiPathItem? pathItem) || pathItem.Operations is null)
    {
      return;
    }

    foreach (OpenApiOperation operation in pathItem.Operations.Values)
    {
      if (operation.Description?.Contains(AliasSentence, StringComparison.Ordinal) == true)
      {
        continue;
      }

      operation.Description = string.IsNullOrWhiteSpace(operation.Description)
        ? AliasSentence
        : operation.Description.TrimEnd() + " " + AliasSentence;
    }
  }

  private static void NoteExclusions(OpenApiDocument document, OpenApiExcludedRoute[] routes)
  {
    if (document.Info.Description?.Contains(OutsideHeading, StringComparison.Ordinal) == true)
    {
      return;
    }

    string note = Description(routes);
    document.Info.Description = string.IsNullOrWhiteSpace(document.Info.Description)
      ? note
      : document.Info.Description.TrimEnd() + "\n\n" + note;
  }

  private static string Description(OpenApiExcludedRoute[] routes)
  {
    string lines =
      OutsideHeading + "\n\n"
      + "This document includes the web-server FastEndpoints operations, `GET " + HealthPath + "`, "
      + "and `GET` and `POST /api` (a rewrite alias of `/api/tip`, including `/api/`).\n\n"
      + "The routes below are served but are not operations. A missing path is one of these "
      + "exclusions, not a missing route. Not listed: " + UnlistedFamilies + ". "
      + "Scalar ([/scalar/v1](/scalar/v1)) shows this note. "
      + "Index: [" + LlmsPath + "](" + LlmsPath + "). The same list is `" + ExtensionName + "`.\n";

    foreach (OpenApiExcludedRoute route in routes)
    {
      lines += "\n- `" + route.Method + " " + route.Path + "` — " + route.Reason;
      if (route.See is not null)
      {
        lines += " See [" + route.See + "](" + route.See + ").";
      }
    }

    return lines;
  }

  private static JsonArray ExcludedArray(OpenApiExcludedRoute[] routes)
  {
    JsonArray array = [];
    foreach (OpenApiExcludedRoute route in routes)
    {
      JsonObject entry = new()
      {
        ["path"] = route.Path,
        ["method"] = route.Method,
        ["reason"] = route.Reason,
      };
      if (route.See is not null)
      {
        entry["see"] = route.See;
      }

      array.Add(entry);
    }

    return array;
  }

  private static OpenApiOperation AnonymousOperation
  (
    OpenApiDocument document,
    string operationId,
    string summary,
    string description,
    string tag,
    params (string Status, OpenApiResponse Response)[] responses
  )
  {
    OpenApiResponses openApiResponses = new();
    foreach ((string status, OpenApiResponse response) in responses)
    {
      openApiResponses.Add(status, response);
    }

    return new OpenApiOperation
    {
      OperationId = operationId,
      Summary = summary,
      Description = description,
      Security = [],
      Tags = new HashSet<OpenApiTagReference>
      {
        new(tag, document, null),
      },
      Responses = openApiResponses,
    };
  }

  private static OpenApiResponse PlainText(string description)
  {
    return new OpenApiResponse
    {
      Description = description,
      Content = new Dictionary<string, IOpenApiMediaType>
      {
        ["text/plain"] = new OpenApiMediaType
        {
          Schema = new OpenApiSchema
          {
            Type = JsonSchemaType.String,
          },
        },
      },
    };
  }
}
